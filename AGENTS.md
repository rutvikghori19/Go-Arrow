# AGENTS.md

## Cursor Cloud specific instructions

**Project:** Go-Arrow — a **Unity `6000.0.70f1` (Unity 6)** 2D game. Layout, folder
conventions, and the GUID/`.meta` rule for safe refactors are documented in
`PROJECT_STRUCTURE.md` — read that first. In short: game content lives under
`Assets/_Game/`, the reusable framework under `Assets/SerapKeremGameKit/`, and third-party
code under `Assets/Packages/` and `Assets/Plugins/`. The playable scene is
`Assets/_Game/Scenes/GameScene.unity`, and gameplay levels are the `Level 1..10` prefabs in
`Assets/_Game/Resources/Levels/` (driven by `SerapKeremGameKit/Scripts/LevelSystem/LevelManager.cs`).

### How this project is built / run / tested
- Everything goes through the **Unity Editor** (matching version `6000.0.70f1`, revision
  `0d9e1a373c8b`). There is **no host-side package manager step**: Unity resolves its
  dependencies from `Packages/manifest.json` itself the first time the project is opened
  (note several packages are **Git URLs**, so opening the project the first time needs network
  + `git`). This is why the startup/update script is intentionally a no-op.
- **Build / Run:** open the project in the Unity Editor (or `-batchmode -buildTarget ...
  -executeMethod <BuildMethod>` for CI). The default target is mobile (URP Mobile renderer).
- **Tests:** Unity Test Framework (`com.unity.test-framework`) — run with
  `-runTests -testPlatform EditMode` (or `PlayMode`) `-batchmode`.
- **"Lint":** there is no standalone linter; script-compile correctness is reported by the
  Editor (Console / `-logFile`). A clean Console after import == no broken references / compile
  errors.

### ⚠️ Key gotcha: Unity licensing in the cloud VM
The Unity **Editor itself installs and boots fine headlessly** on this Linux VM (download the
Linux Editor tarball for revision `0d9e1a373c8b`; it needs the usual GUI/runtime libraries —
GTK, NSS, GBM, ALSA, GL, plus `xvfb` for a virtual display). Account email/password login also
works in batch mode.

**However, you cannot license a free Unity Personal seat on a headless/CI machine.** Unity has
**discontinued manual (`.alf`→`.ulf`) activation of Personal licenses**, and headless
`-username/-password` activation returns `Found 0 free entitlements` /
`No valid Unity Editor license found`. Consequences:
- With only a **free Personal** account, you **cannot import/compile/build/run/test** the
  project in this cloud VM. Verify changes by opening the project in a **local** Unity Editor
  instead (a clean Console confirms asset/reference integrity).
- To do verification/builds **in the cloud**, a **Unity Pro/Plus** license is required.
  Provide `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` (or a Pro `.ulf` as
  `UNITY_LICENSE`) as Secrets, then activate in batch mode.

### Refactor safety (recap of PROJECT_STRUCTURE.md)
Unity references assets by the **GUID** in their `.meta` files, not by path. When moving or
renaming assets, always move the asset **and** its `.meta` together (`git mv`), and never move
items out of a path-loaded `Resources/` folder (e.g. `Assets/Resources/DOTweenSettings.asset`).
The Editor-generated `Library/`, `Temp/`, `Logs/`, and `UserSettings/` folders are gitignored —
do not commit them.
