// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Renderer;
using ClassicUO.Utility.Collections;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal class TextContainer : LinkedObject
    {
        public int Size,
            MaxSize = 5;

        public void Add(TextObject obj)
        {
            PushToBack(obj);

            if (Size >= MaxSize)
            {
                ((TextObject)Items)?.Destroy();
                Remove(Items);
            }
            else
            {
                Size++;
            }
        }

        public new void Clear()
        {
            TextObject item = (TextObject)Items;
            Items = null;

            while (item != null)
            {
                TextObject next = (TextObject)item.Next;
                item.Next = null;
                item.Destroy();
                Remove(item);

                item = next;
            }

            Size = 0;
        }
    }

    internal class OverheadDamage
    {
        private const int DAMAGE_Y_MOVING_TIME = 25;
        private readonly Deque<TextObject> _messages;
        private Rectangle _rectangle;
        private readonly World _world;

        public OverheadDamage(World world, GameObject parent)
        {
            _world = world;
            Parent = parent;
            _messages = new Deque<TextObject>();
        }

        public GameObject Parent { get; private set; }
        public bool IsDestroyed { get; private set; }
        public bool IsEmpty => _messages.Count == 0;

        public void SetParent(GameObject parent)
        {
            Parent = parent;
        }

        public void Add(int damage)
        {
            Parent?.AddDamage(damage);

            TextObject text_obj = TextObject.Create(_world);

            string dmgString = damage.ToString();

            if (ProfileManager.CurrentProfile.ShowDPSWithDamageNumbers && Parent != null)
                dmgString += $" (DPS: {Parent.GetCurrentDPS()})";

            text_obj.RenderedText = RenderedText.Create(
                dmgString,
                (ushort)(ReferenceEquals(Parent, _world.Player) ? 0x0034 : 0x0021),
                3,
                false
            );

            text_obj.Time = Time.Ticks + 1500;

            _messages.AddToFront(text_obj);

            if (_messages.Count > 10)
            {
                _messages.RemoveFromBack()?.Destroy();
            }
        }

        public void Update()
        {
            if (IsDestroyed)
            {
                return;
            }

            _rectangle.Width = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                TextObject c = _messages[i];

                float delta = c.Time - Time.Ticks;

                if (c.SecondTime < Time.Ticks)
                {
                    c.OffsetY += 1;
                    c.SecondTime = Time.Ticks + DAMAGE_Y_MOVING_TIME;
                }

                if (delta <= 0)
                {
                    _rectangle.Height -= c.RenderedText?.Height ?? 0;
                    c.Destroy();
                    _messages.RemoveAt(i--);
                }
                //else if (delta < 250)
                //    c.Alpha = 1f - delta / 250;
                else if (c.RenderedText != null)
                {
                    if (_rectangle.Width < c.RenderedText.Width)
                    {
                        _rectangle.Width = c.RenderedText.Width;
                    }
                }
            }
        }

        public void Draw(UltimaBatcher2D batcher)
        {
            if (IsDestroyed || _messages.Count == 0)
            {
                return;
            }

            int offY = 0;

            Point p = new Point();

            if (Parent != null)
            {
                p.X += Parent.RealScreenPosition.X;
                p.Y += Parent.RealScreenPosition.Y;

                _rectangle.X = Parent.RealScreenPosition.X;
                _rectangle.Y = Parent.RealScreenPosition.Y;

                if (Parent is Mobile m)
                {
                    if (m.IsGargoyle && m.IsFlying)
                    {
                        offY += 22;
                    }
                    else if (!m.IsMounted)
                    {
                        offY = -22;
                    }

                    Client.Game.UO.Animations.GetAnimationDimensions(
                        m.AnimIndex,
                        m.GetGraphicForAnimation(),
                        /*(byte) m.GetDirectionForAnimation()*/
                        0,
                        /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                        0,
                        m.IsMounted,
                        /*(byte) m.AnimIndex*/
                        0,
                        out int centerX,
                        out int centerY,
                        out int width,
                        out int height
                    );

                    p.X += (int)m.Offset.X + 22;
                    p.Y += (int)(m.Offset.Y - m.Offset.Z - (height + centerY + 8));
                }
                else
                {
                    ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(Parent.Graphic);

                    if (artInfo.Texture != null)
                    {
                        p.X += 22;
                        int yValue = artInfo.UV.Height >> 1;

                        if (Parent is Item it)
                        {
                            if (it.IsCorpse)
                            {
                                offY = -22;
                            }
                        }
                        else if (Parent is Static || Parent is Multi)
                        {
                            offY = -44;
                        }

                        p.Y -= yValue;
                    }
                }
            }

            p = Client.Game.Scene.Camera.WorldToScreen(p);

            foreach (TextObject item in _messages)
            {
                if (item.IsDestroyed || item.RenderedText == null || item.RenderedText.IsDestroyed)
                {
                    continue;
                }

                item.X = p.X - (item.RenderedText.Width >> 1);
                item.Y = p.Y - offY - item.RenderedText.Height - item.OffsetY;

                item.RenderedText.Draw(batcher, item.X, item.Y, item.Alpha / 255f);
                offY += item.RenderedText.Height;
            }
        }

        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;

            foreach (TextObject item in _messages)
            {
                item.Destroy();
            }

            _messages.Clear();
        }
    }
}
