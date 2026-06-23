#if UNITY_EDITOR
using System.Text;
using GameLine = _Game.Line.Line;
using _Game.ProceduralLevels;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public class ProceduralLevelEditorWindow : EditorWindow
    {
        int _levelNumber = 12;
        bool _useShapeOverride;
        LevelDefinition _previewDefinition;
        Vector2 _scroll;

        [MenuItem("Go-Arrow/Procedural Level Editor")]
        public static void Open()
        {
            GetWindow<ProceduralLevelEditorWindow>("Procedural Level Editor");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Procedural Level Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Levels 1–50 = handcrafted prefabs. Levels 51–100 = mirrored/rotated copies of those templates.",
                MessageType.Info);

            _levelNumber = EditorGUILayout.IntSlider("Level Number", _levelNumber, 1, ProceduralLevelConstants.TotalLevelCount);
            var previewProfile = DifficultyProfile.ForLevel(_levelNumber);
            EditorGUILayout.LabelField("Tier", previewProfile.Tier.ToString());
            EditorGUILayout.LabelField("Target Arrows", previewProfile.LineCount.ToString());
            EditorGUILayout.LabelField("Grid Size", previewProfile.GridSize.ToString());

            _useShapeOverride = EditorGUILayout.Toggle("Show Shape For Level", _useShapeOverride);
            if (_useShapeOverride)
                EditorGUILayout.LabelField("Shape", previewProfile.Shape.ToString());

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Preview"))
                GeneratePreview();

            if (GUILayout.Button("Build In Open Scene (Play Mode Safe)"))
                BuildInScene();

            if (GUILayout.Button("Validate This Level"))
                ValidateCurrent();

            if (GUILayout.Button("Export JSON To Clipboard") && _previewDefinition != null)
            {
                EditorGUIUtility.systemCopyBuffer = LevelDefinitionJson.ToJson(_previewDefinition);
                Debug.Log($"Copied level {_levelNumber} JSON to clipboard.");
            }

            EditorGUILayout.Space();

            if (_previewDefinition == null)
                return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDefinition(_previewDefinition);
            EditorGUILayout.EndScrollView();
        }

        void GeneratePreview()
        {
            ProceduralLevelCache.Clear();
            _previewDefinition = ProceduralLevelGenerator.Generate(_levelNumber);
        }

        void BuildInScene()
        {
            if (_previewDefinition == null)
                GeneratePreview();

            var template = Resources.Load<Level>("Levels/Level_Base");
            var linePrefab = Resources.Load<GameLine>("Line/Line (1)");
            if (template == null || linePrefab == null)
            {
                Debug.LogError("Missing Level_Base or Line prefab in Resources.");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(template) as Level;
            if (instance == null)
                return;

            instance.gameObject.name = $"Preview Level {_levelNumber}";
            var host = instance.GetComponent<ProceduralLevelHost>() ??
                       instance.gameObject.AddComponent<ProceduralLevelHost>();

            ProceduralLevelBuilder.Build(_previewDefinition, instance.transform.Find("LINES"), linePrefab);
            Selection.activeGameObject = instance.gameObject;
            Debug.Log($"Built preview for level {_levelNumber} ({_previewDefinition.LineCount} lines, {_previewDefinition.Shape}).");
        }

        void ValidateCurrent()
        {
            if (_previewDefinition == null)
                GeneratePreview();

            bool solvable = LevelSolvabilityValidator.IsSolvable(_previewDefinition);
            var order = LevelSolvabilityValidator.FindRemovalOrder(_previewDefinition.Lines);
            string message = solvable
                ? $"Level {_levelNumber} is solvable. Removal order length: {order?.Count ?? 0}"
                : $"Level {_levelNumber} is NOT solvable.";
            Debug.Log(message);
        }

        static void DrawDefinition(LevelDefinition definition)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Level: {definition.LevelNumber}");
            sb.AppendLine($"Tier: {definition.Tier}");
            sb.AppendLine($"Shape: {definition.Shape}");
            sb.AppendLine($"Grid: {definition.GridSize}");
            sb.AppendLine($"Arrows: {definition.LineCount} (target {definition.TargetLineCount})");
            sb.AppendLine($"Difficulty: {definition.DifficultyScore}");
            sb.AppendLine();

            for (int i = 0; i < definition.Lines.Count; i++)
            {
                var line = definition.Lines[i];
                sb.Append($"Line {i + 1}: ");
                for (int p = 0; p < line.PointCount; p++)
                {
                    var pt = line.Points[p];
                    sb.Append($"({pt.X},{pt.Y})");
                    if (p < line.PointCount - 1)
                        sb.Append(" -> ");
                }
                sb.AppendLine();
            }

            EditorGUILayout.TextArea(sb.ToString(), GUILayout.ExpandHeight(true));
        }
    }
}
#endif
