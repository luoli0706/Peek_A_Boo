# Phase 1 — Unity Scene Setup Guide / Unity 场景搭建指南

**Date / 日期:** 2026-05-05  
**Prerequisite / 前置条件:** Phase 0 verified (server connects, Welcome/JoinRoom works) / Phase 0 已验证通过（服务器可连接，Welcome/JoinRoom 正常工作）  
**Target / 目标:** Mount all Phase 1 scripts, configure UI and Input, ready for end-to-end test / 挂载所有 Phase 1 脚本，配置 UI 和输入系统，准备端到端测试

---

## 0. Pre-flight Checks / 前置检查

Open the Unity project. In **Window > Package Manager**, ensure these packages are installed:  
打开 Unity 项目，在 **Window > Package Manager** 中确认以下包已安装：

| Package / 包名 | Required by / 用途 |
|---------|-------------|
| `com.unity.textmeshpro` | All UI / 全部 UI（TMP_Text, TMP_InputField） |
| `com.unity.inputsystem` | PlayerController / 玩家控制器（OnMove/OnLook/OnCrouch/OnJump） |

If Input System is newly installed, Unity will prompt to restart — accept it.  
如果 Input System 是新安装的，Unity 会提示重启——接受即可。

Open `Assets/Scripts/` and confirm all 7 scripts compile without errors in the Console:  
打开 `Assets/Scripts/`，确认以下 7 个脚本在 Console 中无编译错误：  
`NetworkManager.cs`, `GameManager.cs`, `MenuController.cs`, `LevelTimer.cs`, `PlayerController.cs`, `RemotePlayerManager.cs`, `ProceduralMapGenerator.cs`

Plus generated / 以及生成的: `ClientProtocol.cs`, `GeneratedProtocol.cs`

---

## 1. Create Scene Objects / 创建场景对象

Open **SampleScene** (or create a new empty scene).  
打开 **SampleScene**（或新建空场景）。

### 1.1 Persistent Singletons / 持久化单例

Create two empty GameObjects (right-click Hierarchy → Create Empty):  
创建两个空 GameObject（Hierarchy 右键 → Create Empty）：

```
GameObject "NetworkManager"
  └─ Add Component → NetworkManager
     Server IP: 127.0.0.1
     Server Port: 9000
     Player Name: Player

GameObject "GameManager"
  └─ Add Component → GameManager
```

Both singletons use `DontDestroyOnLoad` — they will survive scene reloads. Only one of each should exist in the scene.  
两个单例都使用 `DontDestroyOnLoad`——它们会在场景重载时保留。场景中每种只能存在一个。

### 1.2 Map Generator / 地图生成器

```
GameObject "MapGenerator"
  └─ Add Component → ProceduralMapGenerator
     Map Width / 地图宽度: 50
     Map Length / 地图长度: 50
     Wall Height / 墙壁高度: 4
     Obstacle Count / 障碍物数量: 20
     Seed / 随机种子: 42
```

**Test / 测试:** Press Play. Hierarchy should show `ProceduralMap` with Ground, 4 Wall_* cubes, Obstacle_* objects, and 7 Spawn_* points. Stop.  
按 Play。Hierarchy 中应出现 `ProceduralMap`，包含 Ground、4 个 Wall_* 方块、Obstacle_* 物体和 7 个 Spawn_* 出生点。然后 Stop。

### 1.3 Player (Local) / 本地玩家

Create the player object that carries input + camera:  
创建承载输入和摄像机的玩家对象：

```
GameObject "LocalPlayer"
  ├─ Add Component → PlayerController
  │    Move Speed / 移动速度: 5
  │    Crouch Speed / 蹲伏速度: 2
  │    Mouse Sensitivity / 鼠标灵敏度: 2
  │    Camera Transform: (drag child Camera here — see below / 拖入子摄像机)
  ├─ Add Component → PlayerInput          ← from Input System package / 来自 Input System 包
  └─ Child: Camera (Create → Camera)
       └─ Position: (0, 1.7, 0)          ← eye height / 眼睛高度
```

**PlayerInput component setup** (detailed in Section 3 below).  
**PlayerInput 组件设置**（详见第 3 节）。

