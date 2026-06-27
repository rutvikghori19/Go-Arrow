#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace _Game.UI.Editor
{
    internal static class ScenePrefabEditorUtility
    {
        public static void SpawnManagerPrefab<T>(Transform parent, string resourcePath) where T : Component
        {
            var source = Resources.Load<GameObject>(resourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Missing manager prefab at Resources/{resourcePath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = source.name;
        }

        public static GameObject SavePrefab(GameObject source, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, assetPath);
            Object.DestroyImmediate(source);
            return prefab;
        }
    }
}
#endif
