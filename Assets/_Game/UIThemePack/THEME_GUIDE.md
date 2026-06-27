# Go-Arrow Neon UI Theme Pack

Reference package for the Go-Arrow mobile UI (neon / cyberpunk style).

## Contents

| File | Description |
|------|-------------|
| `GoArrowLogo.png` | Official game logo (use on splash, main menu, store art) |
| `UI-Mockup-Reference.png` | Full-screen UI mockup (all panels) |
| `theme-colors.json` | Hex color tokens used in code |
| `THEME_GUIDE.md` | Layout sizes, button specs, typography |

## Color tokens

| Token | Hex | Usage |
|-------|-----|--------|
| Cyan border / HUD text | `#33F2FF` | PLAY, restart icon ring, level pill, timer |
| Magenta border / accent | `#FF33BF` | LEVELS, settings icon ring, fail borders |
| Panel fill | `#0F0A1F` | Dialog backgrounds |
| Success / win | `#59FF59` | Win panel border, toggle ON |
| Fail | `#FF4073` | Lose panel, RETRY |
| Screen background | `#020208` | All screens |

## Layout (reference 1080×1920)

- **Banner ad reserve:** 150px bottom inset
- **HUD header height:** 128px, no background bar
- **HUD circle buttons:** 88×88px (restart left, settings beside it)
- **Main menu buttons:** 620×110px
- **Dialog buttons:** 620×96px
- **Logo size:** 920×340px centered upper area

## Typography

- **Titles:** Bangers SDF (bold italic) — LEVEL CLEAR, OUT OF LIVES, SETTINGS
- **Buttons / body:** Oswald Bold SDF

## In Unity project

Logo loads from (in order):

1. `Resources/UI/GoArrowLogo`
2. `StreamingAssets/UI/GoArrowLogo.png`

Code: `GoArrowBranding.LoadLogoSprite()`

## Screens included in mockup

1. Main Menu — logo, PLAY / LEVELS / SETTINGS
2. Level Select — grid 1–20, magenta cells, cyan active
3. Gameplay HUD — neon header, hearts, level pill + timer
4. Level Win — green border, NEXT LEVEL / LEVEL SELECT
5. Level Lose — magenta border, RETRY / LEVEL SELECT
6. Settings — SOUND / HAPTICS toggles, cyan border
7. Restart confirm — YES (magenta) / NO (cyan) stacked
