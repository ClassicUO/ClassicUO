using ClassicUO.Game.Renderer.Views;
using ClassicUO.IO.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.GameObjects
{
    public class Ground : GameObject
    {
        private readonly LandTiles _tileData;

        public Ground(Graphic tileID) : base(World.Map)
        {
            Graphic = tileID;
            //_tileData = TileData.LandData[Graphic & 0x3FFF];
        }

        public bool IsIgnored => Graphic < 3 || Graphic == 0x1DB || Graphic >= 0x1AE && Graphic <= 0x1B5;
        public bool IsStretched { get; set; }
        public LandTiles TileData => _tileData;

        //protected override View CreateView()
        //{
        //    return new TileView(this);
        //}


    }
}
