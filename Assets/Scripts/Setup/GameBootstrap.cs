using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 游戏启动引导组件
    /// 挂载到场景中的任意对象上，在 Awake 时自动检查并补充场景对象
    /// 同时也提供运行时自动创建初始场景的能力
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("自动搭建")]
        [Tooltip("勾选后，如果场景缺少必要对象，将自动创建")]
        public bool autoSetup = true;

        private void Awake()
        {
            if (autoSetup)
            {
                // 检查是否需要自动搭建
                if (GameManager.Instance == null || AudioManager.Instance == null)
                {
                    Debug.Log("[GameBootstrap] 检测到场景不完整，自动搭建中...");
                    RuntimeSceneSetup.EnsureSceneSetup();
                    Debug.Log("[GameBootstrap] 场景搭建完成！");
                }
            }
        }

        /// <summary>
        /// 运行时动态搭建整个场景（可在任何地方调用）
        /// </summary>
        public static void Bootstrap()
        {
            // 如果已经有 GameBootstrap，不需要重复
            if (FindObjectOfType<GameBootstrap>() != null) return;

            GameObject go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>().autoSetup = true;
            DontDestroyOnLoad(go);
        }

        private static bool hasInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            // 仅在首次加载场景后检查一次（避免重复创建）
            if (hasInitialized) return;
            hasInitialized = true;

            // 在构建版本中，如果没有预先搭建的场景，则自动创建
            if (GameManager.Instance == null)
            {
                Debug.Log("[GameBootstrap] 构建版本自动初始化场景...");
                RuntimeSceneSetup.EnsureSceneSetup();
            }
        }
    }
}