### 1.4 Remote Player Manager / 远程玩家管理器

```
GameObject "RemotePlayerManager"
  └─ Add Component → RemotePlayerManager
     Remote Player Prefab / 远程玩家预制体: (leave empty — uses default Capsule at runtime / 留空——运行时使用默认胶囊体)
```

---

## 2. Canvas & UI / 画布与界面

Create the UI hierarchy (right-click Hierarchy → UI → ...):  
创建 UI 层级（Hierarchy 右键 → UI → ...）：

### 2.1 Canvas / 画布

```
Canvas (Render Mode: Screen Space - Overlay / 渲染模式: 屏幕空间覆盖)
  └─ Add Component → MenuController
       Main Menu Panel / 主菜单面板: → MainMenuPanel
       Player Name Input / 玩家名输入: → PlayerNameInput
       Server IP Input / 服务器IP输入: → ServerIPInput
       Connect Button / 连接按钮: → ConnectButton
       Status Text / 状态文本: → StatusText
       HUD Panel / HUD面板: → HUDPanel
       Role Label / 角色标签: → RoleLabel
       State Label / 状态标签: → StateLabel
```

### 2.2 Main Menu Panel / 主菜单面板

Create as child of Canvas (right-click → UI → Panel, rename to "MainMenuPanel"):  
创建为 Canvas 的子对象（右键 → UI → Panel，重命名为 "MainMenuPanel"）：

```
MainMenuPanel
  ├─ Title / 标题 (UI → Text - TextMeshPro)
  │    Text / 文本: "Peek-A-Boo"
  │    Font Size / 字号: 48
  │    Alignment / 对齐: Center / 居中
  │    RectTransform: Y=120, Width=400, Height=60
  │
  ├─ PlayerNameLabel / 玩家名标签 (TMP_Text)
  │    Text / 文本: "Player Name"
  │    RectTransform: Y=40, Width=200, Height=30
  ├─ PlayerNameInput (UI → Input Field - TextMeshPro)
  │    RectTransform: Y=0, Width=300, Height=40
  │    (uses TMP_InputField component / 使用 TMP_InputField 组件)
  │
  ├─ ServerIPLabel / 服务器IP标签 (TMP_Text)
  │    Text / 文本: "Server IP"
  │    RectTransform: Y=-50, Width=200, Height=30
  ├─ ServerIPInput (UI → Input Field - TextMeshPro)
  │    RectTransform: Y=-90, Width=300, Height=40
  │
  ├─ ConnectButton / 连接按钮 (UI → Button - TextMeshPro)
  │    RectTransform: Y=-150, Width=200, Height=50
  │    Child TMP_Text / 子文本: "Connect"
  │
  └─ StatusText / 状态文本 (TMP_Text)
       Text / 文本: ""  (empty, shows "Connecting..." etc. / 空，显示 "Connecting..." 等)
       RectTransform: Y=-200, Width=400, Height=30
```

### 2.3 HUD Panel / 抬头显示面板

Create as child of Canvas (right-click → UI → Panel, rename to "HUDPanel").  
创建为 Canvas 的子对象（右键 → UI → Panel，重命名为 "HUDPanel"）。  
Set **initially disabled** (uncheck the checkbox next to GameObject name in Inspector).  
设为 **初始禁用**（取消 Inspector 中 GameObject 名称旁的勾选）。

```
HUDPanel (initially INACTIVE / 初始非激活)
  ├─ RoleLabel / 角色标签 (UI → Text - TextMeshPro)
  │    RectTransform: top-left anchor / 左上锚点, Y=-20, Width=200, Height=30
  │    Text / 文本: "Role: --"
  │
  ├─ StateLabel / 状态标签 (UI → Text - TextMeshPro)
  │    RectTransform: top-center anchor / 顶部居中锚点, Y=-20, Width=200, Height=30
  │    Text / 文本: "Waiting for players..."
  │
  └─ TimerText / 计时器文本 (UI → Text - TextMeshPro)
       RectTransform: top-right anchor / 右上锚点, Y=-20, Width=150, Height=40
       Text / 文本: "00:00"
       Font Size / 字号: 36
       └─ Add Component → LevelTimer
            Timer Text: (drag TimerText here / 将 TimerText 拖入此处)
```

