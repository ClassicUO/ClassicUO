using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Coroutines
{
    class WaitTime : WaitCondition<float>
    {
        public WaitTime(TimeSpan time) : base(
                                           (s) => s - Engine.TicksFrame,
                                           (s) => s <= float.Epsilon,
                                           (float)time.TotalMilliseconds)
        { }
    }
}
