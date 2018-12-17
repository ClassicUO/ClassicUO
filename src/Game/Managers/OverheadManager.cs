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
using System.Linq;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.Views;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Managers
{
    public class OverheadManager : IUpdateable, IDisposable
    {
        private readonly Dictionary<GameObject, Deque<DamageOverhead>> _damageOverheads = new Dictionary<GameObject, Deque<DamageOverhead>>();
        private readonly List<GameObject> _toRemoveDamages = new List<GameObject>();
        //private readonly List<Tuple<TextOverhead, Vector3>> _allOverheads = new List<Tuple<TextOverhead, Vector3>>();
        private readonly List<Static> _staticToUpdate = new List<Static>();

        private TextOverhead _firstNode;

        private void DrawTextOverheads(Batcher2D batcher, MouseOverList list)
        {
            if (_firstNode != null)
            {
                //for (int i = 0; i < _allOverheads.Count; i++)

                for (TextOverhead overhead = _firstNode; overhead != null; overhead = (TextOverhead) overhead.Right)
                {
                   // TextOverhead overhead = _allOverheads[i].Item1;
                    GameObject owner = overhead.Parent;

                    if (overhead.IsDisposed || owner.IsDisposed)
                    {
                        //_allOverheads.RemoveAt(i--);
                        continue;
                    }


                    Vector3 position = owner.RealScreenPosition; // _allOverheads[i].Item2;

                    if (owner is Mobile m)
                    {
                        GetAnimationDimensions(m, 0xFF, out int height, out int centerY);

                        position = new Vector3
                        {
                            X = position.X + m.Offset.X,
                            Y = position.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8),
                            Z = position.Z
                        };
                    }
                    //else if (owner is Static st)
                    //    position.Y -= st.ItemData.Height /*((ArtTexture)st.View.Texture).ImageRectangle.Height / 2*/;

                    View v = overhead.View;
                    Rectangle current = new Rectangle((int) position.X - v.Bounds.X, (int) position.Y - v.Bounds.Y, v.Bounds.Width, v.Bounds.Height);

                    for (TextOverhead ov = (TextOverhead) overhead.Right; ov != null; ov = (TextOverhead)ov.Right)
                    //for (int j = i + 1; j < _allOverheads.Count; j++)
                    {
                        //var a = _allOverheads[j];
                        //TextOverhead ov = a.Item1;
                        View b = ov.View;
                        Vector3 pos2 = ov.RealScreenPosition; // a.Item2;

                        if (ov.Parent is Mobile mm)
                        {
                            GetAnimationDimensions(mm, 0xFF, out int height, out int centerY);

                            pos2 = new Vector3
                            {
                                X = pos2.X + mm.Offset.X,
                                Y = pos2.Y + (mm.Offset.Y - mm.Offset.Z) - (height + centerY + 8),
                                Z = pos2.Z
                            };
                        }

                        Rectangle next = new Rectangle((int)pos2.X - b.Bounds.X, (int)pos2.Y - b.Bounds.Y, b.Bounds.Width, b.Bounds.Height);

                        if (overhead.IsOverlapped = current.Intersects(next))
                            break;
                    }

                    v.Draw(batcher, position, list);
                }

                //_allOverheads.Clear();

                GameObject last = _firstNode;

                while (last != null)
                {
                    GameObject temp = last.Right;

                    last.Left = null;
                    last.Right = null;

                    last = temp;
                }

                _firstNode = null;

            }
        }

        public void AddOverhead(TextOverhead overhead, Vector3 position)
        {
            if (overhead.Parent is Static st && !_staticToUpdate.Contains(st))
                _staticToUpdate.Add(st);


            overhead.Right = null;

            if (_firstNode == null)
            {
                overhead.Left = null;
                _firstNode = overhead;
            }
            else
            {
                var last = (GameObject) _firstNode;

                while (last.Right != null)
                    last = last.Right;

                last.Right = overhead;
                overhead.Left = last;
            }

            //_allOverheads.Add(Tuple.Create(overhead, position));
        }

        

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _staticToUpdate.Count; i++)
            {
                Static st = _staticToUpdate[i];
                st.Update(totalMS, frameMS);

                if (st.IsDisposed)
                    _staticToUpdate.RemoveAt(i--);
            }

            UpdateDamageOverhead(totalMS, frameMS);

            if (_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    _damageOverheads.Remove(s);
                });
                _toRemoveDamages.Clear();
            }

        }


        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int height, out int centerY)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;
            Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte)mobile.AnimIndex;
            Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out int x, out centerY, out int w, out height);
            if (x == 0 && centerY == 0 && w == 0 && height == 0) height = mobile.IsMounted ? 100 : 60;
        }

        public bool Draw(Batcher2D batcher, MouseOverList list, Point offset)
        {
            DrawTextOverheads(batcher, list);
          

            foreach (KeyValuePair<GameObject, Deque<DamageOverhead>> pair in _damageOverheads)
            {
                Mobile parent = (Mobile)pair.Key;
                var deque = pair.Value;

                Vector3 position = Vector3.Zero;
       
                position.X = parent.ScreenPosition.X - offset.X - 22;
                position.Y = parent.ScreenPosition.Y - offset.Y - 22;

                if (parent is Mobile m)
                {
                    GetAnimationDimensions(m, 0xFF, out int height, out int centerY);

                    position = new Vector3
                    {
                        X = position.X + m.Offset.X,
                        Y = position.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8),
                        Z = position.Z
                    };
                }
 
                foreach (DamageOverhead damageOverhead in deque)                  
                    damageOverhead.View.Draw(batcher, position, list);              
            }

            return true;
        }

        private void UpdateDamageOverhead(double totalMS, double frameMS)
        {
            foreach (KeyValuePair<GameObject, Deque<DamageOverhead>> pair in _damageOverheads)
            {
                Mobile parent = (Mobile) pair.Key;
                var deque = pair.Value;

                int offY = parent.IsMounted ? 0 : -22;

                for (int i = 0; i < deque.Count; i++)
                {
                    DamageOverhead obj = deque[i];
                    obj.Update(totalMS, frameMS);

                    if (obj.IsDisposed)
                    {                       
                        deque.RemoveAt(i--);
                    }
                    else
                    {
                        View v = obj.View;

                        v.Bounds.X = (v.Texture.Width >> 1) - 22;
                        v.Bounds.Y = offY + v.Texture.Height - obj.OffsetY;
                        v.Bounds.Width = v.Texture.Width;
                        v.Bounds.Height = v.Texture.Height;

                        offY += v.Texture.Height;
                    }
                }

                if (deque.Count == 0)
                    _toRemoveDamages.Add(parent);
            }
        }


        public void AddDamage(GameObject obj, DamageOverhead text)
        {
            if (!_damageOverheads.TryGetValue(obj, out Deque<DamageOverhead> deque) || deque == null)
            {
                deque = new Deque<DamageOverhead>(10);
                _damageOverheads[obj] = deque;
            }

            deque.AddToFront(text);

            if (deque.Count > 10)
                deque.RemoveFromBack();
        }

        public void Dispose()
        {
            foreach (var deque in _damageOverheads.Values)
            {
                foreach (DamageOverhead damageOverhead in deque)
                {
                    damageOverhead.Dispose();
                }
            }


            if (_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    _damageOverheads.Remove(s);
                });
                _toRemoveDamages.Clear();
            }


            //foreach (Tuple<TextOverhead, Vector3> tuple in _allOverheads)
            //{
            //    tuple.Item1.Dispose();
            //}

            //_allOverheads.Clear();

            GameObject last = _firstNode;

            while (last != null)
            {
                GameObject temp = last.Right;

                last.Dispose();

                last.Left = null;
                last.Right = null;

                last = temp;
            }

            _firstNode = null;

            _staticToUpdate.ForEach( s=> s.Dispose());
            _staticToUpdate.Clear();
        }
    }
}