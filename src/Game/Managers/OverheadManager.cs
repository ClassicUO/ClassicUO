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
        private readonly Dictionary<GameObject, Deque<TextOverhead>> _textOverheads = new Dictionary<GameObject, Deque<TextOverhead>>();
        private readonly Dictionary<GameObject, Deque<DamageOverhead>> _damageOverheads = new Dictionary<GameObject, Deque<DamageOverhead>>();
        private readonly List<GameObject> _toRemoveDamages = new List<GameObject>();
        private readonly List<GameObject> _toRemoveText = new List<GameObject>();

        //private readonly Dictionary<GameObject, Vector3> _bounds = new Dictionary<GameObject, Vector3>();

        public void Update(double totalMS, double frameMS)
        {
            UpdateTextOverhead(totalMS, frameMS);
            UpdateDamageOverhead(totalMS, frameMS);

            if (_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    //if (!_textOverheads.ContainsKey(s))
                    //    _bounds.Remove(s);
                    _damageOverheads.Remove(s);
                });
                _toRemoveDamages.Clear();
            }

            if (_toRemoveText.Count > 0)
            {
                _toRemoveText.ForEach(s =>
                {
                    //if (!_damageOverheads.ContainsKey(s))
                    //    _bounds.Remove(s);
                    _textOverheads.Remove(s);
                });
                _toRemoveText.Clear();
            }
        }

        public bool HasOverhead(GameObject obj) => _textOverheads.ContainsKey(obj);

        public bool HasDamage(GameObject obj) => _damageOverheads.ContainsKey(obj);

        public Deque<TextOverhead> GeTextOverheads(GameObject obj)
        {
            return _textOverheads[obj];
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
            List<Rectangle> rectangles = new List<Rectangle>();

            foreach (KeyValuePair<GameObject, Deque<TextOverhead>> pair in _textOverheads)
            {
                GameObject parent = pair.Key;
                var deque = pair.Value;

                if (parent.IsDisposed)
                {
                    continue;
                }

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
                else if (parent is Static st)
                    position.Y -= st.ItemData.Height /*((ArtTexture)st.View.Texture).ImageRectangle.Height / 2*/;
                

                foreach (TextOverhead textOverhead in deque)
                {
                    View v = textOverhead.View;
                    Rectangle next = new Rectangle((int)position.X - v.Bounds.X, (int)position.Y - v.Bounds.Y, v.Bounds.Width, v.Bounds.Height);
                    textOverhead.IsOverlapped = rectangles.Any(s => s.Intersects(next));
                    rectangles.Add(next);

                    textOverhead.View.Draw(batcher, position, list);
                }
            }


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


        //public void UpdatePosition(GameObject obj, Vector3 position)
        //    => _bounds[obj] = position;

        private void UpdateTextOverhead(double totalMS, double frameMS)
        {
            //List<Rectangle> rectangles = new List<Rectangle>();

            foreach (KeyValuePair<GameObject, Deque<TextOverhead>> pair in _textOverheads)
            {
                GameObject parent = pair.Key;
                Deque<TextOverhead> deque = pair.Value;

                if (parent.IsDisposed)
                {
                    foreach (TextOverhead overhead in deque)
                        overhead.Dispose();
                    deque.Clear();
                }
                else
                {
                    int offY = parent is Mobile mob && mob.IsMounted ? 0 : -22;


                    for (int i = 0; i < deque.Count; i++)
                    {
                        TextOverhead obj = deque[i];
                        obj.Update(totalMS, frameMS);

                        if (obj.IsDisposed)
                            deque.RemoveAt(i--);
                        else
                        {
                            View v = obj.View;

                            v.Bounds.X = (v.Texture.Width >> 1) - 22;
                            v.Bounds.Y = offY + v.Texture.Height;
                            v.Bounds.Width = v.Texture.Width;
                            v.Bounds.Height = v.Texture.Height;

                            offY += v.Texture.Height;

                            //Vector3 position = Vector3.Zero;
                            
                            //position.X = parent.ScreenPosition.X - offset.X - 22;
                            //position.Y = parent.ScreenPosition.Y - offset.Y - 22;

                            //if (parent is Mobile m)
                            //{
                            //    GetAnimationDimensions(m, 0xFF, out int height, out int centerY);

                            //    position = new Vector3
                            //    {
                            //        X = position.X + m.Offset.X,
                            //        Y = position.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8),
                            //        Z = position.Z
                            //    };
                            //    //_bounds[m] = position;
                            //}

                            //if (_bounds.TryGetValue(parent, out Vector3 position))
                            //{
                            //    int aX = (int) position.X - v.Bounds.X;
                            //    int aY = (int) position.Y - v.Bounds.Y;

                            //    Rectangle next = new Rectangle(aX, aY, v.Bounds.Width, v.Bounds.Height);
                            //    obj.IsOverlapped = rectangles.Any(s => s.Intersects(next));
                            //    rectangles.Add(next);
                            //}

                        }
                    }
                }

                if (deque.Count == 0)
                    _toRemoveText.Add(parent);
            }
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
            if (!_damageOverheads.TryGetValue(obj, out var deque) || deque == null)
            {
                deque = new Deque<DamageOverhead>();
                _damageOverheads[obj] = deque;
            }

            deque.AddToFront(text);

            if (deque.Count > 10)
                deque.RemoveFromBack();
        }

        public void AddText(GameObject obj, TextOverhead text)
        {
            if (!_textOverheads.TryGetValue(obj, out Deque<TextOverhead> deque) || deque == null)
            {
                deque = new Deque<TextOverhead>();
                _textOverheads[obj] = deque;
            }
      
            deque.AddToFront(text);

            if (deque.Count > 5)
                deque.RemoveFromBack();
        }

        public void RemoveTextOverheadList(GameObject obj)
        {
            if (_textOverheads.TryGetValue(obj, out Deque<TextOverhead> deque))
            {
                if (deque != null && deque.Count > 0)
                {
                    foreach (TextOverhead overhead in deque)
                    {
                        overhead.Dispose();
                    }
                }

                _textOverheads.Remove(obj);
            }
        }

        public void Dispose()
        {
            foreach (Deque<TextOverhead> deque in _textOverheads.Values)
            {
                foreach (TextOverhead textOverhead in deque)
                {
                    textOverhead.Dispose();
                }
            }

            foreach (var deque in _damageOverheads.Values)
            {
                foreach (DamageOverhead damageOverhead in deque)
                {
                    damageOverhead.Dispose();
                }
            }

            //_bounds.Clear();

            if (_toRemoveDamages.Count > 0)
            {
                _toRemoveDamages.ForEach(s =>
                {
                    //_bounds.Remove(s);
                    _damageOverheads.Remove(s);
                });
                _toRemoveDamages.Clear();
            }

            if (_toRemoveText.Count > 0)
            {
                _toRemoveText.ForEach(s =>
                {
                    //_bounds.Remove(s);
                    _textOverheads.Remove(s);
                });
                _toRemoveText.Clear();
            }
        }
    }
}