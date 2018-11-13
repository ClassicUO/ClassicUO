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

using ClassicUO.Game.Scenes;
using ClassicUO.Game.Views;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects.Managers
{
    public static class OverheadManager
    {
        private static readonly Dictionary<View, Vector3> _overheads = new Dictionary<View, Vector3>();
        private static readonly Dictionary<View, Vector3> _damages = new Dictionary<View, Vector3>();
        private static readonly List<View> _toRemove = new List<View>();

        private static readonly List<Tuple<View,Vector3>> _overheadsList = new List<Tuple<View, Vector3>>();


        public static void AddOverhead(View view, Vector3 position)
        {
            _overheads[view] = position;
        }

        public static void AddDamage(View view, Vector3 position)
        {
            _damages[view] = position;
        }


        public static void Draw(SpriteBatch3D spriteBatch, MouseOverList objectList)
        {
            DrawOverheads(spriteBatch, objectList);
            DrawDamages(spriteBatch, objectList);
        }

        private static void DrawOverheads(SpriteBatch3D spriteBatch, MouseOverList objectList)
        {
            if (_overheads.Count > 0)
            {

                //for (int i = 0; i < _overheadsList.Count; i++)
                //{
                //    var t = _overheadsList[i];

                //    View v = t.Item1;

                //    v.Draw(spriteBatch, t.Item2, objectList);
                //}

                //_overheadsList.Clear();

               // GameScene gs = Service.Get<SceneManager>().GetScene<GameScene>();

                foreach (KeyValuePair<View, Vector3> info in _overheads)
                {
                    if (!info.Key.Draw(spriteBatch, info.Value, objectList) || info.Key.GameObject.IsDisposed)
                        _toRemove.Add(info.Key);
                }

                if (_toRemove.Count > 0)
                {
                    _toRemove.ForEach(s => _overheads.Remove(s));
                    _toRemove.Clear();
                }
            }
        }

        private static void DrawDamages(SpriteBatch3D spriteBatch, MouseOverList objectList)
        {
            if (_damages.Count > 0)
            {
                foreach (KeyValuePair<View, Vector3> damage in _damages)
                {
                    if (!damage.Key.Draw(spriteBatch, damage.Value, objectList) || damage.Key.GameObject.IsDisposed)
                        _toRemove.Add(damage.Key);
                }

                if (_toRemove.Count > 0)
                {
                    _toRemove.ForEach(s => _damages.Remove(s));
                    _toRemove.Clear();
                }
            }
        }

    }
}