using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 键盘输入处理器
    /// 监听 D/F/J/K 四键击打，基于音频真实播放时间进行三段判定
    /// 单次点击只判定对应轨道中最近的未判定音符
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("键位映射")]
        public KeyCode lane0Key = KeyCode.D;
        public KeyCode lane1Key = KeyCode.F;
        public KeyCode lane2Key = KeyCode.J;
        public KeyCode lane3Key = KeyCode.K;

        // 快速访问
        private KeyCode[] laneKeys;

        private void Start()
        {
            laneKeys = new KeyCode[] { lane0Key, lane1Key, lane2Key, lane3Key };
        }

        private void Update()
        {
            // 只在游戏进行中处理输入
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // 检查四个键的按下
            for (int lane = 0; lane < 4; lane++)
            {
                if (Input.GetKeyDown(laneKeys[lane]))
                {
                    HandleKeyPress(lane);
                }
            }
        }

        /// <summary>
        /// 处理某个轨道的按键
        /// </summary>
        private void HandleKeyPress(int lane)
        {
            if (NoteSpawner.Instance == null) return;
            if (AudioManager.Instance == null) return;

            // 获取当前音频播放时间（唯一时间源，禁止使用 Time.time）
            float audioTime = AudioManager.Instance.CurrentTime;

            // 获取该轨道最近的未判定音符
            NoteController targetNote = NoteSpawner.Instance.GetNearestUnjudgedNote(lane, audioTime);

            if (targetNote == null)
            {
                // 该轨道无可判定音符（空按不扣分）
                return;
            }

            // 执行判定
            JudgmentType judgment = targetNote.GetJudgment(audioTime);

            // 如果偏差太大（>0.2s），不记录为击打成功
            if (judgment == JudgmentType.Miss)
            {
                // 偏差过大视为空按，不消耗音符（音符可被后续按键判定）
                return;
            }

            // 标记音符为已判定
            targetNote.MarkJudged();

            // 记录分数
            ScoreManager.Instance?.RecordJudgment(judgment);

            // 播放击打音效
            AudioManager.Instance?.PlayHitSound(judgment);

            // 调试输出
            Debug.Log($"[Input] Lane {lane} → {judgment} | Δt={Mathf.Abs(audioTime - targetNote.HitTime):F3}s | Score={ScoreManager.Instance?.Score} | Combo={ScoreManager.Instance?.Combo}");
        }

        /// <summary>
        /// 在编辑器中可视化键位提示
        /// </summary>
        private void OnGUI()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            // 简易键位提示（半透明）
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, 1f, 1f, 0.3f);

            string[] labels = { "D", "F", "J", "K" };
            float[] xPositions = { -1.5f, -0.5f, 0.5f, 1.5f };

            Camera cam = Camera.main;
            if (cam == null) return;

            for (int i = 0; i < 4; i++)
            {
                Vector3 worldPos = new Vector3(xPositions[i], NoteController.JUDGE_Y + 0.5f, 0f);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                // Unity GUI 的 Y 轴是从上往下的
                screenPos.y = Screen.height - screenPos.y;

                Rect rect = new Rect(screenPos.x - 25, screenPos.y - 15, 50, 30);
                GUI.Label(rect, labels[i], style);
            }
        }
    }
}
