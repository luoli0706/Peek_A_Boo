# Phase 2 核心玩法系统——客户端组件部署与使用说明书

本说明文档详细阐述了在 **Phase 2（核心玩法系统）** 阶段为躲猫猫原型项目新增的 3 个客户端核心 Unity 组件：**`TagSystem.cs`**、**`HighlightSystem.cs`** 和 **`ScoreManager.cs`**。

本阶段完全重构了以往单一的“FPS 准星对准秒抓”的传统射击体验，升级为需要**持续长按交互键 F 并贴身博弈的“猫鼠拉扯”高阶交互**，并在此基础上提供了全局心跳曝光与结算积分面板系统。

---

## 1. 架构总览与事件分发解耦

为了确保网络底层与业务逻辑的解耦，我们升级了底层的 `NetworkManager.cs`。当客户端接收到服务器发来的网络载荷后，不再是单纯的 `Debug.Log` 打印，而是转换为公共的 C# 委托事件进行分发：

```csharp
// NetworkManager.cs
public event Action<Highlight> OnHighlightReceived;
public event Action<TagResult> OnTagResultReceived;
public event Action<ScoreBoard> OnScoreBoardReceived;
```

各业务系统（抓捕、曝光、结算）在 `Start()` 时直接订阅对应的事件，极大地提高了代码的模块化和健壮性。

---

## 2. 核心组件部署与挂载指南

### 🛠️ 组件一：TagSystem.cs（交互式抓捕与观战系统）
挂载于**玩家角色对象**（本地 Seeker 实例，以及控制本地 Hider 物体）。

#### 1. 核心玩法机制
* **准星射线检测**：每帧从屏幕中央发射一条 `ViewportPointToRay` 射线。对准带有 `RemotePlayer_` 标识前缀的躲藏者时，会在屏幕下方淡入操作提示。
* **按住 F 蓄力博弈**：
  Seeker 玩家必须**持续按住键盘上的 F 键**方可进行捕获蓄力。在此期间，准心正下方将浮现动态的赛博朋克蓝绿发光进度条，并实时显示百分比数值（由 0% 增长至 100%）。
* **动态距离重置**：
  若在 2 秒满额之前，Hider 运用身法或卡视野使彼此的距离超出了 `maxInteractDistance`（默认 3 米），则**蓄力瞬间中断，进度值立刻清零并淡出**。这为躲藏者死里逃生创造了微操对抗可能。
* **被捕获锁定与观战转换**：
  一旦蓄力满 2 秒，Seeker 端自动向服务器提交 `TagAttempt` 数据包。服务器核对通过后广播 `TagResult`。被抓住的 Hider 实例收到广播后，本地将：
  1. 彻底禁用移动控制：自动运行 `PlayerController.enabled = false`；
  2. 强制相机视角重定位：本地主摄像机 `Camera.main` 自动更改 Parent 节点至捕获它的 Seeker 节点上，实现肩膀观战（Over-The-Shoulder View）的“旁观者”模式转换。

#### 2. Inspector 核心参数配置
| 参数字段 | 默认值 | 作用说明 |
| :--- | :--- | :--- |
| `baseInteractTime` | `2.0f` | 基础蓄力时间（秒）。可在 Inspector 自由微调为 1.5 秒或 3.0 秒。 |
| `maxInteractDistance` | `3.0f` | 有效抓捕的最大三维几何距离（米）。超出此值交互重置。 |
| `promptText` | `None` | (可选) 拖入 UI Canvas 下的 Text，用于显示 `[F] 捕获玩家` 的提示。若为空，将采用高解析度的 GUI 默认绘制。 |
| `progressSlider` | `None` | (可选) 拖入 UI Canvas 下的 Slider，用于映射蓄力百分比。若为空，将采用手绘赛博朋克蓄力条进行屏幕渲染。 |

---

### 📡 组件二：HighlightSystem.cs（心跳高亮暴露系统）
通常挂载于 **GameManager 节点** 或 **UI Canvas 管理器** 上。

#### 1. 核心玩法机制
* 为了打破躲藏者躲在死角导致比赛僵死的局面，服务端每隔 30 秒广播一次全局高亮脉冲。
* 本地 Seeker 客户端收到 `OnHighlightReceived` 订阅事件后：
  1. 从世界空间中检索对应的 `RemotePlayer_xx` 实体坐标；
  2. 开启一个独立的高亮协程（由 `highlightDuration` 决定每次脉冲显示几秒）；
  3. **世界投影渲染**：如果被曝光的 Hider 处于 Seeker 视野前方，直接在屏幕投影坐标上手绘带有闪烁微动画的警示小红点与测距 `HIDER_ID (XX米)`；
  4. **屏幕边缘指向警报**：如果被曝光的 Hider 躲在 Seeker 的身后或视野外，直接在屏幕边缘进行动态警示（例如“⚠️ 警报: 左后方有躲藏者曝光！”），引导 Seeker 转身。

