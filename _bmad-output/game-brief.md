---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments:
  - _bmad-output/brainstorming-session-2026-05-02.md
documentCounts:
  brainstorming: 1
  research: 0
  notes: 0
workflowType: 'game-brief'
lastStep: 0
project_name: 'Peek-A-Boo'
user_name: 'Master'
date: '2026-05-02'
game_name: 'Peek-A-Boo（躲猫猫）'
---

# Game Brief: Peek-A-Boo（躲猫猫）

**Date:** {{date}}
**Author:** {{user_name}}
**Status:** Draft for GDD Development

---

## Executive Summary

{{executive_summary}}

---

## Game Vision

### Core Concept

Peek-A-Boo 是一款 7 人 PVP 第一人称躲猫猫游戏——一位寻找者在限定时间内搜索六位躲藏者，每30秒全局高亮为双方制造紧张的信息博弈。

### Elevator Pitch

Peek-A-Boo 将童年经典"躲猫猫"转化为肾上腺素拉满的 PVP 竞技体验。1位寻找者对6位躲藏者——纯第一人称、极简操作、30秒一次的"心跳"暴露机制。你会在寻找者的脚步声逼近时屏住呼吸，也会在猎物近在咫尺时露出微笑。一局三分钟，重玩无数遍。

### Vision Statement

我们相信最简单的快乐来自人与人之间的心理博弈。Peek-A-Boo 不依赖复杂的技能树或道具系统——它只用移动、躲藏和观察三个动词，创造出让朋友尖叫、欢笑、互相吐槽的时刻。我们希望每个玩家放下手柄时，记住的不是分数，而是"那次你居然藏在我背后一整局！"的故事。

---

## Target Market

### 项目定位

Peek-A-Boo 是一个 **技术 Demo / 原型项目**，定位为派对类游戏的衍生体验。当前阶段聚焦于构建可玩的基础框架，后续迭代将引入 AI + 自然语言生成道具系统来丰富玩法。

**当前阶段（Phase 1）：** 完成 PVP 躲猫猫基础循环——联网、移动、躲藏、寻找、结算。

**后续阶段（Phase 2+）：** 引入 AI 驱动的道具变形系统，玩家可通过自然语言描述生成可伪装的环境道具，大幅丰富策略深度和趣味性。

### Primary Audience

核心受众是 **朋友聚会场景中的休闲玩家**——快速上手、即时欢乐、无学习门槛。

**用户画像：**
- **年龄：** 10岁+
- **游戏经验：** 休闲（WASD基础即可）
- **场景：** 朋友聚会、家庭娱乐、线下派对
- **动机：** 共同欢笑、心理博弈、简单直接的快乐

### Secondary Audience

**技术验证/演示场景：** 本项目的另一目的是验证 Unity 6.3 LTS + Netcode for GameObjects 在局域网 PVP 场景下的技术可行性，为后续 AI 道具系统的集成铺路。

### Market Context

**类型定位：** 派对游戏 / 社交多人 / 非对称 PVP

**参考游戏：**
- Roblox Hide & Seek — 大众吸引力验证
- GMod Prop Hunt — 长期生命力验证
- 各种派对游戏（Among Us, Goose Goose Duck）— 社交推理+简单规则的乐趣

**差异化：** 独立可执行文件、局域网直连、极低门槛。后续 AI 道具系统将成为核心差异化竞争力。

---

## Game Fundamentals

### Core Gameplay Pillars

| 支柱 | 说明 | 优先级 |
|------|------|--------|
| **1. 极简上手** | 3个手指WASD + 鼠标，30秒内任何人可玩。零教学文本，操作即理解 | 最高 |
| **2. 心理博弈** | 躲藏者的欺骗 vs 寻找者的推理——这是游戏的核心乐趣来源，而非复杂的系统 | 最高 |
| **3. 紧张节奏** | 30秒高亮是"心跳"，120秒倒计时是"呼吸"。每30秒制造一次决策高峰 | 高 |
| **4. 社交欢乐** | 被抓时的搞笑反应、观战时的上帝视角、"你居然藏在那！"的惊呼——笑比赢重要 | 高 |

**支柱冲突时的优先级：** 心理博弈 > 紧张节奏 > 社交欢乐 > 极简上手

### Primary Mechanics

**寻找者动词：** 观察(鼠标扫描) → 标记(左键Tag) → 追捕(追击逃逸者) → 读图(高亮记忆)

**躲藏者动词：** 选位(准备阶段30s) → 潜伏(蹲伏静默听脚步) → 转移(高亮后冒险换位) → 逃逸(被发现后3秒脱离视线)

**核心循环：** `选位(30s) → 潜伏 → 高亮(每30s) → [被发现]→逃逸→重新潜伏 | [被抓]→观战 | [120s结束]→幸存胜利`

### Player Experience Goals

| 角色 | 情绪曲线 |
|------|----------|
| **躲藏者** | 匆忙选位(30s) → 毛骨悚然(听到脚步) → 释然(脚步远去) → 恐慌(高亮瞬间) → 笑容/叹息(被抓) |
| **寻找者** | 自信开局 → 焦虑上升(找不到) → 警觉(高亮线索) → 兴奋(发现/追捕) → 成就感(抓到) |
| **观战者** | 上帝视角的"信息差快感" — "他就在你身后！" — 期待下一轮角色 |

