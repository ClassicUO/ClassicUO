using System;

namespace ClassicUO.Utility.Coroutines
{
    internal class WaitTime : WaitCondition<float>
    {
        public WaitTime(TimeSpan time) : base(
                                              s => s < Time.Ticks,
                                              (float) Time.Ticks + (uint) time.TotalMilliseconds)
        {
        }
    }
}