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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Views;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public static class GameTextManager
    {
        private static readonly List<ViewWithDrawInfo> _views = new List<ViewWithDrawInfo>();

        public static void AddView(View view,  Vector3 position) => _views.Add(new ViewWithDrawInfo() { View = view, DrawPosition = position });

        public static void Render(SpriteBatchUI spriteBatch)
        {
            if (_views.Count > 0)
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    _views[i].View.Draw(spriteBatch, _views[i].DrawPosition);
                }

                _views.Clear();
            }
        }

        struct ViewWithDrawInfo
        {
            public View View;
            public Vector3 DrawPosition;
        }
    }
}
