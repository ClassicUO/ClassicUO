using ClassicUO.Game.WorldObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game
{
    public static class MovementSpeed
    {
        const int STEP_DELAY_MOUNT_RUN = 100;
        const int STEP_DELAY_MOUNT_WALK = 200;
        const int STEP_DELAY_RUN = 200;
        const int STEP_DELAY_WALK = 400;


        public static int TimeToCompleteMovement(in Mobile mobile, in bool run)
        {
            if (mobile.IsMounted)
                return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;
            return run ? STEP_DELAY_RUN : STEP_DELAY_WALK;
        }
    }
}
