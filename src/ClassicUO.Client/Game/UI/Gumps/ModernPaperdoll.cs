using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernPaperdoll : Gump
    {
        #region CONST
        private const int WIDTH = 250, HEIGHT = 380;
        private const int CELL_SPACING = 2, TOP_SPACING = 40;
        #endregion

        #region VARS
        private readonly Dictionary<Layer[], ItemSlot> itemLayerSlots;
        private Label titleLabel;
        private bool isMinimized = false;
        private static int lastX = 100, lastY = 100;
        #endregion

        public ModernPaperdoll(uint localSerial) : base(localSerial, 0)
        {
            #region ASSIGN FIELDS
            AcceptMouseInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;
            #endregion
            #region SET VARS
            Width = WIDTH;
            Height = HEIGHT;
            X = lastX;
            Y = lastY;
            itemLayerSlots = new Dictionary<Layer[], ItemSlot>();
            #endregion

            Add(new AlphaBlendControl(0.8f) { Width = WIDTH, Height = HEIGHT, Hue = 32 });

            MenuButton menu = new MenuButton(25, Color.White.PackedValue, 0.8f, "Open menu") { X = Width - 26, Y = 1 };
            menu.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.GetGump<MenuGump>()?.Dispose();
                    UIManager.Add(new MenuGump(Mouse.Position.X - 5, Mouse.Position.Y - 5, localSerial));
                }
            };
            Add(menu);

            #region SET UP ITEM SLOTS
            ItemSlot _;

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Earrings }) { X = 100 - 35 - CELL_SPACING, Y = TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Earrings

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Helmet }) { X = 100, Y = TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Head

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Necklace }) { X = 150 + CELL_SPACING, Y = TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Amulet


            _ = new ItemSlot(50, 75, new Layer[] { Layer.OneHanded }) { X = 50 - CELL_SPACING, Y = 50 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //L Wep

            _ = new ItemSlot(50, 75, new Layer[] { Layer.Torso, Layer.Tunic, Layer.Shirt }) { X = 100, Y = 50 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Chest

            _ = new ItemSlot(50, 75, new Layer[] { Layer.TwoHanded }) { X = 150 + CELL_SPACING, Y = 50 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //R Wep


            _ = new ItemSlot(50, 50, new Layer[] { Layer.Arms }) { X = 50 - CELL_SPACING, Y = 125 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Arms

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Robe }) { X = 100, Y = 125 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Robe

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Cloak }) { X = 150 + CELL_SPACING, Y = 125 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Cloak


            _ = new ItemSlot(35, 35, new Layer[] { Layer.Ring }) { X = 50 - CELL_SPACING, Y = 175 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Ring

            _ = new ItemSlot(80, 35, new Layer[] { Layer.Waist }) { X = 85, Y = 175 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Belt

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Bracelet }) { X = 165 + CELL_SPACING, Y = 175 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Bracelet


            _ = new ItemSlot(50, 50, new Layer[] { Layer.Gloves }) { X = 50 - CELL_SPACING, Y = 210 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Gloves

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Legs, Layer.Pants, Layer.Skirt }) { X = 100, Y = 210 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Legs

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Shoes }) { X = 150 + CELL_SPACING, Y = 210 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Boots



            _ = new ItemSlot(35, 35, new Layer[] { Layer.Talisman }) { X = 1, Y = 225 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Backpack }) { X = Width - 36, Y = 225 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);

            #endregion

            BuildLayerSlots();

            GumpPic _virtueMenuPic;
            Add(_virtueMenuPic = new GumpPic((WIDTH / 2) - 16, 1, 0x0071, 0));
            _virtueMenuPic.MouseDoubleClick += (s, e) =>
            {
                GameActions.ReplyGump
                (
                    World.Player,
                    0x000001CD,
                    0x00000001,
                    new[]
                    {
                                        LocalSerial
                    },
                    new Tuple<ushort, string>[0]
                );
            };

            Add(titleLabel = new Label("", true, 0xffff, maxwidth: WIDTH - 2, align: TEXT_ALIGN_TYPE.TS_CENTER) { X = 1, Y = 265 + CELL_SPACING + TOP_SPACING, AcceptMouseInput = false });

            GumpPic _minimize = new GumpPic(1, 1, 0x38, 0);
            _minimize.MouseUp += (s, e) =>
            {
                Dispose();
                UIManager.Add(new MinimizedPaperdoll(LocalSerial) { X = X, Y = Y });
            };
            Add(_minimize);

            RequestUpdateContents();

            Add(new SimpleBorder() { Width = WIDTH, Height = HEIGHT, Alpha = 0.8f });
        }

        public void UpdateTitle(string text)
        {
            titleLabel.Text = text;
        }

        private void BuildLayerSlots()
        {
            foreach (var layerSlot in itemLayerSlots)
            {
                Add(layerSlot.Value);
            }
        }

        protected override void UpdateContents()
        {
            base.UpdateContents();
            if (World.Player == null)
                return;

            foreach (var layerSlot in itemLayerSlots)
            {
                layerSlot.Value.ClearItems();

                foreach (Layer layer in layerSlot.Key)
                {
                    Item i = World.Player.FindItemByLayer(layer);
                    if (i != null && i.IsLootable)
                    {
                        layerSlot.Value.AddItem(i);
                    }
                }
            }

            Mobile m = World.Mobiles.Get(LocalSerial);
            if (m != null)
                UpdateTitle(m.Title);
        }

        public override void Update()
        {
            base.Update();

            if (X != lastX)
                lastX = X;
            if (Y != lastY)
                lastY = Y;
        }

        public override void Dispose()
        {
            base.Dispose();
            lastX = X;
            lastY = Y;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (Client.Game.GameCursor.ItemHold.Enabled)
            {
                if (LocalSerial == World.Player.Serial)
                {
                    if (SelectedObject.Object is Item item && (item.Layer == Layer.Backpack || item.ItemData.IsContainer))
                    {
                        GameActions.DropItem
                        (
                            Client.Game.GameCursor.ItemHold.Serial,
                            0xFFFF,
                            0xFFFF,
                            0,
                            item.Serial
                        );

                        Mouse.CancelDoubleClick = true;
                    }
                    else
                    {
                        if (Client.Game.GameCursor.ItemHold.ItemData.IsWearable)
                        {
                            Item equipment = World.Player.FindItemByLayer((Layer)Client.Game.GameCursor.ItemHold.ItemData.Layer);

                            if (equipment == null)
                            {
                                GameActions.Equip(World.Player);
                                Mouse.CancelDoubleClick = true;
                            }
                        }
                    }
                }
            }
        }

        private class ItemSlot : Control
        {
            public readonly Layer[] layers;
            private Area itemArea;

            public ItemSlot(int width, int height, Layer[] layers)
            {
                #region ASSIGN FIELDS
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = false;
                #endregion
                #region SET VARS
                Width = width;
                Height = height;
                #endregion

                Add(itemArea = new Area(false) { Width = Width, Height = Height, AcceptMouseInput = true, CanMove = true });

                Add(new SimpleBorder() { Width = Width, Height = Height, Alpha = 0.8f });
                this.layers = layers;
            }

            public void AddItem(Item item)
            {
                itemArea.Add(new ItemGumpFixed(item, Width, Height) { HighlightOnMouseOver = false });

                ItemGumpFixed highestLayer = null;
                foreach (Control c in itemArea.Children)
                {
                    if (c is ItemGumpFixed)
                    {
                        ItemGumpFixed itemG = (ItemGumpFixed)c;
                        itemG.IsVisible = false;

                        if (highestLayer == null)
                            highestLayer = itemG;

                        if (itemG.item.ItemData.Layer < highestLayer.item.ItemData.Layer)
                            highestLayer = itemG;
                    }
                }
                if (highestLayer != null)
                    highestLayer.IsVisible = true;
            }

            public void ClearItems()
            {
                itemArea.Children.Clear();
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                Parent?.InvokeMouseUp(new Point(x, y), button);
            }
        }

        private class ItemGumpFixed : ItemGump
        {
            private readonly Point _originalSize;
            private readonly Point _point;
            private readonly Rectangle _rect;
            public readonly Item item;

            public ItemGumpFixed(Item item, int w, int h) : base
            (
                item.Serial,
                item.DisplayedGraphic,
                item.Hue,
                item.X,
                item.Y
            )
            {
                if ((Layer)item.ItemData.Layer == Layer.Backpack && item.Container == World.Player.Serial)
                    CanPickUp = false;
                Width = w;
                Height = h;
                WantUpdateSize = false;

                _rect = ArtLoader.Instance.GetRealArtBounds(item.DisplayedGraphic);

                _originalSize.X = Width;
                _originalSize.Y = Height;

                if (_rect.Width < Width)
                {
                    _originalSize.X = _rect.Width;
                    _point.X = (Width >> 1) - (_originalSize.X >> 1);
                }

                if (_rect.Height < Height)
                {
                    _originalSize.Y = _rect.Height;
                    _point.Y = (Height >> 1) - (_originalSize.Y >> 1);
                }

                this.item = item;
            }


            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();
                }

                if (IsDisposed)
                {
                    return false;
                }

                Vector3 hueVector = ShaderHueTranslator.GetHueVector
                                    (
                                        MouseIsOver && HighlightOnMouseOver ? 0x0035 : item.Hue,
                                        item.ItemData.IsPartialHue,
                                        1,
                                        true
                                    );

                var texture = ArtLoader.Instance.GetStaticTexture(item.DisplayedGraphic, out var bounds);

                if (texture != null)
                {
                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + _point.X,
                            y + _point.Y,
                            _originalSize.X,
                            _originalSize.Y
                        ),
                        new Rectangle
                        (
                            bounds.X + _rect.X,
                            bounds.Y + _rect.Y,
                            _rect.Width,
                            _rect.Height
                        ),
                        hueVector
                    );

                    return true;
                }

                return false;
            }

            public override bool Contains(int x, int y)
            {
                return true;
            }
        }

        private class MenuButton : Control
        {
            public MenuButton(int width, uint hue, float alpha, string tooltip = "")
            {
                Width = width;
                Height = 16;
                AcceptMouseInput = true;
                Area _ = new Area() { Width = Width, Height = Height, AcceptMouseInput = false };

                Add(_);
                Add(new Line(2, 2, Width - 4, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
                Add(new Line(2, 7, Width - 4, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
                Add(new Line(2, 12, Width - 4, 2, hue) { Alpha = alpha, AcceptMouseInput = false });
                SetTooltip(tooltip);
                //_.SetTooltip(tooltip);
            }

            public override bool Contains(int x, int y)
            {
                return true;
            }
        }

        private class MenuGump : Gump
        {
            public MenuGump(int x, int y, uint localSerial) : base(localSerial, 0)
            {
                X = x;
                Y = y;
                Width = 150;
                Height = 241;
                AcceptMouseInput = true;

                Add(new AlphaBlendControl(0.85f) { Width = Width, Height = Height, AcceptMouseInput = false });

                NiceButton preview = new NiceButton(1, 1, Width - 2, 20, ButtonAction.Activate, "Preview");
                preview.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        UIManager.Add(new CharacterPreview(localSerial) { X = 100, Y = 100 });
                    }
                };
                Add(preview);

                NiceButton help = new NiceButton(1, 21, Width - 2, 20, ButtonAction.Activate, "Help");
                help.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.RequestHelp();
                    }
                };
                Add(help);

                NiceButton options = new NiceButton(1, 41, Width - 2, 20, ButtonAction.Activate, "Options");
                options.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenSettings();
                    }
                };
                Add(options);

                NiceButton logout = new NiceButton(1, 61, Width - 2, 20, ButtonAction.Activate, "Log Out");
                logout.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        Client.Game.GetScene<GameScene>()?.RequestQuitGame();
                    }
                };
                Add(logout);

                NiceButton quests = new NiceButton(1, 81, Width - 2, 20, ButtonAction.Activate, "Quests");
                quests.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.RequestQuestMenu();
                    }
                };
                Add(quests);

                NiceButton skills = new NiceButton(1, 101, Width - 2, 20, ButtonAction.Activate, "Skills");
                skills.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenSkills();
                    }
                };
                Add(skills);

                NiceButton guild = new NiceButton(1, 121, Width - 2, 20, ButtonAction.Activate, "Guild");
                guild.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenGuildGump();
                    }
                };
                Add(guild);

                NiceButton peace = new NiceButton(1, 141, Width - 2, 20, ButtonAction.Activate, "Peace/War");
                peace.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.ToggleWarMode();
                    }
                };
                Add(peace);

                NiceButton status = new NiceButton(1, 161, Width - 2, 20, ButtonAction.Activate, "Status");
                status.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        if (LocalSerial == World.Player)
                        {
                            UIManager.GetGump<BaseHealthBarGump>(LocalSerial)?.Dispose();

                            StatusGumpBase status = StatusGumpBase.GetStatusGump();

                            if (status == null)
                            {
                                UIManager.Add(StatusGumpBase.AddStatusGump(Mouse.Position.X - 100, Mouse.Position.Y - 25));
                            }
                            else
                            {
                                status.BringOnTop();
                            }
                        }
                        else
                        {
                            if (UIManager.GetGump<BaseHealthBarGump>(LocalSerial) != null)
                            {
                                return;
                            }

                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                            {
                                Rectangle bounds = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);

                                UIManager.Add
                                (
                                    new HealthBarGumpCustom(LocalSerial)
                                    {
                                        X = Mouse.Position.X - (bounds.Width >> 1),
                                        Y = Mouse.Position.Y - 5
                                    }
                                );
                            }
                            else
                            {
                                _ = GumpsLoader.Instance.GetGumpTexture(0x0804, out var bounds);

                                UIManager.Add
                                (
                                    new HealthBarGump(LocalSerial)
                                    {
                                        X = Mouse.Position.X - (bounds.Width >> 1),
                                        Y = Mouse.Position.Y - 5
                                    }
                                );
                            }
                        }
                    }
                };
                Add(status);

                NiceButton party = new NiceButton(1, 181, Width - 2, 20, ButtonAction.Activate, "Party");
                party.MouseUp += (s, e) =>
                {
                    PartyGump party = UIManager.GetGump<PartyGump>();

                    if (party == null)
                    {
                        int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                        int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                        UIManager.Add(new PartyGump(x, y, World.Party.CanLoot));
                    }
                    else
                    {
                        party.BringOnTop();
                    }
                };
                Add(party);

                NiceButton profileEditor = new NiceButton(1, 201, Width - 2, 20, ButtonAction.Activate, "Profile");
                profileEditor.MouseUp += (s, e) =>
                {
                    GameActions.RequestProfile(LocalSerial);
                };
                Add(profileEditor);

                NiceButton abilities = new NiceButton(1, 221, Width - 2, 20, ButtonAction.Activate, "Abilities");
                abilities.MouseUp += (s, e) =>
                {
                    if (UIManager.GetGump<RacialAbilitiesBookGump>() == null)
                    {
                        UIManager.Add(new RacialAbilitiesBookGump(100, 100));
                    }
                };
                Add(abilities);

                Add(new SimpleBorder() { Width = Width, Height = Height });
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);
                Dispose();
            }
        }

        private class CharacterPreview : Gump
        {
            public CharacterPreview(uint localSerial) : base(localSerial, 0)
            {
                Width = 200;
                Height = 250;
                CanCloseWithRightClick = true;
                CanMove = true;
                AcceptMouseInput = true;
                Add(new AlphaBlendControl(0.75f) { CanCloseWithRightClick = true, CanMove = true, Width = Width, Height = Height });

                Add(new PaperDollInteractable(0, 0, LocalSerial, null) { AcceptMouseInput = false });

                Add(new SimpleBorder() { Width = Width, Height = Height, Alpha = 0.85f });
            }
        }

        private class MinimizedPaperdoll : Gump
        {
            public MinimizedPaperdoll(uint localSerial) : base(localSerial, 0)
            {
                Width = 66;
                Height = 23;
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = true;

                Add(new GumpPic(0, 0, 0x7EE, 0));
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                if (button == MouseButtonType.Left)
                {
                    Dispose();
                    UIManager.GetGump<ModernPaperdoll>()?.Dispose();
                    UIManager.Add(new ModernPaperdoll(LocalSerial));
                }
            }
        }
    }
}
