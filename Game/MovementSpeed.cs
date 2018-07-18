using ClassicUO.Game.WorldObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public static class MovementSpeed
    {
        const double TIME_WALK_FOOT = (8d / 20d) * 1000d;
        const double TIME_RUN_FOOT = (4d / 20d) * 1000d;
        const double TIME_WALK_MOUNT = (4d / 20d) * 1000d;
        const double TIME_RUN_MOUNT = (2d / 20d) * 1000d;


        public static double TimeToCompleteMovement(in Mobile mobile, in Direction direction)
        {
            bool isrunning = (direction & Direction.Running) == Direction.Running;

            if (mobile.IsMounted)
                return isrunning ? TIME_RUN_MOUNT : TIME_WALK_MOUNT;
            return isrunning ? TIME_RUN_FOOT : TIME_WALK_FOOT;
        }
    }
}
