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

        public static float CalculateSpawnProgressNormalized(float remainingSpawnDelay, float totalSpawnDelay)
        {
            if (totalSpawnDelay <= 0f)
            {
                return 1f;
            }

            return 1f - Clamp01(remainingSpawnDelay / totalSpawnDelay);
        }

        public static float CalculateCloneAgeAlpha(float newestAlpha, float alphaStep, float minimumAlpha, int ageFromNewest)
        {
            if (ageFromNewest < 0)
            {
                ageFromNewest = 0;
            }

            float alpha = newestAlpha - alphaStep * ageFromNewest;
            if (alpha < minimumAlpha)
            {
                alpha = minimumAlpha;
            }

            return Clamp01(alpha);
        }

        public static bool CanRequestManualRewind(bool levelEnded, bool controlledActorAlive, bool deathAlreadyFixed)
        {
            return !levelEnded && controlledActorAlive && !deathAlreadyFixed;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }
}
