// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal readonly struct PopupMenuItem
    {
        public PopupMenuItem(int cliloc, ushort index, ushort hue, ushort replaced, ushort flags)
        {
            Cliloc = cliloc;
            Index = index;
            Hue = hue;
            ReplacedHue = replaced;
            Flags = flags;
        }

        public readonly int Cliloc;
        public readonly ushort Index;
        public readonly ushort Hue, ReplacedHue;
        public readonly ushort Flags;
    }
}