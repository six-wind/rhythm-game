using UnityEngine;
using UnityEngine.Events;

namespace RhythmGame
{
    /// <summary>
    /// 分数管理器（单例）
    /// 管理分数、当前连击、最大连击，提供判定分值查询
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        #region Singleton
        public static ScoreManager Instance { get; private set; }
        #endregion

        #region 分数数据
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        // 各判定计数
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int NormalCount { get; private set; }
        public int MissCount { get; private set; }
        #endregion

        #region 分值常量
        public const int PERFECT_SCORE = 100;
        public const int GOOD_SCORE = 50;
        public const int NORMAL_SCORE = 0;
        public const int MISS_SCORE = 0;
        #endregion

        #region 事件
        [Header("分数更新事件")]
        public UnityEvent<int> OnScoreChanged;
        public UnityEvent<int> OnComboChanged;
        public UnityEvent<int> OnMaxComboChanged;
        public UnityEvent<JudgmentType, int> OnJudgment; // 判定类型, 当前连击
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ResetScore();
        }

        /// <summary>
        /// 根据判定类型记录分数和连击
        /// </summary>
        /// <param name="judgment">判定类型</param>
        /// <returns>获得的分数</returns>
        public int RecordJudgment(JudgmentType judgment)
        {
            int scoreGain = 0;

            switch (judgment)
            {
                case JudgmentType.Perfect:
                    scoreGain = PERFECT_SCORE;
                    Combo++;
                    PerfectCount++;
                    break;

                case JudgmentType.Good:
                    scoreGain = GOOD_SCORE;
                    Combo++;
                    GoodCount++;
                    break;

                case JudgmentType.Normal:
                    scoreGain = NORMAL_SCORE;
                    // 不断连击（行业惯例：微小偏差不中断连击）
                    NormalCount++;
                    break;

                case JudgmentType.Miss:
                    scoreGain = MISS_SCORE;
                    Combo = 0;
                    MissCount++;
                    break;
            }

            Score += scoreGain;

            // 更新最大连击
            if (Combo > MaxCombo)
            {
                MaxCombo = Combo;
                OnMaxComboChanged?.Invoke(MaxCombo);
            }

            // 触发事件
            OnScoreChanged?.Invoke(Score);
            OnComboChanged?.Invoke(Combo);
            OnJudgment?.Invoke(judgment, Combo);

            return scoreGain;
        }

        /// <summary>
        /// 重置所有分数数据
        /// </summary>
        public void ResetScore()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            NormalCount = 0;
            MissCount = 0;

            OnScoreChanged?.Invoke(0);
            OnComboChanged?.Invoke(0);
            OnMaxComboChanged?.Invoke(0);
        }

        /// <summary>
        /// 获取统计数据摘要
        /// </summary>
        public string GetSummary()
        {
            int total = PerfectCount + GoodCount + NormalCount + MissCount;
            return $"总分：{Score}\n" +
                   $"最大连击：{MaxCombo}\n" +
                   $"Perfect：{PerfectCount}\n" +
                   $"Good：{GoodCount}\n" +
                   $"Normal：{NormalCount}\n" +
                   $"Miss：{MissCount}\n" +
                   $"总音符：{total}";
        }
    }
}
