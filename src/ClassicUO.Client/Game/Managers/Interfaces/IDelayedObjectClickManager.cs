// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Managers
{
    internal interface IDelayedObjectClickManager
    {
        uint Serial { get; }

        bool IsEnabled { get; }

        uint Timer { get; }

        int X { get; set; }

        int Y { get; set; }

        int LastMouseX { get; set; }

        int LastMouseY { get; set; }

        void Update();

        void Set(uint serial, int x, int y, uint timer);

        void Clear();

        void Clear(uint serial);
    }
}
