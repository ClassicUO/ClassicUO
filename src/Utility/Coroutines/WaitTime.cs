using System;

namespace ClassicUO.Utility.Coroutines
{
    internal class WaitTime : WaitCondition<float>
    {
        public WaitTime(TimeSpan time) : base(
                                              s => s - Engine.TicksFrame,
                                              s => s <= float.Epsilon,
                                              (float) time.TotalMilliseconds)
        {
        }
    }
}