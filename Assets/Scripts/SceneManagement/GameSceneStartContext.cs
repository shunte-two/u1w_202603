namespace U1W.SceneManagement
{
    public static class GameSceneStartContext
    {
        private static string requestedChapterId;

        public static void SetRequestedChapter(string chapterId)
        {
            requestedChapterId = string.IsNullOrWhiteSpace(chapterId) ? null : chapterId;
        }

        public static bool TryConsumeRequestedChapter(out string chapterId)
        {
            chapterId = requestedChapterId;
            requestedChapterId = null;
            return !string.IsNullOrWhiteSpace(chapterId);
        }
    }
}
