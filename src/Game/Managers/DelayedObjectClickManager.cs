using ClassicUO.Game.GameObjects;


namespace ClassicUO.Game.Managers
{
    static class DelayedObjectClickManager
    {
        public static uint Serial { get; private set; }
        public static bool IsEnabled { get; private set; }
        public static uint Timer { get; private set; }
        public static int X { get; private set; }
        public static int Y { get; private set; }



        public static void Update()
        {
            if (!IsEnabled || Timer > Time.Ticks)
                return;

            Entity entity = World.Get(Serial);

            if (entity != null)
            {
                if (!World.ClientFeatures.TooltipsEnabled || 
                    (SerialHelper.IsItem(Serial) &&
                    ((Item) entity).IsLocked &&
                    ((Item) entity).ItemData.Weight == 255 &&
                    !((Item) entity).ItemData.IsContainer))
                {
                    GameActions.SingleClick(Serial);
                }

                if (World.ClientFeatures.PopupEnabled)
                {
                    GameActions.OpenPopupMenu(Serial);
                }
            }

            Clear();
        }

        public static void Set(uint serial, int x, int y, uint timer)
        {
            Serial = serial;
            X = x;
            Y = y;
            Timer = timer;
            IsEnabled = true;
        }

        public static void Clear(uint serial = 0)
        {
            if (Serial == serial || serial == 0)
            {
                Timer = 0;
                Serial = 0;
                IsEnabled = false;
            }
        }
    }
}
