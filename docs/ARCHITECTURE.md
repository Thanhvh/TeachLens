# Architecture

TeachLens intentionally uses a small architecture suitable for an offline Windows utility.

## Components

- `src/TeachLens.cs` contains the WinForms interface, settings, global shortcuts, overlay controller, Magnification API integration and self-test.
- `src/IconBuilder.cs` deterministically generates the application icon during the build.
- `src/app.manifest` declares Windows compatibility and per-monitor DPI awareness.
- `build.ps1` compiles a portable x64 executable with the .NET Framework compiler already included with Windows.

## Overlay model

The pointer highlight and spotlight use click-through topmost layered windows. The magnifier uses the native Windows Magnification API. Every overlay is constrained to the monitor containing the pointer. The controller updates stacking only when state changes, avoiding continuous z-order churn and border flicker.

## Data and network behavior

TeachLens has no network client, analytics library, browser component or updater. The only persisted data is a small INI settings file in `%LOCALAPPDATA%\TeachLens`.

## Design constraints

- Windows 10/11 x64.
- .NET Framework 4.8 or later.
- Single portable executable.
- No administrator privileges.
- No third-party runtime packages.

