using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace RhythmGame.Editor
{
    /// <summary>
    /// 可视化谱面编辑器窗口
    /// 提供音符的增删改查、排序、JSON 导入/导出、预览播放
    /// 菜单路径：Tools → RhythmGame → Beatmap Editor
    /// </summary>
    public class BeatmapEditorWindow : EditorWindow
    {
        private BeatmapData beatmap;
        private Vector2 scrollPosition;
        private Vector2 timelineScrollPosition;

        // 新增音符参数
        private float newNoteTime = 0f;
        private int newNoteLane = 0;

        // JSON 导入导出
        private string jsonFilePath = "";

        // 预览
        private bool isPreviewPlaying = false;
        private double previewStartTime;

        // GUI 样式
        private GUIStyle headerStyle;
        private GUIStyle perfectStyle;
        private GUIStyle goodStyle;

        [MenuItem("Tools/RhythmGame/Beatmap Editor", priority = 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<BeatmapEditorWindow>("谱面编辑器");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // 尝试加载当前选中的谱面
            if (Selection.activeObject is BeatmapData selected)
            {
                beatmap = selected;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            DrawToolbar();
            EditorGUILayout.Space(10);

            if (beatmap == null)
            {
                EditorGUILayout.HelpBox("请先选择一个 BeatmapData 资源，或点击 [新建谱面] 创建。", MessageType.Info);

                if (GUILayout.Button("新建谱面", GUILayout.Height(30)))
                    CreateNewBeatmap();

                if (GUILayout.Button("从 JSON 导入谱面", GUILayout.Height(30)))
                    ImportBeatmapFromJSON();

                return;
            }

            // 有谱面时显示编辑器
            DrawBeatmapInfo();
            EditorGUILayout.Space(5);
            DrawNoteList();
            EditorGUILayout.Space(5);
            DrawAddNoteSection();
            EditorGUILayout.Space(5);
            DrawPreviewSection();
            EditorGUILayout.Space(5);
            DrawJSONSection();
        }

        #region 绘制方法

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("选择谱面", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                // 打开资源选择器
                var newBeatmap = EditorGUILayout.ObjectField(beatmap, typeof(BeatmapData), false) as BeatmapData;
                if (newBeatmap != beatmap)
                {
                    beatmap = newBeatmap;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(50)))
                CreateNewBeatmap();

            if (GUILayout.Button("导入 JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ImportBeatmapFromJSON();

            if (GUILayout.Button("导出 JSON", EditorStyles.toolbarButton, GUILayout.Width(80)))
                ExportBeatmapToJSON();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBeatmapInfo()
        {
            EditorGUILayout.LabelField("谱面信息", headerStyle);

            EditorGUI.BeginChangeCheck();

            beatmap.songName = EditorGUILayout.TextField("歌曲名称", beatmap.songName);
            beatmap.music = EditorGUILayout.ObjectField("音乐文件", beatmap.music, typeof(AudioClip), false) as AudioClip;
            beatmap.bpm = EditorGUILayout.Slider("BPM", beatmap.bpm, 40f, 300f);
            beatmap.fallSpeed = EditorGUILayout.Slider("下落速度", beatmap.fallSpeed, 1f, 20f);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(beatmap);
            }

            // 显示歌曲时长
            float duration = beatmap.GetDuration();
            EditorGUILayout.LabelField($"歌曲时长: {duration:F1}s | 音符数: {beatmap.notes.Count}");
        }

        private void DrawNoteList()
        {
            EditorGUILayout.LabelField($"音符列表 ({beatmap.notes.Count})", headerStyle);

            // 工具按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("按时间排序", GUILayout.Width(100)))
            {
                Undo.RecordObject(beatmap, "Sort Notes");
                beatmap.SortNotes();
                EditorUtility.SetDirty(beatmap);
            }
            if (GUILayout.Button("清空所有音符", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清空所有音符吗？此操作不可撤销。", "确定", "取消"))
                {
                    Undo.RecordObject(beatmap, "Clear Notes");
                    beatmap.notes.Clear();
                    EditorUtility.SetDirty(beatmap);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // 表头
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("#", GUILayout.Width(40));
            EditorGUILayout.LabelField("时间 (秒)", GUILayout.Width(100));
            EditorGUILayout.LabelField("轨道", GUILayout.Width(60));
            EditorGUILayout.LabelField("操作", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // 音符列表（可滚动）
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));

            int removeIndex = -1;
            for (int i = 0; i < beatmap.notes.Count; i++)
            {
                NoteData note = beatmap.notes[i];

                EditorGUILayout.BeginHorizontal();

                // 序号
                EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(40));

                // 时间编辑
                EditorGUI.BeginChangeCheck();
                float newTime = EditorGUILayout.FloatField(note.time, GUILayout.Width(100));
                if (newTime < 0f) newTime = 0f;

                // 轨道编辑
                int newLane = EditorGUILayout.IntSlider(note.lane, 0, 3, GUILayout.Width(60));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(beatmap, "Edit Note");
                    beatmap.notes[i] = new NoteData(newTime, newLane);
                    EditorUtility.SetDirty(beatmap);
                }

                // 跳转到时间按钮
                if (GUILayout.Button("▶", GUILayout.Width(25)))
                {
                    if (beatmap.music != null)
                    {
                        // 在编辑器中预览该时间点的音频
                        PlayAudioPreview(note.time);
                    }
                }

                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    removeIndex = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                Undo.RecordObject(beatmap, "Remove Note");
                beatmap.notes.RemoveAt(removeIndex);
                EditorUtility.SetDirty(beatmap);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAddNoteSection()
        {
            EditorGUILayout.LabelField("添加音符", headerStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("时间 (秒)", GUILayout.Width(80));
            newNoteTime = EditorGUILayout.FloatField(newNoteTime, GUILayout.Width(100));
            if (newNoteTime < 0f) newNoteTime = 0f;

            EditorGUILayout.LabelField("轨道", GUILayout.Width(40));
            newNoteLane = EditorGUILayout.IntSlider(newNoteLane, 0, 3, GUILayout.Width(80));

            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                Undo.RecordObject(beatmap, "Add Note");
                beatmap.notes.Add(new NoteData(newNoteTime, newNoteLane));
                EditorUtility.SetDirty(beatmap);

                // 时间自动递增一拍
                float beatDuration = 60f / beatmap.bpm;
                newNoteTime += beatDuration;
                newNoteLane = (newNoteLane + 1) % 4;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("预览", headerStyle);

            EditorGUILayout.BeginHorizontal();

            if (beatmap.music == null)
            {
                EditorGUILayout.HelpBox("请先拖入音乐文件才能预览。", MessageType.Warning);
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (!isPreviewPlaying)
            {
                if (GUILayout.Button("▶ 播放预览", GUILayout.Height(30)))
                {
                    StartPreview();
                }
            }
            else
            {
                if (GUILayout.Button("■ 停止预览", GUILayout.Height(30)))
                {
                    StopPreview();
                }
            }

            // 显示当前播放进度
            if (isPreviewPlaying)
            {
                double elapsed = EditorApplication.timeSinceStartup - previewStartTime;
                EditorGUILayout.LabelField($"播放中... {elapsed:F1}s / {beatmap.music.length:F1}s");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawJSONSection()
        {
            EditorGUILayout.LabelField("JSON 导入/导出", headerStyle);

            EditorGUILayout.BeginHorizontal();
            jsonFilePath = EditorGUILayout.TextField("文件路径", jsonFilePath);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFilePanel("选择 JSON 文件", "", "json");
                if (!string.IsNullOrEmpty(path))
                    jsonFilePath = path;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("导入 JSON 到当前谱面", GUILayout.Height(25)))
            {
                if (!string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath))
                {
                    ImportJSONToBeatmap(jsonFilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "JSON 文件路径无效！", "好的");
                }
            }

            if (GUILayout.Button("导出当前谱面为 JSON", GUILayout.Height(25)))
            {
                string defaultPath = EditorUtility.SaveFilePanel("导出谱面 JSON", "", beatmap.songName, "json");
                if (!string.IsNullOrEmpty(defaultPath))
                {
                    ExportBeatmapToJSONPath(defaultPath);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 功能方法

        private void CreateNewBeatmap()
        {
            string path = EditorUtility.SaveFilePanelInProject("新建谱面", "NewBeatmap.asset",
                "asset", "选择保存位置");

            if (string.IsNullOrEmpty(path)) return;

            BeatmapData newBeatmap = ScriptableObject.CreateInstance<BeatmapData>();
            AssetDatabase.CreateAsset(newBeatmap, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            beatmap = newBeatmap;
            Selection.activeObject = beatmap;
        }

        private void ImportBeatmapFromJSON()
        {
            string path = EditorUtility.OpenFilePanel("导入谱面 JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                jsonFilePath = path;
                ImportJSONToBeatmap(path);
            }
        }

        private void ExportBeatmapToJSON()
        {
            string path = EditorUtility.SaveFilePanel("导出谱面 JSON", "", beatmap.songName, "json");
            if (!string.IsNullOrEmpty(path))
            {
                ExportBeatmapToJSONPath(path);
            }
        }

        private void ImportJSONToBeatmap(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<BeatmapJSON>(json);

                if (data == null)
                {
                    EditorUtility.DisplayDialog("错误", "JSON 格式无效！", "好的");
                    return;
                }

                Undo.RecordObject(beatmap, "Import JSON Beatmap");
                beatmap.songName = data.songName;
                beatmap.bpm = data.bpm > 0 ? data.bpm : 120f;
                beatmap.fallSpeed = data.fallSpeed > 0 ? data.fallSpeed : 8f;
                beatmap.notes = new List<NoteData>(data.notes);
                beatmap.SortNotes();
                EditorUtility.SetDirty(beatmap);

                Debug.Log($"[BeatmapEditor] 成功从 JSON 导入谱面：{data.songName}，音符数：{data.notes.Length}");
                EditorUtility.DisplayDialog("导入成功",
                    $"谱面已导入！\n歌曲：{data.songName}\n音符数：{data.notes.Length}", "好的");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导入失败", $"读取 JSON 出错：\n{e.Message}", "好的");
                Debug.LogError($"[BeatmapEditor] JSON 导入失败：{e}");
            }
        }

        private void ExportBeatmapToJSONPath(string path)
        {
            try
            {
                var data = new BeatmapJSON
                {
                    songName = beatmap.songName,
                    bpm = beatmap.bpm,
                    fallSpeed = beatmap.fallSpeed,
                    notes = beatmap.notes.ToArray()
                };

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);

                Debug.Log($"[BeatmapEditor] 谱面已导出为 JSON：{path}");
                EditorUtility.DisplayDialog("导出成功",
                    $"谱面已导出！\n路径：{path}", "好的");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("导出失败", $"写入 JSON 出错：\n{e.Message}", "好的");
                Debug.LogError($"[BeatmapEditor] JSON 导出失败：{e}");
            }
        }

        private void StartPreview()
        {
            if (beatmap.music == null) return;
            isPreviewPlaying = true;
            previewStartTime = EditorApplication.timeSinceStartup;
            PlayAudioPreview(0);
        }

        private void StopPreview()
        {
            isPreviewPlaying = false;
            StopAllClips();
        }

        private void PlayAudioPreview(float startTime)
        {
            // 使用 Editor 的 AudioUtil 预览音频
            // 注意：AudioUtil 是 Unity 内部 API，在部分版本可能不可用
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            if (audioUtilClass != null)
            {
                var playMethod = audioUtilClass.GetMethod("PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var stopMethod = audioUtilClass.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

                stopMethod?.Invoke(null, null);
                playMethod?.Invoke(null, new object[] { beatmap.music, (int)(startTime * beatmap.music.frequency), false });
            }
        }

        private void StopAllClips()
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");

            if (audioUtilClass != null)
            {
                var stopMethod = audioUtilClass.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                stopMethod?.Invoke(null, null);
            }
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
        }

        #endregion

        #region JSON 数据结构

        [System.Serializable]
        private class BeatmapJSON
        {
            public string songName;
            public float bpm;
            public float fallSpeed;
            public NoteData[] notes;
        }

        #endregion
    }
}
