using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal class OverheadMessage
    {
        private readonly Deque<MessageInfo> _messages;
        private float _alpha;
        private Rectangle _rectangle;


        public OverheadMessage(GameObject parent)
        {
            Parent = parent;
            _messages = new Deque<MessageInfo>();
        }

        public OverheadMessage Left { get; set; }
        public OverheadMessage Right { get; set; }

        public GameObject Parent { get; }
        public bool IsDestroyed { get; private set; }

        public bool IsEmpty => _messages.Count == 0;

        private static float CalculateTimeToLive(RenderedText rtext)
        {
            float timeToLive;

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = 4000 * rtext.LinesCount * delay / 100.0f;
            }
            else
            {
                long delay = (5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Engine.Ticks;

            return timeToLive;
        }

        public void AddMessage(string msg, Hue hue, byte font, bool isunicode, MessageType type, bool ishealthmessage = false)
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                var a = _messages[i];

                if (type == MessageType.Label && a.RenderedText != null && (ishealthmessage && a.IsHealthMessage || a.RenderedText.Text == msg) && a.Type == type)
                {
                    if (a.RenderedText.Hue != hue || ishealthmessage)
                    {
                        a.RenderedText.Hue = hue;

                        if (ishealthmessage)
                        {
                            a.Time = CalculateTimeToLive(a.RenderedText);
                            a.RenderedText.Text = msg;
                        }
                        else
                            a.RenderedText.CreateTexture();
                    }

                    _messages.RemoveAt(i);

                    if (_messages.Count == 0 || _messages.Front().Type != MessageType.Label)
                        _messages.AddToFront(a);
                    else
                        _messages.Insert(1, a);

                    return;
                }
            }


            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, msg) : FileManager.Fonts.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder);
            else
                width = 0;

            RenderedText rtext = new RenderedText
            {
                Font = font,
                MaxWidth = width,
                Hue = hue,
                IsUnicode = isunicode,
                SaveHitMap = true,
                FontStyle = FontStyle.BlackBorder,
                Text = msg
            };


            var msgInfo = new MessageInfo
            {
                Alpha = 0,
                RenderedText = rtext,
                Time = CalculateTimeToLive(rtext),
                Type = type,
                Parent = this,
                IsHealthMessage = ishealthmessage
            };

            int max = Parent is Static || Parent is Multi || Parent is AnimatedItemEffect ef && ef.Source is Static ? 0 : 5;

            for (int i = 0, limit3 = 0; i < _messages.Count; i++)
            {
                if (i < max)
                {
                    var c = _messages[i];

                    if (c.Type == MessageType.Limit3Spell)
                    {
                        if (++limit3 > 3)
                        {
                            _rectangle.Height -= c.RenderedText.Height;
                            c.RenderedText.Destroy();
                            _messages.RemoveAt(i--);
                        }
                    }
                }
                else
                {
                    var c = _messages[i];
                    _rectangle.Height -= c.RenderedText.Height;
                    c.RenderedText.Destroy();
                    _messages.RemoveAt(i--);
                }
            }

            if (_messages.Count == 0 || _messages.Front().Type != MessageType.Label)
                _messages.AddToFront(msgInfo);
            else
                _messages.Insert(1, msgInfo);


            if (_rectangle.Width < rtext.Width)
                _rectangle.Width = rtext.Width;
            _rectangle.Height += rtext.Height;
        }

        public float IsOverlap(OverheadMessage firstNode)
        {
            int count = 0;

            for (var ov = firstNode; ov != null; ov = ov.Right)
            {
                if (ov != this && ov._rectangle.Intersects(_rectangle))
                {
                    count++;
                    ov._alpha = 0.3f;
                }
            }

            float alpha = _alpha;
            _alpha = alpha * count;

            return alpha;
        }

        public void SetAlpha(float alpha)
        {
            _alpha = alpha;
        }

        public void Update()
        {
            if (Parent == null)
                Destroy();

            if (IsDestroyed)
                return;

            _rectangle.Width = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                var c = _messages[i];

                float delta = c.Time - Engine.Ticks;

                if (delta <= 0)
                {
                    c.RenderedText.Destroy();
                    _rectangle.Height -= c.RenderedText.Height;
                    _messages.RemoveAt(i--);
                }
                else if (delta < 250)
                    c.Alpha = 1f - delta / 250;
                else
                {
                    if (_rectangle.Width < c.RenderedText.Width)
                        _rectangle.Width = c.RenderedText.Width;
                }
            }
        }

        public void Draw(Batcher2D batcher, int x, int y, float scale)
        {
            if (IsDestroyed || _messages.Count == 0)
                return;


            int screenX = Engine.Profile.Current.GameWindowPosition.X;
            int screenY = Engine.Profile.Current.GameWindowPosition.Y;
            int screenW = Engine.Profile.Current.GameWindowSize.X;
            int screenH = Engine.Profile.Current.GameWindowSize.Y;

            x += Parent.RealScreenPosition.X;
            y += Parent.RealScreenPosition.Y;

            int offY = 0;

            if (Parent is Mobile m)
            {
                if (!m.IsMounted)
                    offY = -22;

                GetAnimationDimensions(m, 0, out int centerX, out int centerY, out int width, out int height);

                x += (int) m.Offset.X;
                x += 22;
                y += (int) (m.Offset.Y - m.Offset.Z - (height + centerY + 8));
            }
            else if (Parent.Texture != null)
            {
                if (Parent is Item it && it.IsCorpse)
                    offY = -22;
                else if (Parent is Static || Parent is Multi)
                    offY = -44;

                x += 22;
                y -= Parent.Texture.Height >> 1;
            }

            x = (int) (x / scale);
            y = (int) (y / scale);

            x -= (int) (screenX / scale);
            y -= (int) (screenY / scale);

            x += screenX;
            y += screenY;


            if (x - (_rectangle.Width >> 1) + 6 < screenX)
                x = screenX + (_rectangle.Width >> 1) + 6;
            else if (x > screenX + screenW - ((_rectangle.Width >> 1) - 3))
                x = screenX + screenW - ((_rectangle.Width >> 1) - 3);

            if (y < screenY + _rectangle.Height + offY)
                y = screenY + _rectangle.Height + offY;
            else if (y > screenY + screenH + offY)
                y = screenY + screenH + offY;


            _rectangle.X = x - (_rectangle.Width >> 1);
            _rectangle.Y = y - offY - _rectangle.Height;


            //int startY = offY;
            foreach (var item in _messages)
            {
                ushort hue = 0;

                if (Engine.Profile.Current.HighlightGameObjects)
                    if (item.IsSelected)
                        hue = 23;

                item.X = x - (item.RenderedText.Width >> 1);
                item.Y = y - offY - item.RenderedText.Height;
                item.RenderedText.Draw(batcher, item.X, item.Y, _alpha != 0.0f ? _alpha : item.Alpha, hue);
                offY += item.RenderedText.Height;
            }

            /* batcher.DrawRectangle(Textures.GetTexture(Color.Green),
                                  x - (_rectangle.Width >> 1),
                                  y - startY - _rectangle.Height, 
                                  _rectangle.Width, 
                                  _rectangle.Height,
                                  Vector3.Zero); 
            */
        }

        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int centerX, out int centerY, out int width, out int height)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte) mobile.AnimIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out centerX, out centerY, out width, out height);
            if (centerX == 0 && centerY == 0 && width == 0 && height == 0) height = mobile.IsMounted ? 100 : 60;
        }

        public bool Contains(int x, int y)
        {
            if (IsDestroyed)
                return false;

            foreach (var item in _messages)
            {
                if (item.RenderedText.Texture.Contains(x - item.X, y - item.Y))
                {
                    SelectedObject.Object = item;

                    return true;
                }
            }

            return false;
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            foreach (var item in _messages)
                item.RenderedText.Destroy();

            _messages.Clear();
        }
    }

    internal class OverheadDamage
    {
        private const int DAMAGE_Y_MOVING_TIME = 25;

        private readonly Deque<MessageInfo> _messages;

        private Rectangle _rectangle;


        public OverheadDamage(GameObject parent)
        {
            Parent = parent;
            _messages = new Deque<MessageInfo>();
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
            _messages.AddToFront(new MessageInfo
            {
                RenderedText = new RenderedText
                {
                    IsUnicode = false,
                    Font = 3,
                    Hue = (Hue) (Parent == World.Player ? 0x0034 : 0x0021),
                    Text = damage.ToString()
                },
                Time = Engine.Ticks + 1500
            });


            if (_messages.Count > 10)
                _messages.RemoveFromBack()?.RenderedText?.Destroy();
        }

        public void Update()
        {
            if (IsDestroyed)
                return;

            _rectangle.Width = 0;

            for (int i = 0; i < _messages.Count; i++)
            {
                var c = _messages[i];

                float delta = c.Time - Engine.Ticks;

                if (c.SecondTime < Engine.Ticks)
                {
                    c.OffsetY += 1;
                    c.SecondTime = Engine.Ticks + DAMAGE_Y_MOVING_TIME;
                }

                if (delta <= 0)
                {
                    c.RenderedText.Destroy();
                    _rectangle.Height -= c.RenderedText.Height;
                    _messages.RemoveAt(i--);
                }
                else if (delta < 250)
                    c.Alpha = 1f - delta / 250;
                else
                {
                    if (_rectangle.Width < c.RenderedText.Width)
                        _rectangle.Width = c.RenderedText.Width;
                }
            }
        }

        public void Draw(Batcher2D batcher, int x, int y, float scale)
        {
            if (IsDestroyed || _messages.Count == 0)
                return;

            int screenX = Engine.Profile.Current.GameWindowPosition.X;
            int screenY = Engine.Profile.Current.GameWindowPosition.Y;
            int screenW = Engine.Profile.Current.GameWindowSize.X;
            int screenH = Engine.Profile.Current.GameWindowSize.Y;

            int offY = 0;


            if (Parent != null)
            {
                x += Parent.RealScreenPosition.X;
                y += Parent.RealScreenPosition.Y;

                _rectangle.X = Parent.RealScreenPosition.X;
                _rectangle.Y = Parent.RealScreenPosition.Y;

                if (Parent is Mobile m)
                {
                    if (!m.IsMounted)
                        offY = -22;

                    GetAnimationDimensions(m, 0, out int centerX, out int centerY, out int width, out int height);

                    x += (int) m.Offset.X;
                    x += 22;
                    y += (int) (m.Offset.Y - m.Offset.Z - (height + centerY + 8));
                }
                else if (Parent.Texture != null)
                {
                    x += 22;
                    int yValue = Parent.Texture.Height >> 1;

                    if (Parent is Item it)
                    {
                        if (it.IsCorpse)
                            offY = -22;
                        else if (it.ItemData.IsAnimated)
                        {
                            Texture2D texture = FileManager.Art.GetTexture(it.Graphic);

                            if (texture != null)
                                yValue = texture.Height >> 1;
                        }
                    }
                    else if (Parent is Static || Parent is Multi)
                        offY = -44;

                    y -= yValue;
                }
            }


            x = (int) (x / scale);
            y = (int) (y / scale);

            x -= (int) (screenX / scale);
            y -= (int) (screenY / scale);

            x += screenX;
            y += screenY;


            foreach (var item in _messages)
            {
                ushort hue = 0;

                if (Engine.Profile.Current.HighlightGameObjects)
                    if (item.IsSelected)
                        hue = 23;

                item.X = x - (item.RenderedText.Width >> 1);
                item.Y = y - offY - item.RenderedText.Height - item.OffsetY;

                item.RenderedText.Draw(batcher, item.X, item.Y, item.Alpha, hue);
                offY += item.RenderedText.Height;
            }
        }

        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int centerX, out int centerY, out int width, out int height)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte) mobile.AnimIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out centerX, out centerY, out width, out height);
            if (centerX == 0 && centerY == 0 && width == 0 && height == 0) height = mobile.IsMounted ? 100 : 60;
        }


        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            foreach (var item in _messages)
                item.RenderedText.Destroy();

            _messages.Clear();
        }
    }

    internal class MessageInfo : IGameEntity
    {
        public float Alpha;
        public bool IsHealthMessage;

        public OverheadMessage Parent;
        public RenderedText RenderedText;
        public float Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;

        public bool IsSelected { get; set; }
    }
}