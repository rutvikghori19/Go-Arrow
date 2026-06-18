# Procedural Level Generator — Solvability Verifier

A small standalone (Unity-independent) console program that proves every procedurally
generated level (1..500) is **100% completable**.

It reuses the real generator source from
`Assets/_Game/Scripts/Generation/` (the pure-C# files have no Unity dependency) and, for each
level, checks solvability two independent ways:

1. `ProceduralLevelSolver.IsSolvable` — a greedy solver that repeatedly removes any line whose
   forward sweep is clear of all still-present lines.
2. The construction's promised **reverse-placement** removal order actually clears the board.

It also checks determinism and prints the difficulty curve (line count / board size).

## How to run

Any C# toolchain works. Examples:

### Using Unity's bundled Mono (no extra install)
```bash
MONO_BIN="<UnityEditor>/Data/MonoBleedingEdge/bin"   # Linux/Windows
# macOS: <UnityEditor>/Unity.app/Contents/MonoBleedingEdge/bin
"$MONO_BIN/mcs" Tools/LevelGenVerify/Program.cs \
  Assets/_Game/Scripts/Generation/ProceduralLevelModels.cs \
  Assets/_Game/Scripts/Generation/ProceduralLevelBuilder.cs \
  Assets/_Game/Scripts/Generation/ProceduralLevelSolver.cs \
  -out:verify.exe
"$MONO_BIN/mono" verify.exe
```

### Using the .NET SDK
```bash
dotnet run --project Tools/LevelGenVerify   # (after adding a minimal .csproj), or compile with csc
```

Exit code is `0` when all levels are solvable.
