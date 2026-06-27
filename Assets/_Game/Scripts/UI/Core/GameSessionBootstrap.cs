using UnityEngine;

namespace _Game.UI
{
    public static class GameSessionBootstrap
    {
        const string PendingLevelKey = "goarrow.pending_level";
        const int NoPendingLevel = -1;

        public static bool ShouldStartLevelOnLoad { get; private set; } = true;
        public static int PendingLevelNumber { get; private set; } = NoPendingLevel;

        public static void RequestContinue()
        {
            ShouldStartLevelOnLoad = true;
            PendingLevelNumber = NoPendingLevel;
        }

        public static void RequestPlay(int levelNumber)
        {
            ShouldStartLevelOnLoad = true;
            PendingLevelNumber = levelNumber;
            PlayerPrefs.SetInt(PendingLevelKey, levelNumber);
            PlayerPrefs.Save();
        }

        public static void ClearAfterLoad()
        {
            ShouldStartLevelOnLoad = false;
            PendingLevelNumber = NoPendingLevel;
            PlayerPrefs.DeleteKey(PendingLevelKey);
        }

        public static void PrepareEditorPlayInGameScene()
        {
            ShouldStartLevelOnLoad = true;
            PendingLevelNumber = NoPendingLevel;
        }

        public static void ResetForMenu()
        {
            ShouldStartLevelOnLoad = false;
            PendingLevelNumber = NoPendingLevel;
            PlayerPrefs.DeleteKey(PendingLevelKey);
        }
    }
}
