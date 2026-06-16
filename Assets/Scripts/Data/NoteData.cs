using System;

namespace RhythmGame
{
    /// <summary>
    /// 单个音符的谱面数据，包含击打时间和轨道序号
    /// </summary>
    [Serializable]
    public struct NoteData
    {
        /// <summary>音符击打时间（秒），相对于音乐开始</summary>
        public float time;

        /// <summary>轨道序号：0/1/2/3，对应 X 坐标 -1.5/-0.5/0.5/1.5</summary>
        public int lane;

        public NoteData(float time, int lane)
        {
            this.time = time;
            this.lane = lane;
        }

        public override string ToString()
        {
            return $"Note(time={time:F3}s, lane={lane})";
        }
    }
}
