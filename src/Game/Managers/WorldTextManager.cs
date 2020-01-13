#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;

using Microsoft.Xna.Framework;


namespace ClassicUO.Game.Managers
{
    internal class TextRenderer
    {
        private readonly List<Rectangle> _bounds = new List<Rectangle>();
        protected TextOverhead _firstNode = new TextOverhead(), _drawPointer;

        public virtual void Update(double totalMS, double frameMS)
        {
            ProcessWorldText(false);
        }

        public void Select(int startX, int startY, int renderIndex)
        {
            ProcessWorldText(false);

            int mouseX = Mouse.Position.X;
            int mouseY = Mouse.Position.Y;

            for (var item = _drawPointer; item != null; item = item.Left)
            {
                if (item.RenderedText == null || item.RenderedText.IsDestroyed || item.RenderedText.Texture == null)
                    continue;

                if (item.Time >= Time.Ticks)
                {
                    if (item.Owner == null || item.Owner.UseInRender != renderIndex)
                        continue;
                }

                if (item.RenderedText.Texture.Contains(mouseX - startX - item.RealScreenPosition.X, mouseY - startY - item.RealScreenPosition.Y))
                {
                    SelectedObject.LastObject = item;
                }
            }
        }

        public virtual void Draw(UltimaBatcher2D batcher, int startX, int startY, int renderIndex, bool isGump = false)
        {
            ProcessWorldText(false);

            int mouseX = Mouse.Position.X;
            int mouseY = Mouse.Position.Y;

            TextOverhead last = null;

            for (var o = _drawPointer; o != null; o = o.Left)
            {
                if (o.RenderedText == null || o.RenderedText.IsDestroyed || o.RenderedText.Texture == null || o.Time < Time.Ticks || (o.Owner.UseInRender != renderIndex && !isGump))
                    continue;

                ushort hue = 0;

                float alpha = 1f - o.Alpha / 255f;

                if (o.IsTransparent)
                {
                    if (o.Alpha == 0xFF)
                        alpha = 1f - 0x7F / 255f;
                }

                if (o.RenderedText.Texture.Contains( mouseX - startX - o.RealScreenPosition.X, mouseY - startY - o.RealScreenPosition.Y))
                {
                    SelectedObject.Object = o;
                    last = o;
                }

                if (!isGump && SelectedObject.LastObject == o)
                {
                    hue = 0x35;
                }

                o.RenderedText.Draw(batcher, startX + o.RealScreenPosition.X, startY + o.RealScreenPosition.Y, alpha, hue);
            }

            if (last != null)
            {
                if (last.Right != null)
                    last.Right.Left = last.Left;

                if (last.Left != null)
                    last.Left.Right = last.Right;

                last.Left = last.Right = null;


                var next = _firstNode.Right;
                _firstNode.Right = last;
                last.Left = _firstNode;
                last.Right = next;

                if (next != null)
                    next.Left = last;
            }
        }

        public void ProcessWorldText(bool doit)
        {
            if (doit)
            {
                if (_bounds.Count != 0)
                    _bounds.Clear();
            }

            for (_drawPointer = _firstNode; _drawPointer != null; _drawPointer = _drawPointer.Right)
            {
                var t = _drawPointer;

                if (doit)
                {
                    if (t.Time >= Time.Ticks && !t.RenderedText.IsDestroyed)
                    {
                        if (t.Owner != null)
                        {
                            t.IsTransparent = Collides(t);
                            CalculateAlpha(t);
                        }
                    }
                }

                if (_drawPointer.Right == null)
                    break;
            }
        }

        private void CalculateAlpha(TextOverhead msg)
        {
            int delta = (int)(msg.Time - Time.Ticks);

            if (delta >= 0 && delta <= 1000)
            {
                delta /= 10;

                if (delta > 100)
                    delta = 100;
                else if (delta < 1)
                    delta = 0;

                delta = (255 * delta) / 100;

                if (!msg.IsTransparent || delta <= 0x7F)
                {
                    msg.Alpha = (byte)delta;
                }

                msg.IsTransparent = true;
            }
        }

