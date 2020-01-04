using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    internal readonly struct ContainerData
    {
        public ContainerData(ushort graphic, ushort sound, ushort closed, int x, int y, int w, int h, ushort iconizedgraphic = 0, int minimizerX = 0, int minimizerY = 0)
        {
            Graphic = graphic;
            Bounds = new Rectangle(x, y, w, h);
            OpenSound = sound;
            ClosedSound = closed;
            MinimizerArea = (minimizerX == 0 && minimizerY == 0 ? Rectangle.Empty : new Rectangle(minimizerX, minimizerY, 16, 16));
            IconizedGraphic = iconizedgraphic;
        }

        public readonly ushort Graphic;
        public readonly Rectangle Bounds;
        public readonly ushort OpenSound;
        public readonly ushort ClosedSound;
        public readonly Rectangle MinimizerArea;
        public readonly ushort IconizedGraphic;
    }
}
