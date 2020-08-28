#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using ClassicUO.Game.GameObjects;
using ClassicUO.Input;


namespace ClassicUO.Game.Managers
{
    static class DelayedObjectClickManager
    {
        public static uint Serial { get; private set; }
        public static bool IsEnabled { get; private set; }
        public static uint Timer { get; private set; }
        public static int X { get; set; }
        public static int Y { get; set; }
        public static int LastMouseX { get; set; }
        public static int LastMouseY { get; set; }


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
            LastMouseX = Mouse.Position.X;
            LastMouseY = Mouse.Position.Y;
            X = x;
            Y = y;
            Timer = timer;
            IsEnabled = true;
        }

        public static void Clear()
        {
            IsEnabled = false;
            Serial = 0xFFFF_FFFF;
            Timer = 0;
        }

        public static void Clear(uint serial)
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