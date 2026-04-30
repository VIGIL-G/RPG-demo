# Ascension_SRPG: 2D 修仙战棋 RPG 原型

## 项目简介
本项目是一个基于 Unity 2D 开发的修仙题材网格战棋 RPG 游戏原型（Demo）。项目核心目标是验证游戏底层系统架构的稳定性，打通“剧情触发-网格战斗-数据结算-进入完整世界”的完整游戏生命周期。

当前项目处于灰盒（Graybox）开发阶段，使用基础几何体作为视觉占位符，优先确保系统逻辑与数据管理的工程落地。

## 核心系统与技术实现

### 1. 跨场景数据管理 (Data Persistence)
* **实现逻辑**：针对 Unity 默认的场景卸载机制，构建了全局数据中心 (`GameManager`)。
* **技术方案**：应用单例模式 (Singleton) 结合 `DontDestroyOnLoad` 生命周期控制。确保玩家核心养成数据（如灵根、修为数值、当前境界）在主菜单、剧情场景和战斗场景切换时实现持久化存储与精确累加。

### 2. 场景流转与 UI 架构 (Scene & UI Management)
* **实现逻辑**：实现游戏状态机的物理隔离与平滑过渡。
* **技术方案**：基于 Unity `SceneManager` 完成独立场景的加载与卸载。使用 UGUI 构建交互面板，通过事件绑定 (Event Binding) 将 UI 点击事件与 C# 后端逻辑解耦，实现了完整的 `StartMenu -> StoryScene -> BattleScene` 流程闭环。

### 3. 网格战斗基础框架 (Grid-based Combat System)
* **实现逻辑**：基于二维网格的战棋回合制交战逻辑。
* **技术方案**：编写基础的行动序列管控规则，实现角色在网格矩阵中的坐标移动判定与基础的伤害/状态结算逻辑（平衡性与扩展性待后续迭代）。

## 开发管线与工作流 (Workflow)

* **引擎与语言**：Unity 2022+ / C#
* **AI 辅助工程化**：在开发过程中，将系统设计文档与底层规则作为输入，深度运用 Cursor 等 AI 辅助编程工具。构建了从“策划需求”到“C# 代码生成”、“逻辑查错”及“组件挂载”的标准验证工作流，大幅提升了跨语言（Python 逻辑思维向 C# 语法落地）的开发与排障效率。

## 运行说明

1. 确保已安装 Unity Editor。
2. 克隆本仓库至本地：
   ```bash
   git clone [https://github.com/VIGIL-G/RPG-demo.git](https://github.com/VIGIL-G/RPG-demo.git)
