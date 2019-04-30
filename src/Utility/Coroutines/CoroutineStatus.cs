namespace ClassicUO.Utility.Coroutines
{
    public enum CoroutineStatus : byte
    {
        Paused,
        Running,
        Complete,
        Cancelled,
        Disposed,
        Error
    }
}