SHAPE LIBRARY — drop silhouette icons here to turn them into level shapes.

HOW IT WORKS
  The Level Designer (Go-Arrow > Level Designer) has a "Shape library (icons -> shapes)"
  section. It scans this folder for textures and lists each as a pickable shape. Choose one,
  set Arrows + Difficulty, and click "Generate level in this icon" — it traces the icon's
  silhouette and fills it with a solvable, bent puzzle. This is how you scale to 200+ shapes.

WHAT TO DROP HERE
  - PNG (or any texture) files of BOLD, SOLID silhouettes — a filled black shape on a white or
    transparent background reads best. Fine-detail photos trace poorly (the grid is only ~15-40
    cells wide). Simple icon-style art is ideal.
  - Name each file what you want the shape called (the filename shows in the dropdown).
  - Higher-res sources trace crisper. The tracer supersamples 4x4 per grid cell (anti-aliased
    edges), and shape detail scales with the "Shape size" slider — keep it >= ~20 for detailed
    silhouettes (dog, elephant, guitar), higher for finer curves.

IMPORT SETTINGS
  None required. The tracer blits each texture through a RenderTexture and reads it back, so
  Read/Write does NOT need to be enabled and compression is fine.

BUNDLED ICONS (40)
  Source : https://game-icons.net   (GitHub: game-icons/icons)
  License: Creative Commons Attribution 3.0 (CC BY 3.0)
           https://creativecommons.org/licenses/by/3.0/
  Rasterized from the project's SVGs (inverted to dark-on-white).

  Attribution by author (required by CC BY 3.0):
    Delapouite (https://delapouite.com/):
      Dog, HorseHead, Elephant, Flamingo, Seagull, Hummingbird, Duck, Penguin, Chicken,
      Rabbit, Kangaroo, Unicorn, Dolphin, Whale, Fish, Cupcake, Sailboat
    Lorc (https://lorcblog.blogspot.com/):
      Cat, Sparrow, Dove, Eagle, Owl, StagHead, Lion, Turtle, Snail, Frog, Crab, Octopus,
      Guitar, Crown, Trophy, Anchor, Rocket, Tree, Ghost, Butterfly
    Caro Asercion: Deer, Fox
    Sparker: Bear

MORE FREE SILHOUETTE PACKS (check each license before shipping)
  - game-icons.net    ~4,000 flat game silhouettes (CC BY 3.0 / CC0) — best fit
  - Tabler / Lucide   open-source icon sets (MIT)
  - Material Symbols   Google, Apache-2.0
  Export as solid black on transparent/white PNG at ~256px+, drop them in, hit "Rescan folder".

The "Dark threshold" slider controls how dark a pixel must be to count as inside the shape —
nudge it if an icon traces too full or too empty.
