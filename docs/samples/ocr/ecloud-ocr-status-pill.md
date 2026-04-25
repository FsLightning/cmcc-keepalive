# Ecloud OCR Probe

- Captured at: 2026-04-25 23:32:33 +08:00
- Target process path: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- Selected PID: `12124`
- Selected main window title: `移动云电脑`
- Selected main window handle: `0x30494`
- OCR language: `zh-CN`
- Capture image: `C:\MakeX\cmcc-keepalive\docs\samples\ocr\ecloud-ocr-status-pill.png`

## Window Info

- Window class: `Chrome_WidgetWin_0`
- Window visible: `True`
- Window minimized: `False`
- Window bounds: `(55, 116) 2048x1113`
- Client bounds: `(63, 147) 2032x1074`

## Capture Region

- Relative offset: `(250, 240)`
- Relative size: `620x280`
- Absolute capture rect: `(313, 387) 620x280`

## OCR Result

```text
<empty>
```

Normalized OCR text:
```text
<empty>
```

## Keyword Match

- Configured keywords: `Windows, 运行中, 关机中`
- Matched keywords: `<none>`
- Any keyword matched: `False`
- All keywords matched: `False`
- Page hit: `False`

## OCR Lines

No OCR text lines were recognized in the selected region.

## Notes

- This probe is designed for fixed-region OCR only.
- It captures the screen pixels from the selected visible window client area.
- It does not use OCR for full-page understanding; it only checks whether configured keywords appear in the selected region.
