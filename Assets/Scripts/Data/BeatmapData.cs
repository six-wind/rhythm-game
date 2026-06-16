using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 谱面 ScriptableObject — 存储一首歌的所有音符数据、BPM、下落速度等。
    /// 可在 Unity 编辑器中通过 Create Asset Menu 创建，
    /// 也可在 BeatmapEditorWindow 中可视化编辑。
    /// </summary>
    [CreateAssetMenu(fileName = "NewBeatmap", menuName = "RhythmGame/BeatmapData", order = 1)]
    public class BeatmapData : ScriptableObject
    {
        [Header("歌曲信息")]
        [Tooltip("歌曲名称（显示在选曲界面）")]
        public string songName = "New Song";

        [Tooltip("音乐文件（MP3/WAV）")]
        public AudioClip music;

        [Header("谱面参数")]
        [Tooltip("每分钟节拍数，影响音符密度参考")]
        [Range(40f, 300f)]
        public float bpm = 120f;

        [Tooltip("音符下落速度（单位/秒）")]
        [Range(1f, 20f)]
        public float fallSpeed = 8f;

        [Header("音符列表")]
        [Tooltip("所有音符数据，按时间排序")]
        public List<NoteData> notes = new List<NoteData>();

        /// <summary>
        /// 按时间升序排序音符列表
        /// </summary>
        public void SortNotes()
        {
            notes.Sort((a, b) => a.time.CompareTo(b.time));
        }

        /// <summary>
        /// 获取歌曲总时长（秒）。优先使用音乐长度，其次使用最后一个音符时间+2秒。
        /// </summary>
        public float GetDuration()
        {
            if (music != null)
                return music.length;

            if (notes.Count > 0)
                return notes[notes.Count - 1].time + 2f;

            return 60f; // 默认1分钟
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 确保轨道序号在有效范围内
            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                note.lane = Mathf.Clamp(note.lane, 0, 3);
                notes[i] = note;
            }
        }
#endif
    }
}
