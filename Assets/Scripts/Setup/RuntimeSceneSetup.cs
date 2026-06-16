using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// 运行时场景自动搭建
    /// 当场景中缺少必要对象时，自动创建完整的游戏场景层级
    /// </summary>
    public static class RuntimeSceneSetup
    {
        // ──────────── 固定配置常量 ────────────
        public const float CAMERA_SIZE = 5f;
        public const float JUDGE_LINE_Y = -3.8f;
        public const float NOTE_SPAWN_Y = 6f;
        public static readonly Color JUDGE_LINE_COLOR = new Color(1f, 0.3f, 0.3f, 0.8f);
        public static readonly Color BACKGROUND_COLOR = Color.black;
        public static readonly float[] LANE_X = { -1.5f, -0.5f, 0.5f, 1.5f };

        /// <summary>
        /// 检查并创建所有必需的对象。可在场景加载后调用。
        /// </summary>
        public static void EnsureSceneSetup()
        {
            SetupCamera();
            SetupGameManager();
            SetupAudioManager();
            SetupScoreManager();
            SetupNoteSpawner();
            SetupInputHandler();
            SetupJudgeLine();
            SetupCanvas();
        }

        #region 各组件创建

        private static void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = CAMERA_SIZE;
            mainCam.backgroundColor = BACKGROUND_COLOR;
            mainCam.transform.position = new Vector3(0f, 0f, -10f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;

            // 确保没有额外的 AudioListener（AudioManager 会管理）
            var listener = mainCam.GetComponent<AudioListener>();
            if (listener != null)
                Object.DestroyImmediate(listener);
        }

        private static void SetupGameManager()
        {
            if (GameManager.Instance != null) return;
            CreateSingletonObject<GameManager>("GameManager");
        }

        private static void SetupAudioManager()
        {
            if (AudioManager.Instance != null) return;
            GameObject go = CreateSingletonObject<AudioManager>("AudioManager");

            // 添加 AudioSource
            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null)
            {
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.volume = 0.8f;
            }
        }

        private static void SetupScoreManager()
        {
            if (ScoreManager.Instance != null) return;
            CreateSingletonObject<ScoreManager>("ScoreManager");
        }

        private static void SetupNoteSpawner()
        {
            if (NoteSpawner.Instance != null) return;
            GameObject go = CreateSingletonObject<NoteSpawner>("NoteSpawner");

            // 创建音符对象池父节点
            Transform poolParent = go.transform.Find("NotePool");
            if (poolParent == null)
            {
                GameObject pool = new GameObject("NotePool");
                pool.transform.SetParent(go.transform);
            }
        }

        private static void SetupInputHandler()
        {
            if (Object.FindObjectOfType<InputHandler>() != null) return;
            GameObject go = new GameObject("InputHandler");
            go.AddComponent<InputHandler>();
        }

        private static void SetupJudgeLine()
        {
            if (GameObject.Find("JudgeLine") != null) return;

            GameObject line = new GameObject("JudgeLine");
            line.transform.position = new Vector3(0f, JUDGE_LINE_Y, 0f);

            LineRenderer lr = line.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-2.5f, JUDGE_LINE_Y, 0f));
            lr.SetPosition(1, new Vector3(2.5f, JUDGE_LINE_Y, 0f));
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = JUDGE_LINE_COLOR;
            lr.endColor = JUDGE_LINE_COLOR;
            lr.sortingOrder = 5;
        }

        private static void SetupCanvas()
        {
            if (GameObject.Find("GameCanvas") != null) return;

            // 创建 Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建 HUD
            GameObject hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform, false);
            HUDController hud = hudObj.AddComponent<HUDController>();

            // ── 分数文本（左上角） ──
            GameObject scoreObj = CreateTMPText("ScoreText", hudObj.transform,
                new Vector2(20, -20), new Vector2(200, 60),
                "0", 36, TextAlignmentOptions.TopLeft);
            hud.scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

            // ── 连击文本（顶部中央） ──
            GameObject comboObj = CreateTMPText("ComboText", hudObj.transform,
                new Vector2(0, -20), new Vector2(300, 80),
                "", 48, TextAlignmentOptions.Top);
            // 锚点居中
            RectTransform comboRT = comboObj.GetComponent<RectTransform>();
            comboRT.anchorMin = new Vector2(0.5f, 1f);
            comboRT.anchorMax = new Vector2(0.5f, 1f);
            comboRT.anchoredPosition = new Vector2(0, -60);
            hud.comboText = comboObj.GetComponent<TextMeshProUGUI>();

            // ── 最大连击文本（右上角） ──
            GameObject maxComboObj = CreateTMPText("MaxComboText", hudObj.transform,
                new Vector2(-20, -20), new Vector2(200, 40),
                "Max: 0", 24, TextAlignmentOptions.TopRight);
            RectTransform maxRT = maxComboObj.GetComponent<RectTransform>();
            maxRT.anchorMin = new Vector2(1f, 1f);
            maxRT.anchorMax = new Vector2(1f, 1f);
            maxRT.anchoredPosition = new Vector2(-20, -20);
            hud.maxComboText = maxComboObj.GetComponent<TextMeshProUGUI>();

            // ── 判定弹出文字（屏幕中央偏下） ──
            GameObject judgmentObj = CreateTMPText("JudgmentPopup", hudObj.transform,
                new Vector2(0, -200), new Vector2(300, 80),
                "", 40, TextAlignmentOptions.Center);
            RectTransform jRT = judgmentObj.GetComponent<RectTransform>();
            jRT.anchorMin = new Vector2(0.5f, 0.5f);
            jRT.anchorMax = new Vector2(0.5f, 0.5f);
            jRT.anchoredPosition = new Vector2(0, -150);
            hud.judgmentPopup = judgmentObj.GetComponent<TextMeshProUGUI>();
            judgmentObj.SetActive(false);

            // ── 选曲面板 ──
            SetupSongSelectPanel(canvasObj.transform);
        }

        /// <summary>
        /// 创建 TextMeshPro 文本对象
        /// </summary>
        private static GameObject CreateTMPText(string name, Transform parent,
            Vector2 anchoredPos, Vector2 size, string text, float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            return obj;
        }

        /// <summary>
        /// 创建选曲面板
        /// </summary>
        private static void SetupSongSelectPanel(Transform canvasParent)
        {
            if (GameObject.Find("SongSelectPanel") != null) return;

            GameObject panelObj = new GameObject("SongSelectPanel");
            panelObj.transform.SetParent(canvasParent, false);

            RectTransform panelRT = panelObj.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.95f);

            SongSelectUI songSelect = panelObj.AddComponent<SongSelectUI>();

            // 标题
            GameObject titleObj = CreateTMPText("Title", panelObj.transform,
                new Vector2(0, -40), new Vector2(400, 60),
                "Rhythm Game", 42, TextAlignmentOptions.Center);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -50);

            // 歌曲列表容器
            GameObject listObj = new GameObject("SongList");
            listObj.transform.SetParent(panelObj.transform, false);
            RectTransform listRT = listObj.AddComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0.1f, 0.3f);
            listRT.anchorMax = new Vector2(0.9f, 0.85f);
            listRT.offsetMin = Vector2.zero;
            listRT.offsetMax = Vector2.zero;

            ScrollRect scrollRect = listObj.AddComponent<ScrollRect>();
            Image listBg = listObj.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(listObj.transform, false);
            RectTransform contentRT = contentObj.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRT;
            songSelect.songListContent = contentObj.transform;
            songSelect.songListPanel = panelObj;

            // 歌曲按钮预制体
            GameObject btnPrefab = new GameObject("SongButtonPrefab");
            btnPrefab.transform.SetParent(panelObj.transform, false);
            RectTransform btnRT = btnPrefab.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(0, 70);
            Image btnBg = btnPrefab.AddComponent<Image>();
            btnBg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
            Button btnComp = btnPrefab.AddComponent<Button>();
            ColorBlock cb = btnComp.colors;
            cb.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            cb.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
            btnComp.colors = cb;

            GameObject btnLabel = new GameObject("Label");
            btnLabel.transform.SetParent(btnPrefab.transform, false);
            RectTransform labelRT = btnLabel.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(10, 5);
            labelRT.offsetMax = new Vector2(-10, -5);
            TextMeshProUGUI label = btnLabel.AddComponent<TextMeshProUGUI>();
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            btnPrefab.SetActive(false);

            songSelect.songButtonPrefab = btnPrefab;

            // 开始按钮
            GameObject startBtn = new GameObject("StartButton");
            startBtn.transform.SetParent(panelObj.transform, false);
            RectTransform startRT = startBtn.AddComponent<RectTransform>();
            startRT.anchorMin = new Vector2(0.5f, 0.05f);
            startRT.anchorMax = new Vector2(0.5f, 0.05f);
            startRT.sizeDelta = new Vector2(300, 70);
            startRT.anchoredPosition = new Vector2(0, 50);
            Image startBg = startBtn.AddComponent<Image>();
            startBg.color = new Color(0.1f, 0.6f, 0.2f, 1f);
            Button startComp = startBtn.AddComponent<Button>();
            ColorBlock startCB = startComp.colors;
            startCB.highlightedColor = new Color(0.15f, 0.75f, 0.25f, 1f);
            startComp.colors = startCB;

            GameObject startLabel = CreateTMPText("StartLabel", startBtn.transform,
                Vector2.zero, new Vector2(200, 40), "START", 28, TextAlignmentOptions.Center);
            RectTransform slRT = startLabel.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0.5f, 0.5f);
            slRT.anchorMax = new Vector2(0.5f, 0.5f);
            slRT.anchoredPosition = Vector2.zero;

            songSelect.startButton = startComp;

            // 歌曲信息
            GameObject songName = CreateTMPText("SongName", panelObj.transform,
                new Vector2(-20, -90), new Vector2(300, 40),
                "请选择歌曲", 24, TextAlignmentOptions.Left);
            RectTransform snRT = songName.GetComponent<RectTransform>();
            snRT.anchorMin = new Vector2(0.5f, 1f);
            snRT.anchorMax = new Vector2(0.5f, 1f);
            snRT.anchoredPosition = new Vector2(0, -100);
            songSelect.songNameText = songName.GetComponent<TextMeshProUGUI>();

            GameObject songInfo = CreateTMPText("SongInfo", panelObj.transform,
                new Vector2(0, -140), new Vector2(400, 60),
                "", 18, TextAlignmentOptions.Center);
            RectTransform siRT = songInfo.GetComponent<RectTransform>();
            siRT.anchorMin = new Vector2(0.5f, 1f);
            siRT.anchorMax = new Vector2(0.5f, 1f);
            siRT.anchoredPosition = new Vector2(0, -140);
            songSelect.songInfoText = songInfo.GetComponent<TextMeshProUGUI>();

            // 状态文本
            GameObject statusObj = CreateTMPText("StatusText", panelObj.transform,
                new Vector2(0, -180), new Vector2(500, 40),
                "", 16, TextAlignmentOptions.Center);
            RectTransform stRT = statusObj.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.5f, 1f);
            stRT.anchorMax = new Vector2(0.5f, 1f);
            stRT.anchoredPosition = new Vector2(0, -180);
            songSelect.statusText = statusObj.GetComponent<TextMeshProUGUI>();
            songSelect.statusText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        #endregion

        #region 工具方法

        private static GameObject CreateSingletonObject<T>(string name) where T : Component
        {
            if (Object.FindObjectOfType<T>() != null) return null;

            GameObject go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        #endregion
    }
}
