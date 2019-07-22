#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using System.Collections.Generic;

using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal class EntityTextContainer
    {
        public Rectangle _rectangle;
        private readonly Deque<MessageInfo> _messages = new Deque<MessageInfo>();


        public EntityTextContainer(GameObject parent, int maxSize)
        {
            Parent = parent;
            MaxSize = maxSize;
        }

        public int MaxSize { get; }

        public GameObject Parent { get; }
        public bool IsDestroyed { get; private set; }

        public EntityTextContainer Left, Right;

        public bool IsEmpty => _messages.Count == 0;

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            long timeToLive;

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = (long) (4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Engine.Ticks;

            return timeToLive;
        }

        public MessageInfo AddMessage(string msg, Hue hue, byte font, bool isunicode, MessageType type)
        {
            if (Engine.Profile.Current != null && Engine.Profile.Current.OverrideAllFonts)
            {
                font = Engine.Profile.Current.ChatFont;
                isunicode = Engine.Profile.Current.OverrideAllFontsIsUnicode;
            }

            //for (int i = 0; i < _messages.Count; i++)
            //{
            //    var a = _messages[i];

            //    if (type == MessageType.Label && a.RenderedText != null && (ishealthmessage && a.IsHealthMessage || a.RenderedText.Text == msg) && a.Type == type)
            //    {
            //        if (a.RenderedText.Hue != hue || ishealthmessage)
            //        {
            //            a.Hue = hue;
            //            a.RenderedText.Hue = hue;

            //            if (ishealthmessage)
            //            {
            //                a.Time = CalculateTimeToLive(a.RenderedText);
            //                a.RenderedText.Text = msg;
            //            }
            //            else
            //                a.RenderedText.CreateTexture();
            //        }
                    
            //        _messages.RemoveAt(i);

            //        if (_messages.Count == 0 || _messages.Front().Type != MessageType.Label)
            //            _messages.AddToFront(a);
            //        else
            //            _messages.Insert(1, a);

            //        return null;
            //    }
            //}


            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, msg) : FileManager.Fonts.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
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
                Alpha = 255,
                RenderedText = rtext,
                Time = CalculateTimeToLive(rtext),
                Type = type,
                Hue = hue,
                Parent = this,
            };

            //int max = Parent is Static || Parent is Multi || Parent is AnimatedItemEffect ef && ef.Source is Static ? 0 : 4;

            for (int i = 0, limit3 = 0; i < _messages.Count; i++)
            {
                if (i < MaxSize - 1)
                {
                    var c = _messages[i];

                    if (c.Type == MessageType.Limit3Spell)
                    {
                        if (++limit3 > 3)
                        {
                            c.RenderedText.Destroy();
                            _messages.RemoveAt(i--);

                            if (c.Right != null)
                                c.Right.Left = c.Left;

                            if (c.Left != null)
                                c.Left.Right = c.Right;

                            c.Left = c.Right = null;
                        }
                    }
                }
                else
                {
                    var c = _messages[i];
                    c.RenderedText.Destroy();
                    _messages.RemoveAt(i--);

                    if (c.Right != null)
                        c.Right.Left = c.Left;

                    if (c.Left != null)
                        c.Left.Right = c.Right;

                    c.Left = c.Right = null;
                }
            }


            _messages.AddToFront(msgInfo);

            //if (_messages.Count == 0 || _messages.Front().Type != MessageType.Label)
            //    _messages.AddToFront(msgInfo);
            //else
            //    _messages.Insert(1, msgInfo);


          
            return msgInfo;
        }

      



        public void Update()
        {
            if (Parent == null || Parent.IsDestroyed)
                Destroy();

            if (IsDestroyed)
                return;

            _rectangle.Width = 0;
            _rectangle.Height = 0;

            int offY = 0;
            
            for (int i = 0; i < _messages.Count; i++)
            {
                var obj1 = _messages[i];

                long delta = obj1.Time - Engine.Ticks;
                if (delta <= 0)
                {
                    obj1.RenderedText.Destroy();

                    _messages.RemoveAt(i--);

                    if (obj1.Right != null)
                        obj1.Right.Left = obj1.Left;

                    if (obj1.Left != null)
                        obj1.Left.Right = obj1.Right;

                    obj1.Left = obj1.Right = null;
                }
                else
                {
                    if (_rectangle.Width < obj1.RenderedText.Width)
                        _rectangle.Width = obj1.RenderedText.Width;

                    if (_rectangle.Height < obj1.RenderedText.Height)
                        _rectangle.Height = obj1.RenderedText.Height;

                    obj1.OffsetY = offY;
                    offY += obj1.RenderedText.Height;
                }
                
            }
        }


        public void Clear()
        {
            foreach (var item in _messages)
            {
                item.RenderedText.Destroy();

                if (item.Right != null)
                    item.Right.Left = item.Left;

                if (item.Left != null)
                    item.Left.Right = item.Right;

                item.Left = item.Right = null;
            }

            _messages.Clear();
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            Clear();
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
                //else if (delta < 250)
                //    c.Alpha = 1f - delta / 250;
                else
                {
                    if (_rectangle.Width < c.RenderedText.Width)
                        _rectangle.Width = c.RenderedText.Width;
                }
            }
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y, float scale)
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


                    FileManager.Animations.GetAnimationDimensions(m.AnimIndex,
                                                                  m.GetGraphicForAnimation(),
                                                                  /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                                  /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                                  m.IsMounted,
                                                                  /*(byte) m.AnimIndex*/ 0,
                                                                  out int centerX,
                                                                  out int centerY,
                                                                  out int width,
                                                                  out int height);
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
                            ArtTexture texture = FileManager.Art.GetTexture(it.Graphic);

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

                //if (Engine.Profile.Current.HighlightGameObjects)
                //{
                //    if (SelectedObject.LastObject == item)
                //        hue = 23;
                //}
                //else if (SelectedObject.LastObject == item)
                //    hue = 23;

                item.X = x - (item.RenderedText.Width >> 1);
                item.Y = y - offY - item.RenderedText.Height - item.OffsetY;

                item.RenderedText.Draw(batcher, item.X, item.Y, item.Alpha, hue);
                offY += item.RenderedText.Height;
            }
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

    internal class MessageInfo : BaseGameObject
    {
        public byte Alpha;
        public ushort Hue;
        public bool IsTransparent;

        public EntityTextContainer Parent;
        public RenderedText RenderedText;
        public long Time, SecondTime;
        public MessageType Type;
        public int X, Y, OffsetY;

        public MessageInfo Left, Right;
    }
}