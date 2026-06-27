# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-26

### Added

- Initial package release.
- `ScriptableSettings` base class with self-registration on load.
- `ScriptableSettingsRegistry` with `TryGet<T>` (pure lookup) and `Get<T>` (logs and returns null when missing).
- Per-class drawer system (`SettingsDrawerBase<T>`) with a standardized default drawer.
- Single hub window (`Window/Fy/Scriptable Settings`) with create, preload toggle, build indicator, and a
  configurable folder for newly created assets.
- Build preload synchronization, including a build preprocessor that reconciles the preloaded-assets list.
