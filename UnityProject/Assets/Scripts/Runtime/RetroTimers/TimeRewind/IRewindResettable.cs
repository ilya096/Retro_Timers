namespace RetroTimers.TimeRewind
{
    public interface IRewindResettable
    {
        void CaptureRewindStartState();
        void ResetForRewind();
    }
}
