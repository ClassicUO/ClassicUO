using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridLootGump : Gump
    {
        private readonly AlphaBlendControl _background;
        private readonly NiceButton _buttonPrev, _buttonNext;
        private readonly Item _corpse;
        private readonly Label _currentPageLabel;

        private int _currentPage = 1;
        private int _pagesCount;

        private static int _lastX = ProfileManager.Current.GridLootType == 2 ? 200 : 100;
        private static int _lastY = 100;

        public GridLootGump(Serial local) : base(local, 0)
        {
            _corpse = World.Items.Get(local);

            if (_corpse == null)
            {
                Dispose();

                return;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;

            _background = new AlphaBlendControl();
            _background.Width = 300;
            _background.Height = 400;
            Add(_background);

            Width = _background.Width;
            Height = _background.Height;

            NiceButton setLootBag = new NiceButton(3, Height - 23, 100, 20, ButtonAction.Activate, "Set loot bag") { ButtonParameter = 2, IsSelectable = false };
            Add(setLootBag);

            _buttonPrev = new NiceButton(Width - 80, Height - 20, 40, 20, ButtonAction.Activate, "<<") {ButtonParameter = 0, IsSelectable = false};
            _buttonNext = new NiceButton(Width - 40, Height - 20, 40, 20, ButtonAction.Activate, ">>") {ButtonParameter = 1, IsSelectable = false};

            _buttonNext.IsEnabled = _buttonPrev.IsEnabled = false;
            _buttonNext.IsVisible = _buttonPrev.IsVisible = false;


            Add(_buttonPrev);
            Add(_buttonNext);
            Add(_currentPageLabel = new Label("1", true, 999, align: IO.Resources.TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = Width / 2 - 5,
                Y = Height - 20,
            });

            _corpse.Items.Added += Items_Added;
            _corpse.Items.Removed += Items_Removed;      
        }

        private void Items_Removed(object sender, CollectionChangedEventArgs<Serial> e)
        {
            RedrawItems();
        }

        private void Items_Added(object sender, CollectionChangedEventArgs<Serial> e)
        {
            RedrawItems();
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
                GameActions.Print("Target the container to Grab items into.");
                TargetManager.SetTargeting(CursorTarget.SetGrabBag, Serial.INVALID, TargetType.Neutral);
            }
            else
                base.OnButtonClick(buttonID);
        }

        public void RedrawItems()
        {
            int x = 20;
            int y = 20;

            foreach (GridLootItem gridLootItem in Children.OfType<GridLootItem>()) gridLootItem.Dispose();

            int count = 0;
            _pagesCount = 1;

            foreach (Item item in _corpse.Items.Where(s => s != null && s.IsLootable))
            {
                GridLootItem gridItem = new GridLootItem(item);

                if (x >= _background.Width - 20)
                {
                    x = 20;
                    y += gridItem.Height + 20;

                    if (y >= _background.Height - 40)
                    {
                        _pagesCount++;
                        y = 20;
                    }
                }             

                gridItem.X = x;
                gridItem.Y = y;
                Add(gridItem, _pagesCount);

                x += gridItem.Width + 20;

                count++;
            }

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
                GameActions.Print("[GridLoot]: Corpse is empty!");
                Dispose();
            }
        }

        public override void Dispose()
        {
            if (_corpse != null)
            {
                if (_corpse == SelectedObject.CorpseObject)
                    SelectedObject.CorpseObject = null;

                _corpse.Items.Added -= Items_Added;
                _corpse.Items.Removed -= Items_Removed;
            }

            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            base.Draw(batcher, x, y);
            ResetHueVector();
            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            return true;
        }


        public override void Update(double totalMS, double frameMS)
        {
            if (_corpse == null || _corpse.IsDestroyed || _corpse.OnGround && _corpse.Distance > 3)
            {
                Dispose();

                return;
            }


            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            if (_corpse != null && !_corpse.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _corpse;
                SelectedObject.LastObject = _corpse;
                SelectedObject.CorpseObject = _corpse;
            }
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (_corpse != null && !_corpse.IsDestroyed) SelectedObject.CorpseObject = null;
        }


        private class GridLootItem : Control
        {
            private readonly TextureControl _texture;

            public GridLootItem(Serial serial)
            {
                LocalSerial = serial;

                Item item = World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                const int SIZE = 50;

                CanMove = false;

                HSliderBar amount = new HSliderBar(0, 0, SIZE, 1, item.Amount, item.Amount, HSliderBarStyle.MetalWidgetRecessedBar, true, color: 0xFFFF, drawUp: true);
                Add(amount);

                amount.IsVisible = amount.IsEnabled = amount.MaxValue > 1;


                AlphaBlendControl background = new AlphaBlendControl();
                background.Y = 15;
                background.Width = SIZE;
                background.Height = SIZE;
                Add(background);


                _texture = new TextureControl();
                _texture.IsPartial = item.ItemData.IsPartialHue;
                _texture.ScaleTexture = true;
                _texture.Hue = item.Hue;
                _texture.Texture = UOFileManager.Art.GetTexture(item.DisplayedGraphic);
                _texture.Y = 15;
                _texture.Width = SIZE;
                _texture.Height = SIZE;
                _texture.CanMove = false;

                if (World.ClientFeatures.TooltipsEnabled) _texture.SetTooltip(item);

                Add(_texture);


                _texture.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButton.Left)
                    {
                        GameActions.GrabItem(item, (ushort)amount.Value);
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

                batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y + 15, Width, Height - 15, ref _hueVector);

                if (_texture.MouseIsOver)
                {
                    _hueVector.Z = 0.7f;
                    batcher.Draw2D(Textures.GetTexture(Color.Yellow), x + 1, y + 15, Width - 1, Height - 15, ref _hueVector);
                    _hueVector.Z = 0;
                }

                return true;
            }
        }
    }
}