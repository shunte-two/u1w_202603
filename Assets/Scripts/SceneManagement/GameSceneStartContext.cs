namespace U1W.SceneManagement
{
    public static class GameSceneStartContext
    {
        private static string requestedChapterId;
        private static bool shouldSkipOpeningStory;

        public static void SetRequestedChapter(string chapterId)
        {
            requestedChapterId = string.IsNullOrWhiteSpace(chapterId) ? null : chapterId;
            shouldSkipOpeningStory = false;
        }

        public static void SetRequestedChapter(string chapterId, bool skipOpeningStory)
        {
            requestedChapterId = string.IsNullOrWhiteSpace(chapterId) ? null : chapterId;
            shouldSkipOpeningStory = requestedChapterId != null && skipOpeningStory;
        }

        public static bool TryConsumeRequestedChapter(out string chapterId)
        {
            chapterId = requestedChapterId;
            requestedChapterId = null;
            return !string.IsNullOrWhiteSpace(chapterId);
        }

        public static bool TryConsumeShouldSkipOpeningStory()
        {
            bool skipOpeningStory = shouldSkipOpeningStory;
            shouldSkipOpeningStory = false;
            return skipOpeningStory;
        }
    }
}
