using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// 音符生成器（单例）
    /// 职责：对象池管理、音符生成调度、活跃音符查询
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        #region Singleton
        public static NoteSpawner Instance { get; private set; }
        #endregion

        [Header("对象池配置")]
        [Tooltip("预实例化音符数量")]
        public int poolSize = 30;

        [Tooltip("音符预制体（运行时自动创建，也可手动指定）")]
        public GameObject notePrefab;

        [Header("调试")]
        [SerializeField] private List<NoteController> activeNotes = new List<NoteController>();

        // 对象池
        private Queue<GameObject> pool = new Queue<GameObject>();

        // 谱面数据引用
        private BeatmapData beatmap;
        private int nextNoteIndex;
        private bool isSpawning;

        // 固定参数
        private const float JUDGE_Y = NoteController.JUDGE_Y;
        private const float SPAWN_Y = NoteController.SPAWN_Y;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializePool();
        }

        /// <summary>
        /// 初始化对象池 — 预创建音符并设为非激活
        /// </summary>
        private void InitializePool()
        {
            // 如果没有手动指定预制体，自动创建一个
            if (notePrefab == null)
            {
                notePrefab = CreateDefaultNotePrefab();
            }

            // 创建池父节点
            Transform poolParent = transform.Find("NotePool");
            if (poolParent == null)
            {
                GameObject go = new GameObject("NotePool");
                go.transform.SetParent(transform);
                poolParent = go.transform;
            }

            // 预实例化
            for (int i = 0; i < poolSize; i++)
            {
                GameObject note = Instantiate(notePrefab, poolParent);
                note.name = $"Note_Pooled_{i}";
                note.SetActive(false);

                // 确保有 NoteController 组件
                if (note.GetComponent<NoteController>() == null)
                    note.AddComponent<NoteController>();

                pool.Enqueue(note);
            }

            Debug.Log($"[NoteSpawner] 对象池初始化完成，共 {poolSize} 个音符");
        }

        /// <summary>
        /// 创建默认音符预制体 — 白色方块 Sprite + BoxCollider2D
        /// </summary>
        private GameObject CreateDefaultNotePrefab()
        {
            // 创建一个白色方形纹理
            Texture2D tex = new Texture2D(64, 51); // Width=1, Height=0.8 比例
            Color[] pixels = new Color[tex.width * tex.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            // 创建 Sprite
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                64f // pixels per unit
            );
            sprite.name = "DefaultNoteSprite";

            // 创建预制体对象
            GameObject prefab = new GameObject("NotePrefab");
            prefab.SetActive(false); // 预制体不激活

            SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            BoxCollider2D col = prefab.AddComponent<BoxCollider2D>();
            col.isTrigger = true; // 使用触发器便于判定
            col.size = new Vector2(1f, 0.8f);

            prefab.AddComponent<NoteController>();

            return prefab;
        }

        /// <summary>
        /// 开始生成音符
        /// </summary>
        public void StartSpawning(BeatmapData data)
        {
            beatmap = data;
            nextNoteIndex = 0;
            isSpawning = true;
            beatmap.SortNotes(); // 确保按时间排序
        }

        /// <summary>
        /// 停止生成，回收所有活跃音符
        /// </summary>
        public void StopSpawning()
        {
            isSpawning = false;
            RecycleAllActiveNotes();
        }

        private void Update()
        {
            if (!isSpawning || beatmap == null) return;

            float currentTime = AudioManager.Instance != null
                ? AudioManager.Instance.CurrentTime
                : Time.time;

            float fallSpeed = beatmap.fallSpeed;
            float travelTime = (SPAWN_Y - JUDGE_Y) / fallSpeed;

            // 检查是否有音符需要生成
            while (nextNoteIndex < beatmap.notes.Count)
            {
                NoteData noteData = beatmap.notes[nextNoteIndex];
                float spawnTime = noteData.time - travelTime;

                if (currentTime >= spawnTime)
                {
                    SpawnNote(noteData, fallSpeed);
                    nextNoteIndex++;
                }
                else
                {
                    break; // 还没到下一个音符的生成时间
                }
            }

            // 清理已被回收但仍留在列表中的引用
            activeNotes.RemoveAll(n => n == null || !n.gameObject.activeSelf);
        }

        /// <summary>
        /// 从对象池取一个音符并初始化
        /// </summary>
        private void SpawnNote(NoteData data, float fallSpeed)
        {
            GameObject noteObj = GetFromPool();
            if (noteObj == null) return;

            NoteController controller = noteObj.GetComponent<NoteController>();
            if (controller == null)
            {
                controller = noteObj.AddComponent<NoteController>();
            }

            controller.Initialize(data, fallSpeed, JUDGE_Y);
            activeNotes.Add(controller);
        }

        /// <summary>
        /// 从对象池获取音符（池空则扩容）
        /// </summary>
        private GameObject GetFromPool()
        {
            if (pool.Count == 0)
            {
                // 池空扩容
                Debug.LogWarning("[NoteSpawner] 对象池耗尽，动态扩容中...");
                if (notePrefab != null)
                {
                    GameObject newNote = Instantiate(notePrefab, transform.Find("NotePool"));
                    newNote.name = $"Note_Pooled_Extra_{poolSize++}";
                    return newNote;
                }
                return null;
            }

            return pool.Dequeue();
        }

        /// <summary>
        /// 将音符回收到对象池
        /// </summary>
        public void ReturnNoteToPool(GameObject note)
        {
            if (note == null) return;

            // 立即从活跃列表中移除
            NoteController controller = note.GetComponent<NoteController>();
            if (controller != null)
                activeNotes.Remove(controller);

            note.transform.SetParent(transform.Find("NotePool"));
            note.SetActive(false);
            pool.Enqueue(note);
        }

        /// <summary>
        /// 获取指定轨道中最近的未判定音符（供 InputHandler 调用）
        /// </summary>
        /// <param name="lane">轨道序号 (0-3)</param>
        /// <param name="audioTime">当前音频时间</param>
        /// <returns>最近的音符，若无可判定音符则返回 null</returns>
        public NoteController GetNearestUnjudgedNote(int lane, float audioTime)
        {
            NoteController best = null;
            float bestDelta = float.MaxValue;

            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var note = activeNotes[i];
                if (note == null || !note.gameObject.activeSelf || note.IsJudged || note.Lane != lane)
                    continue;

                float delta = Mathf.Abs(audioTime - note.HitTime);
                if (delta < bestDelta && delta <= 0.2f)
                {
                    bestDelta = delta;
                    best = note;
                }
            }

            return best;
        }

        /// <summary>
        /// 回收所有活跃音符
        /// </summary>
        public void RecycleAllActiveNotes()
        {
            foreach (var note in activeNotes)
            {
                if (note != null && note.gameObject.activeSelf)
                {
                    note.gameObject.SetActive(false);
                    pool.Enqueue(note.gameObject);
                }
            }
            activeNotes.Clear();
        }
    }
}
