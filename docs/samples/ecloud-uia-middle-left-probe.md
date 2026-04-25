# Ecloud UI Automation Middle-Left Probe

- Captured at: 2026-04-25 16:xx:xx +08:00
- Target executable: `C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe`
- Selected PID: `12124`
- Selected top-level window handle: `0x30494`
- Selected top-level window title: `移动云电脑`
- Selected top-level window class: `Chrome_WidgetWin_0`

## Purpose

This probe attempted to extract more detailed information from the middle-left area of the client window without using OCR.

The probe used two read-only approaches:

1. UI Automation tree inspection from the selected window handle and the render-host child handle.
2. Screen-point hit testing around middle-left coordinates on the current desktop.

## UI Automation Tree Probe

### Selected main window

- Handle: `0x30494`
- Name: `移动云电脑`
- Class: `Chrome_WidgetWin_0`
- Control type: `ControlType.Window`
- Bounds returned by UIA: `Empty`
- Supported patterns:
  - `WindowPatternIdentifiers.Pattern`
  - `TransformPatternIdentifiers.Pattern`
- Descendant count in UI Automation control view: `0`
- Raw-view child count sampled: `0`

### Render-host child window

- Handle: `0x70526`
- Name: `Chrome Legacy Window`
- Class: `Chrome_RenderWidgetHostHWND`
- Control type: `ControlType.Pane`
- Bounds returned by UIA: `-32000,-32000,2560,1600`
- Supported patterns: none reported
- Descendant count in UI Automation tree: `0`
- Raw-view child count sampled: `0`

### Other candidate top-level windows

| Handle | Name | Class | Control type | Bounds | Supported patterns |
| --- | --- | --- | --- | --- | --- |
| `0x80480` | `` | `Chrome_WidgetWin_0` | `ControlType.Window` | `352,352,1920,1134` | `WindowPattern`, `TransformPattern` |
| `0x8052A` | `settingWindow` | `Chrome_WidgetWin_0` | `ControlType.Window` | `0,0,2560,1600` | `WindowPattern` |
| `0x2603AE` | `` | `Chrome_WidgetWin_0` | `ControlType.Window` | `12,13,1229,815` | none reported |

No candidate window exposed descendant automation elements during this capture.

## Middle-Left Point Hit Test

The following screen coordinates were probed with `AutomationElement.FromPoint`:

| Label | Coordinate | Result name | Result class | Result type | Native handle |
| --- | --- | --- | --- | --- | --- |
| `screen-middle-left` | `(640, 800)` | `` | `View` | `ControlType.Pane` | `0x0` |
| `screen-far-left-middle` | `(320, 800)` | `` | `View` | `ControlType.Pane` | `0x0` |
| `screen-upper-left-middle` | `(640, 400)` | `` | `View` | `ControlType.Pane` | `0x0` |

These hit tests did not resolve to Ecloud-specific elements. They resolved to a generic pane with native handle `0x0`, which indicates the current on-screen compositor surface rather than a detailed client sub-element.

## Interpretation

1. The client is exposing a valid top-level UI Automation window, but it is not exposing a deeper control tree through standard UI Automation at this capture time.
2. The render-host child window also does not expose a usable descendant tree.
3. The selected main window was minimized in the earlier HWND capture, which likely reduces how much meaningful on-screen information can be resolved by point-based probing.
4. The combination of `Chrome_WidgetWin_0` and `Chrome_RenderWidgetHostHWND` strongly suggests a Chromium/Electron-style client with custom-drawn content.
5. Without OCR, the next best non-invasive options are:
   - capture while the client is restored and not minimized,
   - continue expanding Win32 metadata collection,
   - test additional accessibility APIs if the application enables them.

## Conclusion

This attempt did not reveal detailed middle-left semantic elements inside the client window. The application currently exposes only a shallow window shell and no deeper UI Automation descendants, so there is no reliable non-OCR element data yet for that region.
