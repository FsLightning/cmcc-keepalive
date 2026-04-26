# Ecloud OCR 探测报告

- 采集时间: 2026-04-26 08:45:19 +08:00
- 目标进程路径: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- 目标进程 PID: `14088`
- 目标主窗口标题: `移动云电脑`
- OCR 语言: `zh-CN`
- 抓图模式: `Window`

## 预处理窗口布局

- 已请求: `True`
- 目标普通窗体尺寸: `(120, 80) 1600x900`
- 预处理前是否最大化: `False`
- 预处理前是否最小化: `True`
- 已恢复为普通窗体: `True`
- 已应用目标尺寸: `True`
- 结果说明: `OCR 前已尝试恢复普通窗体并调整到目标分辨率。`
- 预处理前区域: `(-25600, -25600) 128x22`
- 预处理后区域: `(120, 80) 1638x985`

## 选中捕获窗口

- 句柄: `0x40494`
- 标题: `Chrome Legacy Window`
- 类名: `Chrome_RenderWidgetHostHWND`
- 来源: `RenderHost`
- 选择原因: `可见，未最小化，可容纳默认状态区域，RenderHost 内容窗口`
- 可见: `True`
- 最小化: `False`
- 启用: `True`
- 窗口区域: `(127, 110) 1624x948`
- Client 区域: `(127, 110) 1624x948`

## 默认状态区域

- 区域策略: `adaptive-scaled`
- 相对区域: `(96, 194) 959x618`
- 绝对区域: `(223, 304) 959x618`
- 说明: `默认区域已经固化为中部偏左状态检测区域。`

## OCR 文本

```text
我 的 电 脑 0 办 公 型 I 公 众 版 0 华 北 ． 北 京 3 自 动 连 接 Windows 已 关 机
```

归一化文本:
```text
我的电脑0办公型I公众版0华北．北京3自动连接Windows已关机
```

## 自定义关键词结果

- 配置关键词: ``
- 命中关键词: `<none>`
- 任意命中: `False`
- 全部命中: `False`

## 三态识别结果

- 当前识别状态: `Windows 已关机`
- 状态命中: `True`

状态规则:
- `Windows 已关机`: 命中=`True`，已满足=`Windows, 已关机`，缺失=``
- `Windows 关机中`: 命中=`False`，已满足=`Windows`，缺失=`关机中`
- `Windows 运行中`: 命中=`False`，已满足=`Windows`，缺失=`运行中`

## OCR 行明细

| 文本 | 边界 |
| --- | --- |
| `我 的 电 脑` | `706,303,105,30` |
| `0 办 公 型 I 公 众 版` | `725,367,159,27` |
| `0 华 北 ． 北 京 3` | `705,422,101,21` |
| `自 动 连 接` | `708,523,81,23` |
| `Windows 已 关 机` | `364,536,198,28` |

## 窗口候选尝试

| 句柄 | 来源 | 区域策略 | 标题 | 类名 | 可见 | 最小化 | 区域可用 | 抓图成功 | 识别状态 | 文本长度 | 结果 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `0x40494` | `RenderHost` | `initial/fixed` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | False | False | True | True | `<none>` | 0 | `[initial/fixed] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x40494` | `RenderHost` | `initial/adaptive-scaled` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | False | False | True | True | `<none>` | 0 | `[initial/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x5B0030` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x5B0030` | `TopLevelWindow` | `initial/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/fit-to-client] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2803AE` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2803AE` | `TopLevelWindow` | `initial/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/fit-to-client] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2604BA` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[initial/adaptive-scaled] 窗口抓图失败。` |
| `0x2604BA` | `TopLevelWindow` | `initial/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[initial/fit-to-client] 窗口抓图失败。` |
| `0x80528` | `TopLevelWindow` | `initial/<none>` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `[initial] 没有可用于当前窗口尺寸的捕获区域。` |
| `0x70482` | `MainWindow` | `initial/<none>` | `移动云电脑` | `Chrome_WidgetWin_0` | True | True | False | False | `<none>` | 0 | `[initial] 没有可用于当前窗口尺寸的捕获区域。` |
| `0x70482` | `MainWindow` | `normalized-retry/fixed` | `移动云电脑` | `Chrome_WidgetWin_0` | True | False | True | True | `<none>` | 0 | `[normalized-retry/fixed] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x70482` | `MainWindow` | `normalized-retry/adaptive-scaled` | `移动云电脑` | `Chrome_WidgetWin_0` | True | False | True | True | `<none>` | 0 | `[normalized-retry/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x40494` | `RenderHost` | `normalized-retry/fixed` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | True | False | True | True | `Windows 已关机` | 33 | `[normalized-retry/fixed] 抓图与 OCR 成功。` |
| `0x40494` | `RenderHost` | `normalized-retry/adaptive-scaled` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | True | False | True | True | `Windows 已关机` | 33 | `[normalized-retry/adaptive-scaled] 抓图与 OCR 成功。` |
| `0x5B0030` | `TopLevelWindow` | `normalized-retry/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[normalized-retry/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x5B0030` | `TopLevelWindow` | `normalized-retry/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[normalized-retry/fit-to-client] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2803AE` | `TopLevelWindow` | `normalized-retry/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[normalized-retry/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2803AE` | `TopLevelWindow` | `normalized-retry/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[normalized-retry/fit-to-client] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2604BA` | `TopLevelWindow` | `normalized-retry/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[normalized-retry/adaptive-scaled] 窗口抓图失败。` |
| `0x2604BA` | `TopLevelWindow` | `normalized-retry/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[normalized-retry/fit-to-client] 窗口抓图失败。` |
| `0x80528` | `TopLevelWindow` | `normalized-retry/<none>` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `[normalized-retry] 没有可用于当前窗口尺寸的捕获区域。` |

## 说明

- 该工具只做固定区域 OCR 与只读状态识别。
- `window` 模式会优先尝试通过窗口句柄抓图，以减少前台遮挡的影响。
- `screen` 模式仍然依赖目标窗口实际显示在屏幕上。