### 2.4 Wire Up MenuController References / 接线 MenuController 引用

Select the Canvas GameObject. In the MenuController component, drag:  
选中 Canvas GameObject，在 MenuController 组件中拖入：

| Field / 字段 | Target / 目标 |
|-------|--------|
| Main Menu Panel | MainMenuPanel |
| Player Name Input | PlayerNameInput (TMP_InputField) |
| Server IP Input | ServerIPInput (TMP_InputField) |
| Connect Button | ConnectButton (Button) |
| Status Text | StatusText (TMP_Text) |
| HUD Panel | HUDPanel |
| Role Label | RoleLabel (TMP_Text) |
| State Label | StateLabel (TMP_Text) |

---

## 3. Input System Setup / 输入系统设置

### 3.1 Create Input Actions Asset / 创建输入动作资源

In Project window: right-click → **Create → Input Actions**, name it `PlayerControls`.  
在 Project 窗口中：右键 → **Create → Input Actions**，命名为 `PlayerControls`。

Double-click to open the Input Actions editor.  
双击打开 Input Actions 编辑器。

### 3.2 Define Action Map / 定义动作映射

Create an Action Map named **"Player"** with these Actions:  
创建一个名为 **"Player"** 的 Action Map，包含以下 Actions：

| Action / 动作 | Type / 类型 | Control Type / 控制类型 | Bindings / 绑定 |
|--------|------|-------------|----------|
| **Move / 移动** | Value | Vector2 | WASD (composite: 2D Vector) |
| **Look / 视角** | Value | Vector2 | Mouse Delta / 鼠标增量 |
| **Crouch / 蹲伏** | Button | (default / 默认) | Left Ctrl [Keyboard] |
| **Jump / 跳跃** | Button | (default / 默认) | Space [Keyboard] |

For **Move** composite: Up=W, Down=S, Left=A, Right=D  
**Move** 组合键：上=W, 下=S, 左=A, 右=D  
For **Look**: Delta [Mouse] (set Path to `Delta [Mouse]`)  
**Look**：Delta [Mouse]（将 Path 设为 `Delta [Mouse]`）

Click **Save Asset**.  
点击 **Save Asset**。

### 3.3 Enable "Generate C# Class" / 启用"生成 C# 类"

In the Input Actions inspector, check **Generate C# Class**, then click **Apply**.  
在 Input Actions 的 Inspector 中，勾选 **Generate C# Class**，然后点击 **Apply**。  
This generates `Assets/PlayerControls.cs`.  
这将生成 `Assets/PlayerControls.cs`。

### 3.4 Bind to LocalPlayer / 绑定到本地玩家

Select the **LocalPlayer** GameObject.  
选中 **LocalPlayer** GameObject。  
On the **PlayerInput** component:  
在 **PlayerInput** 组件上：

| Setting / 设置 | Value / 值 |
|---------|-------|
| Actions / 动作资源 | PlayerControls |
| Default Scheme / 默认方案 | (leave empty / 留空) |
| Behavior / 行为 | Invoke Unity Events |

Expand **Events** → **Player** → map each action:  
展开 **Events** → **Player** → 映射每个动作：

| Action / 动作 | Target / 目标 | Method / 方法 |
|--------|--------|--------|
| Move / 移动 | LocalPlayer → PlayerController | OnMove |
| Look / 视角 | LocalPlayer → PlayerController | OnLook |
| Crouch / 蹲伏 | LocalPlayer → PlayerController | OnCrouch |
| Jump / 跳跃 | LocalPlayer → PlayerController | OnJump |

(Select "PlayerController.OnMove" etc. from the dropdown / 从下拉菜单中选择 "PlayerController.OnMove" 等。)

---

## 4. Final Wiring Verification / 最终接线验证

### 4.1 Script Execution Order / 脚本执行顺序

Unity's `Awake()` ensures singletons are set before any `Start()` runs. No manual execution order changes needed for Phase 1.  
Unity 的 `Awake()` 确保所有单例在任意 `Start()` 执行前均已就绪。Phase 1 无需手动调整执行顺序。

### 4.2 Reference Checklist / 引用检查清单

