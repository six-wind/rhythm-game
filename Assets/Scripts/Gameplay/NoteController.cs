using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 单个音符的控制器
    /// 负责：基于音频时间的精确定位、被击中判定、超出屏幕回池
    /// </summary>
    public class NoteController : MonoBehaviour
    {
        [Header("组件引用")]
        public SpriteRenderer spriteRenderer;
        public BoxCollider2D boxCollider;

        /// <summary>该音符对应的谱面数据</summary>
        public NoteData Data { get; private set; }

        /// <summary>是否已被判定（防止重复击打）</summary>
        public bool IsJudged { get; private set; }

        /// <summary>所属轨道索引 (0-3)</summary>
        public int Lane => Data.lane;

        /// <summary>击打目标时间（秒）</summary>
        public float HitTime => Data.time;

        // 固定配置（由 NoteSpawner 设置）
        private float fallSpeed;
        private float judgeY;

        // 轨道 X 坐标映射
        public static readonly float[] LaneXPositions = { -1.5f, -0.5f, 0.5f, 1.5f };
        public const float SPAWN_Y = 6f;
        public const float DESPAWN_Y = -6.5f;
        public const float JUDGE_Y = -3.8f;

        public void Initialize(NoteData data, float speed, float judgeLineY)
        {
            Data = data;
            fallSpeed = speed;
            judgeY = judgeLineY;
            IsJudged = false;

            // 设置 X 坐标（轨道位置）
            float xPos = (data.lane >= 0 && data.lane < 4) ? LaneXPositions[data.lane] : 0f;
            transform.position = new Vector3(xPos, SPAWN_Y, 0f);

            gameObject.name = $"Note_L{data.lane}_T{data.time:F2}";
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // 基于音频时间计算精确位置（确保音画同步）
            float currentTime = GameManager.Instance.GameTime;
            float timeUntilHit = HitTime - currentTime;
            float targetY = judgeY + timeUntilHit * fallSpeed;

            // 更新位置
            Vector3 pos = transform.position;
            pos.y = targetY;
            transform.position = pos;

            // 超出屏幕底部 → 回池
            if (pos.y < DESPAWN_Y)
            {
                // 未被判定且超出判定窗口 → 记为 Miss
                if (!IsJudged && currentTime > HitTime + 0.2f)
                {
                    ScoreManager.Instance?.RecordJudgment(JudgmentType.Miss);
                    IsJudged = true;
                }
                ReturnToPool();
            }
        }

        /// <summary>
        /// 判断给定时间是否在有效判定窗口内
        /// </summary>
        public bool IsInJudgeWindow(float audioTime)
        {
            float delta = Mathf.Abs(audioTime - HitTime);
            return delta <= 0.2f; // 最大判定窗口
        }

        /// <summary>
        /// 根据音频时间与音符目标时间的偏差，返回判定类型
        /// </summary>
        public JudgmentType GetJudgment(float audioTime)
        {
            float delta = Mathf.Abs(audioTime - HitTime);

            if (delta <= 0.05f)
                return JudgmentType.Perfect;
            else if (delta <= 0.12f)
                return JudgmentType.Good;
            else if (delta <= 0.2f)
                return JudgmentType.Normal;
            else
                return JudgmentType.Miss;
        }

        /// <summary>
        /// 标记为已判定并播放击中反馈
        /// </summary>
        public void MarkJudged()
        {
            IsJudged = true;
            // 击中反馈：缩小消失
            StartCoroutine(HitFeedback());
        }

        private System.Collections.IEnumerator HitFeedback()
        {
            float duration = 0.15f;
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                yield return null;
            }

            ReturnToPool();
        }

        /// <summary>
        /// 回收到对象池
        /// </summary>
        public void ReturnToPool()
        {
            transform.localScale = Vector3.one;
            IsJudged = false;
            gameObject.SetActive(false);
            NoteSpawner.Instance?.ReturnNoteToPool(gameObject);
        }
    }
}
