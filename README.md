# TeachLens 1.0 “Acorn”

**Make every pixel teach.**

[Tiếng Việt](README.vi.md) · [Project on GitHub](https://github.com/Thanhvh/TeachLens)

TeachLens is a compact Windows utility for teaching and software demonstrations. It keeps attention on the right place without taking over the whole desktop.

## What it does

- Adds a clean green highlight around the pointer, with a subtle click pulse.
- Dims only the monitor under the pointer and leaves a circular spotlight.
- Magnifies a small local area at 1.5×, 2.0×, 2.5× or 3.0× using the Windows Magnification API.
- Uses a circular lens by default, with a rounded rectangle as an option.
- Keeps the lens and its source inside the current monitor, including negative coordinates in multi-monitor layouts.
- Handles per-monitor DPI and lets mouse clicks pass through every overlay.
- Uses `Esc` to turn off the most recently enabled TeachLens feature.
- Runs from the system tray and remembers settings in `%LOCALAPPDATA%\TeachLens\settings.ini`.
- Switches between English and Vietnamese from the main window. English is the default.

## Run

1. Open `TeachLens.exe`.
2. Choose a lens shape, zoom level and size.
3. Optionally click **Hide to tray**. Global shortcuts keep working in other apps.

No installer or administrator access is required.

## Shortcuts

| Shortcut | Action |
|---|---|
| `Ctrl + Alt + 1` | Toggle pointer highlight |
| `Ctrl + Alt + 2` | Toggle spotlight |
| `Ctrl + Alt + M` | Toggle magnifier |
| `Ctrl + Alt + 0` | Turn every overlay off |
| `Ctrl + Alt + ↑` | Increase zoom |
| `Ctrl + Alt + ↓` | Decrease zoom |
| `Esc` | Turn off the most recently enabled TeachLens feature |

`Esc` is registered only while at least one overlay is active. If `Ctrl + Alt + M` is already in use, TeachLens falls back to `Ctrl + Alt + L` and shows a note in the main window.

## Privacy and safety

- No telemetry or analytics.
- No network access.
- No ads or bundled software.
- No administrator privileges.
- Settings stay on this computer.

TeachLens is currently unsigned, so Windows SmartScreen may display a warning the first time it runs. Download releases only from the project's GitHub page and compare the published SHA-256 checksum when available.

## Requirements

- Windows 10 or Windows 11, 64-bit.
- .NET Framework 4.8 or later.

DRM-protected content, the secure UAC desktop and some specialized rendering surfaces may not appear inside the magnifier.

## Credits

- Author: **Vũ Hữu Thành**
- Email: **thanh.vuh@gmail.com**
- GitHub: **https://github.com/Thanhvh**
- Engineering assistance: **OpenAI Codex**

Codex is credited as engineering assistance, not as a human or legal co-author. Copyright remains with Vũ Hữu Thành.

## Version

Public version: **1.0**  
Codename: **Acorn** — a small beginning designed to grow.

## License

TeachLens is released under the MIT License.
