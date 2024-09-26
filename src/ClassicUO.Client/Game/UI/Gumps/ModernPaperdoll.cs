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
using System.Linq;
using System.Xml;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ModernPaperdoll : AnchorableGump
    {
        #region CONST
        private const int WIDTH = 250, HEIGHT = 380;
        private const int CELL_SPACING = 2, TOP_SPACING = 40;
        #endregion

        #region VARS
        private readonly Dictionary<Layer[], ItemSlot> itemLayerSlots;
        private Label titleLabel;
        private static int lastX = 100, lastY = 100;
        private GumpPic backgroundImage;
        #endregion

        public override GumpType GumpType => GumpType.PaperDoll;

        public ModernPaperdoll(uint localSerial) : base(localSerial, 0)
        {
            UIManager.GetGump<MinimizedPaperdoll>()?.Dispose();
            #region SET VARS
            AcceptMouseInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;
            AnchorType = ProfileManager.CurrentProfile.ModernPaperdollAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            Width = WIDTH;
            Height = HEIGHT;
            GroupMatrixHeight = Height;
            GroupMatrixWidth = Width;
            if (ProfileManager.CurrentProfile != null)
            {
                lastX = ProfileManager.CurrentProfile.ModernPaperdollPosition.X;
                lastY = ProfileManager.CurrentProfile.ModernPaperdollPosition.Y;
                IsLocked = ProfileManager.CurrentProfile.PaperdollLocked;
            }
            X = lastX;
            Y = lastY;

            itemLayerSlots = new Dictionary<Layer[], ItemSlot>();
            #endregion

            Add(backgroundImage = new GumpPic(0, 0, 40312, ProfileManager.CurrentProfile.ModernPaperDollHue));

            HitBox _menuHit = new HitBox(Width - 26, 1, 25, 16, alpha: 0f);
            Add(_menuHit);
            _menuHit.SetTooltip("Open paperdoll menu");
            _menuHit.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    UIManager.GetGump<MenuGump>()?.Dispose();
                    UIManager.Add(new MenuGump(Mouse.Position.X - 145, Mouse.Position.Y - 5, localSerial));
                }
            };

            #region SET UP ITEM SLOTS
            ItemSlot _;

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Earrings }) { X = 100 - 35 - CELL_SPACING, Y = TOP_SPACING + 15 };
            itemLayerSlots.Add(_.layers, _); //Earrings

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Helmet }) { X = 100, Y = TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Head

            _ = new ItemSlot(35, 35, new Layer[] { Layer.Necklace }) { X = 150 + CELL_SPACING, Y = TOP_SPACING + 15 };
            itemLayerSlots.Add(_.layers, _); //Amulet


            _ = new ItemSlot(50, 75, new Layer[] { Layer.OneHanded }) { X = 50 - CELL_SPACING, Y = 50 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //L Wep

            _ = new ItemSlot(50, 75, new Layer[] { Layer.Torso }) { X = 100, Y = 50 + CELL_SPACING + TOP_SPACING };
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

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Pants }) { X = 100, Y = 210 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Legs

            _ = new ItemSlot(50, 50, new Layer[] { Layer.Shoes }) { X = 150 + CELL_SPACING, Y = 210 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Boots



            _ = new ItemSlot(33, 34, new Layer[] { Layer.Talisman }) { X = 3, Y = 225 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Talisman

            _ = new ItemSlot(33, 34, new Layer[] { Layer.Backpack }) { X = Width - 36, Y = 225 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _); //Backpack


            _ = new ItemSlot(24, 24, new Layer[] { Layer.Tunic }) { X = 8, Y = 163 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);

            _ = new ItemSlot(24, 24, new Layer[] { Layer.Shirt }) { X = 8, Y = 193 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);


            _ = new ItemSlot(24, 24, new Layer[] { Layer.Skirt }) { X = Width - 32, Y = 163 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);

            _ = new ItemSlot(24, 24, new Layer[] { Layer.Legs }) { X = Width - 32, Y = 193 + CELL_SPACING + TOP_SPACING };
            itemLayerSlots.Add(_.layers, _);
            #endregion

            BuildLayerSlots();

            HitBox _virtueHitBox = new HitBox((WIDTH / 2) - 16, 1, 32, 32, "Virtues menu", 0f);
            _virtueHitBox.MouseDoubleClick += (s, e) =>
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
            Add(_virtueHitBox);

            Add(titleLabel = new Label("", true, 0xffff, maxwidth: WIDTH - 30, align: TEXT_ALIGN_TYPE.TS_CENTER) { X = 15, Y = 273 + CELL_SPACING + TOP_SPACING, AcceptMouseInput = false });

            HitBox _minHit = new HitBox(1, 1, 14, 18, alpha: 0f);
            _minHit.SetTooltip("Minimize paperdoll");
            _minHit.MouseUp += (s, e) =>
            {
                Dispose();
                UIManager.Add(new MinimizedPaperdoll(LocalSerial) { X = X, Y = Y });
            };
            Add(_minHit);

            RequestUpdateContents();
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

            UIManager.GetGump<CharacterPreview>()?.PaperDollPreview.RequestUpdate();

            Mobile m = World.Mobiles.Get(LocalSerial);
            if (m != null)
                UpdateTitle(m.Title);
        }

        public void UpdateOptions()
        {
            backgroundImage.Hue = ProfileManager.CurrentProfile.ModernPaperDollHue;
            AnchorType = ProfileManager.CurrentProfile.ModernPaperdollAnchorEnabled ? ANCHOR_TYPE.NONE : ANCHOR_TYPE.DISABLED;
            foreach (var layerSlot in itemLayerSlots)
            {
                layerSlot.Value.UpdateOptions();
            }
        }

        public static void UpdateAllOptions()
        {
            foreach (ModernPaperdoll p in UIManager.Gumps.OfType<ModernPaperdoll>())
            {
                p.UpdateOptions();
            }
        }

        public override void Update()
        {
            base.Update();

            if (X != lastX || Y != lastY)
            {
                lastX = X;
                lastY = Y;
                if (ProfileManager.CurrentProfile != null)
                    ProfileManager.CurrentProfile.ModernPaperdollPosition = new Point(X, Y);
            }
        }

        public override void Dispose()
        {
            if (ProfileManager.CurrentProfile != null)
                ProfileManager.CurrentProfile.ModernPaperdollPosition = new Point(X, Y);
            lastX = X;
            lastY = Y;

            base.Dispose();
            //World.OPL.OPLOnReceive -= OPL_OnOPLReceive;
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            if (ProfileManager.CurrentProfile != null)
            {
                X = lastX = ProfileManager.CurrentProfile.ModernPaperdollPosition.X;
                Y = lastY = ProfileManager.CurrentProfile.ModernPaperdollPosition.Y;
            }
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
            else if (TargetManager.IsTargeting)
            {
                if (SelectedObject.Object is Item item)
                    TargetManager.Target(item.Serial);
            }
        }

        private class ItemSlot : Control
        {
            public readonly Layer[] layers;
            private Area itemArea;

            private AlphaBlendControl durablityBar;

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
                itemArea.SetTooltip(layers[0].ToString());

                Add(durablityBar = new AlphaBlendControl(0.75f) { Width = 7, Height = Height, Hue = ProfileManager.CurrentProfile.ModernPaperDollDurabilityHue, IsVisible = false });

                this.layers = layers;
            }

            public void UpdateOptions()
            {
                durablityBar.Hue = ProfileManager.CurrentProfile.ModernPaperDollDurabilityHue;
            }

            public void AddItem(Item item)
            {
                itemArea.Add(new ItemGumpFixed(item, Width, Height) { HighlightOnMouseOver = false });
                UpdateDurability(item);
            }

            private void UpdateDurability(Item item)
            {
                if (IsDisposed || durablityBar.IsDisposed || item == null)
                {
                    return;
                }

                durablityBar.Hue = ProfileManager.CurrentProfile.ModernPaperDollDurabilityHue;

                if (World.DurabilityManager.TryGetDurability(item.Serial, out DurabiltyProp durabilty))
                {
                    if (durabilty.Percentage > (float)ProfileManager.CurrentProfile.ModernPaperDoll_DurabilityPercent / (float)100)
                    {
                        durablityBar.IsVisible = false;
                        return;
                    }
                    durablityBar.Height = (int)(Height * durabilty.Percentage);
                    durablityBar.Y = Height - durablityBar.Height;
                    durablityBar.IsVisible = true;
                }
                else
                {
                    durablityBar.IsVisible = false;
                }
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

                this.item = item;
            }

            private static ushort GetAnimID(ushort graphic, ushort animID, bool isfemale)
            {
                int offset = isfemale ? Constants.FEMALE_GUMP_OFFSET : Constants.MALE_GUMP_OFFSET;

                if (Client.Version >= Utility.ClientVersion.CV_7000 && animID == 0x03CA                          // graphic for dead shroud
                                                            && (graphic == 0x02B7 || graphic == 0x02B6)) // dead gargoyle graphics
                {
                    animID = 0x0223;
                }

                Client.Game.Animations.ConvertBodyIfNeeded(ref graphic);

                if (AnimationsLoader.Instance.EquipConversions.TryGetValue(graphic, out Dictionary<ushort, EquipConvData> dict))
                {
                    if (dict.TryGetValue(animID, out EquipConvData data))
                    {
                        if (data.Gump > Constants.MALE_GUMP_OFFSET)
                        {
                            animID = (ushort)(data.Gump >= Constants.FEMALE_GUMP_OFFSET ? data.Gump - Constants.FEMALE_GUMP_OFFSET : data.Gump - Constants.MALE_GUMP_OFFSET);
                        }
                        else
                        {
                            animID = data.Gump;
                        }
                    }
                }

                if (animID + offset > GumpsLoader.MAX_GUMP_DATA_INDEX_COUNT || Client.Game.Gumps.GetGump((ushort)(animID + offset)).Texture == null)
                {
                    // inverse
                    offset = isfemale ? Constants.MALE_GUMP_OFFSET : Constants.FEMALE_GUMP_OFFSET;
                }

                return (ushort)(animID + offset);
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
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

                ref readonly var texture = ref Client.Game.Arts.GetArt((uint)item.DisplayedGraphic);
                Rectangle _rect = Client.Game.Arts.GetRealArtBounds((uint)item.DisplayedGraphic);


                Point _originalSize = new Point(Width, Height);
                Point _point = new Point((Width >> 1) - (_originalSize.X >> 1), (Height >> 1) - (_originalSize.Y >> 1));

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

                if (_rect.Width > Width)
                {
                    _originalSize.X = Width;
                    _point.X = 0;
                }

                if (_rect.Height > Height)
                {
                    _originalSize.Y = Height;
                    _point.Y = 0;
                }

                if (texture.Texture != null)
                {
                    batcher.Draw
                    (
                        texture.Texture,
                        new Rectangle
                        (
                            x + _point.X,
                            y + _point.Y,
                            _originalSize.X,
                            _originalSize.Y
                        ),
                        new Rectangle
                        (
                            texture.UV.X + _rect.X,
                            texture.UV.Y + _rect.Y,
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
                Height = 281;
                AcceptMouseInput = true;

                Add(new AlphaBlendControl(0.85f) { Width = Width, Height = Height, AcceptMouseInput = false });

                var i = 1;

                NiceButton preview = new NiceButton(1, 1, Width - 2, 20, ButtonAction.Activate, "Preview");
                preview.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        UIManager.GetGump<CharacterPreview>()?.Dispose();
                        UIManager.Add(new CharacterPreview(localSerial) { X = 100, Y = 100 });
                    }
                };
                Add(preview);

                NiceButton help = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Help");
                help.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.RequestHelp();
                    }
                };
                Add(help);

                NiceButton options = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Options");
                options.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenSettings();
                    }
                };
                Add(options);

                NiceButton logout = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Log Out");
                logout.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        Client.Game.GetScene<GameScene>()?.RequestQuitGame();
                    }
                };
                Add(logout);

                NiceButton quests = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Quests");
                quests.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.RequestQuestMenu();
                    }
                };
                Add(quests);

                NiceButton skills = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Skills");
                skills.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenSkills();
                    }
                };
                Add(skills);

                NiceButton guild = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Guild");
                guild.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.OpenGuildGump();
                    }
                };
                Add(guild);

                NiceButton peace = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Peace/War");
                peace.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        GameActions.ToggleWarMode();
                    }
                };
                Add(peace);

                NiceButton durability = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Durability Tracker");
                durability.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        UIManager.GetGump<DurabilitysGump>()?.Dispose();
                        UIManager.Add(new DurabilitysGump());
                    }
                };
                Add(durability);

                NiceButton status = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Status");
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
                                UIManager.Add(StatusGumpBase.AddStatusGump(ProfileManager.CurrentProfile.StatusGumpPosition.X, ProfileManager.CurrentProfile.StatusGumpPosition.Y));
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
                                Rectangle bounds = Client.Game.Gumps.GetGump(0x0804).UV;

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

                NiceButton party = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Party");
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

                NiceButton profileEditor = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Profile");
                profileEditor.MouseUp += (s, e) =>
                {
                    GameActions.RequestProfile(LocalSerial);
                };
                Add(profileEditor);

                NiceButton abilities = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Abilities");
                abilities.MouseUp += (s, e) =>
                {
                    if (UIManager.GetGump<RacialAbilitiesBookGump>() == null)
                    {
                        UIManager.Add(new RacialAbilitiesBookGump(100, 100));
                    }
                };
                Add(abilities);

                NiceButton weaponAbilities = new NiceButton(1, 1 + 20 * i++, Width - 2, 20, ButtonAction.Activate, "Weapon abilities");
                weaponAbilities.MouseUp += (s, e) =>
                {
                    GameActions.OpenAbilitiesBook();
                };
                Add(weaponAbilities);

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
            public readonly PaperDollInteractable PaperDollPreview;
            public CharacterPreview(uint localSerial) : base(localSerial, 0)
            {
                Width = 190;
                Height = 250;
                CanCloseWithRightClick = true;
                CanMove = true;
                AcceptMouseInput = true;
                Add(new AlphaBlendControl(0.75f) { CanCloseWithRightClick = true, CanMove = true, Width = Width, Height = Height });

                Add(PaperDollPreview = new PaperDollInteractable(0, 0, LocalSerial, null) { AcceptMouseInput = false });

                Add(new SimpleBorder() { Width = Width - 1, Height = Height - 1, Alpha = 0.85f });

            }
        }

        private class MinimizedPaperdoll : Gump
        {
            public MinimizedPaperdoll(uint localSerial) : base(localSerial, 0)
            {
                Width = 86;
                Height = 23;
                AcceptMouseInput = true;
                CanMove = true;
                CanCloseWithRightClick = true;

                Add(new GumpPic(0, 0, 0x7EE, 0));

                Checkbox _;

                Add(_ = new Checkbox(0x00D2, 0x00D3) { X = 66, Y = 2 });
                _.IsChecked = ProfileManager.CurrentProfile.OpenModernPaperdollAtMinimizeLoc;
                _.SetTooltip("Open paperdoll at this location");
                _.MouseUp += (s, e) =>
                {
                    ProfileManager.CurrentProfile.OpenModernPaperdollAtMinimizeLoc = _.IsChecked;
                };
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);
                if (button == MouseButtonType.Left)
                {
                    Dispose();
                    UIManager.GetGump<ModernPaperdoll>()?.Dispose();

                    ModernPaperdoll pd = new ModernPaperdoll(LocalSerial);

                    if (ProfileManager.CurrentProfile.OpenModernPaperdollAtMinimizeLoc)
                    {
                        pd.X = X;
                        pd.Y = Y;
                    }

                    UIManager.Add(pd);
                }
            }
        }
    }
}
