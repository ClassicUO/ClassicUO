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

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal class TextObject : BaseGameObject
    {
        private static readonly QueuedPool<TextObject> _queue = new QueuedPool<TextObject>
        (
            1000,
            o =>
            {
                o.IsDestroyed = false;
                o.Alpha = 0xFF;
                o.Hue = 0;
                o.Time = 0;
                o.IsTransparent = false;
                o.SecondTime = 0;
                o.Type = 0;
                o.X = 0;
                o.Y = 0;
                o.RealScreenPosition = Point.Zero;
                o.OffsetY = 0;
                o.Owner = null;
                o.UnlinkD();
                o.IsTextGump = false;
                o.TextBox?.Dispose();
                o.TextBox = null;
                o.Clear();
            }
        );

        public byte Alpha;
        public TextObject DLeft, DRight;
        public ushort Hue;
        public bool IsDestroyed;
        public bool IsTextGump;
        public bool IsTransparent;
        public GameObject Owner;

        public TextBox TextBox;
        public long Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;


        public static TextObject Create()
        {
            return _queue.GetOne();
        }


        public virtual void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            UnlinkD();

            RealScreenPosition = Point.Zero;
            IsDestroyed = true;
            TextBox?.Dispose();
            TextBox = null;
            Owner = null;

            _queue.ReturnOne(this);
        }

        public void UnlinkD()
        {
            if (DRight != null)
            {
                DRight.DLeft = DLeft;
            }

            if (DLeft != null)
            {
                DLeft.DRight = DRight;
            }

            DRight = null;
            DLeft = null;
        }

        public void ToTopD()
        {
            TextObject obj = this;

            while (obj != null)
            {
                if (obj.DLeft == null)
                {
                    break;
                }

                obj = obj.DLeft;
            }

            TextRenderer next = (TextRenderer) obj;
            next.MoveToTop(this);
        }
    }
}