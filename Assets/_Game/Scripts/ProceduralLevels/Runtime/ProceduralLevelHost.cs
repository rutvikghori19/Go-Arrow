using GameLine = _Game.Line.Line;
using SerapKeremGameKit._LevelSystem;
using UnityEngine;

namespace _Game.ProceduralLevels
{
    [DisallowMultipleComponent]
    public class ProceduralLevelHost : MonoBehaviour
    {
        [SerializeField] private Level _level;
        [SerializeField] private Transform _linesParent;
        [SerializeField] private GameLine _linePrefab;

        public int BuiltLevelNumber { get; private set; }
        public LevelDefinition CurrentDefinition { get; private set; }

        public void Build(int levelNumber)
        {
            BuiltLevelNumber = levelNumber;
            ResolveReferences();

            CurrentDefinition = ProceduralLevelCache.GetOrGenerate(levelNumber);
            if (CurrentDefinition == null || _linesParent == null || _linePrefab == null)
                return;

            ProceduralLevelBuilder.Build(CurrentDefinition, _linesParent, _linePrefab);
        }

        void ResolveReferences()
        {
            if (_level == null)
                _level = GetComponent<Level>();

            if (_linesParent == null)
            {
                var lines = transform.Find("LINES");
                if (lines != null)
                    _linesParent = lines;
            }

            if (_linePrefab == null)
                _linePrefab = Resources.Load<GameLine>("Line/Line (1)");
        }
    }
}
