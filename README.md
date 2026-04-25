# cmcc-keepalive

This repository hosts a Windows recognition MVP for a local guard process. Version 1 is intentionally narrow: it only detects the target process, inspects the target window, classifies a small set of read-only session states, and writes structured logs for sample collection.

## Version 1 scope

Included:

- .NET 8 Worker Service skeleton.
- Process detection by process name and optional executable path.
- Top-level window detection with HWND, title, class name, visibility, minimized state, and bounds.
- Session classification into `DesktopReady`, `ClientVisibleButUnknown`, `ProcessOnly`, and `NotRunning`.
- Structured logging and readable summaries for each polling cycle.

Excluded:

- Webhook notifications.
- OCR or image recognition in the main GuardService flow.
- Auto-recovery or auto-restart logic.
- Foreground activation or input simulation.
- Complex abnormal-state detection.

## Project layout

- `src/GuardService`: Worker Service implementation.
- `src/WindowInspector`: read-only Win32 window tree inspector for the target client.
- `src/OcrProbe`: experimental fixed-region OCR probe for page detection by Chinese keywords.
- `docs/recognition-mvp.md`: architecture, state definitions, and configuration notes.
- `docs/samples/ecloud-window-elements.md`: latest captured window-element snapshot from the target client.
- `docs/samples/ocr`: OCR probe samples and captured region images.

## Run locally

```powershell
dotnet run --project .\src\GuardService\GuardService.csproj
```

```powershell
dotnet run --project .\src\WindowInspector\WindowInspector.csproj
```

```powershell
dotnet run --project .\src\OcrProbe\OcrProbe.csproj -- --keywords 页面关键词1,页面关键词2
```

## Configuration

Edit `src/GuardService/appsettings.json` and set the target process name, optional executable path, and window keyword rules.

The current confirmed target process for this repository is `Ecloud Cloud Computer Application.exe`.

The window-inspector tool defaults to the same target executable path and exports a Markdown snapshot under `docs/samples`.

The OCR probe also defaults to the same target executable path. It captures a fixed client-area region, runs Chinese OCR, and records whether any configured keywords are present.

## Collaboration notes

- Early-stage changes can be committed directly to `main`.
- PRs should only be introduced when the user explicitly requests them.
- Keep documentation updated as the recognition rules evolve so other AI agents can continue from the same state.
