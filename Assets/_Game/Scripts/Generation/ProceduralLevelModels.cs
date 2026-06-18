using System;
using System.Collections.Generic;

namespace _Game.Generation
{
    /// <summary>
    /// Pure-C# (Unity-independent) data model describing a procedurally generated level.
    /// Coordinates are on an integer grid; the Unity layer converts these to world positions.
    /// A line is a straight, axis-aligned segment that the player consumes by tapping: its
    /// head sweeps forward along (DirX, DirY) until it leaves the board.
    /// </summary>
    public struct GeneratedLine
    {
        public int TailX;
        public int TailY;
        public int DirX; // one of -1, 0, 1 (exactly one axis non-zero)
        public int DirY;
        public int Length; // in grid units (>= 1); head = tail + dir * Length

        public int HeadX => TailX + DirX * Length;
        public int HeadY => TailY + DirY * Length;

        /// <summary>Returns true if this line's body occupies the given cell.</summary>
        public bool OccupiesCell(int x, int y)
        {
            for (int i = 0; i <= Length; i++)
            {
                if (TailX + DirX * i == x && TailY + DirY * i == y)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public sealed class GeneratedLevel
    {
        public int LevelNumber;
        public int Width;
        public int Height;
        public readonly List<GeneratedLine> Lines = new List<GeneratedLine>();
    }
}
