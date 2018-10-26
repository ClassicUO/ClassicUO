namespace ClassicUO.Utility.Logging
{
    internal class Log
    {
        private static Logger logger;

        public static void Start(LogTypes logTypes, LogFile logFile = null)
        {
            logger = logger ?? new Logger {LogTypes = logTypes};
            logger?.Start(logFile);
        }

        public static void Stop()
        {
            logger?.Stop();
            logger = null;
        }

        public static void Resume(LogTypes logTypes)
        {
            logger.LogTypes = logTypes;
        }

        public static void Pause()
        {
            logger.LogTypes = LogTypes.None;
        }

        public static void Message(LogTypes logType, string text)
        {
            logger.Message(logType, text);
        }

        public static void NewLine()
        {
            logger.NewLine();
        }

        public static void WaitForKey()
        {
            logger.WaitForKey();
        }

        public static void Clear()
        {
            logger.Clear();
        }
    }
}