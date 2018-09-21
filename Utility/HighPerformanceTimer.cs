using System.Runtime.InteropServices;

namespace ClassicUO.Utility
{
    /// <summary>
    ///     A high resolution query performance timer.
    /// </summary>
    public class HighPerformanceTimer
    {
        #region Imported Methods

        /// <summary>
        ///     The current system ticks (count).
        /// </summary>
        /// <param name="lpPerformanceCount">Current performance count of the system.</param>
        /// <returns>False on failure.</returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        /// <summary>
        ///     Ticks per second (frequency) that the high performance counter performs.
        /// </summary>
        /// <param name="lpFrequency">Frequency the higher performance counter performs.</param>
        /// <returns>False if the high performance counter is not supported.</returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        #endregion

        #region Member Variables

        #endregion

        #region Methods

        public void Start()
        {
            // Record when the timer was started.
            StartTime = Counter;
        }

        public static double SecondsFromTicks(long ticks) => (double) ticks / Frequency;

        #endregion

        #region Static Properties

        private static readonly long frequency;

        static HighPerformanceTimer()
        {
            QueryPerformanceFrequency(out frequency);
        }

        /// <summary>
        ///     Gets the frequency that this HighPerformanceTimer performs at.
        /// </summary>
        public static long Frequency => frequency;

        /// <summary>
        ///     Gets the current system ticks.
        /// </summary>
        public static long Counter
        {
            get
            {
                long ticks = 0;
                QueryPerformanceCounter(out ticks);
                return ticks;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the tick count of when this HighPerformanceTimer was started.
        /// </summary>
        public long StartTime { get; private set; }

        public long ElapsedTicks => Counter - StartTime;

        public double ElapsedSeconds => (double) ElapsedTicks / Frequency;

        #endregion
    }
}