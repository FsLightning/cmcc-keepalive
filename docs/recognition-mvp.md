# Recognition MVP

## Goal

The first implementation focuses on technical validation only. It should answer three questions reliably on a Windows 11 host:

- Is the target process running?
- Can the target top-level window be identified?
- Does the observed window look like a normal ready desktop?

The MVP must not perform recovery, notifications, OCR, or any input automation.

The currently confirmed target process is `Ecloud Cloud Computer Application.exe`.

## Runtime flow

Each polling cycle follows the same order:

1. Probe the target process.
2. Probe the target window for the selected process.
3. Classify the current session state.
4. Log a readable summary and a structured payload.

## Session states

- `NotRunning`: No matching process instance was selected.
- `ProcessOnly`: A process was selected, but no usable top-level window was found.
- `ClientVisibleButUnknown`: A top-level window was found, but it did not satisfy the current desktop-ready rules.
- `DesktopReady`: A top-level window was found and matched the configured desktop-ready rules.

## Desktop-ready rules

The first implementation uses simple and transparent rules:

- The window must exist.
- The window must be visible if `RequireVisibleWindow` is enabled.
- The window must not be minimized if `AllowMinimizedWindow` is disabled.
- The window bounds must be at least `MinimumDesktopWidth` x `MinimumDesktopHeight`.
- If title or class keywords are configured, at least one configured keyword must match.
- If no keywords are configured, size and visibility rules alone are enough for `DesktopReady`.

## Process selection rules

- Match the process by normalized process name.
- If `TargetExecutablePath` is configured, require an exact path match.
- If only one candidate exists, select it.
- If multiple candidates exist and exactly one has a main window handle, select that one.
- Otherwise, pick the most recently started candidate and keep the full candidate list in the log payload.

## Window selection rules

For the selected process, enumerate all top-level windows and rank them by:

1. Visible first.
2. Non-minimized first.
3. Larger area first.
4. Higher handle value last as a deterministic tie-breaker.

The chosen window is logged together with the full candidate list.

## Configuration fields

The `Guard` section in `appsettings.json` controls the MVP:

- `PollIntervalSeconds`
- `TargetProcessName`: currently set to `Ecloud Cloud Computer Application.exe`
- `TargetExecutablePath`
- `DesktopTitleKeywords`
- `DesktopClassKeywords`
- `MinimumDesktopWidth`
- `MinimumDesktopHeight`
- `RequireVisibleWindow`
- `AllowMinimizedWindow`

## Validation checklist

- Start the service while the client is closed and verify `NotRunning`.
- Launch the client and verify that the process metadata is logged.
- Confirm that the expected top-level window metadata is logged.
- Observe a normal ready desktop and verify `DesktopReady`.
- Observe a client window that does not match the rules and verify `ClientVisibleButUnknown`.
- Review the structured log payload before tightening any recognition rule.
