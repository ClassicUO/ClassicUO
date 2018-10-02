#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
        private float _picUpTime;
        private Point _clickedPoint, _labelClickedPosition;
        private bool _sendClickIfNotDClick;
        private float _sClickTime;


        private RenderedText _labelText;
        private Label _label;

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

            if (_labelText != null && !Item.IsDisposed && Item.OverHeads.Count > 0 && !Item.OverHeads[0].IsDisposed)
            _labelText.Draw(spriteBatch,
                new Vector3(position.X + _labelClickedPosition.X - _labelText.Width / 2, position.Y + _labelClickedPosition.Y - _labelText.Height / 2, 0), RenderExtentions.GetHueVector(0, false, Item.OverHeads[0].Alpha, false));

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
            float totalMS = World.Ticks;
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
                float totalMS = World.Ticks;
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
                if (_label == null)
                {
                    TextOverhead overhead = Item.OverHeads[0];

                    //_labelText = new RenderedText()
                    //{
                    //    Hue = overhead.Hue,
                    //    Font = overhead.Font,
                    //    IsUnicode = overhead.IsUnicode,
                    //    MaxWidth = overhead.MaxWidth,
                    //    FontStyle = overhead.Style,
                    //    Align =  TEXT_ALIGN_TYPE.TS_CENTER,
                    //    Text = overhead.Text
                    //};

                    _label = new Label(overhead.Text, overhead.IsUnicode, overhead.Hue, overhead.MaxWidth,
                        overhead.Style, TEXT_ALIGN_TYPE.TS_CENTER);

                    _label.ControlInfo.Layer = UILayer.Over;
                    UIManager.Add(_label);
                }

                _label.X = ScreenCoordinateX + _clickedPoint.X - _label.Width / 2;
                _label.Y = ScreenCoordinateY + _clickedPoint.Y - _label.Height / 2;
            }
            else if (_label != null)
            {
                _label.Dispose();
                _label = null;
            }

            //int i = 0;
            //    while (_labels.Count < Item.OverHeads.Count)
            //    {
            //        TextOverhead overhead = Item.OverHeads[i++];
            //        Label label = new Label(overhead.Text, overhead.IsUnicode, overhead.Hue, overhead.MaxWidth,
            //            overhead.Style, TEXT_ALIGN_TYPE.TS_CENTER)
            //        {
            //            X = _clickedPoint.X,
            //            Y = _clickedPoint.Y
            //        };
            //        label.ControlInfo.Layer = UILayer.Over;

            //        //AddChildren(label);

            //        UIManager.Add(label);

            //        _labels.Add(label);
            //    }
            //}
            //else if (_labels.Count > 0)
            //{
            //    for (int i = 0; i < _labels.Count; i++)
            //    {
            //        _labels[i].Dispose();
            //        _labels[i] = null;
            //    }
            //    _labels.Clear();
            //}
        }

    }
}