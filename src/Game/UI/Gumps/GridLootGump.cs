﻿#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridLootGump : Gump
    {
        private const int MAX_WIDTH = 300;
        private const int MAX_HEIGHT = 400;

        private static int _lastX = ProfileManager.CurrentProfile.GridLootType == 2 ? 200 : 100;
        private static int _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly NiceButton _buttonPrev, _buttonNext, _setlootbag;
        private readonly Item _corpse;

        private int _currentPage = 1;
        private readonly Label _currentPageLabel;
        private readonly bool _hideIfEmpty;
        private int _pagesCount;

        public GridLootGump(uint local) : base(local, 0)
        {
            _corpse = World.Items.Get(local);

            if (_corpse == null)
            {
                Dispose();

                return;
            }

            if (World.Player.ManualOpenedCorpses.Contains(LocalSerial))
            {
                World.Player.ManualOpenedCorpses.Remove(LocalSerial);
            }
            else if (World.Player.AutoOpenedCorpses.Contains(LocalSerial) && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.SkipEmptyCorpse)
            {
                IsVisible = false;
                _hideIfEmpty = true;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;
            _background = new AlphaBlendControl();
            //_background.Width = MAX_WIDTH;
            //_background.Height = MAX_HEIGHT;
            Add(_background);

            Width = _background.Width;
            Height = _background.Height;

            _setlootbag = new NiceButton
            (
                3,
                Height - 23,
                100,
                20,
                ButtonAction.Activate,
                ResGumps.SetLootBag
            ) { ButtonParameter = 2, IsSelectable = false };

            Add(_setlootbag);

            _buttonPrev = new NiceButton
            (
                Width - 80,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Prev
            ) { ButtonParameter = 0, IsSelectable = false };

            _buttonNext = new NiceButton
            (
                Width - 40,
                Height - 20,
                40,
                20,
                ButtonAction.Activate,
                ResGumps.Next
            ) { ButtonParameter = 1, IsSelectable = false };

            _buttonNext.IsVisible = _buttonPrev.IsVisible = false;


            Add(_buttonPrev);
            Add(_buttonNext);

            Add
            (
                _currentPageLabel = new Label("1", true, 999, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = Width / 2 - 5,
                    Y = Height - 20
                }
            );
        }


        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _currentPage--;

                if (_currentPage <= 1)
                {
                    _currentPage = 1;
                    _buttonPrev.IsVisible = false;
                }

                _buttonNext.IsVisible = true;
                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 1)
            {
                _currentPage++;

                if (_currentPage >= _pagesCount)
                {
                    _currentPage = _pagesCount;
                    _buttonNext.IsVisible = false;
                }

                _buttonPrev.IsVisible = true;

                ChangePage(_currentPage);

                _currentPageLabel.Text = ActivePage.ToString();
                _currentPageLabel.X = Width / 2 - _currentPageLabel.Width / 2;
            }
            else if (buttonID == 2)
            {
                GameActions.Print(ResGumps.TargetContainerToGrabItemsInto);
                TargetManager.SetTargeting(CursorTarget.SetGrabBag, 0, TargetType.Neutral);
            }
            else
            {
                base.OnButtonClick(buttonID);
            }
        }


        protected override void UpdateContents()
        {
            const int GRID_ITEM_SIZE = 50;

            int x = 20;
            int y = 20;

            foreach (GridLootItem gridLootItem in Children.OfType<GridLootItem>())
            {
                gridLootItem.Dispose();
            }

            int count = 0;
            _pagesCount = 1;

            _background.Width = x;
            _background.Height = y;

            int line = 1;
            int row = 0;

            for (LinkedObject i = _corpse.Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (it.IsLootable)
                {
                    GridLootItem gridItem = new GridLootItem(it, GRID_ITEM_SIZE);

                    if (x >= MAX_WIDTH - 20)
                    {
                        x = 20;
                        ++line;

                        y += gridItem.Height + 20;

                        if (y >= MAX_HEIGHT - 40)
                        {
                            _pagesCount++;
                            y = 20;
                            //line = 1;
                        }
                    }

                    gridItem.X = x;
                    gridItem.Y = y;
                    Add(gridItem, _pagesCount);

                    x += gridItem.Width + 20;
                    ++row;
                    ++count;
                }
            }

            _background.Width = (GRID_ITEM_SIZE + 20) * row + 20;
            _background.Height = 20 + 40 + (GRID_ITEM_SIZE + 20) * line + 20;


            if (_background.Height >= MAX_HEIGHT - 40)
            {
                _background.Height = MAX_HEIGHT;
            }

            _background.Width = MAX_WIDTH;

            if (ActivePage <= 1)
            {
                ActivePage = 1;
                _buttonNext.IsVisible = _pagesCount > 1;
                _buttonPrev.IsVisible = false;
            }
            else if (ActivePage >= _pagesCount)
            {
                ActivePage = _pagesCount;
                _buttonNext.IsVisible = false;
                _buttonPrev.IsVisible = _pagesCount > 1;
            }
            else if (ActivePage > 1 && ActivePage < _pagesCount)
            {
                _buttonNext.IsVisible = true;
                _buttonPrev.IsVisible = true;
            }

            if (count == 0)
            {
                GameActions.Print(ResGumps.CorpseIsEmpty);
                Dispose();
            }
            else if (_hideIfEmpty && !IsVisible)
            {
                IsVisible = true;
            }
        }

        public override void Dispose()
        {
            if (_corpse != null)
            {
                if (_corpse == SelectedObject.CorpseObject)
                {
                    SelectedObject.CorpseObject = null;
                }
            }

            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible || IsDisposed)
            {
                return false;
            }

            ResetHueVector();
            base.Draw(batcher, x, y);
            ResetHueVector();

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                ref HueVector
            );

            return true;
        }


        public override void Update(double totalTime, double frameTime)
        {
            if (_corpse == null || _corpse.IsDestroyed || _corpse.OnGround && _corpse.Distance > 3)
            {
                Dispose();

                return;
            }

            base.Update(totalTime, frameTime);

            if (IsDisposed)
            {
                return;
            }

            if (_background.Width < 100)
            {
                _background.Width = 100;
            }

            if (_background.Height < 100)
            {
                _background.Height = 100;
            }

            Width = _background.Width;
            Height = _background.Height;

            _buttonPrev.X = Width - 80;
            _buttonPrev.Y = Height - 23;
            _buttonNext.X = Width - 40;
            _buttonNext.Y = Height - 20;
            _setlootbag.X = 3;
            _setlootbag.Y = Height - 23;
            _currentPageLabel.X = Width / 2 - 5;
            _currentPageLabel.Y = Height - 20;

            WantUpdateSize = true;

            if (_corpse != null && !_corpse.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _corpse;
                SelectedObject.LastObject = _corpse;
                SelectedObject.CorpseObject = _corpse;
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_corpse != null && !_corpse.IsDestroyed)
            {
                SelectedObject.CorpseObject = null;
            }
        }


        private class GridLootItem : Control
        {
            private readonly TextureControl _texture;

            public GridLootItem(uint serial, int size)
            {
                LocalSerial = serial;

                Item item = World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                CanMove = false;

                HSliderBar amount = new HSliderBar
                (
                    0,
                    0,
                    size,
                    1,
                    item.Amount,
                    item.Amount,
                    HSliderBarStyle.MetalWidgetRecessedBar,
                    true,
                    color: 0xFFFF,
                    drawUp: true
                );

                Add(amount);

                amount.IsVisible = amount.IsEnabled = amount.MaxValue > 1;


                AlphaBlendControl background = new AlphaBlendControl();
                background.Y = 15;
                background.Width = size;
                background.Height = size;
                Add(background);


                _texture = new TextureControl();
                _texture.IsPartial = item.ItemData.IsPartialHue;
                _texture.ScaleTexture = true;
                _texture.Hue = item.Hue;
                _texture.Texture = ArtLoader.Instance.GetTexture(item.DisplayedGraphic);
                _texture.Y = 15;
                _texture.Width = size;
                _texture.Height = size;
                _texture.CanMove = false;

                if (World.ClientFeatures.TooltipsEnabled)
                {
                    _texture.SetTooltip(item);
                }

                Add(_texture);


                _texture.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.GrabItem(item, (ushort) amount.Value);
                    }
                };

                Width = background.Width;
                Height = background.Height + 15;

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                base.Draw(batcher, x, y);
                ResetHueVector();

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x,
                    y + 15,
                    Width,
                    Height - 15,
                    ref HueVector
                );

                if (_texture.MouseIsOver)
                {
                    HueVector.Z = 0.7f;

                    batcher.Draw2D
                    (
                        SolidColorTextureCache.GetTexture(Color.Yellow),
                        x + 1,
                        y + 15,
                        Width - 1,
                        Height - 15,
                        ref HueVector
                    );

                    HueVector.Z = 0;
                }

                return true;
            }
        }
    }
}