**目标情感：** 每局结束时，无论输赢，玩家都因为某个具体时刻而想"再来一局"。

---

## Scope and Constraints

### Target Platforms
- **平台：** Windows PC
- **引擎：** Unity 6.3 LTS（URP渲染管线，FPS模板）
- **联机：** 局域网 LAN（Unity Netcode for GameObjects + Unity Transport UDP）

### Development Timeline
- **Phase 0（1-2天）：** 搭建联机环境，两人同场景移动
- **Phase 1（3-5天）：** 核心循环 — 1v1 Tag + 倒计时 + 单轮结束
- **Phase 2（2-3天）：** 扩展到1vN + 基础Lobby + 结果展示
- **Phase 3（持续迭代）：** 正式地图 + 打磨 + 30秒高亮 + 计分 + 观战

### Team Resources
- 独立开发（单人），C#基础薄弱

### Technical Constraints
- **Unity Netcode for GameObjects (NGO)** — Host模式，Server权威
- **灰盒优先** — Phase 0-2使用白模几何体，Phase 3做正式地图
- **最小自定义代码** — 优先使用Unity内置组件
- **IP直连** — MVP阶段不做Lobby系统

---

## Reference Framework

### Inspiration Games
- **Roblox Hide & Seek** — 躲猫猫大众化验证
- **GMod Prop Hunt** — 非对称PVP长线生命力（15年+）
- **第五人格** — 非对称竞技在中国市场验证
- **Among Us** — 社交推理+极简操作=病毒传播

### Key Differentiators
- 独立可执行文件，下载即玩（不需要母游戏）
- 局域网直连，零配置，适合线下聚会
- 100%第一人称——更沉浸紧张
- 后期AI+自然语言道具系统——核心竞争力

---

## Content Framework

### World and Setting
- **MVP地图：** 1张Low-Poly卡通室内场景
- **规模：** 2000-3000平米，三层垂直空间，15-20个躲藏点
- **特征：** 多材质地面（声景差异化），次级通道，2-3个动态元素

### Narrative Approach
- 无剧情叙事——玩家行为即故事生成器
- 每局的躲藏-发现-追逐-逃脱构成自发叙事

### Content Volume (MVP)
- 1张地图 | 7名玩家（1 Seeker + 6 Hiders）| 单回合120秒 | 1套得分系统 | 1个观战模式

---

## Art and Audio Direction

### Visual Style
- **Low-Poly 卡通风格** — 简洁友好，色彩分区辅助空间定位
- **角色设计** — Seeker视觉更大/有威胁感，Hider小巧灵活
- **高亮效果** — 瞬间亮色轮廓（MVP先做Minimap红点）

### Audio Style
- **脚步声** — 不同地面材质差异化
- **30秒提示** — 递增心跳声
- **被抓音效** — 幽默搞笑而非恐怖

### Production Approach
- MVP使用FPS模板自带模型和动画
- 灰盒搭建第一张地图
- 音效用免费素材库占位

---

## Risk Assessment

### Key Risks

| 风险 | 严重度 | 缓解策略 |
|------|--------|----------|
| 联机调试挫败感 | 🔴 致命 | Phase 0第一优先级，两台机器/双开验证 |
| 找不到人测试 | 🔴 致命 | 找到至少一位固定联机测试伙伴 |
| 地图设计替代写代码 | 🟡 高 | Phase 0-2禁止使用精美模型 |
| 完美主义/教程地狱 | 🟡 高 | 按需学习，学了立即用 |
| Seeker体验过差 | 🟡 中 | 高亮机制是Seeker核心补偿，不可砍掉 |

### Technical Challenges
- Gamemode状态机（Server侧NetworkVariable<GameState>）
- 高亮系统表现层（墙透Shader vs Minimap红点）
- 观战系统视角切换

---

## Success Criteria

### MVP Definition
> 两个人在同一个灰盒场景中，一个人能追另一个人，左键Tag判定生效，120秒倒计时结束显示结果。到达此点即为MVP。

### Success Metrics
- 7人能在同一局域网房间内正常移动
- Tag判定无网络延迟感知
- 一局完整结束（120秒→结算→重新开始）
- 核心指标：测试者自发要求"再来一局"

### Launch Goals
- Phase 2+：引入AI+自然语言生成道具系统
- 更多地图和游戏模式

---

## Next Steps

### Immediate Actions
1. 搭建Unity NGO联机环境（两台设备或双开实例）
2. 配置FPS模板的角色预制体为NetworkObject
3. 实现灰盒场景 + 7人同时移动

### Open Questions
- 高亮实现的最终方案（Minimap红点 vs 墙透Shader）— Phase 1验证手感后决定
- 观战模式交互细节 — Phase 2细化
- 计分系统是否在对局中显示 — 倾向于隐藏，结算展示

---

## Appendices

### A. 头脑风暴摘要
详见 `_bmad-output/brainstorming-session-2026-05-02.md`（23+创意、4专家评审、完整计分设计）

### B. 专家评审
Samus Shepard（关卡/平衡）、Cloud Dragonborn（架构/网络）、Sally（UX/新手体验）、Indie（MVP/开发路径）

### C. 参考
- Brainstorming Session: `_bmad-output/brainstorming-session-2026-05-02.md`
- GDS Workflow Status: `_bmad-output/gds-workflow-status.yaml`

---

_本Game Brief作为Game Design Document (GDD)创建的基础输入。_
