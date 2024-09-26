#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    public class TextRenderer : TextObject
    {
        private readonly List<Rectangle> _bounds = new List<Rectangle>();

        public TextRenderer(World world) : base (world)
        {
            FirstNode = this;
        }

        protected TextObject FirstNode;
        protected TextObject DrawPointer;

        public override void Destroy()
        {
            //Clear();
        }

        public virtual void Update()
        {
            ProcessWorldText(false);
        }

        public virtual void Draw(UltimaBatcher2D batcher, int startX, int startY, bool isGump = false)
        {
            ProcessWorldText(false);

            int mouseX = Mouse.Position.X;
            int mouseY = Mouse.Position.Y;

            for (TextObject o = DrawPointer; o != null; o = o.DLeft)
            {
                if (o.IsDestroyed || o.TextBox == null || o.TextBox.IsDisposed || o.Time < ClassicUO.Time.Ticks)
                {
                    continue;
                }

                //ushort hue = 0;

                float alpha = o.Alpha / 255f;

                if (o.IsTransparent)
                {
                    if (o.Alpha == 0xFF)
                    {
                        alpha = 0x7F / 255f;
                    }
                }

                int x = o.RealScreenPosition.X;
                int y = o.RealScreenPosition.Y;

                if (o.TextBox.PixelCheck(mouseX - x - startX, mouseY - y - startY))
                {
                    SelectedObject.Object = o;
                }
                bool highlight = false;
                if (!isGump)
                {
                    if (o.Owner is Entity && SelectedObject.Object == o)
                    {
                        highlight = true;
                    }
                }
                else
                {
                    x += startX;
                    y += startY;
                }
                o.TextBox.Alpha = alpha;
                if (highlight)
                    o.TextBox.Draw
                    (
                        batcher,
                        x,
                        y,
                        Color.Yellow
                    );
                else
                    o.TextBox.Draw
                    (
                        batcher,
                        x,
                        y
                    );
            }
        }

        public void MoveToTop(TextObject obj)
        {
            if (obj == null)
            {
                return;
            }

            obj.UnlinkD();

            TextObject next = FirstNode.DRight;
            FirstNode.DRight = obj;
            obj.DLeft = FirstNode;
            obj.DRight = next;

            if (next != null)
            {
                next.DLeft = obj;
            }
        }

        public void ProcessWorldText(bool doit)
        {
            if (doit)
            {
                if (_bounds.Count != 0)
                {
                    _bounds.Clear();
                }
            }

            for (DrawPointer = FirstNode; DrawPointer != null; DrawPointer = DrawPointer.DRight)
            {
                if (doit)
                {
                    TextObject t = DrawPointer;

                    if (t.Time >= ClassicUO.Time.Ticks && t.TextBox != null && !t.TextBox.IsDisposed)
                    {
                        if (t.Owner != null)
                        {
                            t.IsTransparent = Collides(t);
                            CalculateAlpha(t);
                        }
                    }
                }

                if (DrawPointer.DRight == null)
                {
                    break;
                }
            }
        }

        private void CalculateAlpha(TextObject msg)
        {
            if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextFading)
            {
                int delta = (int)(msg.Time - ClassicUO.Time.Ticks);

                if (delta >= 0 && delta <= 1000)
                {
                    delta /= 10;

                    if (delta > 100)
                    {
                        delta = 100;
                    }
                    else if (delta < 1)
                    {
                        delta = 0;
                    }

                    delta = 255 * delta / 100;

                    if (!msg.IsTransparent || delta <= 0x7F)
                    {
                        msg.Alpha = (byte)delta;
                    }

                    msg.IsTransparent = true;
                }
            }
        }

        private bool Collides(TextObject msg)
        {
            bool result = false;

            Rectangle rect = new Rectangle
            {
                X = msg.RealScreenPosition.X,
                Y = msg.RealScreenPosition.Y,
                Width = msg.TextBox.Width,
                Height = msg.TextBox.Height
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
            {
                return;
            }

            obj.UnlinkD();

            TextObject item = FirstNode;

            if (item != null)
            {
                if (item.DRight != null)
                {
                    TextObject next = item.DRight;

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
        }


        public new virtual void Clear()
        {
            if (FirstNode != null)
            {
                TextObject first = FirstNode;

                while (first?.DLeft != null)
                {
                    first = first.DLeft;
                }

                while (first != null)
                {
                    TextObject next = first.DRight;

                    first.Destroy();
                    first.Clear();

                    first = next;
                }
            }

            if (DrawPointer != null)
            {
                TextObject first = DrawPointer;

                while (first?.DLeft != null)
                {
                    first = first.DLeft;
                }

                while (first != null)
                {
                    TextObject next = first.DRight;

                    first.Destroy();
                    first.Clear();

                    first = next;
                }
            }

            FirstNode = this;
            FirstNode.DLeft = null;
            FirstNode.DRight = null;
            DrawPointer = null;
        }
    }
}
