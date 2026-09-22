# TeachLens roadmap

TeachLens develops deliberately: one main user outcome per release, with teaching reliability taking priority over feature count.

## Versioning rules

- `1.0.x`: bug fixes only.
- `1.x`: focused, backward-compatible improvements.
- `2.0`: reserved for a meaningful rendering, interaction or platform change.

## Planned direction

| Version | Codename | Main outcome |
|---|---|---|
| 1.0 | Acorn | First reliable public release |
| 1.1 | Sprout | Stability, onboarding and diagnostics |
| 1.2 | Sapling | A few simple teaching presets |
| 1.3 | Branch | Pin and resume the magnified area with a minimal interaction model |
| 1.4 | Grove | Trusted distribution, packaging and signing |
| 2.0 | Canopy | A next-generation capture and rendering engine, only if evidence justifies it |

## Near-term discipline

Before implementing 1.1, TeachLens 1.0 should be used in real teaching sessions. Repeated problems—not speculative feature ideas—will determine the next release.

TeachLens will remain small, offline and easy to operate. Screen recording, cloud accounts, advertising, telemetry, OCR/AI, complex annotation tools and plugin systems are intentionally outside the near-term scope.

## Technical direction for 2.0

Version 2.0 may prototype a unified overlay compositor and compare the current Windows Magnification API with a Windows Graphics Capture and Direct3D pipeline. It may also move to a supported modern .NET LTS runtime. These changes will be adopted only when they produce a measurable improvement in image quality, latency, monitor handling or maintainability.

