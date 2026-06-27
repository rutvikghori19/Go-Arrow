# Go-Arrow Neon UI Kit

Procedural neon/cyberpunk UI sprites for Unity, generated to match the Go-Arrow mockup style.

**Location:** `Assets/_Game/Sprites/NeonUIKit/`  
**Total sprites:** 102 PNG files (transparent background, trimmed where noted)

---

## Quick start in Unity

1. Open the project — Unity will auto-import all PNGs (meta files are pre-configured).
2. Drag any sprite onto a **UI → Image** component.
3. For resizable panels/buttons/progress bars, set **Image Type → Sliced** (9-slice borders are already set in `.meta`).
4. Use `colors.json` or `colors.txt` for exact hex values in code or TextMeshPro.

### Load via Resources (optional)

These sprites live under `Assets/_Game/Sprites/`, **not** `Resources/`. To load at runtime:

```csharp
// Assign in Inspector, or move/copy needed sprites to Resources/UI/NeonUIKit/
var sprite = Resources.Load<Sprite>("UI/NeonUIKit/Panels/Panel_Medium_Cyan");
```

Recommended: reference sprites on prefabs or use `[SerializeField] Sprite` fields instead of hard-coded paths.

---

## Folder structure

| Folder | Contents |
|--------|----------|
| `Panels/` | Dialog/panel frames (Small → ExtraLarge, cyan/pink). Popups include close button art. |
| `Buttons/` | Long / Medium / Small / Square + Ghost outline variants (Cyan, Pink, Green, Yellow) |
| `Icons/` | Close, Refresh, Settings, Sound, Haptics, Warning, Arrows, Circle, Check, Lock, Unlock |
| `Toggles/` | Toggle on/off backgrounds, knob, checkbox on/off |
| `ProgressBars/` | Track BG + fill bars (Cyan, Pink, Green, Yellow) |
| `LevelCells/` | Locked (purple), Unlocked (pink), Selected (cyan glow) |
| `HeartsStars/` | Heart Full/Half/Empty, Star Full/Empty |
| `Frames/` | Thin/thick frames, corner pieces (TL/TR/BL/BR), dividers |
| `HUD/` | HUD frame, circle ring, bar |
| `Arrows/` | Directional arrows + double-right |
| `Badges/` | Circular badges + diamond/triangle markers |
| `Sliders/` | Track + knob |
| `Effects/` | Radial glows, light streak, sparkle |
| `Decorations/` | Circuit lines, dot row, bottom border flourish |
| `Misc/` | Round mask, soft/hard shadow, highlight overlay |

---

## Color reference

See **`colors.txt`** (human-readable) and **`colors.json`** (machine-readable).

| Token | Hex | Usage |
|-------|-----|--------|
| Primary Cyan | `#25F5FF` | PLAY, HUD, settings, win accents |
| Bright Cyan | `#77FFFF` | Inner glow, highlights |
| Primary Pink | `#FF2FB5` | LEVELS, fail, hearts, close buttons |
| Bright Pink | `#FF71D0` | Icon strokes, secondary pink glow |
| Neon Green | `#8DFF32` | Win borders, toggle ON, success |
| Bright Green | `#BFFF63` | Checkmarks, toggle knob highlight |
| Neon Yellow | `#FFE84D` | Stars, rewards |
| Orange Glow | `#FFC43A` | Warm accent |
| Purple Neon | `#B44BFF` | Locked level cells |
| Dark Background | `#05070A` | Screen backdrop |
| Panel Background | `#0B1018` | Panel/button fill |
| UI Text | `#BFD9FF` | Body copy on dark panels |

### Parse in C#

```csharp
ColorUtility.TryParseHtmlString("#25F5FF", out Color cyan);
```

### Legacy theme mapping

Existing `UIThemePack/theme-colors.json` values are mapped in `colors.json` → `legacyThemeMapping` for consistency with `NeonUiBuilder` code.

---

## 9-slice (Sliced Image) guide

Assets with `nineSlice: 24` in `manifest.json` use **24px borders** on all sides:

- All `Panels/*`
- All `Buttons/*`
- `ProgressBars/*`
- `LevelCells/*`
- `Frames/*` (16px)
- `HUD/HUD_Frame`, `HUD/HUD_Bar`
- `Sliders/Slider_Track`
- `Decorations/Deco_Border_Bottom`

**Unity setup:** Select sprite → Inspector confirms **Sprite Mode: Single** and **Border** values. On Image component: **Type = Sliced**.

Icons and effects are **trimmed** to tight bounds — use **Simple** image type.

---

## Import settings (pre-applied)

| Setting | Value |
|---------|--------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | 100 |
| Filter Mode | Point (no filter) |
| Compression | None |
| Generate Mip Maps | Off |
| Wrap Mode | Clamp |
| Alpha Is Transparency | On |

---

## Screen → sprite mapping (Go-Arrow mockup)

| Screen | Suggested sprites |
|--------|-------------------|
| Main Menu | `Panel_Large_*`, `Button_Long_Cyan/Pink`, logo from `Resources/UI/GoArrowLogo` |
| Level Select | `Popup_Medium`, `Level_Cell_*`, `Icon_Close` |
| Gameplay HUD | `HUD_Frame`, `HUD_Circle`, `Icon_Refresh`, `Icon_Settings`, `Icon_Heart_*` |
| Level Win | `Panel_Medium` green border feel → `Panel_Medium_Cyan` + green tint, `Icon_Star_Full`, `Button_Long_Cyan` |
| Level Lose | `Panel_Medium_Pink`, `Icon_Heart_Empty`, `Button_Long_Pink` |
| Settings | `Popup_Small`, `Toggle_*`, `Icon_Sound`, `Icon_Haptics` |
| Restart confirm | `Popup_Small`, `Icon_Warning`, `Button_Medium_Pink/Cyan` |

---

## Regenerate sprites

If you need to tweak colors or add assets:

```bash
cd Tools/NeonUIKitGenerator
node generate.mjs
```

This overwrites PNGs and `.meta` files in `Assets/_Game/Sprites/NeonUIKit/`.

---

## Files

| File | Purpose |
|------|---------|
| `manifest.json` | Full asset list with 9-slice and trim flags |
| `colors.json` | Color tokens + import settings |
| `colors.txt` | Quick hex reference for designers |
| `NEON_UI_KIT_GUIDE.md` | This document |
