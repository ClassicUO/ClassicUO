// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;
using ClassicUO.Input;

namespace ClassicUO.Game.Managers
{
    internal sealed class DelayedObjectClickManager
    {
        private readonly World _world;

        public DelayedObjectClickManager(World world) { _world = world; }

        public uint Serial { get; private set; }
        public bool IsEnabled { get; private set; }
        public uint Timer { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int LastMouseX { get; set; }
        public int LastMouseY { get; set; }


        public void Update()
        {
            if (!IsEnabled || Timer > Time.Ticks)
            {
                return;
            }

            Entity entity = _world.Get(Serial);

            if (entity != null)
            {
                if (!_world.ClientFeatures.TooltipsEnabled || SerialHelper.IsItem(Serial) && ((Item) entity).IsLocked && ((Item) entity).ItemData.Weight == 255 && !((Item) entity).ItemData.IsContainer)
                {
                    GameActions.SingleClick(_world, Serial);
                }

                if (_world.ClientFeatures.PopupEnabled)
                {
                    GameActions.OpenPopupMenu(Serial);
                }
            }

            Clear();
        }

        public void Set(uint serial, int x, int y, uint timer)
        {
            Serial = serial;
            LastMouseX = Mouse.Position.X;
            LastMouseY = Mouse.Position.Y;
            X = x;
            Y = y;
            Timer = timer;
            IsEnabled = true;
        }

        public void Clear()
        {
            IsEnabled = false;
            Serial = 0xFFFF_FFFF;
            Timer = 0;
        }

        public void Clear(uint serial)
        {
            if (Serial == serial)
            {
                Timer = 0;
                Serial = 0;
                IsEnabled = false;
                X = 0;
                Y = 0;
                LastMouseX = 0;
                LastMouseY = 0;
            }
        }
    }
}