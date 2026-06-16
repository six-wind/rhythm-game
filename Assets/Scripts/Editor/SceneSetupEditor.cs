using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RhythmGame.Editor
{
    /// <summary>
    /// 编辑器菜单：一键搭建完整游戏场景
    /// 菜单路径：Tools → RhythmGame → Setup Scene
    /// </summary>
    public static class SceneSetupEditor
    {
        private const string SCENE_PATH = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/RhythmGame/Setup Scene", priority = 100)]
        public static void SetupScene()
        {
            // 确认操作
            if (!EditorUtility.DisplayDialog("搭建场景",
                "将创建/覆盖 Main 场景，包含所有游戏对象。\n确定继续？", "确定", "取消"))
                return;

            // 创建新场景
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 调用运行时搭建逻辑
            RuntimeSceneSetup.EnsureSceneSetup();

            // 保存场景
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log($"[SceneSetup] 场景已搭建并保存到 {SCENE_PATH}");
            EditorUtility.DisplayDialog("完成",
                "场景搭建完成！\n\n下一步：\n1. 导入音乐到 Resources/\n2. 创建 BeatmapData（右键 → Create → RhythmGame → BeatmapData）\n3. 使用 Beatmap Editor 编辑谱面\n4. 点击 Play 开始游戏", "好的");
        }

        [MenuItem("Tools/RhythmGame/Ensure Scene Objects (Runtime)", priority = 101)]
        public static void EnsureSceneObjects()
        {
            RuntimeSceneSetup.EnsureSceneSetup();
            Debug.Log("[SceneSetup] 已检查并补充场景对象。");
        }

        [MenuItem("Tools/RhythmGame/Create Sample Beatmap", priority = 200)]
        public static void CreateSampleBeatmap()
        {
            // 确保 Resources/Beatmaps 目录存在
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Beatmaps"))
                AssetDatabase.CreateFolder("Assets/Resources", "Beatmaps");

            // 创建示例谱面
            BeatmapData beatmap = ScriptableObject.CreateInstance<BeatmapData>();
            beatmap.songName = "Sample Beatmap";
            beatmap.bpm = 120f;
            beatmap.fallSpeed = 8f;

            // 添加示例音符（一个简单的4/4拍节奏）
            float beatDuration = 60f / beatmap.bpm; // 每拍时长
            for (int bar = 0; bar < 8; bar++)
            {
                for (int beat = 0; beat < 4; beat++)
                {
                    float time = bar * 4 * beatDuration + beat * beatDuration;
                    // 简单的节奏型：每拍一个音符，轨道随机
                    int lane = (bar + beat) % 4;
                    beatmap.notes.Add(new NoteData(time, lane));
                }
            }

            string path = "Assets/Resources/Beatmaps/SampleBeatmap.asset";
            AssetDatabase.CreateAsset(beatmap, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = beatmap;
            Debug.Log($"[SceneSetup] 示例谱面已创建：{path}，音符数：{beatmap.notes.Count}");
            EditorUtility.DisplayDialog("完成",
                $"示例谱面已创建！\n路径：{path}\n音符数：{beatmap.notes.Count}\n\n提示：需要拖入音乐文件到 BeatmapData 的 Music 字段才能播放。", "好的");
        }
    }
}
