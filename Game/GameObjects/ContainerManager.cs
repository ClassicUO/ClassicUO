using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public static class ContainerManager
    {
        private static readonly ContainerData _default = new ContainerData(0x3C, 44, 65, 142, 94, 0x48);


        private static readonly Dictionary<Graphic, ContainerData> _data = new Dictionary<Graphic, ContainerData>
        {
            {0x3C, _default},

            {0x09, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0x2006, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECA, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECB, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECC, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECD, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECE, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xECF, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xED0, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xED1, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},
            {0xED2, new ContainerData(0x09, 20, 85, 104, 111, 0x42)},

            {0x3D, new ContainerData(0x3D, 29, 34, 108, 94, 0x48)},
            {0xE76, new ContainerData(0x3D, 29, 34, 108, 94, 0x48)},
            {0x2256, new ContainerData(0x3D, 29, 34, 108, 94, 0x48)},
            {0x2257, new ContainerData(0x3D, 29, 34, 108, 94, 0x48)},


            {0x3E, new ContainerData(0x3E, 33, 36, 109, 112, 0x42)},
            {0xE77, new ContainerData(0x3E, 33, 36, 109, 112, 0x42)},
            {0xE7F, new ContainerData(0x3E, 33, 36, 109, 112, 0x42)},

            {0x3F, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},
            {0xE7A, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},
            {0x24D5, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},
            {0x24D6, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},
            {0x24D9, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},
            {0x24DA, new ContainerData(0x3F, 19, 47, 163, 76, 0x4F)},


            {0x41, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x990, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x9AC, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x9B1, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x24D7, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x24D8, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},
            {0x24DD, new ContainerData(0x41, 35, 38, 110, 78, 0x4F)},


            {0x42, new ContainerData(0x42, 18, 105, 144, 73, 0x42)},
            {0xE40, new ContainerData(0x42, 18, 105, 144, 73, 0x42)},
            {0xE41, new ContainerData(0x42, 18, 105, 144, 73, 0x42)},

            {0x43, new ContainerData(0x43, 16, 51, 168, 73, 0x42)},
            {0xE7D, new ContainerData(0x43, 16, 51, 168, 73, 0x42)},
            {0x9AA, new ContainerData(0x43, 16, 51, 168, 73, 0x42)},


            {0x44, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0xE7E, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0x9A9, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0xE3C, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0xE3D, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0xE3E, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},
            {0xE3F, new ContainerData(0x44, 20, 10, 150, 90, 0x42)},

            {0x48, new ContainerData(0x48, 76, 12, 64, 56, 0x42)},
            {0xA30, new ContainerData(0x48, 76, 12, 64, 56, 0x42)},
            {0xA38, new ContainerData(0x48, 76, 12, 64, 56, 0x42)},

            {0x49, new ContainerData(0x49, 18, 105, 144, 73, 0x42)},
            {0xE42, new ContainerData(0x49, 18, 105, 144, 73, 0x42)},
            {0xE43, new ContainerData(0x49, 18, 105, 144, 73, 0x42)},


            {0x4A, new ContainerData(0x4A, 18, 105, 144, 73, 0x42)},
            {0xE7C, new ContainerData(0x4A, 18, 105, 144, 73, 0x42)},
            {0x9AB, new ContainerData(0x4A, 18, 105, 144, 73, 0x42)},


            {0x4B, new ContainerData(0x4B, 16, 51, 168, 73, 0x42)},
            {0xE80, new ContainerData(0x4B, 16, 51, 168, 73, 0x42)},
            {0x9A8, new ContainerData(0x4B, 16, 51, 168, 73, 0x42)},

            {0x4C, new ContainerData(0x4C, 46, 74, 150, 110, 0x42)},
            {0x3E65, new ContainerData(0x4C, 46, 74, 150, 110, 0x42)},
            {0x3E93, new ContainerData(0x4C, 46, 74, 150, 110, 0x42)},
            {0x3EAE, new ContainerData(0x4C, 46, 74, 150, 110, 0x42)},
            {0x3EB9, new ContainerData(0x4C, 46, 74, 150, 110, 0x42)},

            {0x4D, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA97, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA98, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA99, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA9A, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA9B, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA9C, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA9D, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},
            {0xA9E, new ContainerData(0x4D, 76, 12, 64, 56, 0x42)},

            {0x4E, new ContainerData(0x4E, 24, 96, 74, 56, 0x42)},
            {0xA4C, new ContainerData(0x4E, 24, 96, 74, 56, 0x42)},
            {0xA4D, new ContainerData(0x4E, 24, 96, 74, 56, 0x42)},
            {0xA50, new ContainerData(0x4E, 24, 96, 74, 56, 0x42)},
            {0xA51, new ContainerData(0x4E, 24, 96, 74, 56, 0x42)},

            {0x4F, new ContainerData(0x4F, 24, 96, 74, 56, 0x42)},
            {0xA4E, new ContainerData(0x4F, 24, 96, 74, 56, 0x42)},
            {0xA4F, new ContainerData(0x4F, 24, 96, 74, 56, 0x42)},
            {0xA52, new ContainerData(0x4F, 24, 96, 74, 56, 0x42)},
            {0xA53, new ContainerData(0x4F, 24, 96, 74, 56, 0x42)},

            {0x51, new ContainerData(0x51, 16, 10, 138, 84, 0x42)},
            {0xA2C, new ContainerData(0x51, 16, 10, 138, 84, 0x42)},
            {0xA34, new ContainerData(0x51, 16, 10, 138, 84, 0x42)},

            {0x52, new ContainerData(0x52, 0, 0, 110, 62, 0x42)},
            {0x1E5E, new ContainerData(0x52, 0, 0, 110, 62, 0x42)},


            {0x91A, new ContainerData(0x91A, 0, 0, 282, 210, 0xFFFF)},
            {0xFA6, new ContainerData(0x91A, 0, 0, 282, 210, 0xFFFF)},

            {0x102, new ContainerData(0x92E, 0, 0, 282, 230, 0xFFFF)},
            {0xE1C, new ContainerData(0x92E, 0, 0, 282, 230, 0xFFFF)},
            {0xFAD, new ContainerData(0x92E, 0, 0, 282, 230, 0xFFFF)},

            {0x105, new ContainerData(0x105, 10, 10, 150, 95, 0x42)},
            {0x2857, new ContainerData(0x105, 10, 10, 150, 95, 0x42)},
            {0x2858, new ContainerData(0x105, 10, 10, 150, 95, 0x42)},

            {0x106, new ContainerData(0x106, 10, 10, 150, 95, 0x42)},
            {0x285B, new ContainerData(0x106, 10, 10, 150, 95, 0x42)},
            {0x285C, new ContainerData(0x106, 10, 10, 150, 95, 0x42)},

            {0x107, new ContainerData(0x107, 10, 10, 150, 95, 0x42)},
            {0x285D, new ContainerData(0x107, 10, 10, 150, 95, 0x42)},
            {0x285E, new ContainerData(0x107, 10, 10, 150, 95, 0x42)},
            {0x2859, new ContainerData(0x107, 10, 10, 150, 95, 0x42)},
            {0x285A, new ContainerData(0x107, 10, 10, 150, 95, 0x42)},

            {0x108, new ContainerData(0x108, 10, 10, 116, 71, 0x4F)},
            {0x24DB, new ContainerData(0x108, 10, 10, 116, 71, 0x4F)},
            {0x24DC, new ContainerData(0x108, 10, 10, 116, 71, 0x4F)},

            {0x109, new ContainerData(0x109, 10, 10, 150, 95, 0x42)},
            {0x280B, new ContainerData(0x109, 10, 10, 150, 95, 0x42)},
            {0x280C, new ContainerData(0x109, 10, 10, 150, 95, 0x42)},

            {0x10A, new ContainerData(0x10A, 10, 10, 150, 95, 0x42)},
            {0x280F, new ContainerData(0x10A, 10, 10, 150, 95, 0x42)},
            {0x2810, new ContainerData(0x10A, 10, 10, 150, 95, 0x42)},

            {0x10B, new ContainerData(0x10B, 10, 10, 150, 95, 0x42)},
            {0x280D, new ContainerData(0x10B, 10, 10, 150, 95, 0x42)},
            {0x280E, new ContainerData(0x10B, 10, 10, 150, 95, 0x42)},

            {0x10C, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2811, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2812, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2815, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2816, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2817, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},
            {0x2818, new ContainerData(0x10C, 10, 10, 150, 95, 0x42)},

            {0x10D, new ContainerData(0x10D, 10, 10, 150, 95, 0x42)},
            {0x2813, new ContainerData(0x10D, 10, 10, 150, 95, 0x42)},
            {0x2814, new ContainerData(0x10D, 10, 10, 150, 95, 0x42)}
        };


        public static ContainerData Get(Graphic graphic) => !_data.TryGetValue(graphic, out ContainerData value) ? _default : value;
    }

    public class ContainerData
    {
        public ContainerData(Graphic graphic, int x, int y, int w, int h, ushort sound)
        {
            Graphic = graphic;
            Bounds = new Rectangle(x, y, w, h);
            DropSound = sound;
        }

        public Graphic Graphic { get; }
        public Rectangle Bounds { get; }
        public ushort DropSound { get; }
    }
}