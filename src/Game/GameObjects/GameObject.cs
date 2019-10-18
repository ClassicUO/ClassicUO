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

using System;
using System.Runtime.CompilerServices;

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


    internal abstract partial class GameObject : BaseGameObject, IUpdateable, INode<GameObject>
    {
        private Position _position = Position.INVALID;
        private Point _screenPosition;

        public Vector3 Offset;


        public bool IsPositionChanged { get; protected set; }

        public TextContainer TextContainer { get; private set; }

        public Position Position
        {
            get => _position;
            [MethodImpl(256)]
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _screenPosition.X = (_position.X - _position.Y) * 22;
                    _screenPosition.Y = (_position.X + _position.Y) * 22 - (_position.Z << 2);
                    IsPositionChanged = true;
                    OnPositionChanged();
                }
            }
        }

        public ushort X
        {
            get => Position.X;
            set => Position = new Position(value, Position.Y, Position.Z);
        }

        public ushort Y
        {
            get => Position.Y;
            set => Position = new Position(Position.X, value, Position.Z);
        }

        public sbyte Z
        {
            get => Position.Z;
            set => Position = new Position(Position.X, Position.Y, value);
        }

        public Hue Hue { get; set; }

        public Graphic Graphic { get; set; }

        public sbyte AnimIndex { get; set; }

        public int CurrentRenderIndex { get; set; }

        public byte UseInRender { get; set; }

        public short PriorityZ { get; set; }

        public bool IsDestroyed { get; protected set; }

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

                if (this is Mobile m && m.IsMoving)
                {
                    Mobile.Step step = m.Steps.Back();
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
        public GameObject Left { get; set; }
        public GameObject Right { get; set; }

        public virtual void Update(double totalMS, double frameMS)
        {
        }

        [MethodImpl(256)]
        public void AddToTile(int x, int y)
        {
            if (World.Map != null)
            {
                if (Position != Position.INVALID)
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
                if (Position != Position.INVALID)
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
        public void UpdateRealScreenPosition(int offsetX, int offsetY)
        {
            RealScreenPosition.X = _screenPosition.X - offsetX - 22;
            RealScreenPosition.Y = _screenPosition.Y - offsetY - 22;
            IsPositionChanged = false;

            UpdateTextCoordsV();
        }

        public int DistanceTo(GameObject entity)
        {
            return Position.DistanceTo(entity.Position);
        }

        public void AddMessage(MessageType type, string message)
        {
            AddMessage(type, message, Engine.Profile.Current.ChatFont, Engine.Profile.Current.SpeechHue, true);
        }

        public virtual void UpdateTextCoordsV()
        {

        }

        protected void FixTextCoordinatesInScreen()
        {
            if (this is Item it && it.Container.IsValid)
                return;

            int offsetY = 0;

            int minX = Engine.Profile.Current.GameWindowPosition.X + 6;
            int maxX = minX + Engine.Profile.Current.GameWindowSize.X;
            int minY = Engine.Profile.Current.GameWindowPosition.Y;
            //int maxY = minY + Engine.Profile.Current.GameWindowSize.Y - 6;

            for (var item = TextContainer.Items; item != null; item = item.ListRight)
            {
                if (item.RenderedText == null || item.RenderedText.IsDestroyed || item.RenderedText.Texture == null || item.Time < Engine.Ticks)
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

        public void AddMessage(MessageType type, string text, byte font, Hue hue, bool isunicode)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var msg = CreateMessage(text, hue, font, isunicode, type);
            AddMessage(msg);
        }

        public void AddMessage(MessageInfo msg)
        {
            if (TextContainer == null)
                TextContainer = new TextContainer();

            msg.Owner = this;
            TextContainer.Add(msg);

            if (this is Item it && it.Container.IsValid)
            {
                UpdateTextCoordsV();
            }
            else
            {
                IsPositionChanged = true;
                World.WorldTextManager.AddMessage(msg);
            }
        }
        private static MessageInfo CreateMessage(string msg, ushort hue, byte font, bool isunicode, MessageType type)
        {
            if (Engine.Profile.Current != null && Engine.Profile.Current.OverrideAllFonts)
            {
                font = Engine.Profile.Current.ChatFont;
                isunicode = Engine.Profile.Current.OverrideAllFontsIsUnicode;
            }

            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, msg) : FileManager.Fonts.GetWidthASCII(font, msg);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, msg, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort)FontStyle.BlackBorder);
            else
                width = 0;

            RenderedText rtext = RenderedText.Create(msg, hue, font, isunicode, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_LEFT, width, 30, false, false, true);

            return new MessageInfo
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

            if (Engine.Profile.Current.ScaleSpeechDelay)
            {
                int delay = Engine.Profile.Current.SpeechDelay;

                if (delay < 10)
                    delay = 10;

                timeToLive = (long)(4000 * rtext.LinesCount * delay / 100.0f);
            }
            else
            {
                long delay = (5497558140000 * Engine.Profile.Current.SpeechDelay) >> 32 >> 5;

                timeToLive = (delay >> 31) + delay;
            }

            timeToLive += Engine.Ticks;

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
            _position = Position.INVALID;
            IsFlipped = false;
            Rotation = 0;
            Graphic = 0;
            UseObjectHandles = ClosedObjectHandles = ObjectHandlesOpened = false;
            Bounds = Rectangle.Empty;
            FrameInfo = Rectangle.Empty;
            DrawTransparent = false;
            
            Texture = null;
        }
    }
}