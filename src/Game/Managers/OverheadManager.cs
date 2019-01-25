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
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.Managers
{
    internal class OverheadManager : IUpdateable, IDisposable
    {
        private readonly Dictionary<GameObject, Deque<DamageOverhead>> _damageOverheads = new Dictionary<GameObject, Deque<DamageOverhead>>();
        private readonly List<GameObject> _toRemoveDamages = new List<GameObject>();
        private readonly List<GameObject> _staticToUpdate = new List<GameObject>();

        private TextOverhead _firstNode;

        private void DrawTextOverheads(Batcher2D batcher, MouseOverList list)
        {
            if (_firstNode != null)
            {
                int skip = 0;

                for (TextOverhead overhead = _firstNode; overhead != null; overhead = (TextOverhead) overhead.Right)
                {
                    GameObject owner = overhead.Parent;

                    if (overhead.IsDisposed || owner.IsDisposed)
                    {
                        continue;
                    }

                    Vector3 position = owner.RealScreenPosition; 

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

                    Rectangle current = new Rectangle((int) position.X - overhead.Bounds.X, (int) position.Y - overhead.Bounds.Y, overhead.Bounds.Width, overhead.Bounds.Height);

                    if (skip == 0)
                    {
                        for (TextOverhead ov = (TextOverhead) overhead.Right; ov != null; ov = (TextOverhead) ov.Right)
                        {
                            Vector3 pos2 = ov.Parent.RealScreenPosition;

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

                            Rectangle next = new Rectangle((int) pos2.X - ov.Bounds.X, (int) pos2.Y - ov.Bounds.Y, ov.Bounds.Width, ov.Bounds.Height);

                            overhead.IsOverlapped = current.Intersects(next);

                            if (overhead.IsOverlapped)
                            {
                                bool startSkip = false;
                                foreach (TextOverhead parentOverhead in owner.Overheads)
                                {
                                    parentOverhead.IsOverlapped = true;

                                    if (parentOverhead == overhead)
                                    {
                                        startSkip = true;
                                    }
                                    else if (startSkip)
                                        skip++;
                                }

                                break;
                            }
                        }
                    }
                    else
                        skip--;

                    overhead.Draw(batcher, position, list);
                }


                GameObject last = _firstNode;

                while (last != null)
                {
                    GameObject temp = last.Right;

                    last.Left = null;
                    last.Right = null;

                    last = temp;
                }

                _firstNode.Left = _firstNode.Right = null;
                _firstNode = null;

            }
        }

        public void AddOverhead(TextOverhead overhead, Vector3 position)
        {
            if ((overhead.Parent is Static || overhead.Parent is Multi) && !_staticToUpdate.Contains(overhead.Parent))
                _staticToUpdate.Add(overhead.Parent);

            overhead.Left = overhead.Right = null;

            if (_firstNode == null)
            {
                _firstNode = overhead;
            }
            else
            {
                GameObject last = _firstNode;

                while (last.Right != null)
                    last = last.Right;

                last.Right = overhead;
                overhead.Right = null;
            }
        }

        

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _staticToUpdate.Count; i++)
            {
                var st = _staticToUpdate[i];
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
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte)mobile.AnimIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out int x, out centerY, out int w, out height);
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
                    damageOverhead.Draw(batcher, position, list);              
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
                        obj.Bounds.X = (obj.Texture.Width >> 1) - 22;
                        obj.Bounds.Y = offY + obj.Texture.Height - obj.OffsetY;
                        obj.Bounds.Width = obj.Texture.Width;
                        obj.Bounds.Height = obj.Texture.Height;

                        offY += obj.Texture.Height;
                    }
                }

                if (deque.Count == 0)
                    _toRemoveDamages.Add(parent);
            }
        }


        internal void AddDamage(GameObject obj, DamageOverhead text)
        {
            if (!_damageOverheads.TryGetValue(obj, out Deque<DamageOverhead> deque) || deque == null)
            {
                deque = new Deque<DamageOverhead>();
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