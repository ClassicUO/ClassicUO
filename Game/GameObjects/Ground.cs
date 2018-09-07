#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
