#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal class TextRenderer : TextObject
    {
        private Rectangle[] _bounds = new Rectangle[2000];
        private int _count;

        public TextRenderer()
        {
            FirstNode = this;
        }

        protected TextObject FirstNode;
        protected TextObject DrawPointer;

        public override void Destroy()
        {
        }

        public virtual void Update(double totalTime, double frameTime)
        {
            ProcessWorldText(false);
        }


        public virtual void Draw(UltimaBatcher2D batcher, int startX, int startY, bool isGump = false)
        {
            ProcessWorldText(false);

            for (TextObject o = DrawPointer; o != null; o = o.DLeft)
            {
                if (o.IsDestroyed || string.IsNullOrEmpty(o.Text) || o.Time < ClassicUO.Time.Ticks)
                {
                    continue;
                }

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

                if (isGump)
                {
                    x += startX;
                    y += startY;
                }

                var hueVec = ShaderHueTranslator.GetHueVector(o.Hue, false, alpha);

                bool selected = UOFontRenderer.Shared.Draw
                (
                    batcher,
                    o.Text.AsSpan(),
                    new Vector2(x, y),
                    1f,
                    o.FontSettings,
                    hueVec,
                    o.ObjectTextType == Data.TextType.OBJECT && !isGump,
                    o.MaxTextWidth
                );

                if (selected && o.Owner is Entity)
                {
                    SelectedObject.Object = o;
                }                
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
                if (_count != 0)
                {
                    _count = 0;
                }
            }

            for (DrawPointer = FirstNode; DrawPointer != null; DrawPointer = DrawPointer.DRight)
            {
                if (doit)
                {
                    TextObject t = DrawPointer;

                    if (t.Time >= ClassicUO.Time.Ticks && !string.IsNullOrEmpty(t.Text))
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
                int delta = (int) (msg.Time - ClassicUO.Time.Ticks);

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

                    delta = (int) MathHelper.Clamp(255 * delta / 100, 0, 255);

                    if (!msg.IsTransparent || delta <= 0x7F)
                    {
                        msg.Alpha = (byte) delta;
                    }

                    msg.IsTransparent = true;
                }
            }
        }

        private static bool Intersects(ref Rectangle r0, ref Rectangle r1)
        {
            return (r1.X < (r0.X + r0.Width) &&
                    r0.X < (r1.X + r1.Width) &&
                    r1.Y < (r0.Y + r0.Height) &&
                    r0.Y < (r1.Y + r1.Height));
        }

        private bool Collides(TextObject msg)
        {
            bool result = false;

            ref var rect = ref _bounds[_count];
            rect.X = msg.RealScreenPosition.X;
            rect.Y = msg.RealScreenPosition.Y;
            rect.Width = (int)msg.TextSize.X;
            rect.Height = (int)msg.TextSize.Y;
        
            for (int i = 0; i < _count - 1; i++)
            {
                if (Intersects(ref _bounds[i], ref rect))
                {
                    result = true;

                    break;
                }
            }

            ++_count;

            if (_count >= _bounds.Length)
            {
                Array.Resize(ref _bounds, _count * 2);
            }

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
