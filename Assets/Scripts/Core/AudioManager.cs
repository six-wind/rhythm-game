using UnityEngine;
using System.Collections;

namespace RhythmGame
{
    /// <summary>
    /// 音频管理器（单例）
    /// 提供精确的音频播放时间、预加载、播放/暂停/停止控制
    /// — 所有判定基于 AudioSource.time，绝不使用 Time.time
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance { get; private set; }
        #endregion

        private AudioSource audioSource;

        [Header("音频设置")]
        [Tooltip("音乐音量")]
        [Range(0f, 1f)]
        public float musicVolume = 0.8f;

        [Tooltip("击打音效")]
        public AudioClip hitSoundPerfect;
        public AudioClip hitSoundGood;
        public AudioClip hitSoundMiss;

        /// <summary>
        /// 当前音乐播放时间（秒）— 基于 AudioSource.time，所有判定的唯一时间源
        /// </summary>
        public float CurrentTime => audioSource != null ? audioSource.time : 0f;

        /// <summary>
        /// 音乐是否正在播放
        /// </summary>
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;

        /// <summary>
        /// 当前播放的音乐长度（秒）
        /// </summary>
        public float MusicLength => audioSource?.clip != null ? audioSource.clip.length : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // 配置 AudioSource 以优化延迟
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = musicVolume;
            audioSource.priority = 0; // 最高优先级，减少延迟
        }

        /// <summary>
        /// 播放音乐（自动预加载音频数据）
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioManager] AudioClip 为空！");
                return;
            }

            StartCoroutine(PlayMusicCoroutine(clip));
        }

        private IEnumerator PlayMusicCoroutine(AudioClip clip)
        {
            audioSource.clip = clip;

            // 预加载音频数据，避免首次播放时的卡顿和延迟
            if (clip.loadType != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                while (clip.loadState != AudioDataLoadState.Loaded)
                    yield return null;
            }

            audioSource.volume = musicVolume;
            audioSource.Play();
            Debug.Log($"[AudioManager] 开始播放：{clip.name}，时长：{clip.length:F1}s");
        }

        /// <summary>
        /// 暂停音乐
        /// </summary>
        public void PauseMusic()
        {
            audioSource?.Pause();
        }

        /// <summary>
        /// 恢复音乐
        /// </summary>
        public void ResumeMusic()
        {
            audioSource?.UnPause();
        }

        /// <summary>
        /// 停止音乐
        /// </summary>
        public void StopMusic()
        {
            audioSource?.Stop();
        }

        /// <summary>
        /// 播放击打音效（通过额外的 AudioSource 避免干扰主音乐时间）
        /// </summary>
        public void PlayHitSound(JudgmentType judgment)
        {
            AudioClip clip = null;
            switch (judgment)
            {
                case JudgmentType.Perfect:
                    clip = hitSoundPerfect;
                    break;
                case JudgmentType.Good:
                    clip = hitSoundGood;
                    break;
                case JudgmentType.Miss:
                    clip = hitSoundMiss;
                    break;
            }

            if (clip != null)
            {
                // 使用 PlayOneShot 或临时 AudioSource
                AudioSource.PlayClipAtPoint(clip, Vector3.zero, 0.6f);
            }
        }

        /// <summary>
        /// 运行时替换音乐文件（用户自定义导入）
        /// </summary>
        public void LoadCustomMusic(string filePath)
        {
            StartCoroutine(LoadAudioFromFile(filePath));
        }

        private IEnumerator LoadAudioFromFile(string filePath)
        {
            string url = "file://" + filePath;
            using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    clip.name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    Debug.Log($"[AudioManager] 成功加载自定义音乐：{clip.name}");
                    // 注意：不自动播放，由 GameManager.StartGame 触发
                }
                else
                {
                    Debug.LogError($"[AudioManager] 加载音乐失败：{www.error}");
                }
            }
        }
    }
}
