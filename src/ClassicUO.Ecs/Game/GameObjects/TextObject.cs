// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal class TextObject : BaseGameObject
    {
        //private static readonly QueuedPool<TextObject> _queue = new QueuedPool<TextObject>
        //(
        //    1000,
        //    o =>
        //    {
        //        o.IsDestroyed = false;
        //        o.Alpha = 0xFF;
        //        o.Hue = 0;
        //        o.Time = 0;
        //        o.IsTransparent = false;
        //        o.SecondTime = 0;
        //        o.Type = 0;
        //        o.X = 0;
        //        o.Y = 0;
        //        o.RealScreenPosition = Point.Zero;
        //        o.OffsetY = 0;
        //        o.Owner = null;
        //        o.UnlinkD();
        //        o.IsTextGump = false;
        //        o.RenderedText?.Destroy();
        //        o.RenderedText = null;
        //        o.Clear();
        //    }
        //);

        public TextObject(World world) : base(world) { }

        public byte Alpha;
        public TextObject DLeft, DRight;
        public ushort Hue;
        public bool IsDestroyed;
        public bool IsTextGump;
        public bool IsTransparent;
        public GameObject Owner;

        public RenderedText RenderedText;
        public long Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;


        public static TextObject Create(World world)
        {
            return new TextObject(world) { Alpha = 0xFF }; // _queue.GetOne();
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
            RenderedText?.Destroy();
            RenderedText = null;
            Owner = null;

            //_queue.ReturnOne(this);
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