using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

namespace RhythmGame
{
    /// <summary>
    /// 歌曲选择界面
    /// 列出 Resources/Beatmaps 中的谱面，支持导入自定义音乐和谱面 JSON
    /// </summary>
    public class SongSelectUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject songListPanel;
        public Transform songListContent;
        public GameObject songButtonPrefab;
        public Button startButton;

        [Header("歌曲信息显示")]
        public TextMeshProUGUI songNameText;
        public TextMeshProUGUI songInfoText; // 显示 BPM、音符数等

        [Header("文件导入")]
        public Button importMusicButton;
        public Button importBeatmapButton;

        [Header("状态")]
        public TextMeshProUGUI statusText;

        private List<BeatmapData> availableBeatmaps = new List<BeatmapData>();
        private BeatmapData selectedBeatmap;
        private AudioClip importedMusic; // 用户导入的音乐

        private void Start()
        {
            // 加载所有谱面
            LoadBeatmaps();

            // 按钮事件
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (importMusicButton != null)
                importMusicButton?.onClick.AddListener(OnImportMusicClicked);

            // 初始状态
            startButton.interactable = false;
            UpdateSongInfo();
        }

        /// <summary>
        /// 从 Resources/Beatmaps 加载所有 BeatmapData
        /// </summary>
        private void LoadBeatmaps()
        {
            availableBeatmaps.Clear();
            BeatmapData[] beatmaps = Resources.LoadAll<BeatmapData>("Beatmaps");

            foreach (var bm in beatmaps)
            {
                availableBeatmaps.Add(bm);
                CreateSongButton(bm);
            }

            Debug.Log($"[SongSelectUI] 加载了 {availableBeatmaps.Count} 个谱面");

            if (availableBeatmaps.Count == 0)
            {
                statusText.text = "未找到谱面。请在 Resources/Beatmaps 中创建谱面，或导入谱面 JSON。";
            }
        }

        /// <summary>
        /// 在列表 UI 中为每个谱面创建按钮
        /// </summary>
        private void CreateSongButton(BeatmapData beatmap)
        {
            if (songButtonPrefab == null || songListContent == null) return;

            GameObject btnObj = Instantiate(songButtonPrefab, songListContent);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI label = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                int noteCount = beatmap.notes != null ? beatmap.notes.Count : 0;
                label.text = $"{beatmap.songName}\n<size=18>BPM:{beatmap.bpm} | 音符:{noteCount}</size>";
            }

            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectBeatmap(beatmap));
            }
        }

        /// <summary>
        /// 选择一个谱面
        /// </summary>
        private void SelectBeatmap(BeatmapData beatmap)
        {
            selectedBeatmap = beatmap;
            startButton.interactable = true;
            UpdateSongInfo();
            statusText.text = $"已选择：{beatmap.songName}";
        }

        /// <summary>
        /// 更新歌曲信息显示
        /// </summary>
        private void UpdateSongInfo()
        {
            if (selectedBeatmap != null)
            {
                songNameText.text = selectedBeatmap.songName;
                int noteCount = selectedBeatmap.notes != null ? selectedBeatmap.notes.Count : 0;
                float duration = selectedBeatmap.GetDuration();
                songInfoText.text = $"BPM: {selectedBeatmap.BPM} | 下落速度: {selectedBeatmap.fallSpeed}\n" +
                                    $"音符数: {noteCount} | 时长: {duration:F1}s";
            }
            else
            {
                songNameText.text = "请选择歌曲";
                songInfoText.text = "";
            }
        }

        /// <summary>
        /// 开始游戏按钮
        /// </summary>
        private void OnStartClicked()
        {
            if (selectedBeatmap == null) return;

            // 隐藏选曲面板
            if (songListPanel != null)
                songListPanel.SetActive(false);

            // 通过 GameManager 开始游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(selectedBeatmap);
            }
        }

        /// <summary>
        /// 导入自定义音乐文件
        /// </summary>
        private void OnImportMusicClicked()
        {
            // 在 Standalone 平台使用文件浏览器
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            string[] extensions = new[] { "mp3", "wav", "ogg" };
            string path = OpenFileDialog("选择音乐文件", extensions);
            if (!string.IsNullOrEmpty(path))
            {
                StartCoroutine(ImportMusicFromPath(path));
            }
#else
            statusText.text = "当前平台不支持文件浏览器，请在编辑器中拖拽音乐文件。";
#endif
        }

        private System.Collections.IEnumerator ImportMusicFromPath(string filePath)
        {
            statusText.text = "正在加载音乐...";
            string url = "file://" + filePath;

            using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    importedMusic = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    importedMusic.name = Path.GetFileNameWithoutExtension(filePath);
                    statusText.text = $"已导入：{importedMusic.name}";
                    Debug.Log($"[SongSelectUI] 成功导入音乐：{importedMusic.name}");
                }
                else
                {
                    statusText.text = $"导入失败：{www.error}";
                    Debug.LogError($"[SongSelectUI] 音乐导入失败：{www.error}");
                }
            }
        }

#if UNITY_STANDALONE_WIN
        /// <summary>
        /// Windows 文件打开对话框（使用 System.Windows.Forms）
        /// </summary>
        private string OpenFileDialog(string title, string[] extensions)
        {
            // 注意：此方法需要项目设置中开启 System.Windows.Forms 兼容性
            // Player Settings → Configuration → Api Compatibility Level → .NET Framework
            try
            {
                var dialog = new System.Windows.Forms.OpenFileDialog();
                dialog.Title = title;
                dialog.Filter = "音频文件|*." + string.Join(";*.", extensions);
                dialog.Multiselect = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SongSelectUI] 文件对话框异常：{e.Message}");
            }
            return null;
        }
#else
        private string OpenFileDialog(string title, string[] extensions)
        {
            return null;
        }
#endif
    }
}
