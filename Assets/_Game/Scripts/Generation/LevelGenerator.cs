using UnityEngine;
using SerapKeremGameKit._LevelSystem;
using SerapKeremGameKit._Logging;

namespace _Game.Generation
{
    /// <summary>
    /// Builds a playable <see cref="Level"/> at runtime from a <see cref="GeneratedLevel"/>.
    ///
    /// It instantiates the existing <c>Level_Base</c> scaffold prefab (which already contains the
    /// LineManager, Vector3ArrayPool, camera point, background and LINES parent) and populates the
    /// LINES parent with instances of the existing <c>Line (1)</c> prefab, setting each line's
    /// LineRenderer positions from the grid definition. The level is then handed back to the
    /// <see cref="SerapKeremGameKit._Managers.LevelManager"/>, which calls Load()/Play() as usual.
    /// </summary>
    public sealed class LevelGenerator : MonoBehaviour
    {
        [Header("Resource Paths (under a Resources folder)")]
        [SerializeField] private string _levelBaseResourcePath = "Levels/Level_Base";
        [SerializeField] private string _lineResourcePath = "Line/Line (1)";

        [Header("Layout")]
        [Tooltip("World units per grid cell. Keep >= 1 so parallel lines never falsely collide.")]
        [SerializeField] private float _cellSize = 1f;
        [SerializeField] private string _linesParentName = "LINES";

        private GameObject _levelBasePrefab;
        private GameObject _linePrefab;

        /// <summary>Builds and returns a fully-populated (but not yet Loaded) level instance.</summary>
        public Level BuildLevel(int levelNumber)
        {
            if (!EnsurePrefabs())
            {
                return null;
            }

            GeneratedLevel definition = ProceduralLevelBuilder.Build(levelNumber);

            GameObject levelObject = Instantiate(_levelBasePrefab);
            levelObject.name = $"GeneratedLevel_{levelNumber}";

            Level level = levelObject.GetComponent<Level>();
            if (level == null)
            {
                TraceLogger.LogError("Level_Base prefab is missing a Level component.", this);
                Destroy(levelObject);
                return null;
            }

            Transform linesParent = FindDeepChild(levelObject.transform, _linesParentName) ?? levelObject.transform;

            // Center the grid around the level origin.
            float offsetX = (definition.Width - 1) * 0.5f;
            float offsetY = (definition.Height - 1) * 0.5f;

            foreach (GeneratedLine spec in definition.Lines)
            {
                CreateLine(spec, linesParent, offsetX, offsetY);
            }

            return level;
        }

        private void CreateLine(GeneratedLine spec, Transform parent, float offsetX, float offsetY)
        {
            GameObject lineObject = Instantiate(_linePrefab, parent);
            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;

            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                TraceLogger.LogWarning("Line prefab is missing a LineRenderer; skipping line.", this);
                return;
            }

            Vector3 tail = new Vector3((spec.TailX - offsetX) * _cellSize, (spec.TailY - offsetY) * _cellSize, 0f);
            Vector3 head = new Vector3((spec.HeadX - offsetX) * _cellSize, (spec.HeadY - offsetY) * _cellSize, 0f);

            lineRenderer.useWorldSpace = false;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, tail);
            lineRenderer.SetPosition(1, head);
        }

        private bool EnsurePrefabs()
        {
            if (_levelBasePrefab == null)
            {
                _levelBasePrefab = Resources.Load<GameObject>(_levelBaseResourcePath);
            }
            if (_linePrefab == null)
            {
                _linePrefab = Resources.Load<GameObject>(_lineResourcePath);
            }

            if (_levelBasePrefab == null)
            {
                TraceLogger.LogError($"LevelGenerator: could not load Level_Base at Resources/{_levelBaseResourcePath}.", this);
            }
            if (_linePrefab == null)
            {
                TraceLogger.LogError($"LevelGenerator: could not load Line prefab at Resources/{_lineResourcePath}.", this);
            }

            return _levelBasePrefab != null && _linePrefab != null;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
