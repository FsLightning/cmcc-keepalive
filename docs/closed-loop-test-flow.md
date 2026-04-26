# 闭环测试流程

## 目的

验证 GuardService 是否能够在无人值守场景下完成闭环：

- 进程未运行时自动拉起。
- 登录界面出现时自动尝试点击登录按钮。
- 到达目标识别页面后，在测试模式下强制关闭进程。
- 自动进入下一轮，固定执行指定轮数（默认 3 轮）。

## 前置配置

在 `src/GuardService/appsettings.json` 中设置：

- `AutoStartWhenNotRunning=true`
- `EnableLoginAssist=true`
- `EnableTestMode=true`
- `TestModeLoopCount=3`
- `HeadlessWindowOnly=true`
- `LoginButtonKeywords` 根据实际页面补充关键词

## 单轮状态机

1. Probe 进程。
2. 若 NotRunning 且允许自动拉起，则执行 StartProcess。
3. Probe 窗口并分类状态。
4. 若未到 DesktopReady，尝试 LoginClick：
   - 先 UIA Invoke；
   - 失败后按配置走鼠标回退。
5. 重新探测并分类。
6. 若 DesktopReady 且测试模式仍未达目标轮次，执行 KillProcess。
7. 记录本轮结果并进入下一轮。

## 终止条件

- `TestLoopsCompleted >= TestModeLoopCount`。
- 到达后停止 kill 行为，继续仅识别运行（或由外层流程停止服务）。

## 关键日志字段

- `SessionState`
- `Actions[].Type`
- `Actions[].Succeeded`
- `Actions[].Method`
- `TestLoopsCompleted`
- `TestLoopsTarget`

## 验收标准

1. 在目标程序初始未运行时，能自动启动。
2. 登录页存在时能看到登录动作日志（UIAInvoke 或 MouseFallback）。
3. 每轮进入 DesktopReady 后能触发一次 KillProcess。
4. 3 轮后完成并输出目标轮次已达成日志。
5. 日志中能完整回放每轮动作链路。
