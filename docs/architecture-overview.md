# 架构总览

## 目标

本仓库面向 Windows 11 环境，提供一个可扩展的“识别 + 闭环测试”基座，核心关注点是：

- 目标进程是否运行。
- 目标主窗体是否可识别。
- 当前是否进入可用页面状态。
- （可选）未运行自动拉起、登录按钮尝试、测试模式闭环。

## 项目组织

- `src/GuardService`
  - 常驻 Worker 主服务。
  - 主流程包含进程探测、窗口探测、状态分类，以及闭环动作调度。
- `src/WindowInspector`
  - 一次性 Win32 窗口树采样工具。
  - 用于定位句柄、标题、类名、层级结构与窗口状态。
- `src/OcrProbe`
  - 固定/自适应区域 OCR 工具。
  - 输出 Markdown、JSON、PNG 样本，验证三态识别策略。

## GuardService 模块分层

- `Configuration/GuardOptions.cs`
  - 统一配置入口。
  - 包含识别参数、自动化参数、测试模式参数、headless 策略开关。
- `Monitoring/ProcessMonitor.cs`
  - 进程识别与候选选择。
- `Monitoring/WindowProbe.cs`
  - 顶层窗口枚举、排序与主窗体选择。
- `Monitoring/SessionClassifier.cs`
  - 会话状态分类（NotRunning / ProcessOnly / ClientVisibleButUnknown / DesktopReady）。
- `Automation/ProcessController.cs`
  - 自动拉起与强制关闭目标进程。
- `Automation/LoginAssist.cs`
  - 登录按钮动作：UIA 优先，失败后鼠标回退（受配置控制）。
- `Worker.cs`
  - 闭环调度器。
  - 流程：探测 -> 自动拉起 -> 登录尝试 -> 识别 -> 测试模式 kill -> 日志输出。

## 运行时主数据流

单轮周期顺序：

1. 进程探测（ProcessMonitor）。
2. 如果未运行且开启自动拉起，则执行启动动作（ProcessController）。
3. 窗口探测（WindowProbe）。
4. 状态分类（SessionClassifier）。
5. 若未到 DesktopReady 且开启登录辅助，执行登录按钮尝试（LoginAssist）。
6. 若开启测试模式且已识别到 DesktopReady，执行强制关闭并计数一轮。
7. 输出 `GuardCycleResult`（结构化 JSON + 摘要）。

## 测试模式闭环

测试模式用于验证“从异常到恢复”的全链路：

- 目标：固定循环 N 轮（默认 3 轮）。
- 每轮触发条件：识别到 DesktopReady。
- 触发动作：强制关闭进程。
- 下一轮：依靠自动拉起与登录辅助继续进入识别页面。

## Headless 运行策略

对于无显示器场景：

- GuardService 侧保留 `HeadlessWindowOnly` 配置，作为统一策略信号。
- OcrProbe 侧建议禁用 `screen` 路径，优先句柄抓图与诊断日志。
- 通过日志区分：抓图失败、空帧黑屏、OCR 空文本。

## 可观测性与日志

关键输出对象：`GuardCycleResult`。

包含：

- ProcessSnapshot
- WindowSnapshot
- SessionState
- Actions（StartProcess / LoginClick / KillProcess / Skip）
- TestLoopsCompleted / TestLoopsTarget

可直接用于回放每轮动作和识别状态。

## 相关文档

- `docs/recognition-mvp.md`：识别 MVP 规则定义。
- `docs/ocr-probe.md`：OCR 采样与策略说明。
- `docs/usage-guide.md`：运行与排障指南。
- `docs/closed-loop-test-flow.md`：闭环测试模式流程。
- `docs/仓库理解与技术拆解.md`：第一性原理与技术拆解背景。
