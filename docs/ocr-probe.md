# OcrProbe 说明

## 目标

`src/OcrProbe` 是一个独立的只读工具，用来截取移动云电脑客户端的固定区域，执行中文 OCR，并根据预定义关键词判断当前页面状态。它不会修改主流程，也不会执行输入模拟、前台切换或主动点击。

## 当前默认值

- 目标进程路径：`C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- 默认抓图模式：`window`
- 默认 OCR 语言：`zh-CN`
- 默认开启窗口预处理：OCR 前先检查主窗体状态，必要时恢复为普通窗体
- 默认普通窗体目标区域：`(120, 80) 1600x900`
- 默认状态检测区域：相对客户端区域 `(120, 220) 1200x700`
- 默认三态规则：`Windows 已关机`、`Windows 关机中`、`Windows 运行中`
- 默认输出目录：`docs/samples/ocr`

## 工作流程

1. 按进程路径筛选目标进程实例。
2. 如果开启窗口预处理，先检查主窗体是否最大化/最小化，必要时恢复普通窗体，并尝试调整到目标区域。
3. 从 `MainWindowHandle`、同进程顶层窗口、主窗口后代窗口中收集候选窗口。
4. 过滤明显无关的辅助窗口，例如通知图标宿主、系统消息窗体、IME 窗体和 `settingWindow`。
5. 按可见性、最小化状态、默认区域是否可容纳、类名特征和窗口面积对候选窗口打分。
6. 在 `window` 模式下优先通过窗口句柄抓图；如果句柄抓图失败且窗口可见且未最小化，再退回 `screen` 抓图。
7. 对截图执行 OCR，输出原始文本、归一化文本、关键词结果、三态状态结果和候选窗口尝试明细。

## 三态规则

当前默认规则全部使用“必须同时命中所有关键词”的方式判断：

- `Windows 已关机`：要求同时命中 `Windows` 和 `已关机`
- `Windows 关机中`：要求同时命中 `Windows` 和 `关机中`
- `Windows 运行中`：要求同时命中 `Windows` 和 `运行中`

OCR 结果在匹配前会去掉空白字符，因此像 `运 行 中` 这种被空格打断的文本也能参与规则判断。

## JSON 输出结构

`docs/samples/ocr/ecloud-ocr-probe.json` 当前使用固定结构，核心字段如下：

- `schemaVersion`：JSON 结构版本号。
- `capturedAt`：采样时间。
- `targetProcessPath`：目标进程路径。
- `process`：已选中进程的 PID、进程名、主窗口标题和主窗口句柄。
- `layoutAdjustment`：OCR 前窗口预处理结果，包括是否最大化/最小化、是否恢复普通窗体、是否调整目标尺寸。
- `selectedCaptureWindow`：最终用于抓图的窗口摘要，包括来源、类名、可见性、最小化状态、评分和选择原因。
- `capture`：抓图模式，以及默认状态区域的相对坐标和绝对坐标。
- `text`：OCR 原始文本、归一化文本和逐行明细。
- `customKeywordDetection`：自定义关键词配置和匹配结果。
- `stateDetection`：三态规则的命中情况和当前识别状态。
- `attemptedWindows`：所有候选窗口的尝试明细，便于回溯为什么最终选中了某个窗口。

## 结果解读

- 如果 `selectedCaptureWindow.isVisible=false` 且 `text.original` 为空，通常说明当前抓图依赖的窗口虽然能被句柄捕获，但目标区域没有返回有效渲染内容。
- 如果某个候选窗口 `regionFits=false`，说明它的客户端区域不足以容纳默认状态检测区域。
- 如果 `stateDetection.detectedState=null`，说明 OCR 文本不足以满足任一三态规则，不代表页面一定异常。
- `attemptedWindows` 中的 `result` 字段用于区分“抓图失败”和“抓图成功但 OCR 为空”。

## 已知限制

- 当主窗口被最小化且 `PrintWindow` 未返回有效内容时，`window` 模式仍可能拿到空白图像。
- 当前客户端更像 Chromium/Electron 自绘界面，标准 Win32 子窗口树和 UI Automation 都无法稳定提供细粒度语义元素。
- 默认区域已经固化，不会自动缩放；如果客户端布局明显变化，需要重新采样并调整默认区域。
- `screen` 模式依赖窗口真实显示在屏幕上，仍会受到遮挡和前台切换影响。

## 常用命令

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj -- --normalize-window-layout true --normal-window-x 120 --normal-window-y 80 --normal-window-width 1600 --normal-window-height 900
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj -- --keywords Windows,运行中
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj -- --capture-mode screen
```
