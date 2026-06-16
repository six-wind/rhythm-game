using UnityEngine;
using UnityEngine.Events;

namespace RhythmGame
{
    /// <summary>
    /// 游戏状态枚举
    /// </summary>
    public enum GameState
    {
        Idle,       // 未开始
        Playing,    // 进行中
        Paused,     // 暂停
        Ended       // 结束
    }

    /// <summary>
    /// 全局游戏管理器（单例）
    /// 管理游戏状态、持有各子系统引用、协调生命周期
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }
        #endregion

        #region 子系统引用
        [HideInInspector] public AudioManager audioManager;
        [HideInInspector] public ScoreManager scoreManager;
        [HideInInspector] public NoteSpawner noteSpawner;
        #endregion

        #region 游戏状态
        public GameState State { get; private set; } = GameState.Idle;

        /// <summary>当前加载的谱面</summary>
        public BeatmapData CurrentBeatmap { get; private set; }

        /// <summary>游戏开始后经过的时间（基于 AudioSource）</summary>
        public float GameTime => audioManager != null ? audioManager.CurrentTime : 0f;
        #endregion

        #region 事件
        [Header("游戏事件")]
        public UnityEvent<BeatmapData> OnGameStart;
        public UnityEvent OnGamePause;
        public UnityEvent OnGameResume;
        public UnityEvent OnGameEnd;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 自动查找同场景的子系统
            audioManager = FindObjectOfType<AudioManager>();
            scoreManager = FindObjectOfType<ScoreManager>();
            noteSpawner = FindObjectOfType<NoteSpawner>();
        }

        /// <summary>
        /// 加载谱面并开始游戏
        /// </summary>
        public void StartGame(BeatmapData beatmap)
        {
            if (beatmap == null)
            {
                Debug.LogError("[GameManager] BeatmapData 为空，无法开始游戏！");
                return;
            }

            if (beatmap.music == null)
            {
                Debug.LogError("[GameManager] BeatmapData 缺少音乐文件！");
                return;
            }

            if (beatmap.notes.Count == 0)
            {
                Debug.LogWarning("[GameManager] 谱面没有音符，请先在编辑器中添加音符。");
            }

            CurrentBeatmap = beatmap;
            State = GameState.Playing;

            // 初始化各子系统
            scoreManager?.ResetScore();
            audioManager?.PlayMusic(beatmap.music);
            noteSpawner?.StartSpawning(beatmap);

            OnGameStart?.Invoke(beatmap);
            Debug.Log($"[GameManager] 游戏开始！歌曲：{beatmap.songName}，音符数：{beatmap.notes.Count}");
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (State != GameState.Playing) return;

            State = GameState.Paused;
            audioManager?.PauseMusic();
            OnGamePause?.Invoke();
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (State != GameState.Paused) return;

            State = GameState.Playing;
            audioManager?.ResumeMusic();
            OnGameResume?.Invoke();
        }

        /// <summary>
        /// 结束游戏（音乐播放完毕或手动结束）
        /// </summary>
        public void EndGame()
        {
            if (State == GameState.Ended) return;

            State = GameState.Ended;
            audioManager?.StopMusic();
            OnGameEnd?.Invoke();
            Debug.Log($"[GameManager] 游戏结束！总分：{scoreManager?.Score}，最大连击：{scoreManager?.MaxCombo}");
        }

        private void Update()
        {
            // 检测音乐播放完毕
            if (State == GameState.Playing && audioManager != null && !audioManager.IsPlaying)
            {
                EndGame();
            }

            // ESC 暂停/恢复
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing)
                    PauseGame();
                else if (State == GameState.Paused)
                    ResumeGame();
            }
        }
    }
}
