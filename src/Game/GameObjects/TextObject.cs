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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal class TextObject : BaseGameObject
    {
        private static readonly Queue<TextObject> _queue = new Queue<TextObject>();

        static TextObject()
        {
            for (int i = 0; i < 1000; i++)
                _queue.Enqueue(new TextObject());
        }


        public static TextObject Create()
        {
            if (_queue.Count != 0)
            {
                TextObject o = _queue.Dequeue();
                o.IsDestroyed = false;

                o.Alpha = 0;
                o.Hue = 0;
                o.Time = 0;
                o.SecondTime = 0;
                o.Type = 0;
                o.X = 0;
                o.Y = 0;
                o.OffsetY = 0;
                o.Owner = null;
                o.UnlinkD();
                o.IsTextGump = false;
                o.RenderedText?.Destroy();
                o.RenderedText = null;

                o.Clear();
            }

            return new TextObject();
        }

        public byte Alpha;
        public ushort Hue;
        public bool IsTransparent;

        public RenderedText RenderedText;
        public long Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;
        public GameObject Owner;
        public TextObject DLeft, DRight;
        public bool IsDestroyed;
        public bool IsTextGump;

        protected TextObject()
        {

        }

        ~TextObject()
        {
           //Destroy();
        }

        public virtual void Destroy()
        {
            if (IsDestroyed)
                return;

            UnlinkD();

            IsDestroyed = true;
            RenderedText?.Destroy();
            RenderedText = null;
            Owner = null;

            _queue.Enqueue(this);
        }

        public void UnlinkD()
        {
            if (DRight != null)
                DRight.DLeft = DLeft;

            if (DLeft != null)
                DLeft.DRight = DRight;

            DRight = null;
            DLeft = null;
        }

        public void ToTopD()
        {
            var obj = this;

            while (obj != null)
            {
                if (obj.DLeft == null)
                    break;

                obj = obj.DLeft;
            }

            var next = (TextRenderer) obj;
            next.MoveToTop(this);
        }
    }
}