#### 2. Inspector 核心参数配置
| 参数字段 | 默认值 | 作用说明 |
| :--- | :--- | :--- |
| `highlightDuration` | `3.0f` | 每次被心跳广播照射后，曝光高亮红点和警报在屏幕上持续停留的秒数。 |
| `indicatorPrefab` | `None` | (可选) UI Canvas 上的三维标记 UI Prefab，若提供，可在 Canvas 下动态生成并跟随 Hider。若空则直接通过高性能 OnGUI 屏幕辅助层完美手绘。 |
| `canvasParent` | `None` | (可选) 指示器 Prefab 实例化时的 Canvas 父节点。 |

---

### 🏆 组件三：ScoreManager.cs（单局计分与结算系统）
挂载于 **GameManager 节点** 或 **UI 结算面板 Canvas** 上。

#### 1. 核心玩法机制
* 当本局比赛结束（时间到或全部抓获，触发 `GameState.RoundEnd` 或 `GameState.GameOver`）时，服务端向全体客户端广播结算 `ScoreBoard` json 字符串。
* **双通道高容错反序列化**：
  * **主通道**：使用 Unity 内置 `JsonUtility.FromJson` 将得分排行榜快速反序列化为 `ScoreBoardPayload` 结构体，获取玩家 ID、名称、阵营角色、抓捕数、生存时长、最终得分。
  * **Fallback 备份通道**：若传入的 JSON 因第三方或自定义的额外字段导致 JsonUtility 报错，系统会自动捕获 Exception 并运行基于字符串切分的**防御性 Fallback 手动解析器**，提取核心成绩列，严防客户端因解析 JSON 错误而闪退。
* **结算 UI 渲染**：
  在结算时自动弹出带有金黄色高亮框线的“磨砂玻璃（Glassmorphism）”磨砂质感全屏结算积分面板，实时展示单局大满贯的冠亚军排行，并集成倒计时同步，在下一轮开始时自动重置隐藏。

#### 2. Inspector 核心参数配置
| 参数字段 | 默认值 | 作用说明 |
| :--- | :--- | :--- |
| `scoreboardPanel` | `None` | (可选) 拖入 UI 结算 Panel 根节点。在 RoundEnd 时自动显示，在下一局 Hiding/Seeking 时自动隐藏。 |
| `scoresDisplayText` | `None` | (可选) 拖入结算 Panel 下的 Text，系统会自动将积分榜格式化填充进去。 |

---

## 3. 编辑器部署实操步骤

为了方便您在 Unity 中进行快速验证，各组件均集成了 **“零配置（Zero-Configuration）”** 级的高品质 OnGUI 表现层。您可以按照以下三步在编辑器中轻松部署：

1. **挂载脚本**：
   * 将 `TagSystem.cs` 拖入您的本地 Player 角色（即挂载了 `PlayerController.cs` 的主体）上。
   * 将 `HighlightSystem.cs` 和 `ScoreManager.cs` 拖入主场景中的 `GameManager` 节点上。
2. **场景关联**：
   * 如果您已拼好 UI Canvas，把 `Text` 和 `Slider` 拖入 `TagSystem` 的 UI 引用中；如果未配置 UI，脚本会自动检测为空，并在运行时通过 OnGUI 的赛博朋克蓝绿蓄力进度条和提示进行渲染。
   * 为 `ScoreManager` 指定您的结算 Panel 即可实现自动显隐控制。
3. **参数调优**：
   * 在 `TagSystem` 中，根据您的地图大小和博弈手感，通过修改 `maxInteractDistance` 来定义射线能射多远，修改 `baseInteractTime` 来控制蓄力快慢。

---

## 4. 防御性设计与零空指针（Null-Safe）规范

* **Layer & 标签过滤**：射线检测自动对自身和场景几何体进行过滤，避免 Seeker 不小心射中自己，且对非 remote 玩家进行静默屏蔽。
* **延迟与离线容错**：在 `SendTagAttempt` 和 `EnterSpectatorMode` 中加入了完善的联机状态和 Peer 可用性检测。在离线调试或局域网高延迟下，蓄力与动作逻辑依旧能流畅退化，不会引发任何阻塞，表现稳定。

