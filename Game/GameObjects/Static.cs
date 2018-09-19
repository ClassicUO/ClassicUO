#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Renderer;
using ClassicUO.Game.Views;
using ClassicUO.IO.Resources;
using ClassicUO.Interfaces;

namespace ClassicUO.Game.GameObjects
{
    public class Static : GameObject, IDynamicItem
    {
        //private StaticTiles? _itemData;

        public Static(Graphic tileID,  Hue hue,  int index) : base(World.Map)
        {
            Graphic = tileID;
            Hue = hue;
            Index = index;
        }

        public int Index { get; }
        //public new StaticView View => (StaticView)base.View;
        public string Name => ItemData.Name;
        public override Position Position { get; set; }

        public StaticTiles ItemData
        {
            get
            {
                //if (!_itemData.HasValue)
                //{
                //    _itemData = 
                //    Name = _itemData.Value.Name;
                //}

                //return _itemData.Value;
                return TileData.StaticData[Graphic];
            }
        }

        public bool IsAtWorld(int x,  int y)
        {
            return Position.X == x && Position.Y == y;
        }

        protected override View CreateView()
        {
            return new StaticView(this);
        }
    }
}