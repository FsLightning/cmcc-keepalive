# 使用说明

## 适用环境

- Windows 11（建议以交互用户会话运行）。
- .NET SDK 8+（当前环境可用 9.x SDK 构建 net8.0 项目）。
- 目标客户端：`Ecloud Cloud Computer Application.exe`。

## 常用命令

### 1. 构建

```powershell
dotnet build .\cmcc-keepalive.sln
```

### 2. 运行 GuardService

```powershell
dotnet run --project .\src\GuardService\GuardService.csproj
```

### 3. 运行 WindowInspector

```powershell
dotnet run --project .\src\WindowInspector\WindowInspector.csproj
```

### 4. 运行 OcrProbe

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj
```

## GuardService 配置

配置文件：`src/GuardService/appsettings.json` 的 `Guard` 节。

关键项：

- `TargetProcessName` / `TargetExecutablePath`
- `AutoStartWhenNotRunning`
- `EnableLoginAssist`
- `LoginButtonKeywords`
- `AllowMouseFallback`
- `EnableTestMode`
- `TestModeLoopCount`
- `HeadlessWindowOnly`

## 闭环测试模式（默认建议 3 轮）

1. 开启：
   - `EnableTestMode=true`
   - `TestModeLoopCount=3`
2. 运行 GuardService。
3. 观察日志是否出现完整循环：
   - StartProcess
   - LoginClick（UIAInvoke 或 MouseFallback）
   - DesktopReady
   - KillProcess
4. 达到目标轮次后，应出现测试模式完成日志。

## 登录辅助策略

- 优先 UIA 按钮触发（Invoke）。
- 若按钮匹配但无法 Invoke，且 `AllowMouseFallback=true`，执行鼠标回退点击。
- 不做账号密码输入，仅点击登录按钮。

## Headless（无显示器）运行建议

1. GuardService：保持 `HeadlessWindowOnly=true`。
2. OcrProbe：优先 `window` 抓图路径，减少 `screen` 回退。
3. 关注日志字段：
   - 窗口可见性/最小化状态
   - 动作执行结果（Actions）
   - OCR 文本是否为空

## 常见问题

### Q1: 进程无法自动拉起

检查：

- `TargetExecutablePath` 是否为空或路径错误。
- 当前用户是否有目标程序启动权限。

### Q2: 登录点击未触发

检查：

- 登录页是否存在可识别按钮文本（`LoginButtonKeywords`）。
- 是否运行在可交互桌面会话（Session 0 场景可能无法点击）。

### Q3: 无显示器时 OCR 不稳定

检查：

- 是否走了 `screen` 抓图路径。
- 是否出现窗口离屏或最小化。
- 结合样本 JSON 判断是否为空帧。

## 推荐排障顺序

1. 先用 WindowInspector 确认窗口句柄/标题/类名。
2. 再用 OcrProbe 检查区域和文本输出。
3. 最后看 GuardService 的闭环动作日志是否按预期触发。
