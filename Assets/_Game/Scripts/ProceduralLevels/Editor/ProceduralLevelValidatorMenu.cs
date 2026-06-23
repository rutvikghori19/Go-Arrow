#if UNITY_EDITOR
using _Game.ProceduralLevels;
using UnityEditor;
using UnityEngine;

namespace _Game.ProceduralLevels.Editor
{
    public static class ProceduralLevelValidatorMenu
    {
        const string MenuRoot = "Go-Arrow/Procedural Levels/";

        [MenuItem(MenuRoot + "Validate All Levels (1-100)")]
        public static void ValidateAllLevels()
        {
            ProceduralLevelValidationRunner.StartValidation();
        }

        [MenuItem(MenuRoot + "Cancel Running Validation/Prewarm")]
        public static void CancelValidation()
        {
            ProceduralLevelValidationRunner.Cancel();
        }

        [MenuItem(MenuRoot + "Cancel Running Validation/Prewarm", true)]
        public static bool CancelValidationValidate()
        {
            return ProceduralLevelValidationRunner.IsRunning;
        }

        [MenuItem(MenuRoot + "Clear Level Cache")]
        public static void ClearCache()
        {
            ProceduralLevelCache.Clear();
            Debug.Log("[ProceduralLevels] Runtime cache cleared.");
        }

        [MenuItem(MenuRoot + "Prewarm Cache (21-100)")]
        public static void PrewarmCache()
        {
            ProceduralLevelValidationRunner.StartPrewarm();
        }
    }
}
#endif
