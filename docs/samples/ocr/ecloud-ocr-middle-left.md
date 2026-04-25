# Ecloud OCR Probe

- Captured at: 2026-04-25 23:36:52 +08:00
- Target process path: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- Selected PID: `12124`
- Selected main window title: `移动云电脑`
- Selected main window handle: `0x30494`
- OCR language: `zh-CN`
- Capture image: `C:\MakeX\cmcc-keepalive\docs\samples\ocr\ecloud-ocr-middle-left.png`

## Window Info

- Window class: `Chrome_WidgetWin_0`
- Window visible: `True`
- Window minimized: `False`
- Window bounds: `(-2, 6) 2048x1114`
- Client bounds: `(5, 37) 2033x1076`

## Capture Region

- Relative offset: `(120, 320)`
- Relative size: `1200x700`
- Absolute capture rect: `(125, 357) 1200x700`

## OCR Result

```text
<empty>
```

Normalized OCR text:
```text
<empty>
```

## Keyword Match

- Configured keywords: `Windows, 运行中`
- Matched keywords: `<none>`
- Any keyword matched: `False`
- All keywords matched: `False`
- Page hit by custom keywords: `False`
- Detected state: `<none>`
- State hit: `False`

Configured states:
- `Windows 已关机` requires `Windows + 已关机`
- `Windows 关机中` requires `Windows + 关机中`
- `Windows 运行中` requires `Windows + 运行中`

## OCR Lines

No OCR text lines were recognized in the selected region.

## Notes

- This probe is designed for fixed-region OCR only.
- It captures the screen pixels from the selected visible window client area.
- It does not use OCR for full-page understanding; it only checks whether configured keywords appear in the selected region.
