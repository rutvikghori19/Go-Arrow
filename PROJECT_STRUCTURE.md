# Go-Arrow — Project Structure & Conventions

A Unity **6000.0.70f1** (Unity 6) 2D game. This document describes how the project is
organized and the conventions to follow so the project stays easy to manage as it grows.

> **Golden rule for Unity refactors:** Unity references every asset by the **GUID** stored
> in its sibling `.meta` file, *not* by file path. When you move or rename anything, always
> move the asset **and** its `.meta` file together (Git/`git mv` or the Unity Editor both do
> this). Never move assets out of a `Resources/` folder that is loaded by string path.

## Top-level layout

```
Go-Arrow/
├── Assets/                     # All Unity assets
│   ├── _Game/                  # ★ Everything specific to THIS game (see below)
│   ├── SerapKeremGameKit/      # Reusable in-house framework (level system, UI, audio, pooling…)
│   ├── Packages/               # Imported Asset Store / vendor packages (TextMesh Pro, Toony Colors Pro, Layer Lab GUI…)
│   ├── Plugins/                # Third-party plugins (DOTween, Array2DEditor)
│   ├── Resources/              # Vendor runtime-loaded assets (DOTweenSettings) — path-sensitive, do not move
│   └── Settings/               # URP render pipeline assets + volume profiles (referenced by Graphics/Quality settings)
├── Packages/                   # Unity Package Manager manifest (manifest.json) — package dependencies
├── ProjectSettings/            # Unity project settings (engine version, input, physics, build…)
└── PROJECT_STRUCTURE.md        # This file
```

### Why the `_` prefix on `_Game`?
The leading underscore sorts the folder to the **top** of the Assets list in the Unity
Project window, keeping the game's own content visually separated from imported/third-party
content. This is the project's primary organizing convention:

- **Your game content → `Assets/_Game/`**
- **Reusable framework → `Assets/SerapKeremGameKit/`**
- **Third-party → `Assets/Packages/` and `Assets/Plugins/`**

## `Assets/_Game/` — game content

```
_Game/
├── Scenes/         # GameScene.unity (the playable scene)
├── Scripts/        # Gameplay code, grouped by feature
│   ├── Line/       # Core line/arrow drawing mechanic (LineManager, Line, LineRendererHead, collisions, pooling…)
│   └── UI/         # In-game UI (LivesManager, HeartPanel, HeartUI)
├── Prefabs/        # Reusable prefabs (Heart, RedHeart)
├── Resources/      # Runtime-loadable game content
│   ├── Levels/     # Level 1..10 prefabs + Level_Base template
│   └── Line/       # Line-related runtime assets
├── Materials/      # Game materials (Background, LineRenderer)
├── Sprites/        # Sprite art (heart, redheart)
├── GameImages/     # Game image textures (1–4.png)  ← consolidated here from Assets root
├── Audio/          # SFX (Collision Sound, Movement Sound)
└── Data/           # ScriptableObjects / input data (PlayerInput.asset)
```

There are **10 playable levels** (`Level 1`…`Level 10` prefabs in `_Game/Resources/Levels/`);
`Level_Base.prefab` is a template, not a playable level. Level flow is driven by
`Assets/SerapKeremGameKit/Scripts/LevelSystem/LevelManager.cs`.

## Conventions

- **One root per concern.** Put new game assets under `Assets/_Game/<Category>/`. Keep
  framework-level, reusable code under `Assets/SerapKeremGameKit/`. Never mix third-party
  files into `_Game`.
- **Group scripts by feature** (e.g. `Scripts/Line`, `Scripts/UI`), matching the existing
  pattern, and keep the C# `namespace` aligned with the folder where practical.
- **`Resources/` is path-sensitive.** Anything inside a `Resources/` folder can be loaded at
  runtime by string path. Do not rename/move items inside `Resources/` unless you also update
  every `Resources.Load("...")` call. `Assets/Resources/DOTweenSettings.asset` in particular
  must stay where it is (DOTween loads it by name).
- **Don't touch `Settings/`** render-pipeline assets casually — they're wired into Graphics/
  Quality settings.

## Recommended next step (requires the Unity Editor)

For larger-scale "better management" the highest-impact improvement is adding **Assembly
Definitions** (`.asmdef`) to split compilation into separate assemblies — typically one for
`SerapKeremGameKit` (framework) and one for `_Game` (gameplay), each declaring its
dependencies (DOTween, TextMesh Pro, Input System, TriInspector, etc.).

This was intentionally **not** applied automatically here because asmdef references must be
compile-verified inside the Unity Editor; getting a reference wrong silently breaks the whole
build. Add them from the Editor (`Create ▸ Assembly Definition`) where you can confirm the
project still compiles.