| Script / 脚本 | Field / 字段 | Should Reference / 应引用 |
|--------|-------|-----------------|
| MenuController | All 8 fields / 全部8个字段 | Corresponding UI objects / 对应 UI 对象 (Section 2.4 / 第2.4节) |
| PlayerController | Camera Transform | LocalPlayer → Camera |
| LevelTimer | Timer Text | HUDPanel → TimerText |
| PlayerInput | Actions | PlayerControls asset / PlayerControls 资源 |
| PlayerInput | Events | PlayerController methods / PlayerController 方法 |

All other script references are resolved at runtime via singleton lookups (`NetworkManager.Instance`, `GameManager.Instance`).  
其余脚本引用均通过单例查找在运行时解析（`NetworkManager.Instance`、`GameManager.Instance`）。

---

## 5. Quick Smoke Test / 快速冒烟测试

1. **Build server / 编译服务器**: `cd server && cmake --build build`
2. **Start server / 启动服务器**: `./build/server.exe` (or `build3/`)
3. **In Unity / 在 Unity 中**: Press Play / 按 Play
   - Main menu should appear with title, inputs, and Connect button / 主菜单应显示标题、输入框和连接按钮
   - Click Connect → StatusText shows "Connecting..." / 点击 Connect → StatusText 显示 "Connecting..."
   - After server Welcome → menu hides, HUD shows role + state / 收到服务器 Welcome 后 → 菜单隐藏，HUD 显示角色和状态
   - ProceduralMap generates in scene / 场景中生成 ProceduralMap
   - Timer text appears (00:00 initially) / 计时器文本出现（初始为 00:00）
4. **Stop / 停止**, then launch a second Unity Editor instance, open same project, Play / 然后启动第二个 Unity Editor 实例，打开同一项目，Play
   - Both clients connect, see each other's capsules moving / 两个客户端均连接，能看到对方的胶囊体移动
   - Check Console for `[Network]`, `[GameManager]`, `[MapGen]` messages / 检查 Console 中的 `[Network]`、`[GameManager]`、`[MapGen]` 日志
5. **Stop** both clients, stop server / **停止**两个客户端，停止服务器

---

## 6. Troubleshooting / 故障排除

| Symptom / 症状 | Likely Cause / 可能原因 | Check / 检查方法 |
|---------|-------------|-------|
| "ENet.Library.Initialize() failed" | enet.dll not found / enet.dll 未找到 | Copy enet.dll to Assets/Plugins/ / 复制 enet.dll 到 Assets/Plugins/ |
| Connect does nothing / 点击连接无反应 | NetworkManager.Start() was already called? / NetworkManager.Start() 已调用过？ | Check NetworkManager has `enetReady=true` log / 检查 NetworkManager 是否有 `enetReady=true` 日志 |
| No player movement / 玩家无法移动 | Input System not wired / Input System 未接线 | Verify PlayerInput events → PlayerController methods / 验证 PlayerInput 事件已连接到 PlayerController 方法 |
| NullReference in GameManager.Start | NetworkManager not in scene / NetworkManager 不在场景中 | Ensure NetworkManager GameObject exists and is active / 确保 NetworkManager GameObject 存在且激活 |
| Timer doesn't show / 计时器不显示 | GameState never changes from WaitingForPlayers / GameState 未从 WaitingForPlayers 变更 | Server must send GameStateChange after enough players ready / 服务器必须在足够玩家就绪后发送 GameStateChange |
| Map doesn't appear / 地图不出现 | ProceduralMapGenerator not in scene / ProceduralMapGenerator 不在场景中 | Check "MapGenerator" GameObject exists and is active / 检查 "MapGenerator" GameObject 存在且激活 |

---

## 7. Next Steps After Verification / 验证后的后续步骤

Once the smoke test passes / 冒烟测试通过后:
- Phase 1 is functionally complete / Phase 1 功能完成
- Move to Phase 2: HighlightSystem, TagSystem, ScoreManager / 进入 Phase 2：HighlightSystem、TagSystem、ScoreManager
- Consider running `Tools/generate_protocol.py` before adding new message types / 添加新消息类型前，建议先运行 `Tools/generate_protocol.py`
