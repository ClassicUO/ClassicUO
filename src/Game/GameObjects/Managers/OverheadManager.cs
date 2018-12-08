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
using System;
using System.Collections.Generic;

using ClassicUO.Game.Views;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects.Managers
{
    public static class OverheadManager
    {
        private static readonly List<OverHeadInfo> _overheadsList = new List<OverHeadInfo>();
        private static readonly List<OverHeadInfo> _damagesList = new List<OverHeadInfo>();

      
        public static void AddOverhead(View view, Vector3 position)
        {
            _overheadsList.Add(new OverHeadInfo(view, position));
        }

        public static void AddDamage(View view, Vector3 position)
        {
            _damagesList.Add(new OverHeadInfo(view, position));
        }

        public static void Draw(Batcher2D batcher, MouseOverList objectList)
        {
            DrawOverheads(batcher, objectList);
            DrawDamages(batcher, objectList);
        }

        private static void DrawOverheads(Batcher2D batcher, MouseOverList objectList)
        {
            if (_overheadsList.Count > 0)
            {
                for (int i = 0; i < _overheadsList.Count; i++)
                {
                    OverHeadInfo t = _overheadsList[i];
                    View view = t.View;

                    Rectangle rect0 = new Rectangle((int)t.Position.X - view.Bounds.X, (int)t.Position.Y - view.Bounds.Y, view.Bounds.Width, view.Bounds.Height);

                    for (int j = i + 1; j < _overheadsList.Count; j++)
                    {
                        var a = _overheadsList[j];
                        View b = a.View;

                        Rectangle rect1 = new Rectangle((int)a.Position.X - b.Bounds.X, (int)a.Position.Y - b.Bounds.Y, b.Bounds.Width, b.Bounds.Height);

                        if ((((TextOverhead)view.GameObject).IsOverlapped = rect0.InRect(rect1)))
                            break;
                    }

                    view.Draw(batcher, t.Position, objectList);
                }

                _overheadsList.Clear();
            }
        }

        private static void DrawDamages(Batcher2D batcher, MouseOverList objectList)
        {
            if (_damagesList.Count > 0)
            {
                for (int i = 0; i < _damagesList.Count; i++)
                {
                    var t = _damagesList[i];
                    t.View.Draw(batcher, t.Position, objectList);
                }

                _damagesList.Clear();
            }
        }


        private struct OverHeadInfo
        {
            public OverHeadInfo(View view, Vector3 pos)
            {
                View = view;
                Position = pos;
            }

            public readonly View View;
            public readonly Vector3 Position;
        }

    }
}