        private bool Collides(TextOverhead msg)
        {
            bool result = false;

            Rectangle rect = new Rectangle()
            {
                X = msg.RealScreenPosition.X,
                Y = msg.RealScreenPosition.Y,
                Width = msg.RenderedText.Width,
                Height = msg.RenderedText.Height
            };

            for (int i = 0; i < _bounds.Count; i++)
            {
                if (_bounds[i].Intersects(rect))
                {
                    result = true;
                    break;
                }
            }

            _bounds.Add(rect);
            return result;
        }

        public void AddMessage(TextOverhead obj)
        {
            if (obj == null)
                return;

            var item = _firstNode;

            if (item != null)
            {
                if (item.Right != null)
                {
                    var next = item.Right;

                    item.Right = obj;
                    obj.Left = item;
                    obj.Right = next;
                    next.Left = obj;
                }
                else
                {
                    item.Right = obj;
                    obj.Left = item;
                    obj.Right = null;
                }
            }
        }


        public virtual void Clear()
        {
            _firstNode = new TextOverhead();
            _drawPointer = null;
        }
    }

    internal class WorldTextManager : TextRenderer
    {
        private readonly Dictionary<uint, OverheadDamage> _damages = new Dictionary<uint, OverheadDamage>();
        private readonly List<Tuple<uint, uint>> _subst = new List<Tuple<uint, uint>>();
        private readonly List<uint> _toRemoveDamages = new List<uint>();


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);


            UpdateDamageOverhead(totalMS, frameMS);

            if (_toRemoveDamages.Count > 0)
            {
                foreach ( uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }
        }




        public override void Draw(UltimaBatcher2D batcher, int startX, int startY, int renderIndex, bool isGump = false)
        {
            float scale = Client.Game.GetScene<GameScene>().Scale;

            base.Draw(batcher, 0, 0, renderIndex, isGump);

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                int x = startX;
                int y = startY;

                Entity mob = World.Get(overheadDamage.Key);

                if (mob == null || mob.IsDestroyed)
                {
                    uint ser = overheadDamage.Key | 0x8000_0000;

                    if (World.CorpseManager.Exists(0, ser))
                    {
                        Item item = World.CorpseManager.GetCorpseObject(ser);

                        if (item != null && item != overheadDamage.Value.Parent)
                        {
                            _subst.Add(Tuple.Create(overheadDamage.Key, item.Serial));
                            overheadDamage.Value.SetParent(item);
                        }
                    }
                    else
                        continue;
                }

                overheadDamage.Value.Draw(batcher, x, y, scale);
            }
        }

        private void UpdateDamageOverhead(double totalMS, double frameMS)
        {
            if (_subst.Count != 0)
            {
                foreach (Tuple<uint, uint> tuple in _subst)
                {
                    if (_damages.TryGetValue(tuple.Item1, out var dmg))
                    {
                        _damages.Remove(tuple.Item1);
                        _damages.Add(tuple.Item2, dmg);
                    }
                }

                _subst.Clear();
            }

            foreach (KeyValuePair<uint, OverheadDamage> overheadDamage in _damages)
            {
                overheadDamage.Value.Update();

                if (overheadDamage.Value.IsEmpty) _toRemoveDamages.Add(overheadDamage.Key);
            }
        }


        internal void AddDamage(uint obj, int dmg)
        {
            if (!_damages.TryGetValue(obj, out var dm) || dm == null)
            {
                dm = new OverheadDamage(World.Get(obj));
                _damages[obj] = dm;
            }

            dm.Add(dmg);
        }

        public override void Clear()
        {
            if (_toRemoveDamages.Count > 0)
            {
                foreach (uint s in _toRemoveDamages)
                {
                    _damages.Remove(s);
                }

                _toRemoveDamages.Clear();
            }

            _subst.Clear();

            //_staticToUpdate.Clear();

            base.Clear();
        }
    }
}