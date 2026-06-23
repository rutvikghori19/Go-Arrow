#if UNITY_EDITOR
using System.IO;
using GameLine = _Game.Line.Line;
using _Game.Line;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public static class HandcraftedLevelPrefabBaker
    {
        const string LevelsFolder = "Assets/_Game/Resources/Levels";
        const string DenseTemplateResource = "Levels/Level 10";
        const string LinePrefabResource = "Line/Line (1)";
        const float DenseLineThickness = 0.3f;
        const float DenseLineScale = 0.9f;
        static readonly Vector3 DenseBackgroundScale = new Vector3(6f, 6f, 6f);
        static readonly Vector3 DenseBackgroundPosition = new Vector3(2.71f, -2f, 0f);

        [MenuItem("Go-Arrow/Procedural Levels/Bake Dense Handcrafted Level")]
        public static void BakeSelectedLevelMenu()
        {
            BakeDenseLevelWindow.ShowWindow();
        }

        sealed class BakeDenseLevelWindow : EditorWindow
        {
            int _levelNumber = DenseHandcraftedProfile.MinLevel;
            int _seedOffset;

            public static void ShowWindow()
            {
                var window = GetWindow<BakeDenseLevelWindow>(true, "Bake Dense Level", true);
                window.minSize = new Vector2(320f, 140f);
                window.ShowUtility();
            }

            void OnGUI()
            {
                EditorGUILayout.LabelField("Bake Dense Handcrafted Level", EditorStyles.boldLabel);
                _levelNumber = EditorGUILayout.IntSlider(
                    "Level Number",
                    _levelNumber,
                    DenseHandcraftedProfile.MinLevel,
                    DenseHandcraftedProfile.MaxLevel);
                _seedOffset = EditorGUILayout.IntField("Seed Offset", _seedOffset);

                EditorGUILayout.Space();
                if (GUILayout.Button("Bake"))
                {
                    bool overwrite = !File.Exists($"{LevelsFolder}/Level {_levelNumber}.prefab") ||
                                     EditorUtility.DisplayDialog(
                                         "Overwrite Prefab",
                                         $"Level {_levelNumber} prefab already exists. Overwrite?",
                                         "Overwrite",
                                         "Cancel");

                    if (overwrite && BakeLevel(_levelNumber, _seedOffset, true))
                        Close();
                }
            }
        }

        [MenuItem("Go-Arrow/Procedural Levels/Bake Dense Handcrafted Levels (11-50)")]
        public static void BakeAllMenu()
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Bake Dense Levels 11-50",
                "Generate and save Level 11 through Level 50 prefabs?\nExisting prefabs will be overwritten.",
                "Bake All",
                "Cancel");

            if (!overwrite)
                return;

            BakeAll(0, true);
        }

        public static void BakeAllFromCommandLine()
        {
            BakeAll(0, true);
            EditorApplication.Exit(0);
        }

        public static void BakeAll(int seedOffset, bool overwrite)
        {
            int success = 0;
            try
            {
                for (int level = DenseHandcraftedProfile.MinLevel; level <= DenseHandcraftedProfile.MaxLevel; level++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Baking Dense Handcrafted Levels",
                        $"Level {level}",
                        (level - DenseHandcraftedProfile.MinLevel) / (float)(DenseHandcraftedProfile.MaxLevel - DenseHandcraftedProfile.MinLevel));

                    if (BakeLevel(level, seedOffset, overwrite))
                        success++;
                    else
                        Debug.LogWarning($"[HandcraftedLevelPrefabBaker] Failed to bake Level {level}.");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HandcraftedLevelPrefabBaker] Baked {success}/{DenseHandcraftedProfile.MaxLevel - DenseHandcraftedProfile.MinLevel + 1} dense handcrafted levels.");
        }

        public static bool BakeLevel(int levelNumber, int seedOffset, bool overwrite)
        {
            if (!DenseHandcraftedProfile.IsDenseHandcraftedLevel(levelNumber))
            {
                Debug.LogError($"[HandcraftedLevelPrefabBaker] Level {levelNumber} is outside the dense handcrafted band.");
                return false;
            }

            string prefabPath = $"{LevelsFolder}/Level {levelNumber}.prefab";
            if (File.Exists(prefabPath) && !overwrite)
            {
                Debug.LogWarning($"[HandcraftedLevelPrefabBaker] Skipping existing prefab: {prefabPath}");
                return false;
            }

            LevelDefinition definition = null;
            for (int offset = seedOffset; offset < seedOffset + 50; offset++)
            {
                definition = DenseHandcraftedLevelGenerator.Generate(levelNumber, offset);
                if (definition != null &&
                    definition.Lines != null &&
                    definition.Lines.Count >= DenseHandcraftedProfile.GetArrowCount(levelNumber) &&
                    LevelSolvabilityValidator.IsSolvable(definition))
                {
                    break;
                }

                definition = null;
            }

            if (definition == null)
            {
                Debug.LogError($"[HandcraftedLevelPrefabBaker] Could not generate a solvable layout for Level {levelNumber}.");
                return false;
            }

            var template = Resources.Load<Level>(DenseTemplateResource);
            var linePrefab = Resources.Load<GameLine>(LinePrefabResource);
            if (template == null || linePrefab == null)
            {
                Debug.LogError("[HandcraftedLevelPrefabBaker] Missing Level 10 template or Line prefab in Resources.");
                return false;
            }

            var instance = PrefabUtility.InstantiatePrefab(template) as Level;
            if (instance == null)
            {
                Debug.LogError("[HandcraftedLevelPrefabBaker] Failed to instantiate dense level template.");
                return false;
            }

            try
            {
                instance.gameObject.name = $"Level {levelNumber}";
                Transform linesParent = instance.transform.Find("LINES");
                if (linesParent == null)
                {
                    Debug.LogError("[HandcraftedLevelPrefabBaker] Template is missing LINES child.");
                    return false;
                }

                BuildDenseLines(definition, linesParent, linePrefab);
                ApplyDenseLineOverrides(linesParent);
                ConfigureDenseBackground(instance.transform.Find("Background"));
                ConfigureLevelConfig(instance.transform.Find("LevelConfig"), levelNumber);

                PrefabUtility.SaveAsPrefabAsset(instance.gameObject, prefabPath);

                var order = LevelSolvabilityValidator.FindRemovalOrder(definition.Lines);
                var bounds = ComputeBounds(definition);
                Debug.Log(
                    $"[HandcraftedLevelPrefabBaker] Saved {prefabPath} | arrows={definition.LineCount} | " +
                    $"removalSteps={order?.Count ?? 0} | bounds={bounds}");

                return true;
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        static void BuildDenseLines(LevelDefinition definition, Transform linesParent, GameLine linePrefab)
        {
            ClearChildren(linesParent);

            float cell = definition.CellSize <= 0f ? ProceduralLevelConstants.DefaultCellSize : definition.CellSize;
            for (int i = 0; i < definition.Lines.Count; i++)
            {
                var lineData = definition.Lines[i];
                if (lineData == null || lineData.PointCount < 2)
                    continue;

                var instance = PrefabUtility.InstantiatePrefab(linePrefab, linesParent) as GameLine;
                if (instance == null)
                    continue;

                instance.name = $"Line ({i + 1})";
                LineRenderer renderer = instance.LineRenderer;
                if (renderer == null)
                    continue;

                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                renderer.positionCount = lineData.PointCount;
                for (int p = 0; p < lineData.PointCount; p++)
                {
                    var grid = lineData.Points[p];
                    renderer.SetPosition(p, new Vector3(grid.X * cell, grid.Y * cell, 0f));
                }

                AlignLineHead(instance, lineData, cell);
            }
        }

        static void AlignLineHead(GameLine line, LevelLineData lineData, float cell)
        {
            if (lineData.PointCount < 2)
                return;

            Transform head = line.transform.Find("Head (1)");
            if (head == null)
                return;

            var last = lineData.Points[lineData.PointCount - 1];
            var prev = lineData.Points[lineData.PointCount - 2];
            head.localPosition = new Vector3(last.X * cell, last.Y * cell, 0f);

            int dx = last.X - prev.X;
            int dy = last.Y - prev.Y;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - 90f;
            head.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        static void ApplyDenseLineOverrides(Transform linesParent)
        {
            foreach (var line in linesParent.GetComponentsInChildren<GameLine>(true))
            {
                line.transform.localScale = Vector3.one * DenseLineScale;

                var snapFixer = line.GetComponent<LineRendererSnapFixer>();
                if (snapFixer != null)
                    snapFixer.enabled = false;

                var spawner = line.GetComponent<LineSegmentColliderSpawner2D>();
                if (spawner != null)
                {
                    var serialized = new SerializedObject(spawner);
                    SerializedProperty thickness = serialized.FindProperty("thickness");
                    if (thickness != null)
                    {
                        thickness.floatValue = DenseLineThickness;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        static void ConfigureDenseBackground(Transform background)
        {
            if (background == null)
                return;

            background.localScale = DenseBackgroundScale;
            background.localPosition = DenseBackgroundPosition;
        }

        static void ConfigureLevelConfig(Transform levelConfigTransform, int levelNumber)
        {
            if (levelConfigTransform == null)
                return;

            var config = levelConfigTransform.GetComponent<SerapKeremGameKit._Levels.LevelConfig>();
            if (config == null)
                return;

            int band = levelNumber - DenseHandcraftedProfile.MinLevel;
            config.TimeThresholdsSec = new[]
            {
                45f + band * 2f,
                60f + band * 2f,
                90f + band * 2f
            };
            config.LivesThresholds = new[] { 5, 3, 1 };
            config.WinCoins = 10 + band;
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static string ComputeBounds(LevelDefinition definition)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var line in definition.Lines)
            {
                foreach (var p in line.Points)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            return $"x[{minX}..{maxX}] y[{minY}..{maxY}]";
        }
    }
}
#endif
