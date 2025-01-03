// SPDX-License-Identifier: BSD-2-Clause

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Data
{
    internal struct ContainerData
    {
        public ContainerData
        (
            ushort graphic,
            ushort sound,
            ushort closed,
            int x,
            int y,
            int w,
            int h,
            ushort iconizedgraphic = 0,
            int minimizerX = 0,
            int minimizerY = 0
        )
        {
            Graphic = graphic;
            Bounds = new Rectangle(x, y, w, h);
            OpenSound = sound;
            ClosedSound = closed;

            MinimizerArea = minimizerX == 0 && minimizerY == 0 ? Rectangle.Empty : new Rectangle(minimizerX, minimizerY, 16, 16);

            IconizedGraphic = iconizedgraphic;
        }

        public ushort Graphic;
        public Rectangle Bounds;
        public ushort OpenSound;
        public ushort ClosedSound;
        public Rectangle MinimizerArea;
        public ushort IconizedGraphic;
    }
}