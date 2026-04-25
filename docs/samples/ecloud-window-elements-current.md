# Ecloud Window Element Snapshot

- Captured at: 2026-04-25 23:30:26 +08:00
- Target process path: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- Target process name: `Ecloud Cloud Computer Application`
- Selected PID: `12124`
- Selected main window title: `移动云电脑`
- Selected main window handle: `0x30494`

## Process Candidates

| PID | Start Time | Main Window Handle | Main Window Title | Executable Path |
| --- | --- | --- | --- | --- |
| 12124 | 2026-04-25 15:01:00 +08:00 | `0x30494` | `移动云电脑` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 11760 | 2026-04-25 15:01:08 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 3044 | 2026-04-25 15:01:05 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 1820 | 2026-04-25 15:01:05 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 10748 | 2026-04-25 15:01:05 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 9236 | 2026-04-25 15:01:05 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 1716 | 2026-04-25 15:01:04 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |
| 4868 | 2026-04-25 15:01:03 +08:00 | `0x0` | `` | `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe` |

## Top-level Windows

| Handle | Class | Title | Visible | Minimized | Enabled | Bounds | Area |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `0x2603AE` | `Chrome_WidgetWin_0` | `` | False | False | True | `(12, 13) - (1241, 828) 1229x815` | 1001635 |
| `0x8052A` | `Chrome_WidgetWin_0` | `settingWindow` | False | False | True | `(0, 0) - (2560, 1600) 2560x1600` | 4096000 |
| `0x30494` | `Chrome_WidgetWin_0` | `移动云电脑` | True | False | True | `(55, 116) - (2103, 1229) 2048x1113` | 2279424 |
| `0xA0478` | `Chrome_WidgetWin_0` | `` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x1104A4` | `Electron_NotifyIconHostWindow` | `` | False | False | True | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x7047A` | `Chrome_SystemMessageWindow` | `` | False | False | True | `(0, 0) - (170, 47) 170x47` | 7990 |
| `0x80480` | `Chrome_WidgetWin_0` | `` | False | False | True | `(352, 352) - (2272, 1486) 1920x1134` | 2177280 |
| `0x50492` | `Base_PowerMessageWindow` | `` | False | False | True | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x70524` | `MSCTFIME UI` | `MSCTFIME UI` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |
| `0x15001C` | `IME` | `Default IME` | False | False | False | `(0, 0) - (0, 0) 0x0` | 0 |

## Selected Main Window

- Handle: `0x30494`
- Class: `Chrome_WidgetWin_0`
- Title: `移动云电脑`
- Visible: `True`
- Minimized: `False`
- Enabled: `True`
- Bounds: `(55, 116) - (2103, 1229) 2048x1113`
- Child/descendant count: `1`

## Descendant Window Elements

| Depth | Handle | Parent | Class | Title | Visible | Enabled | Bounds | Control ID |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `0x70526` | `0x30494` | `Chrome_RenderWidgetHostHWND` | `Chrome Legacy Window` | False | True | `(63, 147) - (2095, 1221) 2032x1074` | 495888 |

## Notes

- This snapshot uses Win32 window metadata only. It does not use OCR.
- Chromium/Electron-based clients often expose only a shallow HWND tree; inner visual elements may be custom-drawn and therefore not visible as child windows.
- If the selected main window is minimized, the descendant tree may be sparse even though the client is running normally.
