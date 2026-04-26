# Ecloud Window Element Snapshot

- Captured at: 2026-04-26 08:35:41 +08:00
- Target process path: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- Target process name: `Ecloud Cloud Computer Application`
- Selected PID: `14088`
- Selected main window title: `移动云电脑`
- Selected main window handle: `0x70482`

## Process Candidates

| PID | Start Time | Main Window Handle | Main Window Title | Executable Path |
| --- | --- | --- | --- | --- |
| 14088 | 2026-04-26 08:21:18 +08:00 | `0x70482` | `移动云电脑` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 2032 | 2026-04-26 08:21:23 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 3916 | 2026-04-26 08:21:21 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 12336 | 2026-04-26 08:21:21 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 2740 | 2026-04-26 08:21:21 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 17788 | 2026-04-26 08:21:21 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 8512 | 2026-04-26 08:21:20 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 15952 | 2026-04-26 08:21:20 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |

## Top-level Windows

| Handle | Class | Title | Visible | Minimized | Enabled | Bounds | Area |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `0x2803AE` | `Chrome_WidgetWin_0` | `` | False | False | True | `(12, 12) - (1241, 827) 1229x815` | 1001635 |
| `0x2604BA` | `Chrome_WidgetWin_0` | `` | False | False | True | `(12, 12) - (1241, 827) 1229x815` | 1001635 |
| `0x70482` | `Chrome_WidgetWin_0` | `移动云电脑` | True | False | True | `(120, 80) - (2168, 1193) 2048x1113` | 2279424 |
| `0x60492` | `Base_PowerMessageWindow` | `` | False | False | True | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x80528` | `Chrome_WidgetWin_0` | `` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |
| `0xF0538` | `Electron_NotifyIconHostWindow` | `` | False | False | True | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x19044E` | `Chrome_SystemMessageWindow` | `` | False | False | True | `(0, 0) - (170, 47) 170x47` | 7990 |
| `0x5B0030` | `Chrome_WidgetWin_0` | `` | False | False | True | `(352, 352) - (2272, 1486) 1920x1134` | 2177280 |
| `0x80526` | `MSCTFIME UI` | `MSCTFIME UI` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x1804FC` | `IME` | `Default IME` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |

## Selected Main Window

- Handle: `0x70482`
- Class: `Chrome_WidgetWin_0`
- Title: `移动云电脑`
- Visible: `True`
- Minimized: `False`
- Enabled: `True`
- Bounds: `(120, 80) - (2168, 1193) 2048x1113`
- Child/descendant count: `1`

## Window Layout Adjustment (Before Probe)

- Requested: `True`
- Target normal bounds: `(120, 80) 1600x900`
- Was maximized: `False`
- Was minimized: `False`
- Restored to normal: `True`
- Resize applied: `True`
- Message: `Window was restored to normal state when needed and resized before detail probing.`
- Bounds before: `(150, 91) - (2198, 1322) 2048x1231`
- Bounds after: `(120, 80) - (2168, 1193) 2048x1113`

## Descendant Window Elements

| Depth | Handle | Parent | Class | Title | Visible | Enabled | Bounds | Control ID |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `0x40494` | `0x70482` | `Chrome_RenderWidgetHostHWND` | `Chrome Legacy Window` | True | True | `(128, 111) - (2160, 1185) 2032x1074` | 426128 |

## Notes

- This snapshot uses Win32 window metadata only. It does not use OCR.
- Chromium/Electron-based clients often expose only a shallow HWND tree; inner visual elements may be custom-drawn and therefore not visible as child windows.
- If the selected main window is minimized, the descendant tree may be sparse even though the client is running normally.
