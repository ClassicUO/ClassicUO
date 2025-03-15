using System;
using System.Collections.Generic;

namespace ClassicUO.Utility
{
    public class AverageOverTime
    {
        private readonly TimeSpan _timeWindow;
        private readonly Queue<(uint Timestamp, double Value)> _values;
        private double _sum;

        public AverageOverTime(TimeSpan timeWindow)
        {
            _timeWindow = timeWindow;
            _values = new Queue<(uint, double)>();
            _sum = 0;
        }

        public double LastAverage { get; private set; }
        public double LastAveragePerSecond { get; private set; }

        /// <summary>
        /// Adds a new number with the current timestamp.
        /// </summary>
        /// <param name="value">The number to add.</param>
        public void AddValue(uint currentTicks, double value)
        {
            _values.Enqueue((currentTicks, value));
            _sum += value;

            RemoveOldValues(currentTicks);

            LastAverage = _values.Count > 0 ? _sum / _values.Count : 0;
            LastAveragePerSecond = AveragePerSecond(currentTicks);
        }

        /// <summary>
        /// Removes numbers from the queue that fall outside the time window.
        /// </summary>
        /// <param name="currentTicks">The current time used for comparison.</param>
        private void RemoveOldValues(uint currentTicks)
        {
            while (_values.Count > 0 && (currentTicks - _values.Peek().Timestamp) > (currentTicks - _timeWindow.TotalMilliseconds))
            {
                var oldItem = _values.Dequeue();
                _sum -= oldItem.Value;
            }
        }

        /// <summary>
        /// Gets the current average of the numbers within the time window.
        /// </summary>
        public double Average(uint currentTicks)
        {
            RemoveOldValues(currentTicks);
            return _values.Count > 0 ? _sum / _values.Count : 0;
        }

        public double AveragePerSecond(uint currentTicks)
        {
            if (_values.Count == 0)
                return 0;

            uint oldestTimestamp = _values.Peek().Timestamp;
            double elapsedMilliseconds = currentTicks - oldestTimestamp;
            elapsedMilliseconds = Math.Max(elapsedMilliseconds, 1000);
            double elapsedSeconds = elapsedMilliseconds / 1000.0;

            return _sum / elapsedSeconds;
        }
    }
}