---

## 5. 👥 组件四：RemotePlayer.cs & RemotePlayerManager.cs（远程玩家行为展示与平滑同步系统）

`RemotePlayer.cs` 挂载于动态生成的**远程玩家角色对象**上，`RemotePlayerManager.cs` 挂载于场景中的 **RemotePlayerManager 节点**上。

### 1. 核心玩法与同步机制
* **平滑插值移动 (Lerp/Slerp)**：
  本地客户端收到服务端发送的最新 `PlayerStates` 网络高频包后，远程玩家不再进行粗暴的硬编码瞬移，而是通过 `Vector3.Lerp` 和 `Quaternion.Slerp` 在 Update 中平滑滑行到目标位置和朝向。
  * **瞬移阈值 (teleportThreshold)**：若远程玩家当前与最新目标的距离大于 `5.0f` 米（例如游戏开始重定位或复位），系统将立刻 Snap（瞬移）定位，防止出现长距离漂移或穿墙的穿帮画面。
  * **被捕获锁定**：若远程玩家的 `PlayerState` 为 `Caught`（被捕获状态），系统会停止所有平滑移动插值，直接锁定在其被捕获的坐标点，确保逻辑一致性。
* **贴地修正 (Ground Snapping)**：
  服务端发来的包仅包含水平坐标 (PosX, PosZ) 与 Yaw 轴朝向。`RemotePlayer.cs` 不再硬编码高度，而是从角色目标坐标上空 `5.0f` 米处向下发射射线检测，根据地形、障碍物或地面的实际高度修正 Y 轴高度。如果是 Capsule primitive 预制体，其中心位置自动修正为 `bestGroundY + 1.0f`。
* **阵营自发光与姿态视觉展示**：
  * **Seeker (Id = 0)**：自动换装为亮眼的**橙红色**，并激活 URP 自发光（Emission）渲染，使其在黑暗地图中也具有极强的压迫感。
  * **Hider (Id > 0)**：正常奔跑状态下呈现高饱和度的**霓虹绿色**；当按下下蹲时（`PlayerState.Crouching`）材质自动切换为**科技蓝色**。
  * **被捕获 (PlayerState.Caught)**：材质自动转换为**冷灰色**，且关闭自发光关键字，彻底表明该玩家已失去游戏机能。
  * **旁观者 (PlayerState.Spectating)**：完全不可见，系统会自动关闭其 `MeshRenderer` 和 `Collider`，防止幽灵角色在场上干扰 Seeker 射线的判定。
* **OnGUI 高级头顶测距标签**：
  在远程玩家头顶 `1.2` 米处渲染精致的身份测距标签，带有深度黑色阴影。实时呈现玩家的 ID、阵营（Seeker/Hider/Crouching Hider/Captured）以及其与本地相机的几何距离。超出 `40` 米视距会自动被裁剪隐藏，以防止拥挤。

### 2. 🛠️ Inspector 核心参数配置
| 参数字段 | 默认值 | 作用说明 |
| :--- | :--- | :--- |
| `lerpSpeed` | `15f` | 平滑移动速度，数值越大跟随越紧密，数值越小跟随越顺滑。 |
| `teleportThreshold` | `5f` | 瞬移阈值（米），超出此距离时判定为网络拉扯或初始出生，直接 Snap 坐标不进行插值。 |
| `spawnTestDummy` | `true` | (管理器参数) 是否自动在 Seeker 眼前生成用于测试 F 交互的 Hider 假人。 |

### 3. 🎯 本地极速单人测试假人机制（One-Click Dummy Verification）
为了解决开发过程中没有多名玩家或服务器环境不完备时无法测试抓捕的难点，`RemotePlayerManager.cs` 内置了 `spawnTestDummy` (默认勾选) 占位测试人机制：
* **单人测试**：无需启动服务器或多开客户端，直接在 Unity 编辑器中按下 Play，系统将在本地 Seeker 出生点前方 `2` 米处自动生成一个名为 `RemotePlayer_99` 的 Hider 假人，并自动挂载 `RemotePlayer` 与 CapsuleCollider。
* Seeker 玩家出生后，可以直接在眼前看到该霓虹绿色的 Hider 测试假人，头顶标签显示 `👥 [HIDER] Player 99 (2.0m)`。
* Seeker 直接走近并对准它，按下并长按 F 键即可完成 2 秒的完美蓄力与交互条捕获测试，充分验证了交互 F 键的所有功能（按住蓄力、拉开距离中断、百分比发光进度条）是否完备。
