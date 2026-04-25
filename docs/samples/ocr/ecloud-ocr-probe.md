# Ecloud OCR 探测报告

- 采集时间: 2026-04-25 23:54:28 +08:00
- 目标进程路径: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- 目标进程 PID: `12124`
- 目标主窗口标题: `移动云电脑`
- OCR 语言: `zh-CN`
- 抓图模式: `Window`

## 选中捕获窗口

- 句柄: `0x70526`
- 标题: `Chrome Legacy Window`
- 类名: `Chrome_RenderWidgetHostHWND`
- 来源: `RenderHost`
- 选择原因: `未最小化，可容纳默认状态区域，RenderHost 内容窗口`
- 可见: `False`
- 最小化: `False`
- 启用: `True`
- 窗口区域: `(-25600, -25600) 1624x948`
- Client 区域: `(-25600, -25600) 1624x948`

## 默认状态区域

- 相对区域: `(120, 220) 1200x700`
- 绝对区域: `(-25480, -25380) 1200x700`
- 说明: `默认区域已经固化为中部偏左状态检测区域。`

## OCR 文本

```text
<empty>
```

归一化文本:
```text
<empty>
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

## OCR 行明细

当前区域没有识别出任何文本行。

## 窗口候选尝试

| 句柄 | 来源 | 标题 | 类名 | 可见 | 最小化 | 区域可用 | 抓图成功 | 识别状态 | 文本长度 | 结果 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `0x70526` | `RenderHost` | `Chrome Legacy Window` | `Chrome_RenderWidgetHostHWND` | False | False | True | True | `<none>` | 0 | `抓图与 OCR 成功。` |
| `0x30494` | `MainWindow` | `移动云电脑` | `Chrome_WidgetWin_0` | True | True | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x8052A` | `TopLevelWindow` | `settingWindow` | `Chrome_WidgetWin_0` | False | False | True | True | `<none>` | 0 | `抓图与 OCR 成功。` |
| `0x80480` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x2603AE` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x7047A` | `TopLevelWindow` | `` | `Chrome_SystemMessageWindow` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x1104A4` | `TopLevelWindow` | `` | `Electron_NotifyIconHostWindow` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x50492` | `TopLevelWindow` | `` | `Base_PowerMessageWindow` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x70524` | `TopLevelWindow` | `MSCTFIME UI` | `MSCTFIME UI` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0x15001C` | `TopLevelWindow` | `Default IME` | `IME` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |
| `0xA0478` | `TopLevelWindow` | `` | `Chrome_WidgetWin_0` | False | False | False | False | `<none>` | 0 | `候选窗口尺寸不足，无法容纳默认状态区域。` |

## 说明

- 该工具只做固定区域 OCR 与只读状态识别。
- `window` 模式会优先尝试通过窗口句柄抓图，以减少前台遮挡的影响。
- `screen` 模式仍然依赖目标窗口实际显示在屏幕上。
