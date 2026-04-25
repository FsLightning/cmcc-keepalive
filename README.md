# cmcc-keepalive

这个仓库用于沉淀一个运行在 Windows 11 宿主机上的只读识别 MVP。当前阶段刻意收窄范围，只做目标进程识别、目标窗口识别、固定区域 OCR 状态判断，以及样本输出，方便后续持续收集规则和验证结果。

## 当前范围

已包含：

- .NET 8 Worker Service 骨架。
- 基于进程名和可选可执行路径的进程识别。
- 基于 Win32 的顶层窗口识别与窗口树采样。
- 只读会话状态分类：`DesktopReady`、`ClientVisibleButUnknown`、`ProcessOnly`、`NotRunning`。
- 面向固定区域的 OCR 探针，用于识别中文关键词和页面三态。
- Markdown、JSON、PNG 三类样本输出。

暂不包含：

- Webhook 通知。
- GuardService 主流程中的 OCR 或图像识别。
- 自动恢复、自动重连或自动拉起。
- 前台激活、输入模拟或主动点击。
- 复杂异常状态诊断。

## 项目结构

- `src/GuardService`：识别 MVP 的 Worker Service。
- `src/WindowInspector`：目标客户端的只读 Win32 窗口树采样工具。
- `src/OcrProbe`：固定区域 OCR 探针，用于按中文关键词识别页面状态。
- `docs/recognition-mvp.md`：识别 MVP 架构、状态定义和配置说明。
- `docs/ocr-probe.md`：OcrProbe 默认区域、窗口模式、JSON 结构和限制说明。
- `docs/samples/ecloud-window-elements.md`：目标客户端窗口树样本。
- `docs/samples/ocr`：OCR 探针输出的 Markdown、JSON、PNG 样本。

## 本地运行

```powershell
dotnet run --project .\src\GuardService\GuardService.csproj
```

```powershell
dotnet run --project .\src\WindowInspector\WindowInspector.csproj
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj -- --keywords Windows,运行中
```

OcrProbe 当前会在只读前提下输出三态页面识别结果：`Windows 已关机`、`Windows 关机中`、`Windows 运行中`。

## 当前默认配置

`GuardService` 的主配置位于 `src/GuardService/appsettings.json`，用于设置目标进程名、可执行路径、窗口关键词和识别阈值。

当前仓库已经确认的目标进程为 `Ecloud Cloud Computer Application.exe`，默认路径为 `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`。

`WindowInspector` 默认使用同一目标路径，并把窗口树样本导出到 `docs/samples`。

`OcrProbe` 也默认使用同一目标路径，默认采用 `window` 模式，并把状态检测区域固化为客户端区域内的 `(120, 220) 1200x700`。它会输出 OCR 文本、关键词匹配、三态状态判断，以及候选窗口尝试明细。更完整说明见 `docs/ocr-probe.md`。

## 协作约束

- 当前处于早期阶段，可以直接提交到 `main`。
- 只有在用户明确要求时才引入 PR 流程。
- 识别规则、样本路径和已验证限制需要持续写回仓库文档，方便后续其他 AI 继续接手。
