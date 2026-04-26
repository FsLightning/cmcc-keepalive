# Recognition MVP

## 目标

第一阶段实现只做技术验证，核心是稳定回答以下三个问题：

- 目标进程是否正在运行。
- 目标顶层窗口是否可以被稳定识别。
- 当前观察到的窗口是否满足“正常桌面已就绪”的只读规则。

GuardService 当前不负责恢复、通知、OCR，也不负责任何输入自动化。

当前已经确认的目标进程为 `Ecloud Cloud Computer Application.exe`。

## 运行流程

每个轮询周期都按同一顺序执行：

1. 识别目标进程。
2. 为被选中的进程识别目标窗口。
3. 计算当前会话状态。
4. 输出可读摘要和结构化日志。

如果要做一次性的 HWND 采样和窗口树导出，使用 `src/WindowInspector`。它不参与状态分类，只负责把进程候选、顶层窗口和后代 HWND 元素导出为 Markdown。

如果要做固定区域页面识别，使用 `src/OcrProbe`。它不会改动 GuardService 主流程，只负责截取指定客户端区域、执行 OCR、记录关键词匹配，并在文本充分时把页面判定为 `Windows 已关机`、`Windows 关机中` 或 `Windows 运行中`。

## 会话状态定义

- `NotRunning`：没有选中任何匹配的目标进程实例。
- `ProcessOnly`：已经选中进程，但没有找到可用的顶层窗口。
- `ClientVisibleButUnknown`：找到了顶层窗口，但不满足当前桌面就绪规则。
- `DesktopReady`：找到了顶层窗口，并满足当前桌面就绪规则。

## 桌面就绪规则

当前实现使用尽量透明的规则，便于后续继续收紧：

- 窗口必须存在。
- 如果启用了 `RequireVisibleWindow`，窗口必须可见。
- 如果禁用了 `AllowMinimizedWindow`，窗口不能处于最小化状态。
- 窗口尺寸至少要达到 `MinimumDesktopWidth x MinimumDesktopHeight`。
- 如果配置了标题关键词或类名关键词，至少要命中一个。
- 如果没有配置任何关键词，仅依赖尺寸和可见性规则即可判定为 `DesktopReady`。

## 进程选择规则

- 先按标准化后的进程名匹配。
- 如果配置了 `TargetExecutablePath`，要求路径完全一致。
- 如果只有一个候选进程，直接选中它。
- 如果有多个候选且只有一个拥有主窗口句柄，优先选它。
- 否则选最近启动的候选进程，并把完整候选列表保留在日志中。

## 窗口选择规则

对于已选中的目标进程，会枚举全部顶层窗口，并按以下顺序排序：

1. 可见窗口优先。
2. 未最小化窗口优先。
3. 面积更大的窗口优先。
4. 句柄值作为最终的稳定性 tie-breaker。

最终选中的窗口会和全部候选窗口一起写入日志，便于后续回放样本。

## 配置项

`appsettings.json` 中的 `Guard` 节控制识别 MVP：

- `PollIntervalSeconds`
- `TargetProcessName`：当前默认值是 `Ecloud Cloud Computer Application.exe`
- `TargetExecutablePath`
- `DesktopTitleKeywords`
- `DesktopClassKeywords`
- `MinimumDesktopWidth`
- `MinimumDesktopHeight`
- `RequireVisibleWindow`
- `AllowMinimizedWindow`

## 验证清单

- 在客户端未启动时运行服务，确认状态为 `NotRunning`。
- 启动客户端，确认日志中能看到目标进程元数据。
- 确认日志中能看到预期的顶层窗口元数据。
- 在正常桌面状态下观察日志，确认能够命中 `DesktopReady`。
- 在不满足规则的客户端窗口下观察日志，确认能够命中 `ClientVisibleButUnknown`。
- 在收紧任何识别规则前，先复核结构化日志负载。
- 运行 `WindowInspector`，确认导出的 Markdown 样本正确反映当前顶层窗口和后代 HWND 树，且不依赖 OCR。
- 在客户端窗口已恢复的情况下运行 `OcrProbe`，确认 Markdown 和 JSON 样本中包含 OCR 文本、关键词结果和三态识别结果。
