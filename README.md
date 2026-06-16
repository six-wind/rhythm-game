# 🎵 Rhythm Game — 4键下落式节奏音游

一款极简的2D下落式节奏音游（对标节奏大师核心玩法），纯 Unity 原生实现。

**核心特性：**
- 🎶 自定义导入任意 MP3/WAV 音乐
- ✏️ 可视化谱面编辑器，自由增删音符
- 🎯 三段精准判定（Perfect / Good / Miss），基于音频真实时间
- ⌨️ D F J K 四键操作
- 📊 实时分数、连击、最大连击统计
- 🖥 支持 Windows / macOS 桌面版 + WebGL 浏览器版

---

## 🚀 快速开始

### 环境要求
- **Unity 2022.3 LTS** 或更高版本
- 安装 Unity 时勾选 **Windows Build Support** / **macOS Build Support** / **WebGL Build Support**

### 打开项目

1. 克隆仓库：
   ```bash
   git clone git@github.com:six-wind/rhythm-game.git
   ```

2. 打开 Unity Hub，点击 **Open** → 选择 `rhythm-game` 文件夹

3. 首次打开后，运行场景搭建：
   - 菜单栏 → **Tools** → **RhythmGame** → **Setup Scene**
   - 这会自动创建主摄像机、UI、管理器等所有游戏对象

4. 点击 Unity 顶部的 **▶ Play** 按钮即可运行！

### 导入你的音乐

#### 方法一：编辑器拖入（推荐）
1. 将 MP3/WAV 文件拖入 `Assets/Resources/Music/` 文件夹
2. 右键 → **Create** → **RhythmGame** → **BeatmapData** 创建谱面
3. 将音乐文件拖到 BeatmapData 的 `Music` 字段
4. 在谱面编辑器中编辑音符

#### 方法二：JSON 导入
使用 JSON 格式批量导入谱面：
```json
{
  "songName": "我的歌曲",
  "bpm": 140,
  "fallSpeed": 8,
  "notes": [
    {"time": 1.0, "lane": 0},
    {"time": 1.5, "lane": 2},
    {"time": 2.0, "lane": 1},
    {"time": 2.5, "lane": 3}
  ]
}
```

在谱面编辑器中点击 **导入 JSON** 即可。

### 创建谱面

1. 菜单栏 → **Tools** → **RhythmGame** → **Beatmap Editor**
2. 选择或新建谱面
3. 手动添加音符：输入时间 + 轨道 (0-3)
4. 或导入 JSON 格式谱面
5. 拖入音乐文件即可预览

### 示例谱面
菜单栏 → **Tools** → **RhythmGame** → **Create Sample Beatmap** 可快速生成示例谱面。

---

## 🎮 游戏操作

| 按键 | 轨道 | X坐标 |
|------|------|-------|
| **D** | Lane 0 | -1.5 |
| **F** | Lane 1 | -0.5 |
| **J** | Lane 2 | 0.5 |
| **K** | Lane 3 | 1.5 |

- **ESC** — 暂停/继续
- 音符从上往下匀速下落，到达判定线时按键击打

---

## 🎯 判定规则

| 判定 | 时间偏差 | 分数 | 连击 |
|------|----------|------|------|
| **Perfect** | ≤ ±0.05s | +100 | +1 |
| **Good** | ≤ ±0.12s | +50 | +1 |
| Normal | ≤ ±0.2s | 0 | 不断 |
| **Miss** | > ±0.2s / 漏击 | 0 | 清零 |

> ⚠️ 判定基于 `AudioSource.time`（音频真实播放时间），确保音画同步！

---

## 📦 构建与发布

### 本地构建
1. **File** → **Build Settings**
2. 选择目标平台（Windows/macOS/WebGL）
3. 点击 **Build**

### 自动构建（GitHub Actions）
推送 tag 即可触发自动构建：
```bash
git tag v1.0.0
git push origin v1.0.0
```

构建产物会自动发布到 GitHub Releases。

### 部署到 GitHub Pages
1. 构建 WebGL 版本
2. 将 `Build/WebGL` 文件夹内容复制到 `docs/webgl/`
3. 推送后 GitHub Pages 自动部署
4. 访问：`https://six-wind.github.io/rhythm-game/`

---

## 📁 项目结构

```
Assets/
├── Scripts/
│   ├── Core/              # 核心系统
│   │   ├── GameManager.cs       # 游戏状态管理
│   │   ├── AudioManager.cs      # 音频播放 & 精确计时
│   │   └── ScoreManager.cs      # 分数 & 连击管理
│   ├── Gameplay/           # 游戏玩法
│   │   ├── NoteController.cs    # 音符运动 & 判定
│   │   └── InputHandler.cs      # D/F/J/K 输入处理
│   ├── Data/               # 数据定义
│   │   ├── BeatmapData.cs       # ScriptableObject 谱面
│   │   ├── NoteData.cs          # 音符数据结构
│   │   └── JudgmentType.cs      # 判定类型枚举
│   ├── UI/                 # 界面
│   │   ├── HUDController.cs     # 分数/连击显示
│   │   └── SongSelectUI.cs      # 选曲界面
│   ├── Editor/             # 编辑器工具
│   │   ├── BeatmapEditorWindow.cs # 谱面编辑器
│   │   └── SceneSetupEditor.cs    # 场景搭建菜单
│   └── Setup/              # 自动搭建
│       ├── RuntimeSceneSetup.cs   # 场景对象创建
│       └── GameBootstrap.cs       # 启动引导
├── Resources/
│   └── Beatmaps/           # 谱面资源（.asset）
└── Scenes/
    └── Main.unity          # 主场景
```

---

## 🔧 技术要点

- **精准计时**：所有判定基于 `AudioSource.time`，禁止使用 `Time.time` 避免帧率影响
- **音频优化**：播放前调用 `LoadAudioData()` 预加载流式音频数据
- **对象池**：音符使用对象池避免 GC 卡顿
- **音符定位**：`y = judgeY + (hitTime - audioTime) * fallSpeed`，确保音画完全同步
- **谱面数据**：ScriptableObject 格式，支持 JSON 互转

---

## 📄 License

MIT License — 自由使用和修改

---

**Made with Unity & ❤️**
