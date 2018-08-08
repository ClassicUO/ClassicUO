using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    public static class MovementSpeed
    {
        private const int STEP_DELAY_MOUNT_RUN = 100;
        private const int STEP_DELAY_MOUNT_WALK = 200;
        private const int STEP_DELAY_RUN = 200;
        private const int STEP_DELAY_WALK = 400;


        public static int TimeToCompleteMovement(in Mobile mobile, in bool run)
        {
            if (mobile.IsMounted)
                return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;
            return run ? STEP_DELAY_RUN : STEP_DELAY_WALK;
        }
    }
}