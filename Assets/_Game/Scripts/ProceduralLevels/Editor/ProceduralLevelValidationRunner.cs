#if UNITY_EDITOR
using System.Text;
using _Game.ProceduralLevels;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public static class ProceduralLevelValidationRunner
    {
        const int LevelsPerFrame = 12;

        static bool _running;
        static int _currentLevel;
        static int _endLevel;
        static int _failures;
        static int _previousDifficulty;
        static StringBuilder _report;
        static string _title;

        public static bool IsRunning => _running;

        public static void StartValidation()
        {
            if (_running)
            {
                Debug.LogWarning("[ProceduralLevels] Validation already running.");
                return;
            }

            ProceduralLevelCache.Clear();
            _running = true;
            _currentLevel = 1;
            _endLevel = ProceduralLevelConstants.TotalLevelCount;
            _failures = 0;
            _previousDifficulty = 0;
            _report = new StringBuilder();
            _title = "Validating Procedural Levels";

            EditorApplication.update += Tick;
            Debug.Log($"[ProceduralLevels] Started validation for levels 1-{_endLevel} (non-blocking).");
        }

        public static void StartPrewarm()
        {
            if (_running)
            {
                Debug.LogWarning("[ProceduralLevels] Prewarm already running.");
                return;
            }

            _running = true;
            _currentLevel = ProceduralLevelConstants.HandcraftedLevelCount + 1;
            _endLevel = ProceduralLevelConstants.TotalLevelCount;
            _failures = 0;
            _previousDifficulty = 0;
            _report = null;
            _title = "Prewarming Level Cache";

            EditorApplication.update += TickPrewarm;
            Debug.Log($"[ProceduralLevels] Started prewarm for levels {_currentLevel}-{_endLevel} (non-blocking).");
        }

        static LevelDefinition GetDefinitionForValidation(int levelNumber)
        {
            if (ProceduralLevelUtility.IsHandcraftedLevel(levelNumber))
            {
                var prefab = Resources.Load<Level>($"Levels/Level {levelNumber}");
                if (prefab == null)
                    return null;

                return HandcraftedLevelExtractor.Extract(prefab, levelNumber);
            }

            return ProceduralLevelGenerator.Generate(levelNumber);
        }

        public static void Cancel()
        {
            if (!_running)
                return;

            Finish(cancelled: true);
        }

        static void Tick()
        {
            if (!_running)
                return;

            int processed = 0;
            while (processed < LevelsPerFrame && _currentLevel <= _endLevel)
            {
                LevelDefinition definition = GetDefinitionForValidation(_currentLevel);
                bool solvable = definition != null && LevelSolvabilityValidator.IsSolvable(definition);

                if (!solvable)
                {
                    _failures++;
                    string detail = definition == null
                        ? "missing definition"
                        : $"{definition.LineCount} lines, {definition.Shape}";
                    _report.AppendLine($"Level {_currentLevel}: NOT SOLVABLE ({detail})");
                }

                if (definition != null &&
                    definition.DifficultyScore < _previousDifficulty &&
                    _currentLevel > ProceduralLevelConstants.HandcraftedLevelCount)
                {
                    _report.AppendLine(
                        $"Level {_currentLevel}: difficulty dipped ({definition.DifficultyScore} < {_previousDifficulty})");
                }

                if (definition != null)
                    _previousDifficulty = definition.DifficultyScore;
                _currentLevel++;
                processed++;
            }

            float progress = (_currentLevel - 1) /
                             (float)(_endLevel - 1);

            if (EditorUtility.DisplayCancelableProgressBar(
                    _title,
                    $"Level {_currentLevel - 1} / {_endLevel}",
                    progress))
            {
                Finish(cancelled: true);
                return;
            }

            if (_currentLevel > _endLevel)
                Finish(cancelled: false);
        }

        static void TickPrewarm()
        {
            if (!_running)
                return;

            int processed = 0;
            while (processed < LevelsPerFrame && _currentLevel <= _endLevel)
            {
                ProceduralLevelCache.GetOrGenerate(_currentLevel);
                _currentLevel++;
                processed++;
            }

            float progress = (_currentLevel - 1) /
                             (float)(_endLevel - 1);

            if (EditorUtility.DisplayCancelableProgressBar(
                    _title,
                    $"Caching level {_currentLevel - 1} / {_endLevel}",
                    progress))
            {
                Finish(cancelled: true);
                return;
            }

            if (_currentLevel > _endLevel)
            {
                Debug.Log($"[ProceduralLevels] Prewarmed level cache (levels {ProceduralLevelConstants.HandcraftedLevelCount + 1}-{ProceduralLevelConstants.TotalLevelCount}).");
                Finish(cancelled: false);
            }
        }

        static void Finish(bool cancelled)
        {
            EditorApplication.update -= Tick;
            EditorApplication.update -= TickPrewarm;
            EditorUtility.ClearProgressBar();
            _running = false;

            if (cancelled)
            {
                Debug.LogWarning("[ProceduralLevels] Operation cancelled by user.");
                return;
            }

            if (_report == null)
                return;

            int proceduralCount = ProceduralLevelConstants.TotalLevelCount;

            if (_failures == 0)
            {
                Debug.Log($"[ProceduralLevels] All {proceduralCount} levels passed solvability validation.");
            }
            else
            {
                Debug.LogError($"[ProceduralLevels] {_failures} levels failed validation.\n{_report}");
            }

            if (_report.Length > 0 && _failures == 0)
                Debug.Log(_report.ToString());
        }
    }
}
#endif
