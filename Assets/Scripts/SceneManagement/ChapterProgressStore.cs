using UnityEngine;

namespace U1W.SceneManagement
{
    public static class ChapterProgressStore
    {
        private const string PlayerPrefsKeyPrefix = "ChapterProgress.Reached.";
        public static void MarkReached(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return;
            }

            string key = BuildKey(chapterId);
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        public static bool IsReached(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return false;
            }

            return PlayerPrefs.GetInt(BuildKey(chapterId), 0) == 1;
        }

        public static bool CanSelectFromTitle(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return false;
            }

            return IsReached(chapterId);
        }

        private static string BuildKey(string chapterId)
        {
            return PlayerPrefsKeyPrefix + chapterId;
        }
    }
}
