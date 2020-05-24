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

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;


namespace ClassicUO.Game.Managers
{
    internal class TextRenderer : TextObject
    {
        private readonly List<Rectangle> _bounds = new List<Rectangle>();
        protected TextObject _firstNode, _drawPointer;

        public TextRenderer()
        {
            _firstNode = this;
        }

        public override void Destroy()
        {
            //Clear();
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            ProcessWorldText(false);
        }

        public void Select(int startX, int startY, int renderIndex, bool isGump = false)
        {
            int mouseX = Mouse.Position.X;
            int mouseY = Mouse.Position.Y;

            for (var item = _drawPointer; item != null; item = item.DLeft)
            {
                if (item.RenderedText == null || item.RenderedText.IsDestroyed || item.RenderedText.Texture == null)
                    continue;

                if (item.Time >= ClassicUO.Time.Ticks)
                {
                    if (item.Owner == null || item.Owner.UseInRender != renderIndex)
                        continue;
                }

                if (item.RenderedText.Texture.Contains(mouseX - startX - item.RealScreenPosition.X, mouseY - startY - item.RealScreenPosition.Y))
                {
                    SelectedObject.LastObject = item;
                }
            }

            if (SelectedObject.LastObject is TextObject t)
            {
                if (isGump)
                {
                    if (t.IsTextGump)
                        t.ToTopD();
                }
                else
                    MoveToTop(t);
            }
        }

        public virtual void Draw(UltimaBatcher2D batcher, int startX, int startY, int renderIndex, bool isGump = false)
        {
            ProcessWorldText(false);

            int mouseX = Mouse.Position.X;
            int mouseY = Mouse.Position.Y;

            var last = SelectedObject.LastObject;

            for (var o = _drawPointer; o != null; o = o.DLeft)
            {
                if (o.IsDestroyed || o.RenderedText == null || o.RenderedText.IsDestroyed || o.RenderedText.Texture == null || o.Time < ClassicUO.Time.Ticks || (o.Owner.UseInRender != renderIndex && !isGump))
                    continue;

                ushort hue = 0;

                float alpha = 1f - o.Alpha / 255f;

                if (o.IsTransparent)
                {
                    if (o.Alpha == 0xFF)
                        alpha = 1f - 0x7F / 255f;
                }

                if (o.RenderedText.Texture.Contains(mouseX - startX - o.RealScreenPosition.X, mouseY - startY - o.RealScreenPosition.Y))
                {
                    if (isGump)
                        SelectedObject.LastObject = o;
                    else
                        SelectedObject.Object = o;
                }

                if (!isGump && o.Owner is Entity && last == o)
                {
                    hue = 0x0035;
                }

                o.RenderedText.Draw(batcher, startX + o.RealScreenPosition.X, startY + o.RealScreenPosition.Y, alpha, hue);
            }
        }

        public void MoveToTop(TextObject obj)
        {
            if (obj == null)
                return;

            obj.UnlinkD();

            var next = _firstNode.DRight;
            _firstNode.DRight = obj;
            obj.DLeft = _firstNode;
            obj.DRight = next;

            if (next != null)
                next.DLeft = obj;
        }

        public void ProcessWorldText(bool doit)
        {
            if (doit)
            {
                if (_bounds.Count != 0)
                    _bounds.Clear();
            }

            for (_drawPointer = _firstNode; _drawPointer != null; _drawPointer = _drawPointer.DRight)
            {
                if (doit)
                {
                    var t = _drawPointer;

                    if (t.Time >= ClassicUO.Time.Ticks && t.RenderedText != null && !t.RenderedText.IsDestroyed)
                    {
                        if (t.Owner != null)
                        {
                            t.IsTransparent = Collides(t);
                            CalculateAlpha(t);
                        }
                    }
                }

                if (_drawPointer.DRight == null)
                    break;
            }
        }

        private void CalculateAlpha(TextObject msg)
        {
            if (ProfileManager.Current != null && ProfileManager.Current.TextFading)
            {
                int delta = (int) (msg.Time - ClassicUO.Time.Ticks);

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
                        msg.Alpha = (byte) delta;
                    }

                    msg.IsTransparent = true;
                }
            }
        }

        private bool Collides(TextObject msg)
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

        public void AddMessage(TextObject obj)
        {
            if (obj == null)
                return;
            
            obj.UnlinkD();
            
            var item = _firstNode;

            if (item != null)
            {
                if (item.DRight != null)
                {
                    var next = item.DRight;

                    item.DRight = obj;
                    obj.DLeft = item;
                    obj.DRight = next;
                    next.DLeft = obj;
                }
                else
                {
                    item.DRight = obj;
                    obj.DLeft = item;
                    obj.DRight = null;
                }
            }
            else
            {
                _firstNode = this;
            }
        }


        public new virtual void Clear()
        {
            if (_firstNode != null)
            {
                var first = _firstNode;

                while (first?.DLeft != null)
                    first = first.DLeft;

                while (first != null)
                {
                    var next = first.DRight;

                    first.Destroy();
                    first.Clear();

                    first = next;
                }
            }

            if (_drawPointer != null)
            {
                var first = _drawPointer;

                while (first?.DLeft != null)
                    first = first.DLeft;

                while (first != null)
                {
                    var next = first.DRight;

                    first.Destroy();
                    first.Clear();

                    first = next;
                }
            }
           
            _firstNode = null;
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
                        _damages[tuple.Item2] = dmg;
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