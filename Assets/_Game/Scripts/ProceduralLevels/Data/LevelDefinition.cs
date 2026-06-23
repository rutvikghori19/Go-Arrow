using System;
using System.Collections.Generic;

namespace _Game.ProceduralLevels
{
    [Serializable]
    public class LevelDefinition
    {
        public int LevelNumber;
        public ShapeType Shape;
        public DifficultyTier Tier;
        public int GridSize;
        public float CellSize = 1f;
        public int DifficultyScore;
        public int TargetLineCount;
        public List<LevelLineData> Lines = new List<LevelLineData>();

        public int LineCount => Lines?.Count ?? 0;
    }
}
