namespace ClassicUO.Utility.Logging
{
    internal class Log
    {
        private static Logger _logger;

        public static void Start(LogTypes logTypes, LogFile logFile = null)
        {
            _logger = _logger ?? new Logger
            {
                LogTypes = logTypes
            };
            _logger?.Start(logFile);
        }

        public static void Stop()
        {
            _logger?.Stop();
            _logger = null;
        }

        public static void Resume(LogTypes logTypes)
        {
            _logger.LogTypes = logTypes;
        }

        public static void Pause()
        {
            _logger.LogTypes = LogTypes.None;
        }

        public static void Message(LogTypes logType, string text)
        {
            _logger.Message(logType, text);
        }

        public static void NewLine()
        {
            _logger.NewLine();
        }

        public static void Clear()
        {
            _logger.Clear();
        }
    }
}