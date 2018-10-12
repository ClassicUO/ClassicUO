﻿#region license

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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps
{
    internal class ItemGumpling : GumpControl
    {
        private bool _clickedCanDrag;
        private Point _clickedPoint, _labelClickedPosition;


        private readonly List<Label> _labels = new List<Label>();
        private float _picUpTime;
        private float _sClickTime;
        private bool _sendClickIfNotDClick;

        public ItemGumpling(Item item)
        {
            AcceptMouseInput = true;

            Item = item;
            X = item.Position.X;
            Y = item.Position.Y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
        }

        public Item Item { get; }
        public bool HighlightOnMouseOver { get; set; }
        public bool CanPickUp { get; set; }


        public override void Update(double totalMS, double frameMS)
        {
            if (Item.IsDisposed)
            {
                Dispose();
                return;
            }

            if (_clickedCanDrag && totalMS >= _picUpTime)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }

            if (_sendClickIfNotDClick && totalMS >= _sClickTime)
            {
                _sendClickIfNotDClick = false;
                GameActions.SingleClick(Item);
            }

            UpdateLabel();

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (Texture == null)
            {
                Texture = Art.GetStaticTexture(Item.DisplayedGraphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            Vector3 huev =
                RenderExtentions.GetHueVector(MouseIsOver && HighlightOnMouseOver
                    ? GameScene.MouseOverItemHue
                    : Item.Hue);

            if (Item.Amount > 1 && TileData.IsStackable((long) Item.ItemData.Flags) &&
                Item.DisplayedGraphic == Item.Graphic)
                spriteBatch.Draw2D(Texture, new Vector3(position.X - 5, position.Y - 5, 0), huev);

            spriteBatch.Draw2D(Texture, position, huev);

            return base.Draw(spriteBatch, position, hue);
        }

        protected override bool Contains(int x, int y)
        {
            if (Art.Contains(Item.DisplayedGraphic, x, y))
                return true;

            if (Item.Amount > 1 && TileData.IsStackable((long) Item.ItemData.Flags))
            {
                if (Art.Contains(Item.DisplayedGraphic, x - 5, y - 5))
                    return true;
            }

            return false;
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            _clickedCanDrag = true;
            float totalMS = CoreGame.Ticks;
            _picUpTime = totalMS + 800f;
            _clickedPoint = new Point(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _clickedCanDrag = false;
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if (_clickedCanDrag && Math.Abs(_clickedPoint.X - x) + Math.Abs(_clickedPoint.Y - y) > 3)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            _labelClickedPosition.X = x;
            _labelClickedPosition.Y = y;

            if (_clickedCanDrag)
            {
                _clickedCanDrag = false;
                _sendClickIfNotDClick = true;
                float totalMS = CoreGame.Ticks;
                _sClickTime = totalMS + 200f;
            }
        }

        protected override void OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            GameActions.DoubleClick(Item);
            _sendClickIfNotDClick = false;
        }

        public override void Dispose()
        {
            UpdateLabel(true);
            base.Dispose();
        }

        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                if (this is ItemGumplingPaperdoll)
                {
                    Rectangle bounds = Art.GetStaticTexture(Item.DisplayedGraphic).Bounds;

                    GameActions.PickUp(Item, bounds.Width / 2, bounds.Height / 2);
                }
                else
                    GameActions.PickUp(Item, _clickedPoint);
            }
        }

        private void UpdateLabel(bool isDisposing = false)
        {
            if (!isDisposing && Item.OverHeads.Count > 0)
            {
                if (_labels.Count <= 0)
                {
                    foreach (TextOverhead overhead in Item.OverHeads)
                    {
                        Label label = new Label(overhead.Text, overhead.IsUnicode, overhead.Hue, overhead.MaxWidth,
                            style: overhead.Style, align: TEXT_ALIGN_TYPE.TS_CENTER, timeToLive: overhead.TimeToLive)
                        {
                            FadeOut = true
                        };

                        label.ControlInfo.Layer = UILayer.Over;

                        UIManager.Add(label);
                        _labels.Add(label);
                    }
                }

                int y = 0;

                for (int i = _labels.Count - 1; i >= 0; i--)
                {
                    Label l = _labels[i];

                    l.X = ScreenCoordinateX + _clickedPoint.X - l.Width / 2;
                    l.Y = ScreenCoordinateY + _clickedPoint.Y - l.Height / 2 + y;

                    y += l.Height;
                }
            }
            else if (_labels.Count > 0)
            {
                _labels.ForEach(s => s.Dispose());
                _labels.Clear();
            }
        }
    }
}