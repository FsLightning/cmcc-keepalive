# Ecloud OCR 探测报告

- 采集时间: 2026-04-26 08:35:47 +08:00
- 目标进程路径: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- 目标进程 PID: `14088`
- 目标主窗口标题: `移动云电脑`
- OCR 语言: `zh-CN`
- 抓图模式: `Window`

## 预处理窗口布局

- 已请求: `True`
- 目标普通窗体尺寸: `(120, 80) 1600x900`
- 预处理前是否最大化: `False`
- 预处理前是否最小化: `False`
- 已恢复为普通窗体: `True`
- 已应用目标尺寸: `True`
- 结果说明: `OCR 前已尝试恢复普通窗体并调整到目标分辨率。`
- 预处理前区域: `(120, 80) 2048x1113`
- 预处理后区域: `(120, 80) 2048x1113`

## 选中捕获窗口

- 句柄: `0x70482`
- 标题: `移动云电脑`
- 类名: `Chrome_WidgetWin_0`
- 来源: `MainWindow`
- 选择原因: `可见，未最小化，可容纳默认状态区域，标题命中移动云电脑`
- 可见: `True`
- 最小化: `False`
- 启用: `True`
- 窗口区域: `(120, 80) 2048x1113`
- Client 区域: `(128, 111) 2032x1074`

## 默认状态区域

- 相对区域: `(120, 220) 1200x700`
- 绝对区域: `(248, 331) 1200x700`
- 说明: `默认区域已经固化为中部偏左状态检测区域。`

## OCR 文本

```text
8 核 9 Windows 我 的 电 脑 0 办 公 型 《 0 华 北 ． 北 京 3 自 动 连 接 已 关 机 ] 5 GB 断 开
```

归一化文本:
```text
8核9Windows我的电脑0办公型《0华北．北京3自动连接已关机]5GB断开
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
| `8 核` | `1036,306,27,15` |
| `9` | `1036,406,22,22` |
| `Windows` | `473,427,82,15` |
| `我 的 电 脑` | `732,236,82,20` |
| `0 办 公 型 《` | `748,285,76,16` |
| `0 华 北 ． 北 京 3` | `735,328,76,15` |
| `自 动 连 接` | `741,405,60,15` |
| `已 关 机` | `578,426,54,17` |
| `] 5 GB` | `1135,309,40,11` |
| `断 开` | `1141,453,26,13` |

## 窗口候选尝试

| 句柄 | 来源 | 标题 | 类名 | 可见 | 最小化 | 区域可用 | 抓图成功 | 识别状态 | 文本长度 | 结果 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `0x70482` | `MainWindow` | `移动云电脑` | `Chrome_WidgetWin_0` | True | False | True | True | `Windows 已关机` | 39 | `抓图与 OCR 成功。` |
| `0x40494` | `RenderHost` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | True | False | True | True | `Windows 已关机` | 39 | `抓图与 OCR 成功。` |
| `0x5B0030` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `抓图与 OCR 成功，但目标区域没有识别到文本。` |
| `0x2803AE` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x2604BA` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x80528` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |

## 说明

- 该工具只做固定区域 OCR 与只读状态识别。
- `window` 模式会优先尝试通过窗口句柄抓图，以减少前台遮挡的影响。
- `screen` 模式仍然依赖目标窗口实际显示在屏幕上。
