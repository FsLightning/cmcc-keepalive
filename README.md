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
- OCR or image recognition.
- Auto-recovery or auto-restart logic.
- Foreground activation or input simulation.
- Complex abnormal-state detection.

## Project layout

- `src/GuardService`: Worker Service implementation.
- `docs/recognition-mvp.md`: architecture, state definitions, and configuration notes.

## Run locally

```powershell
dotnet run --project .\src\GuardService\GuardService.csproj
```

## Configuration

Edit `src/GuardService/appsettings.json` and set the target process name, optional executable path, and window keyword rules.

## Collaboration notes

- Early-stage changes can be committed directly to `main`.
- PRs should only be introduced when the user explicitly requests them.
- Keep documentation updated as the recognition rules evolve so other AI agents can continue from the same state.
