#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoArrow.Editor
{
    [InitializeOnLoad]
    internal static class CursorAgentGameObjectCreator
    {
        const string GameObjectName = "cursor agent";

        static CursorAgentGameObjectCreator()
        {
            EditorApplication.delayCall += TryCreate;
        }

        [MenuItem("GameObject/Cursor Agent/Create GameObject")]
        static void CreateFromMenu()
        {
            CreateIfMissing(select: true);
        }

        static void TryCreate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            CreateIfMissing(select: true);
        }

        static void CreateIfMissing(bool select)
        {
            if (GameObject.Find(GameObjectName) != null)
                return;

            var gameObject = new GameObject(GameObjectName);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {GameObjectName}");

            if (select)
                Selection.activeGameObject = gameObject;

            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"Created GameObject '{GameObjectName}' in scene '{scene.name}'.");
        }
    }
}
#endif
