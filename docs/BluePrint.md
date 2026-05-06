# Peek-A-Boo（躲猫猫）— 游戏蓝图

**版本：** v3.2（Phase 1 完成——客户端已可连接、移动、碰撞）
**日期：** 2026-05-06
**引擎：** Unity 6.3 LTS（URP / FPS 模板）
**类型：** PVP 第一人称躲猫猫 / 派对游戏

> v3.2：Phase 1 客户端实现完成——PlayerController（WASD移动+鼠标视角+跳跃+碰撞）、GameManager状态机、LevelTimer计时器、MenuController主菜单+HUD、RemotePlayerManager远程玩家同步、ProceduralMapGenerator程序化地图。新增协议代码生成器 Tools/generate_protocol.py。修复连接流程（Connect() 显式调用）、光标管理（菜单解锁/游戏中锁定）、Input System 集成（SendMessages + InputSystem_Actions）。

### 版本日志

| 版本 | 日期 | 变更 |
|------|------|------|
| v3.2 | 2026-05-06 | Phase 1 完成：7个客户端脚本调试通过，协议代码生成器，Input System 集成 |
| v3.1 | 2026-05-05 | Phase 0 完成，工具链修正为 WinLibs MinGW 独立安装，ENet-CSharp 单文件源码编译，Skills/ 技能注册体系建立 |
| v3.0 | 2026-05-02 | 架构迁移：WebSocket→ENET UDP，C++17→C++20，新增 BluePrint 文档 |
| v2.0 | — | uWebSockets/WebSocket 方案（已废弃） |

---

## 目录

