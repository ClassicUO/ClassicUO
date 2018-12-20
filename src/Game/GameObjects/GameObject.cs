#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Game.Views;
using ClassicUO.Interfaces;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using IUpdateable = ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    public abstract class GameObject : IUpdateable, IDisposable, INode<GameObject>
    {
        private Position _position = Position.Invalid;
        private View _view;
        public Vector3 Offset;
        private readonly Deque<TextOverhead> _overHeads = new Deque<TextOverhead>(5);
        private Tile _tile;

        protected GameObject()
        {
            
        }

        public GameObject Left { get; set; }
        public GameObject Right { get; set; }



        public Vector3 ScreenPosition { get; private set; }
        
        public Vector3 RealScreenPosition { get; protected set; }

        public bool IsPositionChanged { get; protected set; }

        public Position Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    ScreenPosition = new Vector3((_position.X - _position.Y) * 22, (_position.X + _position.Y) * 22 - _position.Z * 4, 0);
                    IsPositionChanged = true;
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

        public View View => _view ?? (_view = CreateView());

        public sbyte AnimIndex { get; set; }

        public int CurrentRenderIndex { get; set; }

        //public byte UseInRender { get; set; }

        public short PriorityZ { get; set; }

        public IReadOnlyList<TextOverhead> Overheads => _overHeads;

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
                    
                if (World.Player.Steps.Count > 0)
                {
                    Mobile.Step step = World.Player.Steps.Back();

                    return Position.DistanceTo(step.X, step.Y);
                }

                return Position.DistanceTo(World.Player.Position);

            }
        }

        public virtual void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _overHeads.Count; i++)
            {
                var overhead = _overHeads[i];
                overhead.Update(totalMS, frameMS);
                
                if (overhead.IsDisposed)
                    _overHeads.RemoveAt(i--);
            }
        }

        public void AddToTile(int x, int y)
        {
            if (World.Map != null)
            {
                //if (_tile != null && _tile.X == x && _tile.Y == y)
                //    return;

                if (Position != Position.Invalid)
                    _tile?.RemoveGameObject(this);

                _tile = World.Map.GetTile(x, y);
                _tile?.AddGameObject(this);
            }
        }

        public void AddToTile() => AddToTile(X, Y);
      

        public event EventHandler Disposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateRealScreenPosition(Point offset)
        {
            RealScreenPosition = new Vector3(ScreenPosition.X - offset.X - 22, ScreenPosition.Y - offset.Y - 22, 0);
            IsPositionChanged = false;
        }

        public int DistanceTo(GameObject entity) => Position.DistanceTo(entity.Position);

        protected virtual View CreateView()
        {
            return null;
        }

        public TextOverhead AddGameText(MessageType type, string text, byte font, Hue hue, bool isunicode, float timeToLive = 0.0f)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            TextOverhead overhead;

            //for (int i = 0; i < _overHeads.Value.Count; i++)
            //{
            //    overhead = _overHeads.Value[i];

            //    if (type == MessageType.Label && overhead.Text == text && overhead.MessageType == type && !overhead.IsDisposed)
            //    {
            //        overhead.Hue = hue;
            //        _overHeads.Value.RemoveAt(i);
            //        InsertGameText(overhead);

            //        return overhead;
            //    }
            //}

            int width = isunicode ? FileManager.Fonts.GetWidthUnicode(font, text) : FileManager.Fonts.GetWidthASCII(font, text);

            if (width > 200)
                width = isunicode ? FileManager.Fonts.GetWidthExUnicode(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder) : FileManager.Fonts.GetWidthExASCII(font, text, 200, TEXT_ALIGN_TYPE.TS_LEFT, (ushort) FontStyle.BlackBorder);
            else
                width = 0;
            overhead = new TextOverhead(this, text, width, hue, font, isunicode, FontStyle.BlackBorder, timeToLive);

            InsertGameText(overhead);

            if (_overHeads.Count > 5)
            {
                TextOverhead over = _overHeads[_overHeads.Count - 1];

                if (over.MessageType != MessageType.Spell && over.MessageType != MessageType.Label)
                {
                    over.Dispose();
                    _overHeads.RemoveAt(_overHeads.Count - 1);
                }
            }

            return overhead;
        }

        private void InsertGameText(TextOverhead gameText)
        {
            _overHeads.AddToFront(gameText);
            //_overHeads.Insert(_overHeads.Count == 0 || _overHeads[0].MessageType != MessageType.Label ? 0 : 1, gameText);
        }

        protected virtual void OnPositionChanged()
        {

        }

        protected void DisposeView()
        {
            if (_view != null)
                _view = null;
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

            DisposeView();

            _tile?.RemoveGameObject(this);
            _tile = null;

            //Tile = null;

            foreach (TextOverhead textOverhead in _overHeads)
            {
                textOverhead.Dispose();
            }
            _overHeads.Clear();

            //GC.SuppressFinalize(this);
        }
    }
}