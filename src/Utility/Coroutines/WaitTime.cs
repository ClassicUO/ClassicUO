using System;

namespace ClassicUO.Utility.Coroutines
{
    internal class WaitTime : WaitCondition<float>
    {
        public WaitTime(TimeSpan time) : base(
                                              s => s < Engine.Ticks,
                                              (float) Engine.Ticks + (uint) time.TotalMilliseconds)
        {
        }
    }
}