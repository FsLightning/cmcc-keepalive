# Ecloud OCR 探测报告

- 采集时间: 2026-04-26 10:53:19 +08:00
- 目标进程路径: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- 目标进程 PID: `19608`
- 目标主窗口标题: `移动云电脑`
- OCR 语言: `zh-CN`
- 抓图模式: `Window`

## 预处理窗口布局

- 已请求: `False`
- 目标普通窗体尺寸: `(120, 80) 1600x900`
- 预处理前是否最大化: `False`
- 预处理前是否最小化: `False`
- 已恢复为普通窗体: `False`
- 已应用目标尺寸: `False`
- 结果说明: `窗口预处理已关闭。`

## 选中捕获窗口

- 句柄: `0xF093C`
- 标题: `移动云电脑`
- 类名: `Chrome_WidgetWin_0`
- 来源: `MainWindow`
- 选择原因: `可见，未最小化，可容纳默认状态区域，标题命中移动云电脑`
- 可见: `True`
- 最小化: `False`
- 启用: `True`
- 窗口区域: `(0, 0) 2560x1440`
- Client 区域: `(0, 0) 2560x1440`

## 默认状态区域

- 区域策略: `adaptive-scaled`
- 相对区域: `(151, 295) 1512x939`
- 绝对区域: `(151, 295) 1512x939`
- 说明: `默认区域已经固化为中部偏左状态检测区域。`

## OCR 文本

```text
0 user091 忘 记 密 码 ？ _ 登 录 方 式
```

归一化文本:
```text
0user091忘记密码？_登录方式
```

## 自定义关键词结果

- 配置关键词: ``
- 命中关键词: `<none>`
- 任意命中: `False`
- 全部命中: `False`

## 三态识别结果

- 当前识别状态: `<none>`
- 状态命中: `False`

状态规则:
- `Windows 已关机`: 命中=`False`，已满足=``，缺失=`Windows, 已关机`
- `Windows 关机中`: 命中=`False`，已满足=``，缺失=`Windows, 关机中`
- `Windows 运行中`: 命中=`False`，已满足=``，缺失=`Windows, 运行中`

## 自动点击结果

- 已请求自动点击: `False`
- 已执行自动点击: `False`
- 点击来源关键词: `<none>`
- 点击坐标: `<none>`
- 结果说明: `未请求自动点击（--perform-click=false）。`

## OCR 行明细

| 文本 | 边界 |
| --- | --- |
| `0` | `1102,131,53,52` |
| `user091` | `1079,351,98,21` |
| `忘 记 密 码 ？` | `1079,564,100,21` |
| `_ 登 录 方 式` | `1076,631,93,19` |

## 窗口候选尝试

| 句柄 | 来源 | 区域策略 | 标题 | 类名 | 可见 | 最小化 | 区域可用 | 抓图成功 | 识别状态 | 文本长度 | 结果 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `0xF093C` | `MainWindow` | `initial/fixed` | `移动云电脑` | `Chrome_WidgetWin_0` | True | False | True | True | `<none>` | 11 | `[initial/fixed] 抓图与 OCR 成功。` |
| `0xF093C` | `MainWindow` | `initial/adaptive-scaled` | `移动云电脑` | `Chrome_WidgetWin_0` | True | False | True | True | `<none>` | 18 | `[initial/adaptive-scaled] 抓图与 OCR 成功。` |
| `0x290876` | `RenderHost` | `initial/fixed` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | True | False | True | True | `<none>` | 11 | `[initial/fixed] 抓图与 OCR 成功。` |
| `0x290876` | `RenderHost` | `initial/adaptive-scaled` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | True | False | True | True | `<none>` | 18 | `[initial/adaptive-scaled] 抓图与 OCR 成功。` |
| `0xD08AC` | `TopLevelWindow` | `initial/fixed` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/fixed] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0xD08AC` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x240646` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/adaptive-scaled] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x240646` | `TopLevelWindow` | `initial/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `[initial/fit-to-client] 抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x308050C` | `TopLevelWindow` | `initial/adaptive-scaled` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[initial/adaptive-scaled] 窗口抓图失败。` |
| `0x308050C` | `TopLevelWindow` | `initial/fit-to-client` | `` | `Chrome_WidgetWin_0` | False | False | True | False | `<none>` | 0 | `[initial/fit-to-client] 窗口抓图失败。` |
| `0xE08A8` | `TopLevelWindow` | `initial/<none>` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `[initial] 没有可用于当前窗口尺寸的捕获区域。` |

## 说明

- 该工具只做固定区域 OCR 与只读状态识别。
- `window` 模式会优先尝试通过窗口句柄抓图，以减少前台遮挡的影响。
- `screen` 模式仍然依赖目标窗口实际显示在屏幕上。
