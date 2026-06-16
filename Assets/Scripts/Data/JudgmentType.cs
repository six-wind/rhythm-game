namespace RhythmGame
{
    /// <summary>
    /// 三段判定类型：Perfect / Good / Normal / Miss
    /// </summary>
    public enum JudgmentType
    {
        /// <summary>时间偏差 ≤ 0.05s，+100分，连击+1</summary>
        Perfect,
        /// <summary>时间偏差 ≤ 0.12s，+50分，连击+1</summary>
        Good,
        /// <summary>时间偏差 ≤ 0.2s，不加分，不断连击</summary>
        Normal,
        /// <summary>时间偏差 > 0.2s 或漏击，连击清零，不加分</summary>
        Miss
    }
}
