using _Game.ProceduralLevels;
using SerapKeremGameKit._Utilities;
using UnityEngine;

namespace _Game.UI
{
    public static class LevelProgress
    {
        public static int ActiveLevelNumber
        {
            get => PlayerPrefs.GetInt(PreferencesKeys.ProgressData, 1);
            set
            {
                PlayerPrefs.SetInt(PreferencesKeys.ProgressData, value);
                PlayerPrefs.Save();
            }
        }

        public static int TotalLevelCount => ProceduralLevelConstants.TotalLevelCount;
    }
}
