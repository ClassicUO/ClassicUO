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
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Map;
using ClassicUO.Game.Scenes;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    internal class TextOverheadContainer
    {

    }

    internal abstract partial class GameObject : IUpdateable, IDisposable, INode<GameObject>
    {
        private Position _position = Position.INVALID;
        public Vector3 Offset;
        private Deque<TextOverhead> _overHeads;
        private Tile _tile;
        private Vector3 _screenPosition;

        protected GameObject()
        {

        }

        public GameObject Left { get; set; }
        public GameObject Right { get; set; }

        protected virtual bool CanCreateOverheads => true;

        public Vector3 ScreenPosition => _screenPosition;

        public Vector3 RealScreenPosition;

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

        //public byte UseInRender { get; set; }

        public short PriorityZ { get; set; }

        public bool HasOverheads => _overHeads != null;

        public IReadOnlyList<TextOverhead> Overheads => CanCreateOverheads ? _overHeads ?? (_overHeads = new Deque<TextOverhead>()) : null;

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

        public bool IsDisposed { get; private set; }

        public int Distance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (World.Player == null)
                    return ushort.MaxValue;

                if (World.Player.IsMoving && this != World.Player)
                {
                    Mobile.Step step = World.Player.Steps.Back();

                    return Position.DistanceTo(step.X, step.Y);
                }

                return Position.DistanceTo(World.Player.Position);

            }
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            if (_overHeads != null)
            {
                for (int i = 0; i < _overHeads.Count; i++)
                {
                    var overhead = _overHeads[i];
                    overhead.Update(totalMS, frameMS);

                    if (overhead.IsDisposed)
                        _overHeads.RemoveAt(i--);
                }
            }
        }

        public void AddToTile(int x, int y)
        {
            if (World.Map != null)
            {
                if (Position != Position.INVALID)
                    _tile?.RemoveGameObject(this);

                _tile = World.Map.GetTile(x, y);
                _tile?.AddGameObject(this);
            }
        }

        public void AddToTile() => AddToTile(X, Y);

        public void AddToTile(Tile tile)
        {
            if (World.Map != null)
            {
                if (Position != Position.INVALID)
                    _tile?.RemoveGameObject(this);

                _tile = tile;
                _tile?.AddGameObject(this);
            }
        }

        public void RemoveFromTile()
        {
            if (World.Map != null && _tile != null)
            {
                _tile?.RemoveGameObject(this);
                _tile = null;
            }
        }
      

        public event EventHandler Disposed, OverheadAdded;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRealScreenPosition(Point offset)
        {
            RealScreenPosition.X = ScreenPosition.X - offset.X - 22;
            RealScreenPosition.Y = ScreenPosition.Y - offset.Y - 22;
            IsPositionChanged = false;
        }

        public int DistanceTo(GameObject entity) => Position.DistanceTo(entity.Position);

        public TextOverhead AddOverhead(MessageType type, string message)
        {
            return AddOverhead(type, message, Engine.Profile.Current.ChatFont, Engine.Profile.Current.SpeechHue, true);
        }

        public TextOverhead AddOverhead(MessageType type, string text, byte font, Hue hue, bool isunicode, float timeToLive = 0.0f)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            TextOverhead overhead;

            for (int i = 0; i < Overheads.Count; i++)
            {
                overhead = _overHeads[i];

                if (type == MessageType.Label && overhead.Text == text && overhead.MessageType == type && !overhead.IsDisposed)
                {
                    overhead.Hue = hue;
                    _overHeads.RemoveAt(i);
                    InsertGameText(overhead);

                    return overhead;
                }
            }

            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, text) : FileManager.Fonts.GetWidthASCII(font, text);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder);
            else
                width = 0;
            overhead = new TextOverhead(this, text, width, hue, font, isunicode, FontStyle.BlackBorder, timeToLive)
            {
                MessageType = type
            };

            InsertGameText(overhead);
            int limit3text = 0;
            for (int i = 0; i < _overHeads.Count; i++)
            {
                if (i <= 5)
                {
                    if (_overHeads[i].MessageType == MessageType.Limit3Spell)
                    {
                        limit3text++;
                        if (limit3text > 3)
                        {
                            _overHeads[i].Dispose();
                            _overHeads.RemoveAt(i);
                            i--;
                        }
                    }
                }
                else
                {
                    _overHeads[i].Dispose();
                    _overHeads.RemoveAt(i);
                    i--;
                }
            }

            OverheadAdded?.Raise(overhead);

            return overhead;
        }

        private void InsertGameText(TextOverhead gameText)
        {
            if (_overHeads.Count == 0 || _overHeads[0].MessageType != MessageType.Label)
                _overHeads.AddToFront(gameText);
            else
                _overHeads.Insert(1, gameText);

            //_overHeads.Insert(_overHeads.Count == 0 || _overHeads[0].MessageType != MessageType.Label ? 0 : 1, gameText);
        }

        protected virtual void OnPositionChanged()
        {

        }

        //~GameObject()
        //{
        //    Dispose();
        //}

        public virtual void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            Disposed.Raise();

            _tile?.RemoveGameObject(this);
            _tile = null;

            if (_overHeads != null)
            {
                while (_overHeads.Count != 0)
                    _overHeads.RemoveFromBack().Dispose();
                _overHeads = null;
            }

            if (Left != null)
            {
                Left = null;
            }

            if (Right != null)
            {
                Right = null;
            }

            Texture = null;

            GC.SuppressFinalize(this);
        }
    }
}