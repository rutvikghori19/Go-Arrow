#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SerapKeremGameKit._LevelSystem;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public static class HandcraftedLevelExporter
    {
        const string MenuRoot = "Go-Arrow/Procedural Levels/";
        const string OutputFolder = "Assets/_Game/Resources/ProceduralLevels/Templates";

        [MenuItem(MenuRoot + "Export Handcrafted Templates (1-20)")]
        public static void ExportAll()
        {
            Directory.CreateDirectory(OutputFolder);

            for (int i = 1; i <= ProceduralLevelConstants.HandcraftedLevelCount; i++)
            {
                var prefab = Resources.Load<Level>($"Levels/Level {i}");
                if (prefab == null)
                {
                    Debug.LogWarning($"Missing Level {i} prefab.");
                    continue;
                }

                var definition = HandcraftedLevelExtractor.Extract(prefab, i);
                string json = LevelDefinitionJson.ToJson(definition);
                string path = $"{OutputFolder}/Level_{i:D2}.json";
                File.WriteAllText(path, json);
                Debug.Log($"Exported {path} ({definition.LineCount} arrows)");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif
