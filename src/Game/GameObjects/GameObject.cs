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

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    internal interface IGameEntity
    {
        //bool IsSelected { get; set; }
    }


    internal abstract partial class GameObject : IGameEntity, IUpdateable, INode<GameObject>
    {
        private Position _position = Position.INVALID;
        private Point _screenPosition;


        public Vector3 Offset;
        public Point RealScreenPosition;

        public OverheadMessage OverheadMessageContainer { get; protected set; }

        public bool IsPositionChanged { get; protected set; }

        public Position Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _screenPosition.X = (_position.X - _position.Y) * 22;
                    _screenPosition.Y = (_position.X + _position.Y) * 22 - _position.Z * 4;
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

        public virtual Hue Hue { get; set; }

        public virtual Graphic Graphic { get; set; }

        public sbyte AnimIndex { get; set; }

        public int CurrentRenderIndex { get; set; }

        public byte UseInRender { get; set; }

        public short PriorityZ { get; set; }


        //public Tile Tile
        //{
        //    get => _tile;
        //    set
        //    {
        //        if (_tile != value)
        //        {
        //            _tile?.RemoveGameObject(this);
        //            _tile = value;

        //            if (_tile != null)
        //                _tile.AddGameObject(this);
        //            else
        //            {
        //                if (this != World.Player && !IsDisposed) Dispose();
        //            }
        //        }
        //    }
        //}

        public bool IsDestroyed { get; protected set; }

        public int Distance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            OverheadMessageContainer?.Update();
        }

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

        public void AddToTile()
        {
            AddToTile(X, Y);
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRealScreenPosition(Point offset)
        {
            RealScreenPosition.X = _screenPosition.X - offset.X - 22;
            RealScreenPosition.Y = _screenPosition.Y - offset.Y - 22;
            IsPositionChanged = false;
        }

        public int DistanceTo(GameObject entity)
        {
            return Position.DistanceTo(entity.Position);
        }

        public void AddOverhead(MessageType type, string message)
        {
            AddOverhead(type, message, Engine.Profile.Current.ChatFont, Engine.Profile.Current.SpeechHue, true);
        }

        public void AddOverhead(MessageType type, string text, byte font, Hue hue, bool isunicode, float timeToLive = 0.0f, bool ishealthmessage = false)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (OverheadMessageContainer == null)
                OverheadMessageContainer = new OverheadMessage(this);

            OverheadMessageContainer.AddMessage(text, hue, font, isunicode, type, ishealthmessage);

            Engine.SceneManager.GetScene<GameScene>().Overheads.AddOverhead(OverheadMessageContainer);
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

            IsDestroyed = true;
            PriorityZ = 0;
            IsPositionChanged = false;
            Hue = 0;
            AnimIndex = 0;
            Offset = Vector3.Zero;
            CurrentRenderIndex = 0;
            UseInRender = 0;
            RealScreenPosition = Point.Zero;
            IsFlipped = false;
            Rotation = 0;
            UseObjectHandles = ClosedObjectHandles = ObjectHandlesOpened = false;
            Bounds = Rectangle.Empty;
            FrameInfo = Rectangle.Empty;
            DrawTransparent = false;
            Tile?.RemoveGameObject(this);
            Tile = null;

            OverheadMessageContainer?.Destroy();

            Texture = null;
        }
    }
}