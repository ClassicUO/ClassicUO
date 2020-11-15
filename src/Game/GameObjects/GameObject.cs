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
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    internal abstract class BaseGameObject : LinkedObject
    {
        public Point RealScreenPosition;
    }

    internal abstract partial class GameObject : BaseGameObject, IUpdateable
    {
        private Point _screenPosition;

        public bool IsDestroyed { get; protected set; }
        public bool IsPositionChanged { get; protected set; }
        public TextContainer TextContainer { get; private set; }

        public int Distance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (World.Player == null /*|| IsDestroyed*/)
                {
                    return ushort.MaxValue;
                }

                if (this == World.Player)
                {
                    return 0;
                }

                int x = X, y = Y;

                if (this is Mobile mobile && mobile.Steps.Count != 0)
                {
                    ref Mobile.Step step = ref mobile.Steps.Back();
                    x = step.X;
                    y = step.Y;
                }

                int fx = World.RangeSize.X;
                int fy = World.RangeSize.Y;

                return Math.Max(Math.Abs(x - fx), Math.Abs(y - fy));
            }
        }

        public virtual void Update(double totalTime, double frameTime)
        {
        }

        public int CurrentRenderIndex;
        // FIXME: remove it
        public sbyte FoliageIndex = -1;
        public ushort Graphic;
        public ushort Hue;
        public Vector3 Offset;
        public short PriorityZ;
        public GameObject TNext;
        public GameObject TPrevious;
        public byte UseInRender;

        public ushort X, Y;
        public sbyte Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToTile(int x, int y)
        {
            if (World.Map != null)
            {
                RemoveFromTile();

                if (!IsDestroyed)
                {
                    World.Map.GetChunk(x, y)?.AddGameObject(this, x % 8, y % 8);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToTile()
        {
            AddToTile(X, Y);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFromTile()
        {
            if (TPrevious != null)
            {
                TPrevious.TNext = TNext;
            }

            if (TNext != null)
            {
                TNext.TPrevious = TPrevious;
            }

            TNext = null;
            TPrevious = null;
        }

        public virtual void UpdateGraphicBySeason()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateScreenPosition()
        {
            _screenPosition.X = (X - Y) * 22;
            _screenPosition.Y = (X + Y) * 22 - (Z << 2);
            IsPositionChanged = true;
            OnPositionChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRealScreenPosition(int offsetX, int offsetY)
        {
            RealScreenPosition.X = _screenPosition.X - offsetX - 22;
            RealScreenPosition.Y = _screenPosition.Y - offsetY - 22;
            IsPositionChanged = false;

            UpdateTextCoordsV();
        }


        public void AddMessage(MessageType type, string message, TextType text_type)
        {
            AddMessage
                (type, message, ProfileManager.CurrentProfile.ChatFont, ProfileManager.CurrentProfile.SpeechHue, true, text_type);
        }

        public virtual void UpdateTextCoordsV()
        {
            if (TextContainer == null)
            {
                return;
            }

            TextObject last = (TextObject) TextContainer.Items;

            while (last?.Next != null)
            {
                last = (TextObject) last.Next;
            }

            if (last == null)
            {
                return;
            }

            int offY = 0;

            Point p = RealScreenPosition;

            ArtTexture texture = ArtLoader.Instance.GetTexture(Graphic);

            if (texture != null)
            {
                p.Y -= texture.ImageRectangle.Height >> 1;
            }

            p.X += (int) Offset.X + 22;
            p.Y += (int) (Offset.Y - Offset.Z) + 44;

            p = Client.Game.Scene.Camera.WorldToScreen(p);

            for (; last != null; last = (TextObject) last.Previous)
            {
                if (last.RenderedText != null && !last.RenderedText.IsDestroyed)
                {
                    if (offY == 0 && last.Time < Time.Ticks)
                    {
                        continue;
                    }

                    last.OffsetY = offY;
                    offY += last.RenderedText.Height;

                    last.RealScreenPosition.X = p.X - (last.RenderedText.Width >> 1);
                    last.RealScreenPosition.Y = p.Y - offY;
                }
            }

            FixTextCoordinatesInScreen();
        }

        protected void FixTextCoordinatesInScreen()
        {
            if (this is Item it && SerialHelper.IsValid(it.Container))
            {
                return;
            }

            int offsetY = 0;

            int minX = 6;
            int maxX = minX + ProfileManager.CurrentProfile.GameWindowSize.X - 6;
            int minY = 0;
            //int maxY = minY + ProfileManager.CurrentProfile.GameWindowSize.Y - 6;

            for (TextObject item = (TextObject) TextContainer.Items; item != null; item = (TextObject) item.Next)
            {
                if (item.RenderedText == null || item.RenderedText.IsDestroyed || item.RenderedText.Texture == null ||
                    item.Time < Time.Ticks)
                {
                    continue;
                }

                int startX = item.RealScreenPosition.X;
                int endX = startX + item.RenderedText.Width;

                if (startX < minX)
                {
                    item.RealScreenPosition.X += minX - startX;
                }

                if (endX > maxX)
                {
                    item.RealScreenPosition.X -= endX - maxX;
                }

                int startY = item.RealScreenPosition.Y;

                if (startY < minY && offsetY == 0)
                {
                    offsetY = minY - startY;
                }

                //int endY = startY + item.RenderedText.Height;

                //if (endY > maxY)
                //    UseInRender = 0xFF;
                //    //item.RealScreenPosition.Y -= endY - maxY;

                if (offsetY != 0)
                {
                    item.RealScreenPosition.Y += offsetY;
                }
            }
        }

        public void AddMessage
        (
            MessageType type,
            string text,
            byte font,
            ushort hue,
            bool isunicode,
            TextType text_type
        )
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            TextObject msg = MessageManager.CreateMessage(text, hue, font, isunicode, type, text_type);
            AddMessage(msg);
        }

        public void AddMessage(TextObject msg)
        {
            if (TextContainer == null)
            {
                TextContainer = new TextContainer();
            }

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


        protected virtual void OnPositionChanged()
        {
        }

        protected virtual void OnDirectionChanged()
        {
        }

        public virtual void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            Next = null;
            Previous = null;

            Clear();
            RemoveFromTile();
            TextContainer?.Clear();

            IsDestroyed = true;
            PriorityZ = 0;
            IsPositionChanged = false;
            Hue = 0;
            Offset = Vector3.Zero;
            CurrentRenderIndex = 0;
            UseInRender = 0;
            RealScreenPosition = Point.Zero;
            _screenPosition = Point.Zero;
            IsFlipped = false;
            Graphic = 0;
            UseObjectHandles = ClosedObjectHandles = ObjectHandlesOpened = false;
            FrameInfo = Rectangle.Empty;
        }
    }
}