# Contributing to TeachLens

Thank you for helping TeachLens become a clearer teaching tool.

## Before opening a change

1. Search existing issues first.
2. Keep the utility small, offline and easy to understand.
3. Explain the teaching scenario the change improves.
4. Avoid adding telemetry, advertising, administrator requirements or unnecessary dependencies.

## Build and test

Run from Windows PowerShell:

```powershell
.\build.ps1
.\artifacts\TeachLens-v1.0-Acorn\TeachLens.exe --self-test .\artifacts\TeachLens-v1.0-Acorn\self-test.txt
Get-Content .\artifacts\TeachLens-v1.0-Acorn\self-test.txt
```

Test highlight, spotlight, magnifier and `Esc` on at least one Windows 10/11 x64 computer. Multi-monitor changes should also be tested with the secondary monitor positioned to the left of the primary display.

## Pull requests

- Keep each pull request focused.
- Update `CHANGELOG.md` when behavior changes.
- Update both `README.md` and `README.vi.md` for user-facing changes.
- Do not commit files from `artifacts/`.

