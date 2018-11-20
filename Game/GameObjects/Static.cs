#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using ClassicUO.Game.GameObjects.Managers;
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    public class Static : GameObject, IDynamicItem
    {
        private GameEffect _effect;

        public Static(Graphic tileID, Hue hue, int index)
        {
            Graphic = tileID;
            Hue = hue;
            Index = index;
        }

        public int Index { get; }

        public string Name => ItemData.Name;

        public StaticTiles ItemData => TileData.StaticData[Graphic];

        public GameEffect Effect
        {
            get => _effect;
            set
            {
                _effect?.Dispose();
                _effect = value;
                if (_effect != null) Service.Get<StaticManager>().Add(this);
            }
        }

        public bool IsAtWorld(int x, int y)
        {
            return Position.X == x && Position.Y == y;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Effect != null)
            {
                if (Effect.IsDisposed)
                    Effect = null;
                else
                    Effect.Update(totalMS, frameMS);
            }
        }

        public override void Dispose()
        {
            Effect?.Dispose();
            Effect = null;
            base.Dispose();
        }

        protected override View CreateView()
        {
            return new StaticView(this);
        }
    }
}