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
        public TMP_InputField musicPathInput;  // 用户粘贴音乐文件路径

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
                statusText.text = "No beatmaps found. Create one via Tools > RhythmGame > Create Sample Beatmap.";
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
            statusText.text = $"Selected: {beatmap.songName}";
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
                songInfoText.text = $"BPM: {selectedBeatmap.bpm} | Speed: {selectedBeatmap.fallSpeed}\n" +
                                    $"Notes: {noteCount} | Duration: {duration:F1}s";
            }
            else
            {
                songNameText.text = "Select a Song";
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
        /// 导入自定义音乐文件（通过输入文件路径）
        /// </summary>
        private void OnImportMusicClicked()
        {
            if (musicPathInput == null || string.IsNullOrEmpty(musicPathInput.text))
            {
                statusText.text = "Enter music file path first (e.g. D:\\Music\\song.mp3)";
                return;
            }

            string path = musicPathInput.text.Trim();
            if (!File.Exists(path))
            {
                statusText.text = $"File not found: {path}";
                return;
            }

            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".mp3" && ext != ".wav" && ext != ".ogg" && ext != ".aiff")
            {
                statusText.text = $"Unsupported format: {ext} (MP3/WAV/OGG/AIFF only)";
                return;
            }

            StartCoroutine(ImportMusicFromPath(path));
        }

        /// <summary>
        /// 扫描 StreamingAssets/Music 文件夹中的音乐文件
        /// </summary>
        public void ScanMusicFolder()
        {
            string musicDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Music");
            if (!Directory.Exists(musicDir))
            {
                statusText.text = $"Music folder not found: {musicDir}\nCreate it and add MP3/WAV files.";
                return;
            }

            string[] files = Directory.GetFiles(musicDir, "*.*", SearchOption.AllDirectories);
            int count = 0;
            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
                {
                    StartCoroutine(ImportMusicFromPath(file));
                    count++;
                }
            }
            statusText.text = count > 0
                ? $"Importing {count} songs..."
                : "No music found. Put MP3/WAV in StreamingAssets/Music/ folder.";
        }

        private System.Collections.IEnumerator ImportMusicFromPath(string filePath)
        {
            statusText.text = "Loading music...";
            string url = "file://" + filePath;

            using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    importedMusic = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    importedMusic.name = Path.GetFileNameWithoutExtension(filePath);
                    statusText.text = $"Imported: {importedMusic.name}";
                    Debug.Log($"[SongSelectUI] 成功导入音乐：{importedMusic.name}");
                }
                else
                {
                    statusText.text = $"Import failed: {www.error}";
                    Debug.LogError($"[SongSelectUI] 音乐导入失败：{www.error}");
                }
            }
        }
    }
}