1. [项目概述](#1-项目概述)
2. [游戏设计](#2-游戏设计)
3. [详细规则与系统](#3-详细规则与系统)
4. [技术架构 — C++ 服务端 + ENET](#4-技术架构)
5. [通信协议设计](#5-通信协议设计)
6. [分阶段实施计划](#6-分阶段实施计划)
7. [学习路径 — C++ 服务端 + C# 客户端](#7-学习路径)
8. [风险与缓解](#8-风险与缓解)
9. [附录](#9-附录)

---

## 1. 项目概述

### 1.1 项目定位

Peek-A-Boo 是一个 **技术 Demo / 原型项目**，定位为派对类游戏的衍生体验。

- **当前阶段：** 完成 PVP 躲猫猫基础循环——联网、移动、躲藏、寻找、结算
- **后续阶段：** 引入 AI + 自然语言生成道具系统（道具变形 / Prop Hunt 模式）

### 1.2 一句话描述

> 7 人 PVP 第一人称躲猫猫——1 位寻找者，6 位躲藏者，120 秒，每 30 秒全局高亮打破僵局。

### 1.3 电梯演讲

> Peek-A-Boo 将童年经典"躲猫猫"转化为肾上腺素拉满的 PVP 竞技体验。纯第一人称、极简操作、30秒一次的心跳暴露机制。寻找者的脚步声逼近时屏住呼吸，猎物近在咫尺时露出微笑。一局三分钟，重玩无数遍。

### 1.4 愿景

最简单的快乐来自人与人之间的心理博弈。只用移动、躲藏和观察三个动词，创造出让朋友尖叫、欢笑、互相吐槽的时刻。

### 1.5 技术定位

| 组件 | 选型 |
|------|------|
| 客户端 | Unity 6.3 LTS (C#, URP, FPS模板) |
| 服务端 | C++20 独立进程 (WinLibs MinGW GCC 16.1.0 编译, Linux部署) |
| 通信协议 | **ENET-CSharp fork** (UDP + 可靠层) — 可靠有序通道(ch0) + 不可靠通道(ch1) |
| 序列化 | 手写二进制 (memcpy + 固定结构体)，Phase 1 引入代码生成脚本统一双端 |
| 构建系统 | CMake 4.3.2+ (服务端) / Unity默认 (客户端) |
| 服务端部署 | Linux (生产) / Windows本地 (开发测试) |
| ENET 源码 | ENet-CSharp 单文件 (enet.h + enet.c)，18字节 ENetAddress (IPv6) |

---

## 2. 游戏设计

### 2.1 四大核心支柱

| 支柱 | 说明 | 优先级 |
|------|------|--------|
| **极简上手** | 3个手指WASD + 鼠标，30秒内可玩。零教学文本 | 最高 |
| **心理博弈** | 躲藏者的欺骗 vs 寻找者的推理——核心乐趣来源 | 最高 |
| **紧张节奏** | 30秒高亮=心跳，120秒倒计时=呼吸。每30秒一次决策高峰 | 高 |
| **社交欢乐** | 被抓时的搞笑反应、上帝视角观战、"你居然藏在那！" | 高 |

**冲突时优先级：** 心理博弈 > 紧张节奏 > 社交欢乐 > 极简上手

### 2.2 核心循环

```
[准备阶段 10s] → [躲藏阶段 30s] → [搜索阶段 120s] → [结算 15s] → 下一轮
                       │                   │
                  寻找者黑屏          每30s全局高亮
                  躲藏者选位          被抓→观战
```

### 2.3 玩家动词

**寻找者：** 观察(鼠标扫描) → 标记(左键Tag) → 追踪(追击逃逸者) → 读图(高亮记忆)

**躲藏者：** 选位(准备阶段30s) → 潜伏(蹲伏听脚步) → 转移(高亮后换位) → 逃逸(被发现后脱离视线)

### 2.4 情绪曲线

| 角色 | 情绪变化 |
|------|----------|
| **躲藏者** | 匆忙选位 → 毛骨悚然(听到脚步) → 释然(脚步远去) → 恐慌(高亮瞬间) → 笑容/叹息 |
| **寻找者** | 自信开局 → 焦虑上升(找不到) → 警觉(高亮线索) → 兴奋(发现) → 成就感 |
| **观战者** | 上帝视角的信息差快感——"他就在你身后！" |

> 每局结束时，无论输赢，玩家都因为某个具体时刻而想"再来一局"。

---

## 3. 详细规则与系统

### 3.1 角色与人数

| 角色 | 人数 | 视角 | 目标 |
|------|------|------|------|
| 寻找者（Seeker） | 1 | 第一人称 | 在120秒内找到并标记尽可能多的躲藏者 |
| 躲藏者（Hider） | 最多6 | 第一人称 | 在120秒内避免被找到 |
| 观战者（Spectator） | 被淘汰者 | 自由视角 | 观战等待下一轮 |

### 3.2 操作映射（极简方案）

| 按键 | 功能 | 适用角色 |
|------|------|----------|
| W/A/S/D | 前后左右移动 | 全部 |
| Space | 跳跃 | 全部 |
| Ctrl（按住） | 蹲伏（静默移动） | 全部 |
| 鼠标移动 | 视角旋转 | 全部 |
| 鼠标左键 | 标记发现目标 | 仅寻找者 |
| 鼠标滚轮 / 数字键 | 切换观战目标 | 仅观战者 |

> 不引入 E/Q/R/F 功能键。3根手指放WASD，小指够Ctrl，拇指够Space。

### 3.3 回合流程

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌────────────┐
│  准备阶段     │ →  │  躲藏阶段     │ →  │  搜索阶段     │ →  │  结算阶段   │
│  (10 秒)     │    │  (30 秒)     │    │  (120 秒)    │    │  (15 秒)   │
├──────────────┤    ├──────────────┤    ├──────────────┤    ├────────────┤
│ 确认角色分配  │    │ 躲藏者：选位   │    │ 寻找者：搜索   │    │ 展示计分    │
│ 寻找者：黑屏  │    │ 寻找者：黑屏   │    │ 躲藏者：潜伏   │    │ 角色互换    │
│              │    │              │    │ 每30s全局高亮 │    │ 倒计时下轮  │
└──────────────┘    └──────────────┘    └──────────────┘    └────────────┘
```

### 3.4 30秒全局高亮机制

| 参数 | 设定 |
|------|------|
| 触发间隔 | 每30秒 |
| 高亮持续时间 | 0.5 - 1.0 秒 |
| 高亮方式 | 服务端广播所有躲藏者位置 → 客户端显示轮廓/光柱 |
| 可见性 | **仅寻找者收到此消息** |
| 首次触发 | 搜索阶段开始后第30秒 |
| 触发次数 | 第30s、第60s、第90s（共3次有效高亮） |

**MVP实现：** Minimap 红点 + 屏幕边缘方向指示器（纯UI，零Shader复杂度）

### 3.5 发现与抓捕判定

```
寻找者准星对准躲藏者 → 左键点击
       ↓
  客户端发 Raycast → 命中？
       ↓
  发送 TagAttempt(目标玩家ID) → 服务端
       ↓
  服务端验证（距离/视线检查）
       ↓
  ┌────┴────┐
  │ 有效 → 广播 TagResult(success)
  │ 无效 → 广播 TagResult(fail)
  └─────────┘
```

**MVP简化版：** 不验证视线，仅验证距离（< 合理范围即判定成功）。

### 3.6 观战系统

| 功能 | 说明 |
|------|------|
| 存活玩家视角 | 自由切换场上存活玩家的第一人称视角 |
| 上帝视角 | 自由飞行摄像机俯瞰全局 |
| 切换方式 | 鼠标滚轮或数字键切换观战目标 |

**MVP最小方案：** 被抓后固定在寻找者肩膀视角（Phase 2 再切换自由视角）。

### 3.7 计分系统

#### 躲藏者得分

```
最终得分 = 存活基础分 + (存活时间(s) × 2 × 距离系数)

存活基础分：未被抓到 = +100，被抓住 = 0
距离系数（以整局中离寻找者最近距离的50%分位值为准）：
  极近（<2米）   → ×2.0
  近距（2-5米）  → ×1.5
  中距（5-10米） → ×1.0
  远距（>10米）  → ×0.5
```

#### 寻找者得分

```
最终得分 = 抓捕基础分累计 + 衰减奖励 + 全捕获奖励 + 时间奖励

每次抓捕基础分：+150/人
衰减奖励：第1个 +100 → 第2个 +70 → 第3个 +50 → 第4个 +35 → 第5个 +25 → 第6个 +15
全捕获奖励：+200（6人全抓到）
时间奖励：剩余秒数 × 1
```

> **MVP简化版（Phase 0-1）：** 只显示"Seeker抓到 X/6 人"，不做详细计分。

### 3.8 结算面板 UI 布局

```
┌────────────────────────────────────┐
│           🏆 第 X 轮 结算            │
├────────────────────────────────────┤
│   寻找者：PlayerA                   │
│   ┌──────────────────────────┐    │
│   │ 抓捕数：4/6               │    │
│   │ 基础分：4×150 = 600       │    │
│   │ 衰减奖励：100+70+50+35    │    │
│   │ 全捕获奖励：0（未完成）    │    │
│   │ 时间奖励：34秒×1 = 34     │    │
│   │ ─────────────────────     │    │
│   │ 合计：889                 │    │
│   └──────────────────────────┘    │
│                                    │
│   躲藏者排名：                       │
│   1. PlayerC - 存活120s 距离2m ×2.0│
│      存活分100 + 240×2.0 = 580     │
│   2. PlayerD - 存活89s 距离3m ×1.5 │
│      ...                           │
└────────────────────────────────────┘
```

---

## 4. 技术架构 — C++ 服务端 + ENET

### 4.1 架构总览

```
┌─────────────────────────────────────────────────────┐
│                 C++20 Dedicated Server                │
│                  (Linux / Windows)                     │
│                                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │GameMode   │  │Highlight  │  │ScoreManager      │   │
│  │StateMachine│  │System    │  │                   │   │
│  └──────────┘  └──────────┘  └──────────────────┘   │
│  ┌──────────────────────────────────────────────┐    │
│  │              Room Manager                      │    │
│  │  ┌─────────┐ ┌──────────┐ ┌───────────────┐  │    │
│  │  │Player   │ │Position  │ │Tag Validator  │  │    │
│  │  │Manager  │ │Broadcast │ │               │  │    │
│  │  └─────────┘ └──────────┘ └───────────────┘  │    │
│  └──────────────────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────┐    │
│  │         ENET Host (libenet)                    │    │
│  │         UDP port: 9000                         │    │
│  │         Channels: 0=Reliable 1=Unreliable     │    │
│  └──────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
                          │
                  ENET UDP :9000
              (reliable + unreliable channels)
                          │
     ┌────────────────────┼───────────────────┐
     ▼                    ▼                    ▼
┌──────────┐       ┌──────────┐         ┌──────────┐
│ Unity    │       │ Unity    │   ...   │ Unity    │
│ Client 1 │       │ Client 2 │         │ Client 7 │
│ (Seeker) │       │ (Hider)  │         │ (Hider)  │
│ ENet-CS  │       │ ENet-CS  │         │ ENet-CS  │
└──────────┘       └──────────┘         └──────────┘
```

### 4.2 核心设计原则

| # | 原则 | 说明 |
|---|------|------|
| 1 | **Server绝对权威** | 所有判定（Tag、位置验证、得分计算）只在服务端执行 |
| 2 | **客户端只发输入** | 客户端发送键盘/鼠标输入，不自己判定任何逻辑 |
| 3 | **服务端广播状态** | 服务端以固定频率（20Hz）向所有客户端广播世界状态 |
| 4 | **二进制协议** | 最小化带宽，不用JSON传输游戏状态 |
| 5 | **单线程事件循环** | 一个服务端实例 = 一个游戏房间，7人规模无需多线程 |
| 6 | **客户端预测** | 客户端本地立即响应移动输入，服务端位置为最终权威 |
| 7 | **双通道传输** | ENET ch0=可靠有序(状态变更/事件)，ch1=不可靠(位置快照，超时即丢弃) |

### 4.3 技术选型

#### 服务端 (C++20)

| 组件 | 选型 | 原因 |
|------|------|------|
| 语言标准 | **C++20** | `co_await`协程、`std::span`零拷贝视图、`std::jthread`、`std::format`、designated initializers |
| 网络库 | **libenet** (ENET C library) v1.3.x | 超轻量(~3 .c文件)、内置可靠/不可靠通道、UDP NAT穿透友好、MIT协议 |
| 构建系统 | **CMake 3.20+** | 跨平台标准，MinGW友好 |
| 编译器 | **MinGW-W64 (GCC 14+)** | Windows原生编译，无需WSL |
| JSON解析 | **nlohmann/json** (header-only) | 仅用于读取服务端配置文件 |
| 序列化 | 手写二进制（memcpy + 固定结构体）→ Phase 1 换 FlatBuffers | 零依赖，最小延迟 |
| 并发 | 单线程 `enet_host_service()` 事件循环 | ENET内置事件驱动，7人规模足够 |

**为什么是 ENET（而非 WebSocket/TCP）：**
- **无队头阻塞** — 丢一个位置包不会阻塞后续包（TCP致命问题：丢包→所有后续包等待重传）
- **双通道设计** — 状态变更走可靠ch0（Tag、GameState），位置快照走不可靠ch1（过时位置自动丢弃）
- **NAT穿透** — UDP打洞天然优于TCP，适合局域网直连
- **超轻量** — 仅 `enet.c` + `protocol.c` + `host.c` + `peer.c` 几个源文件，无外部依赖
- **稳定性** — Cube World、Minecraft Classic 等商业游戏验证，维护15年+

#### 客户端 (Unity C#)

| 组件 | 选型 | 原因 |
|------|------|------|
| ENET 封装 | **ENet-CSharp** (GitHub: nxrighthere) | MIT开源，提供预编译 `enet.dll`(Windows)，原生 `[DllImport]` P/Invoke |
| 序列化 | `BinaryWriter` / `BinaryReader` / `BitConverter` | C#内置，零额外依赖 |
| 输入系统 | Unity Input System（FPS模板已配置） | 无需改动 |
| 渲染 | URP + FPS模板 | 无需改动 |

### 4.4 服务端项目结构

```
server/                          ← 服务端项目根目录
├── CMakeLists.txt               ← CMake 构建定义（直接编译 enet.c，无需 add_subdirectory）
├── config/
│   └── server.json              ← 运行时配置（端口、tick_rate、max_players）
├── deps/
│   └── enet/                    ← ENet-CSharp 单文件源码（覆盖官方 libenet）
│       ├── enet.h               ← 全部 ENET 实现（18字节 ENetAddress, IPv6）
│       ├── enet.c               ← 24行入口：#define ENET_IMPLEMENTATION + #include "enet.h"
│       └── include/             ← 官方 libenet 残留（不再使用）
├── src/
│   ├── main.cpp                 ← 入口：enet_initialize → enet_host_create(6参数) → 事件循环
│   ├── protocol.h / .cpp        ← 协议：消息类型定义 + 序列化/反序列化
│   ├── types.h                  ← 全局枚举和常量（与 ClientProtocol.cs 镜像）
│   ├── room.h / .cpp            ← [Phase 1] Room 类：单局游戏房间
│   ├── player.h / .cpp          ← [Phase 1] Player 数据结构
│   └── gamemode.h / .cpp        ← [Phase 1] GameMode 状态机
├── build/
│   └── server.exe               ← 编译产物（gitignore）
└── README.md                    ← 编译和运行说明
```

> **实际差异：** ENet-CSharp fork 使用 18 字节 ENetAddress（IPv6 支持），与官方 libenet 的 6 字节不兼容。服务端必须编译 ENet-CSharp 源码（而非官方 libenet）才能与 Unity 客户端互通。`deps/enet/enet.h` 已用 ENet-CSharp 版本覆盖。

### 4.5 服务端数据流

```
1. main.cpp 启动 → 读取 config/server.json
2. enet_initialize() → enet_host_create(&address, MAX_PLAYERS, ENET_CHANNELS, 0, 0, 0)
   参数: address(ENET_HOST_ANY via ipv6), max_peers=7, channels=2, bw_in=0, bw_out=0, bufferSize=0(默认256KB)
   注意: ENet-CSharp fork 使用 address.ipv6 = ENET_HOST_ANY (非 address.host)
         第6参数 bufferSize (官方 libenet 无此参数)
3. 事件循环 (enet_host_service, 50ms超时 / 20Hz)：
   while (true) {
       enet_host_service(host, &event, 50);
       switch (event.type) {
           case ENET_EVENT_TYPE_CONNECT:     → 创建 Player，发 Welcome(ch0)
           case ENET_EVENT_TYPE_RECEIVE:     → 反序列化 → 路由处理
           case ENET_EVENT_TYPE_DISCONNECT:  → 移除 Player，通知 Room
       }
       // 每50ms执行一次 Tick
       if (elapsed >= 50ms) {
           ProcessInputs();
           GameMode::Update(deltaTime);
           BroadcastPlayerStates();  // ch1 不可靠
       }
   }
4. GameMode 状态机驱动回合流转
5. Highlight: 每30秒广播Hider位置（ch0 可靠，仅发给Seeker）
6. TagAttempt: Seeker发起 → 验证距离 → 广播 TagResult（ch0 可靠）
7. 结算: 计算得分 → 广播ScoreBoard（ch0 可靠） → 进入下一轮
```

### 4.6 GameMode 状态机（不变）

```
状态枚举:
  WaitingForPlayers    → 等待足够玩家加入（可配置最少人数）
  Preparing            → 10秒准备，分配角色
  Hiding               → 30秒躲藏阶段，Seeker不可见
  Seeking              → 120秒搜索阶段，每30秒触发Highlight
  RoundEnd             → 15秒结算展示
  GameOver             → 所有轮次结束

转换:
  WaitingForPlayers ──(达到最少人数 && 所有人Ready)──→ Preparing
  Preparing ──(10s Timer)──→ Hiding
  Hiding ──(30s Timer)──→ Seeking
  Seeking ──(120s Timer / 全部被抓)──→ RoundEnd
  RoundEnd ──(15s Timer)──→ Preparing (下一轮) / GameOver (最终轮)
```

### 4.7 ENET 双通道分配策略

| 通道 | 类型 | 标志 | 用途 | 示例消息 |
|------|------|------|------|----------|
| **ch0** | 可靠有序 | `ENET_PACKET_FLAG_RELIABLE` | 状态变更、事件、连接握手 | Welcome, GameStateChange, TagResult, Highlight, ScoreBoard, JoinRoom |
| **ch1** | 不可靠 | `ENET_PACKET_FLAG_UNSEQUENCED` | 高频位置/输入快照 | PlayerInput(上行), PlayerStates(下行) |

> ch1 不可靠包超时即丢弃——新的位置到了旧的就没意义了，不阻塞后续数据。这是 ENET 相比 TCP/WS 的核心优势。ch0 保证 Tag 判定、状态切换等事件绝不丢失。

---

## 5. 通信协议设计

### 5.1 消息帧格式（二进制）

ENET 包自带长度——不需要自定长度的 payload_len。帧头压缩到 **1 字节**：

```
┌──────────────────┬──────────────────────────────┐
│   Message Type   │          Payload              │
│     (1 byte)     │     (N bytes, binary)        │
└──────────────────┴──────────────────────────────┘
```

| 字段 | 大小 | 说明 |
|------|------|------|
| `msg_type` | 1 byte | 消息类型（见5.2/5.3），决定 payload 格式 |
| `payload` | N bytes | 由 `event.packet->dataLength - 1` 确定，无需手动传长度 |

> ENET 每个包自带 dataLength，payload 长度 = dataLength - 1。7人×20Hz，ch1 不可靠包每条 <50 字节。总带宽 < 8KB/s/客户端。

### 5.2 客户端 → 服务端消息

| ID | 名称 | Payload 结构 | 通道 | 说明 |
|----|------|-------------|------|------|
| `0x01` | **JoinRoom** | `string player_name` (1字节长度前缀 + UTF8) | ch0 可靠 | 玩家加入房间 |
| `0x02` | **PlayerInput** | `float moveX, float moveZ, float rotY` + `byte flags` (13 bytes) | ch1 不可靠 | 每帧发送输入意图 |
| `0x03` | **TagAttempt** | `uint8 target_player_id` | ch0 可靠 | 寻找者捕获尝试 |
| `0x04` | **PlayerReady** | 无 payload | ch0 可靠 | 玩家确认就绪 |

> `0x05 Ping` 移除——ENET 内置 RTT 测量 (`peer->roundTripTime`)，无需应用层心跳。连接断开由 `ENET_EVENT_TYPE_DISCONNECT` 事件自动通知。

### 5.3 服务端 → 客户端消息

| ID | 名称 | Payload 结构 | 通道 | 说明 |
|----|------|-------------|------|------|
| `0x10` | **Welcome** | `uint8 player_id` + `uint8 role` (0=Seeker,1=Hider,2=Spectator) | ch0 可靠 | 连接确认 + 角色分配 |
| `0x11` | **GameStateChange** | `uint8 new_state` + `uint16 countdown_seconds` | ch0 可靠 | 游戏状态变更 + 倒计时 |
| `0x12` | **PlayerStates** | `uint8 count` + 每玩家 `[uint8 id, float x,y,z, float rotY, uint8 state]` (1+7×22=155 bytes) | **ch1 不可靠** | 20Hz 全量位置同步 |
| `0x13` | **Highlight** | `uint8 count` + 每躲藏者 `[uint8 id, float x, float z]` | ch0 可靠 | 30秒高亮，仅发给Seeker |
| `0x14` | **TagResult** | `uint8 seeker_id` + `uint8 target_id` + `uint8 success` | ch0 可靠 | 捕获结果 |
| `0x15` | **ScoreBoard** | 字符串（JSON，仅结算时发一次） | ch0 可靠 | 结算面板数据 |
| `0x16` | **Error** | `uint8 code` + `string message` | ch0 可靠 | 错误通知 |

> `0x17 Pong` 移除——ENET 内置 RTT，无需应用层心跳回复。

### 5.4 关键消息详解

#### PlayerInput (0x02) — 客户端每帧发送 (ch1 不可靠)

```
Offset  Size  Field
0       4     float moveX       // 移动方向X (-1.0 ~ 1.0)
4       4     float moveZ       // 移动方向Z (-1.0 ~ 1.0)
8       4     float rotY        // 水平视角旋转 (0 ~ 360, 欧拉角Y)
12      1     byte  flags       // bit0: crouching, bit1: jumping
─────────────────
Total: 13 bytes
```

> 客户端不发送绝对位置，只发送输入意图。服务端根据输入+deltaTime计算最终位置。杜绝位置作弊。

#### PlayerStates (0x12) — 服务端每50ms广播 (ch1 不可靠)

```
Offset  Size  Field
0       1     uint8 player_count
1       N     每个玩家:
  1       uint8  player_id
  5       float  pos_x           // 权威位置
  9       float  pos_y
  13      float  pos_z
  17      float  rot_y           // 朝向
  21      uint8  state           // 0=normal, 1=crouching, 2=caught, 3=spectating
  ─────────────────
  Per player: 22 bytes
  Total (7 players): 1 + 7×22 = 155 bytes
```

> ch1 不可靠通道——丢包不重传，新位置到了旧位置自然被替换。这是 ENET 相比 TCP 的核心优势：位置同步不被重传阻塞。

### 5.5 ENET 连接生命周期

```
Client (Unity + ENet-CSharp)              Server (C++ + libenet)
  │                                              │
  │── enet_host_connect(ip, 9000, 2) ──────────→│  UDP 连接请求
  │←── ENET_EVENT_TYPE_CONNECT ─────────────────│  (双方触发)
  │── Send(Welcome(ch0 可靠)) ─────────────────→│
  │←── Recv(Welcome(player_id, role)) ──────────│  连接确认
  │── Send(JoinRoom(ch0 可靠)) ────────────────→│
  │                                              │
  │── Send(PlayerInput(ch1 不可靠, 每帧)) ─────→│  输入流（不阻塞）
  │←── Send(PlayerStates(ch1 不可靠, 20Hz)) ────│  状态广播（不阻塞）
  │                                              │
  │── Send(TagAttempt(ch0 可靠)) ───────────────→│  事件（保证到达）
  │←── Send(TagResult(ch0 可靠)) ───────────────│
  │                                              │
  │── enet_peer_disconnect() ───────────────────→│  断开
  │←── ENET_EVENT_TYPE_DISCONNECT ──────────────│  清理 Player
```

### 5.6 序列化/反序列化代码模式

#### C++ 服务端序列化（protocol.cpp，基于 ENET）

```cpp
// 创建并发送 PlayerStates 消息 (ch1 不可靠)
void broadcast_player_states(ENetHost* host, const std::vector<Player>& players) {
    // 1 byte msg_type + 1 byte count + 7 * 22 bytes = 156 bytes
    uint8_t buf[256];
    int offset = 0;
    buf[offset++] = 0x12;  // msg_type: PlayerStates
    buf[offset++] = static_cast<uint8_t>(players.size());

    for (const auto& p : players) {
        buf[offset++] = p.id;
        memcpy(buf + offset, &p.pos_x, 4); offset += 4;
        memcpy(buf + offset, &p.pos_y, 4); offset += 4;
        memcpy(buf + offset, &p.pos_z, 4); offset += 4;
        memcpy(buf + offset, &p.rot_y, 4); offset += 4;
        buf[offset++] = p.state;
    }

    // ch1 = 不可靠，新位置替代旧位置
    ENetPacket* packet = enet_packet_create(buf, offset, ENET_PACKET_FLAG_UNSEQUENCED);
    enet_host_broadcast(host, 1, packet);  // channel 1
    // enet_host_broadcast 自动管理 packet 生命周期，无需手动 destroy
}

// 发送 TagResult (ch0 可靠)
void send_tag_result(ENetPeer* peer, uint8_t seeker_id, uint8_t target_id, bool success) {
    uint8_t buf[4];
    buf[0] = 0x14;  // msg_type: TagResult
    buf[1] = seeker_id;
    buf[2] = target_id;
    buf[3] = success ? 1 : 0;

    ENetPacket* packet = enet_packet_create(buf, 4, ENET_PACKET_FLAG_RELIABLE);
    enet_peer_send(peer, 0, packet);  // channel 0
}
```

#### C# 客户端反序列化（基于 ENet-CSharp）

```csharp
// 收到 ENET 包后的分发处理
void HandlePacket(byte[] data) {
    byte msgType = data[0];
    byte[] payload = data[1..];  // C# 8.0 range syntax (Unity 6.3 支持)

    switch (msgType) {
        case 0x10: HandleWelcome(payload);       break;
        case 0x11: HandleGameStateChange(payload); break;
        case 0x12: HandlePlayerStates(payload);  break;
        case 0x13: HandleHighlight(payload);     break;
        case 0x14: HandleTagResult(payload);     break;
        case 0x15: HandleScoreBoard(payload);    break;
    }
}

void HandlePlayerStates(byte[] payload) {
    int offset = 0;
    byte count = payload[offset++];
    for (int i = 0; i < count; i++) {
        byte id = payload[offset++];
        float px = BitConverter.ToSingle(payload, offset); offset += 4;
        float py = BitConverter.ToSingle(payload, offset); offset += 4;
        float pz = BitConverter.ToSingle(payload, offset); offset += 4;
        float ry = BitConverter.ToSingle(payload, offset); offset += 4;
        byte state = payload[offset++];

        if (id != myPlayerId) {
            otherPlayers[id].transform.position = new Vector3(px, py, pz);
            otherPlayers[id].transform.rotation = Quaternion.Euler(0, ry, 0);
        }
    }
}
```

---

## 6. 分阶段实施计划

### 6.1 Phase 0：服务端 Hello World + 客户端连接 ✅ 已完成（2026-05-05）

**目标：** C++ 服务端（ENET Host）跑起来，两个 Unity 客户端实例连上并收发消息。

#### 完成总结

| 项目 | 状态 |
|------|------|
| 服务端编译运行 | ✅ `server/build/server.exe` 监听 UDP :9000 |
| Unity 客户端连接 | ✅ NetworkManager.cs 连接成功 |
| Welcome → JoinRoom 流程 | ✅ 客户端收到 Welcome，自动发送 JoinRoom |
| 双开验证 | ⬜ 待测试 |
| ENET 源码统一 | ✅ ENet-CSharp 单文件源码供服务端和客户端使用 |

#### 实际技术决策（与 BluePrint v3.0 原始方案的差异）

| 原计划 | 实际 | 原因 |
|--------|------|------|
| MSYS2 安装工具链 | **WinLibs 独立 MinGW GCC 16.1.0** | 用户拒绝 MSYS2，手动安装 |
| 官方 libenet（6字节 ENetAddress） | **ENet-CSharp fork（18字节 ENetAddress）** | 必须与 Unity ENet-CSharp 匹配，否则连接超时 |
| `add_subdirectory(deps/enet)` | **直接编译 deps/enet/enet.c** | ENet-CSharp 是单文件库，无 CMakeLists.txt |
| `enet_host_create(5参数)` | **6参数（含 bufferSize）** | ENet-CSharp fork API 差异 |
| `address.host = ENET_HOST_ANY` | **`address.ipv6 = ENET_HOST_ANY`** | ENet-CSharp ENetAddress 使用 union（无 host 成员） |
| `#include <enet/enet.h>` | **`#include "enet.h"`** | 单文件结构，无 include/ 子目录 |
| 官方 libenet 多文件（9个.c） | **ENet-CSharp 单文件（enet.h 包含所有实现）** | fork 将所有代码合并到一个头文件 |

#### 关键修复记录

1. **clock_gettime 冲突** — ENet-CSharp enet.h 与 WinLibs UCRT 重复定义，添加 `#if !defined(_UCRT)` 守卫
2. **CS0019 Peer null 比较** — `Peer` 是 struct，改用 `peer.IsSet`
3. **CS1061 SetPort 不存在** — `Address.Port` 是属性赋值而非方法
4. **连接超时** — 根因：官方 libenet 与 ENet-CSharp ENetAddress 布局不兼容（6字节 vs 18字节）

#### 当前代码文件

| 文件 | 行数 | 说明 |
|------|------|------|
| `server/CMakeLists.txt` | 20 | 直接编译 enet.c，启用 C 和 CXX |
| `server/src/main.cpp` | 111 | ENET 事件循环，连接/消息/断开分发 |
| `server/src/protocol.h` | 17 | send_welcome, send_game_state_change |
| `server/src/protocol.cpp` | ~30 | 手写二进制序列化 |
| `server/src/types.h` | 20 | 枚举和常量定义 |
| `Assets/Scripts/NetworkManager.cs` | 175 | Unity ENET 生命周期，消息分发 |
| `Assets/Scripts/ClientProtocol.cs` | 101 | 镜像 types.h，序列化/反序列化 |
| `Skills/` | 11 files | 脚本技能注册 + Phase 1 计划脚本定义 |

### 6.2 Phase 1：核心循环 — 输入同步 + 基础移动（预计 5-7 天）

**目标：** 两个客户端通过服务端完成"移动→输入→同步→广播"完整数据流。

#### Phase 1 任务分解（按依赖顺序）

```
Phase 1 任务树（依赖从左到右）：
│
├── 1.0 服务端 Player 数据结构 ──────────────────────────────────────────┐
│   ├── 1.0.1 types.h: 定义 Player 结构体 (id:u8, pos[3]:f32, yaw:f32)   │
│   ├── 1.0.2 main.cpp: Player 数组/字典，on_connect 分配 slot             │
│   └── 1.0.3 编译验证，确保结构体 sizeof 符合预期                           │
│                                                                         │
├── 1.1 输入消息管道（关键路径）← 依赖 1.0 ───────────────────────────────┤
│   ├── 1.1.1 ClientProtocol.cs: SerializePlayerInput 验证（已有，需测试）  │
│   ├── 1.1.2 protocol.cpp: read_player_input 反序列化 (ch1 非可靠)        │
│   ├── 1.1.3 main.cpp: on_receive dispatch PlayerInput → 输入队列         │
│   └── 1.1.4 环缓冲区实现 (ring_buffer.h, 避免每帧分配内存)              │
│                                                                         │
├── 1.2 游戏 Tick 循环 ← 依赖 1.1 ──────────────────────────────────────┤
│   ├── 1.2.1 main.cpp: 固定频率 game_loop (30Hz → 33ms fixed timestep)  │
│   ├── 1.2.2 每 tick: 消费输入队列 → 更新 Player 位置 (速度*dt)           │
│   └── 1.2.3 [暂缓] 碰撞检测 (MVP 跳过，Phase 3)                          │
│                                                                         │
├── 1.3 状态广播 ← 依赖 1.2 ───────────────────────────────────────────┤
│   ├── 1.3.1 protocol.cpp: serialize_player_states (全量 pos+yaw)        │
│   ├── 1.3.2 channel 1 不可靠, 20Hz 限频广播 (非每 tick)                  │
│   └── 1.3.3 ClientProtocol.cs: DeserializePlayerStates → 待实现          │
│                                                                         │
└── 1.4 Unity 客户端表现层 ← 依赖 1.3 ─────────────────────────────────┤
    ├── 1.4.1 PlayerController.cs: 本地输入采集 + 本地立即移动               │
    ├── 1.4.2 远程玩家 GameObject 池化管理                                  │
    ├── 1.4.3 远程玩家位置插值 (Lerp, 避免抖动)                              │
    └── 1.4.4 [暂缓] 客户端预测 + 服务端和解 (Phase 3)                       │
```

#### 客户端新增脚本

| 脚本 | 路径 | 说明 | Skills 注册 |
|------|------|------|-------------|
| **GameManager** | `Assets/Scripts/GameManager.cs` | 客户端状态机中枢，协调所有子系统 | `Skills/Core/game-manager.md` |
| **PlayerController** | `Assets/Scripts/PlayerController.cs` | 本地玩家输入 + 移动 | `Skills/Gameplay/player-controller.md` |
| **LevelTimer** | `Assets/Scripts/LevelTimer.cs` | 倒计时显示（服务器权威） | `Skills/Core/level-timer.md` |
| **MenuController** | `Assets/Scripts/MenuController.cs` | 主菜单 + 大厅 + HUD | `Skills/UI/menu-controller.md` |
| **ProceduralMapGenerator** | `Assets/Scripts/ProceduralMapGenerator.cs` | 生成测试地图（地板+墙壁+障碍物） | `Skills/World/procedural-map-generator.md` |

#### 服务端新增文件

| 文件 | 说明 |
|------|------|
| `server/src/player.h/cpp` | Player 结构体 + 管理方法 |
| `server/src/room.h/cpp` | Room 类：管理一局游戏 |
| `server/src/ring_buffer.h` | 无锁环形缓冲区（输入队列） |
| `server/src/tick.h` | 固定频率 tick 循环封装 |

#### 客户端脚本挂载计划

```
场景层级:
  GameManager (Singleton)
  ├── NetworkManager (已存在)
  ├── LevelTimer
  │   └── TimerText (TextMeshPro)
  ├── MenuController
  │   ├── MainMenuPanel
  │   │   ├── PlayerNameInput
  │   │   ├── ServerIPInput
  │   │   └── ConnectButton
  │   └── HUDPanel
  │       ├── RoleLabel
  │       ├── StateLabel
  │       └── TimerLabel
  ├── Player (Local)
  │   └── PlayerController
  └── ProceduralMapGenerator
      ├── Ground (Plane)
      ├── Walls (4x Cube)
      └── Obstacles (N× Cube/Cylinder)
```

#### Phase 1 验收标准

- [ ] 客户端 A 移动 → 服务端接收 PlayerInput → 更新位置 → 广播 PlayerStates
- [ ] 客户端 B 看到客户端 A 的移动（远程玩家位置更新）
- [ ] 移动同步无明显抖动（< 50ms 延迟在局域网内）
- [ ] 服务端 30Hz tick 稳定运行
- [ ] 断线重连：客户端断开后服务端清理该玩家，其他客户端收到更新
- [ ] 程序化地图在客户端生成，两个客户端看到一致的地图布局

#### 用到的知识

- C++：`std::vector`/`std::array`、`memcpy` 二进制写入、固定时间步长 `std::chrono`、环形缓冲区
- C#：`Transform` 操控、`Vector3.Lerp` 插值、Input System、uGUI Canvas/Text

### 6.3 Phase 2：完整人数 + 高亮 + 观战 + 计分（2-3天）

**新增脚本：**

| 脚本 | 路径 | Skills 注册 |
|------|------|-------------|
| **HighlightSystem** | `Assets/Scripts/HighlightSystem.cs` | `Skills/Gameplay/highlight-system.md` |
| **TagSystem** | `Assets/Scripts/TagSystem.cs` | `Skills/Gameplay/tag-system.md` |
| **ScoreManager** | `Assets/Scripts/ScoreManager.cs` | `Skills/Core/score-manager.md` |

```
具体步骤：
1. 扩展至7人：
   - 服务端：管理最多7个Player
   - 客户端：支持任意角色

2. 30秒高亮实现 (HighlightSystem.cs)：
   服务端：
   - Seeking状态中，每30秒触发
   - 收集所有alive Hider位置
   - 仅向Seeker发送 Highlight 消息
   
   客户端 (Seeker)：
   - 收到Highlight → Minimap显示红点
   - 屏幕边缘方向箭头（纯UI）
   - 持续1秒后消失

3. 观战系统：
   服务端：
   - 标记被抓Player为 Spectator
   - 不再广播被抓者的位置
   
   客户端 (被抓者)：
   - 禁用 PlayerController
   - 激活固定跟随摄像机（附着在Seeker肩膀上）
   - 可切上帝视角（自由飞行摄像机）

4. 基础计分 (ScoreManager.cs)：
   - 服务端在RoundEnd时计算得分
   - 广播 ScoreBoard（JSON字符串，仅一次）
   - 客户端解析并展示结算面板
```

**验收标准：**
- 3-7 人完整联机，每30秒高亮正确触发
- 被淘汰者进入观战，不退出游戏
- 结算面板显示得分

### 6.4 Phase 3：打磨 + 部署（持续）

```
1. 正式地图（Low-Poly三层室内）
2. 完整计分（距离系数、衰减奖励）
3. 3回合制 + 角色互换
4. 音效系统
5. Linux 部署：
   - 在 WSL2 或 Linux 机器上 git clone
   - cmake -B build && cmake --build build
   - ./server (配置 systemd 守护进程)
   - 配置防火墙开放 9000 端口
6. 压力测试：7人持续游戏
```

---

## 7. 学习路径 — C++20 服务端 + C# 客户端

### 7.1 学习原则

- **按需学、立即用** — 每学一个概念立刻在项目中使用
- **先跑起来再优化** — Phase 0 只需要 C++ 基础 + ENET Host/Peer 概念
- **两个语言分工明确** — C++ 负责网络和逻辑，C# 负责渲染和输入

### 7.2 C++ 学习路径（服务端，按顺序）

| 顺序 | 概念 | 用途 | 预计时间 |
|------|------|------|----------|
| 1 | C++ 基础语法（变量、函数、循环、if） | 写 main.cpp | 2-3小时 |
| 2 | `std::vector` / `std::string` / `std::span` (C++20) | 数据容器，零拷贝视图 | 1小时 |
| 3 | `struct` + `enum class` | Player结构体，状态枚举 | 1小时 |
| 4 | CMake：`add_executable` / `target_link_libraries` / `add_subdirectory` | 项目构建 | 2小时 |
| 5 | ENET API：`enet_host_create` / `enet_host_service` / `enet_peer_send` / `enet_packet_create` | UDP网络服务 | 3-4小时 |
| 6 | 二进制序列化：`memcpy` + 字节序 + 固定结构体 | 协议编解码 | 2小时 |
| 7 | `std::chrono`（`steady_clock` / `duration`） | Tick定时器、倒计时 | 1小时 |
| 8 | 头文件 (.h) + 实现文件 (.cpp) 分离 | 代码组织 | 1小时 |
| 9 | `std::find_if` + Lambda 表达式 | 查找玩家 | 1小时 |
| 10 | `std::format` (C++20) | 日志/字符串格式化 | 0.5小时 |

**暂不学习：**
- ❌ 模板（template）元编程
- ❌ 智能指针（`unique_ptr` / `shared_ptr`）— MVP阶段直接在vector中存对象
- ❌ 异常处理（try/catch）— 用返回值+日志
- ❌ `std::jthread` / `co_await` — Phase 0 单线程事件循环足够，需要时再学
- ❌ Boost库 — libenet + STL 足够

### 7.3 C# 学习路径（客户端，按顺序）

| 顺序 | 概念 | 用途 | 预计时间 |
|------|------|------|----------|
| 1 | `MonoBehaviour` 生命周期（Awake/Start/Update/OnDestroy） | 脚本骨架 | 1-2小时 |
| 2 | ENet-CSharp：`Host.Create` / `Connect` / `Service` / `Packet.GetData` / `Peer.Send` | ENET通信 | 2小时 |
| 3 | `BinaryWriter` / `BinaryWriter` / `BitConverter` | 协议编解码 | 1小时 |
| 4 | FPS PlayerController 结构理解（读模板代码） | 移动输入接入 | 2-3小时 |
| 5 | `Transform.position` / `Transform.rotation` 操控 | 同步其他玩家位置 | 1小时 |
| 6 | `Physics.Raycast` + `RaycastHit` | Tag判定 | 1小时 |
| 7 | UI Canvas + Text 基本操作 | 倒计时/状态显示 | 1小时 |

### 7.4 编译指南（WinLibs 独立 MinGW）

```
# 工具链（已安装）：
# - WinLibs MinGW GCC 16.1.0 (独立安装，非 MSYS2)
# - CMake 4.3.2 (独立安装)
# - 两者均已在系统 PATH 中

# 编译服务端：
cd /e/U3d/Pros/Peek-A-Boo/server
cmake -B build -G "MinGW Makefiles"
cmake --build build

# 运行：
./build/server.exe
# 输出：=== Peek-A-Boo Server v3.0 (ENET) ===
#       Server listening on UDP port 9000 (max 7 players, 2 channels)
```

> **注意：** 不要使用 MSYS2 shell——项目使用独立 MinGW + CMake，在 Windows 终端或 Git Bash 中直接运行即可。服务端二进制为 `server.exe`（360KB），无需额外运行时依赖。

### 7.5 Linux 部署注意事项

开发时在 Windows 用 MinGW 编译 Windows 版本测试。
部署到 Linux 时，**建议直接在 Linux 上编译**（libenet 原生支持 Linux）：

```bash
# 在 Linux 服务器上
git clone <repo>
cd server
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build
./build/server
```

如果坚持在 Windows 交叉编译到 Linux，需要安装 MinGW-W64 交叉编译器并配置 CMake toolchain 文件，复杂度较高。**不推荐 MVP 阶段这么做。**

---

## 8. 风险与缓解

### 8.1 五大杀手

| # | 风险 | 严重度 | 缓解策略 |
|---|------|--------|----------|
| 1 | **C++学习曲线** | 🔴 致命 | 只学本蓝图列出的10个概念，不系统学C++。从 Phase 0 最简单版本开始 |
| 2 | **双代码库协议不一致** | 🔴 致命 | 先写 test_client.c 验证服务端。协议定义在 protocol.h 中，C#端对照实现。Phase 1 引入 FlatBuffers 统一 schema |
| 3 | **找不到人联机测试** | 🔴 致命 | PVP游戏自测不了。找到至少一个固定测试伙伴是硬性要求 |
| 4 | **CMake/MinGW环境配置耗时** | 🟡 高 | 按7.4指南操作。MSYS2一步到位。不要手动下载配置GCC |
| 5 | **花时间做地图而非写代码** | 🟡 高 | Phase 0-2只用灰盒（Cube/Capsule搭场景）。Phase 3才做正式地图 |

### 8.2 与旧方案（NGO）对比

| 风险 | 旧方案 | 新方案 | 变化 |
|------|--------|--------|------|
| 学习曲线 | C# NGO | C++ + C# ENET | ↑ 增加 |
| 架构复杂度 | 单仓库 | 双仓库 | ↑ 增加 |
| 部署灵活度 | Windows Only | Linux + Windows | ↑ 改善 |
| 扩展性 | Host模式受限 | 独立服务端可扩展 | ↑ 改善 |
| UDP性能 | N/A | ENET双通道 | ↑ 改善 |
| 一周可玩性 | 较可能 | 需要更多时间 | ↓ 降低 |

### 8.3 一周目标

> C++ 服务端在本地跑起来 + Unity 客户端连上 + 收到 Welcome 消息 + 第二个客户端（双开）同时连接验证。

**达到此点即为 Phase 0 成功。**

---

## 9. 附录

### 9.1 决策记录

| 决策 | 结论 | 原因 |
|------|------|------|
| 服务端语言 | C++20 | 独立进程、Linux部署、专业架构 |
| 通信协议 v2→v3 | WebSocket(TCP) → **ENET(UDP)** | Party Mode共识：TCP队头阻塞对游戏致命 |
| ENET 源码 | **ENet-CSharp fork**（非官方 libenet） | 与 Unity 客户端 ENetAddress 布局兼容（18字节 IPv6） |
| C++标准 | C++20 | GCC 16.1.0完全支持 |
| 工具链 | **WinLibs 独立 MinGW**（非 MSYS2） | 用户偏好：手动安装，无需 MSYS2 shell |
| 构建 | CMake 4.3.2 + MinGW Makefiles | 跨平台标准 |
| 序列化 | 手写二进制，Phase 1 引入代码生成脚本 | 零依赖起步，统一双端 |
| 序列化 v4→v5 | 1字节帧头 (无 payload_len) | ENET 包自带长度 |
| 协议双端一致性 | Phase 1 引入 `protocol_defs.h` + Python 代码生成 | 编译期强制，杜绝运行时漂移 |
| 道具系统（MVP） | 砍掉 | Phase 2+ 用AI实现 |
| 第三人称 | 不实现 | 100% FPP |
| MVP地图 | 程序化生成（空物体+平面） | 零资产导入 |

### 9.2 待决问题

| 问题 | 当前倾向 | 何时决定 |
|------|----------|----------|
| 高亮最终方案 | Minimap红点 → 墙透Shader | Phase 1验证手感后 |
| 观战交互细节 | 固定Seeker肩后 → 自由切换 | Phase 2 |
| 计分是否对局中显示 | 对局中隐藏，结算展示 | Phase 3 |
| 3轮制 vs 单轮制 | 先单轮，Phase 3加3轮 | Phase 2 |
| 碰撞检测 | MVP跳过（纯距离判定）→ Phase 3加Unity Physics | Phase 3 |
| 客户端预测+服务端和解 | MVP不做 → Phase 3（走稳再跑） | Phase 3 |

### 9.3 后续扩展路线

```
Phase 1 (当前): C++ Server + ENET 基础PVP躲猫猫
    ↓
Phase 2+: AI + 自然语言生成道具系统
    - 玩家输入"一个红色花瓶"
    - AI生成对应3D模型/材质
    - 躲藏者变身为该道具融入环境
    ↓
Phase 3+: 多房间支持、在线匹配、排位系统
```

### 9.4 参考文档

- 头脑风暴会话：`_bmad-output/brainstorming-session-2026-05-02.md`
- Game Brief：`_bmad-output/game-brief.md`
- Skills 技能注册：`Skills/README.md` — 所有脚本的契约、依赖和状态
- ENET GitHub：https://github.com/lsalzman/enet
- ENet-CSharp (Unity)：https://github.com/nxrighthere/ENet-CSharp
- WinLibs MinGW：https://winlibs.com/

### 9.5 Skills 目录

项目使用 `Skills/` 目录注册所有脚本的技能定义文件，供 Claude Code 审查和使用。每个技能文件记录脚本的用途、依赖、公开 API 和实践说明。详见 `Skills/README.md`。

```
Skills/
├── README.md
├── Network/
│   ├── network-manager.md       ← Phase 0 活跃
│   └── client-protocol.md       ← Phase 0 活跃
├── Core/
│   ├── game-manager.md          ← Phase 1 计划
│   ├── level-timer.md           ← Phase 1 计划
│   ├── score-manager.md         ← Phase 2 计划
│   └── protocol-defs.md         ← Phase 1 基础设施
├── Gameplay/
│   ├── player-controller.md     ← Phase 1 计划
│   ├── highlight-system.md      ← Phase 2 计划
│   └── tag-system.md            ← Phase 2 计划
├── UI/
│   └── menu-controller.md       ← Phase 1 计划
└── World/
    └── procedural-map-generator.md ← Phase 1 计划
```

---

**Phase 0 完成：C++ ENET 服务端 + Unity 客户端已通过 ENet-CSharp 源码互通。Phase 1 目标：输入同步 + 基础移动 + 程序化测试地图。**
