namespace RetroTimers.TimeRewind
{
    public static class TimeRewindRules
    {
        public static float CalculatePlayerControlDelay(float cloneSpawnDelaySeconds, int completedRewindCount)
        {
            if (cloneSpawnDelaySeconds < 0f)
            {
                cloneSpawnDelaySeconds = 0f;
            }

            if (completedRewindCount < 0)
            {
                completedRewindCount = 0;
            }

            return cloneSpawnDelaySeconds * completedRewindCount;
        }

        public static float CalculateRemainingActiveTime(float levelTimeLimitSeconds, float playerControlDelay)
        {
            return levelTimeLimitSeconds - playerControlDelay;
        }

        public static bool HasEnoughActiveTime(float levelTimeLimitSeconds, float playerControlDelay, float minimumActiveTimeAfterDelay)
        {
            return CalculateRemainingActiveTime(levelTimeLimitSeconds, playerControlDelay) >= minimumActiveTimeAfterDelay;
        }

        public static bool CanRequestManualRewind(bool levelEnded, bool controlledActorAlive, bool deathAlreadyFixed)
        {
            return !levelEnded && controlledActorAlive && !deathAlreadyFixed;
        }
    }
}
