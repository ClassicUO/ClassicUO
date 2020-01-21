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
using System.Runtime.CompilerServices;

using ClassicUO.Configuration;
using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class BaseGameObject
    {
        public Point RealScreenPosition;
    }

    internal abstract partial class GameObject : BaseGameObject, IUpdateable
    {
        private Point _screenPosition;

        public ushort X, Y;
        public sbyte Z;
        public ushort Hue;
        public ushort Graphic;
        public sbyte AnimIndex;
        public int CurrentRenderIndex;
        public byte UseInRender;
        public short PriorityZ;
        public GameObject Left;
        public GameObject Right;
        public Vector3 Offset;
        // FIXME: remove it
        public sbyte FoliageIndex = -1;

        public bool IsDestroyed { get; protected set; }
        public bool IsPositionChanged { get; protected set; }
        public TextContainer TextContainer { get; private set; }
        public int Distance
        {
            [MethodImpl(256)]
            get
            {
                if (World.Player == null)
                    return ushort.MaxValue;

                if (this == World.Player)
                    return 0;

                int x, y;

                if (this is Mobile m && m.Steps.Count != 0)
                {
                    ref var step = ref m.Steps.Back();
                    x = step.X;
                    y = step.Y;
                }
                else
                {
                    x = X;
                    y = Y;
                }

                int fx = World.RangeSize.X;
                int fy = World.RangeSize.Y;

                return Math.Max(Math.Abs(x - fx), Math.Abs(y - fy));
            }
        }
        public Tile Tile { get; private set; }
    

        public virtual void Update(double totalMS, double frameMS)
        {
        }

        [MethodImpl(256)]
        public void AddToTile(int x, int y)
        {
            if (World.Map != null)
            {
                Tile?.RemoveGameObject(this);

                if (!IsDestroyed)
                {
                    Tile = World.Map.GetTile(x, y);
                    Tile?.AddGameObject(this);
                }
            }
        }

        [MethodImpl(256)]
        public void AddToTile()
        {
            AddToTile(X, Y);
        }

        [MethodImpl(256)]
        public void AddToTile(Tile tile)
        {
            if (World.Map != null)
            {
                Tile?.RemoveGameObject(this);

                if (!IsDestroyed)
                {
                    Tile = tile;
                    Tile?.AddGameObject(this);
                }
            }
        }

        [MethodImpl(256)]
        public void RemoveFromTile()
        {
            if (World.Map != null && Tile != null)
            {
                Tile.RemoveGameObject(this);
                Tile = null;
            }
        }

        public virtual void UpdateGraphicBySeason()
        {

        }

        [MethodImpl(256)]
        public void UpdateScreenPosition()
        {
            _screenPosition.X = (X - Y) * 22;
            _screenPosition.Y = (X + Y) * 22 - (Z << 2);
            IsPositionChanged = true;
            OnPositionChanged();
        }

        [MethodImpl(256)]
        public void UpdateRealScreenPosition(int offsetX, int offsetY)
        {
            RealScreenPosition.X = _screenPosition.X - offsetX - 22;
            RealScreenPosition.Y = _screenPosition.Y - offsetY - 22;
            IsPositionChanged = false;

            UpdateTextCoordsV();
        }


        public void AddMessage(MessageType type, string message)
        {
            AddMessage(type, message, ProfileManager.Current.ChatFont, ProfileManager.Current.SpeechHue, true);
        }

        public virtual void UpdateTextCoordsV()
        {

        }

        protected void FixTextCoordinatesInScreen()
        {
            if (this is Item it && SerialHelper.IsValid(it.Container))
                return;

            int offsetY = 0;

            int minX = ProfileManager.Current.GameWindowPosition.X + 6;
            int maxX = minX + ProfileManager.Current.GameWindowSize.X;
            int minY = ProfileManager.Current.GameWindowPosition.Y;
            //int maxY = minY + ProfileManager.Current.GameWindowSize.Y - 6;

            for (var item = TextContainer.Items; item != null; item = item.ListRight)
            {
                if (item.RenderedText == null || item.RenderedText.IsDestroyed || item.RenderedText.Texture == null || item.Time < Time.Ticks)
                    continue;

                int startX = item.RealScreenPosition.X;
                int endX = startX + item.RenderedText.Width;

                if (startX < minX)
                    item.RealScreenPosition.X += minX - startX;

                if (endX > maxX)
                    item.RealScreenPosition.X -= endX - maxX;

                int startY = item.RealScreenPosition.Y;

                if (startY < minY && offsetY == 0)
                    offsetY = minY - startY;

                //int endY = startY + item.RenderedText.Height;

                //if (endY > maxY)
                //    UseInRender = 0xFF;
                //    //item.RealScreenPosition.Y -= endY - maxY;

                if (offsetY != 0)
                    item.RealScreenPosition.Y += offsetY;
            }
        }

        public void AddMessage(MessageType type, string text, byte font, ushort hue, bool isunicode)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var msg = CreateMessage(text, hue, font, isunicode, type);
            AddMessage(msg);
        }

        public void AddMessage(TextOverhead msg)
        {
            if (TextContainer == null)
                TextContainer = new TextContainer();

            msg.Owner = this;
            TextContainer.Add(msg);

            if (this is Item it && SerialHelper.IsValid(it.Container))
            {
                UpdateTextCoordsV();
            }
            else
            {
                IsPositionChanged = true;
                World.WorldTextManager.AddMessage(msg);
            }
        }
        private static TextOverhead CreateMessage(string msg, ushort hue, byte font, bool isunicode, MessageType type)
        {
            if (ProfileManager.Current != null && ProfileManager.Current.OverrideAllFonts)
            {
                font = ProfileManager.Current.ChatFont;
                isunicode = ProfileManager.Current.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? FontsLoader.Instance.GetWidthUnicode(font, msg) : FontsLoader.Instance.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FontsLoader.Instance.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : FontsLoader.Instance.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
            else
                width = 0;

            RenderedText rtext = RenderedText.Create(msg, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT, width, 30, false, false, true);

            return new TextOverhead
            {
                Alpha = 255,
                RenderedText = rtext,
                Time = CalculateTimeToLive(rtext),
                Type = type,
                Hue = hue,
            };
        }

        private static long CalculateTimeToLive(RenderedText rtext)
        {
            long timeToLive;

            if (ProfileManager.Current.ScaleSpeechDelay)
            {
                int delay = ProfileManager.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = (long)(4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * ProfileManager.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Time.Ticks;

            return timeToLive;
        }


        protected virtual void OnPositionChanged()
        {
        }

        protected virtual void OnDirectionChanged()
        {
        }

        public virtual void Destroy()
        {
            if (IsDestroyed)
                return;

            Tile?.RemoveGameObject(this);
            Tile = null;

            TextContainer?.Clear();

            IsDestroyed = true;
            PriorityZ = 0;
            IsPositionChanged = false;
            Hue = 0;
            AnimIndex = 0;
            Offset = Vector3.Zero;
            CurrentRenderIndex = 0;
            UseInRender = 0;
            RealScreenPosition = Point.Zero;
            _screenPosition = Point.Zero;
            IsFlipped = false;
            Graphic = 0;
            UseObjectHandles = ClosedObjectHandles = ObjectHandlesOpened = false;
            Bounds = Rectangle.Empty;
            FrameInfo = Rectangle.Empty;
            DrawTransparent = false;
            
            Texture = null;
        }
    }
}