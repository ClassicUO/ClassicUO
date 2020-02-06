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
using System.IO;
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class PaperDollGump : TextContainerGump
    {
        private static readonly ushort[] PeaceModeBtnGumps =
        {
            0x07e5, 0x07e6, 0x07e7
        };
        private static readonly ushort[] WarModeBtnGumps =
        {
            0x07e8, 0x07e9, 0x07ea
        };
        private GumpPic _combatBook, _racialAbilitiesBook;
        private bool _isWarMode, _isMinimized;

        private PaperDollInteractable _paperDollInteractable;
        private GumpPic _partyManifestPic;
        private GumpPic _profilePic;
        private GumpPic _virtueMenuPic;
        private Button _warModeBtn;

        private GumpPic _picBase;
        private HitBox _hitBox;
        private Label _titleLabel;


        public PaperDollGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public PaperDollGump(uint serial) : this()
        {
            LocalSerial = serial;
            Mobile mobile = World.Mobiles.Get(serial);

            if (mobile != null)
            {
                Mobile = mobile;
                BuildGump();
            }
            else
                Dispose();
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_PAPERDOLL;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _picBase.Graphic = value ? (ushort) 0x7EE : (ushort) (0x07d0 + (Mobile == World.Player ? 0 : 1)) ;

                    foreach (var c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _picBase.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        public Mobile Mobile { get; set; }

        public bool CanLift { get; set; }

        public override void Dispose()
        {
            UIManager.SavePosition(LocalSerial, Location);

            if (Mobile == World.Player)
            {
                if (_virtueMenuPic != null)
                    _virtueMenuPic.MouseDoubleClick -= VirtueMenu_MouseDoubleClickEvent;

                if (_partyManifestPic != null)
                    _partyManifestPic.MouseDoubleClick -= PartyManifest_MouseDoubleClickEvent;
            }

            Clear();
            base.Dispose();
        }

        private void _hitBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && !IsMinimized)
            {
                IsMinimized = true;
            }
        }


        protected override void OnMouseExit(int x, int y)
        {
            _paperDollInteractable.AddFakeDress(null);
        }

        protected override void OnMouseEnter(int x, int y)
        {
            if (ItemHold.Enabled)
            {
                Item it = new Item(ItemHold.Serial) { Graphic = ItemHold.Graphic, Hue = ItemHold.Hue };

                _paperDollInteractable.AddFakeDress(it);
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);

            if (ItemHold.Enabled)
            {

            }

        }

        private void BuildGump()
        {
            _picBase?.Dispose();
            _hitBox?.Dispose();


            if (LocalSerial == World.Player)
            {
                Add(_picBase = new GumpPic(0, 0, 0x07d0, 0));
                _picBase.MouseDoubleClick += _picBase_MouseDoubleClick;
                //HELP BUTTON
                Add(new Button((int)Buttons.Help, 0x07ef, 0x07f0, 0x07f1)
                {
                    X = 185,
                    Y = 44 + 27 * 0,
                    ButtonAction = ButtonAction.Activate
                });

                //OPTIONS BUTTON
                Add(new Button((int)Buttons.Options, 0x07d6, 0x07d7, 0x07d8)
                {
                    X = 185,
                    Y = 44 + 27 * 1,
                    ButtonAction = ButtonAction.Activate
                });

                // LOG OUT BUTTON
                Add(new Button((int)Buttons.LogOut, 0x07d9, 0x07da, 0x07db)
                {
                    X = 185,
                    Y = 44 + 27 * 2,
                    ButtonAction = ButtonAction.Activate
                });

                // QUESTS BUTTON
                Add(new Button((int)Buttons.Quests, 0x57b5, 0x57b7, 0x57b6)
                {
                    X = 185,
                    Y = 44 + 27 * 3,
                    ButtonAction = ButtonAction.Activate
                });

                // SKILLS BUTTON
                Add(new Button((int)Buttons.Skills, 0x07df, 0x07e0, 0x07e1)
                {
                    X = 185,
                    Y = 44 + 27 * 4,
                    ButtonAction = ButtonAction.Activate
                });

                // GUILD BUTTON
                Add(new Button((int)Buttons.Guild, 0x57b2, 0x57b4, 0x57b3)
                {
                    X = 185,
                    Y = 44 + 27 * 5,
                    ButtonAction = ButtonAction.Activate
                });
                // TOGGLE PEACE/WAR BUTTON
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;

                Add(_warModeBtn = new Button((int)Buttons.PeaceWarToggle, btngumps[0], btngumps[1], btngumps[2])
                {
                    X = 185,
                    Y = 44 + 27 * 6,
                    ButtonAction = ButtonAction.Activate
                });


                int profileX = 25;
                const int SCROLLS_STEP = 14;

                if (World.ClientFeatures.PaperdollBooks)
                {
                    Add(_combatBook = new GumpPic(156, 200, 0x2B34, 0));
                    _combatBook.MouseDoubleClick += (sender, e) => { GameActions.OpenAbilitiesBook(); };

                    if (Client.Version >= ClientVersion.CV_7000)
                    {
                        Add(_racialAbilitiesBook = new GumpPic(23, 200, 0x2B28, 0));

                        _racialAbilitiesBook.MouseDoubleClick += (sender, e) =>
                        {
                            if (UIManager.GetGump<RacialAbilitiesBookGump>() == null) UIManager.Add(new RacialAbilitiesBookGump(100, 100));
                        };
                        profileX += SCROLLS_STEP;
                    }
                }

                Add(_profilePic = new GumpPic(profileX, 196, 0x07D2, 0));
                _profilePic.MouseDoubleClick += Profile_MouseDoubleClickEvent;

                profileX += SCROLLS_STEP;

                Add(_partyManifestPic = new GumpPic(profileX, 196, 0x07D2, 0));
                _partyManifestPic.MouseDoubleClick += PartyManifest_MouseDoubleClickEvent;


                _hitBox = new HitBox(228, 260, 16, 16);
                _hitBox.MouseUp += _hitBox_MouseUp;

                Add(_hitBox);
            }
            else
            {
                Add(_picBase = new GumpPic(0, 0, 0x07d1, 0));
                Add(_profilePic = new GumpPic(25, 196, 0x07D2, 0));
                _profilePic.MouseDoubleClick += Profile_MouseDoubleClickEvent;
            }

            // STATUS BUTTON
            Add(new Button((int)Buttons.Status, 0x07eb, 0x07ec, 0x07ed)
            {
                X = 185,
                Y = 44 + 27 * 7,
                ButtonAction = ButtonAction.Activate
            });

            // Virtue menu
            Add(_virtueMenuPic = new GumpPic(80, 4, 0x0071, 0));
            _virtueMenuPic.MouseDoubleClick += VirtueMenu_MouseDoubleClickEvent;

            // Equipment slots for hat/earrings/neck/ring/bracelet
            Add(new EquipmentSlot(2, 75, Mobile, Layer.Helmet, this));
            Add(new EquipmentSlot(2, 75 + 21, Mobile, Layer.Earrings, this));
            Add(new EquipmentSlot(2, 75 + 21 * 2, Mobile, Layer.Necklace, this));
            Add(new EquipmentSlot(2, 75 + 21 * 3, Mobile, Layer.Ring, this));
            Add(new EquipmentSlot(2, 75 + 21 * 4, Mobile, Layer.Bracelet, this));
            Add(new EquipmentSlot(2, 75 + 21 * 5, Mobile, Layer.Tunic, this));

            // Paperdoll control!
            _paperDollInteractable = new PaperDollInteractable(8, 19, Mobile, this);
            //_paperDollInteractable.MouseOver += (sender, e) =>
            //{
            //    OnMouseOver(e.X, e.Y);
            //};
            Add(_paperDollInteractable);

            // Name and title
            _titleLabel = new Label(Mobile.Title, false, 0x0386, 185)
            {
                X = 39,
                Y = 262
            };
            Add(_titleLabel);
        }

        private void _picBase_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && IsMinimized)
            {
                IsMinimized = false;
            }
        }

        public void UpdateTitle(string text)
        {
            _titleLabel.Text = text;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            GameScene gs = Client.Game.GetScene<GameScene>();

            if (!ItemHold.Enabled || !gs.IsMouseOverUI || _paperDollInteractable.IsOverBackpack)
                return;

            gs.WearHeldItem(Mobile);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            return true;
        }

        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                GameActions.ReplyGump(World.Player,
                                      0x000001CD,
                                      0x00000001,
                                      new[]
                                      {
                                          Mobile.Serial
                                      }, new Tuple<ushort, string>[0]);
            }
        }

        private void Profile_MouseDoubleClickEvent(object o, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left) GameActions.RequestProfile(Mobile.Serial);
        }

        private void PartyManifest_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                var party = UIManager.GetGump<PartyGump>();

                if (party == null)
                {
                    int x = Client.Game.Window.ClientBounds.Width / 2 - 272;
                    int y = Client.Game.Window.ClientBounds.Height / 2 - 240;
                    UIManager.Add(new PartyGump(x, y, World.Party.CanLoot));
                }
                else
                    party.BringOnTop();
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (Mobile != null && Mobile.IsDestroyed)
                Mobile = null;

            if (Mobile == null)
            {
                Dispose();

                return;
            }

            // This is to update the state of the war mode button.
            if (_isWarMode != Mobile.InWarMode && Mobile == World.Player)
            {
                _isWarMode = Mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                _warModeBtn.ButtonGraphicNormal = btngumps[0];
                _warModeBtn.ButtonGraphicPressed = btngumps[1];
                _warModeBtn.ButtonGraphicOver = btngumps[2];
            }


            base.Update(totalMS, frameMS);
        }




        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(Mobile.Serial);
            writer.Write(IsMinimized);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            if (Configuration.Profile.GumpsVersion == 2)
            {
                reader.ReadUInt32();
                _isMinimized = reader.ReadBoolean();
            }
            LocalSerial = reader.ReadUInt32();
            Client.Game.GetScene<GameScene>().DoubleClickDelayed(LocalSerial);
            if (Profile.GumpsVersion >= 3)
            {
                _isMinimized = reader.ReadBoolean();
            }
            Dispose();
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("isminimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);


            if (LocalSerial == World.Player)
            {
                Mobile = World.Player;
                BuildGump();

                Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);
                //GameActions.OpenPaperdoll(World.Player);
                IsMinimized = bool.Parse(xml.GetAttribute("isminimized"));

                Dispose();
            }
            else 
                Dispose();
        }

        public void Update()
        {
            _paperDollInteractable.Update();
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons)buttonID)
            {
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;

                case Buttons.Options:

                    OptionsGump gump = UIManager.GetGump<OptionsGump>();

                    if (gump == null)
                    {
                        UIManager.Add(new OptionsGump
                        {
                            X = (Client.Game.Window.ClientBounds.Width >> 1) - 300,
                            Y = (Client.Game.Window.ClientBounds.Height >> 1) - 250
                        });
                    }
                    else
                    {
                        gump.SetInScreen();
                        gump.BringOnTop();
                    }


                    break;

                case Buttons.LogOut:
                    Client.Game.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case Buttons.Quests:
                    GameActions.RequestQuestMenu();

                    break;

                case Buttons.Skills:

                    World.SkillsRequested = true;
                    NetClient.Socket.Send(new PSkillsRequest(World.Player));

                    break;

                case Buttons.Guild:
                    GameActions.OpenGuildGump();

                    break;

                case Buttons.PeaceWarToggle:
                    GameActions.ChangeWarMode();

                    break;

                case Buttons.Status:

                    if (Mobile == World.Player)
                    {
                        UIManager.GetGump<BaseHealthBarGump>(Mobile)?.Dispose();

                        StatusGumpBase status = StatusGumpBase.GetStatusGump();

                        if (status == null)
                            StatusGumpBase.AddStatusGump(Mouse.Position.X - 100, Mouse.Position.Y - 25);
                        else
                            status.BringOnTop();
                    }
                    else
                    {
                        if (UIManager.GetGump<BaseHealthBarGump>(Mobile) != null)
                            break;

                        GameActions.RequestMobileStatus(Mobile);

                        if (ProfileManager.Current.CustomBarsToggled)
                        {
                            Rectangle bounds = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);

                            UIManager.Add(new HealthBarGumpCustom(Mobile)
                            {
                                X = Mouse.Position.X - (bounds.Width >> 1),
                                Y = Mouse.Position.Y - 5
                            });
                        }
                        else
                        {
                            Rectangle bounds = GumpsLoader.Instance.GetTexture(0x0804).Bounds;

                            UIManager.Add(new HealthBarGump(Mobile)
                            {
                                X = Mouse.Position.X - (bounds.Width >> 1),
                                Y = Mouse.Position.Y - 5
                            });
                        }
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Help,
            Options,
            LogOut,
            Quests,
            Skills,
            Guild,
            PeaceWarToggle,
            Status
        }



        private class EquipmentSlot : Control
        {
            private readonly Layer _layer;
            private readonly Mobile _mobile;
            private ItemGumpFixed _itemGump;
            private readonly PaperDollGump _paperDollGump;
            private Item _item;

            public EquipmentSlot(int x, int y, Mobile mobile, Layer layer, PaperDollGump paperDollGump)
            {
                X = x;
                Y = y;
                Width = 19;
                Height = 20;
                _paperDollGump = paperDollGump;
                _mobile = mobile;
                _layer = layer;

                Add(new GumpPicTiled(0, 0, 19, 20, 0x243A)
                {
                    AcceptMouseInput = false
                });

                Add(new GumpPic(0, 0, 0x2344, 0)
                {
                    AcceptMouseInput = false
                });
                AcceptMouseInput = true;

                WantUpdateSize = false;
            }

            public override void Update(double totalMS, double frameMS)
            {
                if (_item != null && _item.IsDestroyed)
                {
                    _item = null;
                    _itemGump.Dispose();
                    _itemGump = null;
                }

                if (_item != _mobile.Equipment[(int)_layer])
                {
                    if (_itemGump != null)
                    {
                        _itemGump.Dispose();
                        _itemGump = null;
                    }

                    _item = _mobile.Equipment[(int)_layer];

                    if (_item != null)
                    {
                        Add(_itemGump = new ItemGumpFixed(_item, 18, 18)
                        {
                            X = 0,
                            Y = 0,
                            Width = 18,
                            Height = 18,
                            HighlightOnMouseOver = false,
                            CanPickUp = World.InGame && (World.Player == _mobile || _paperDollGump.CanLift),

                        });
                    }
                }


                base.Update(totalMS, frameMS);
            }

            private class ItemGumpFixed : ItemGump
            {
                private readonly Point _originalSize;
                private readonly Point _point;
                private readonly Rectangle _rect;

                public ItemGumpFixed(Item item, int w, int h) : base(item)
                {
                    Width = w;
                    Height = h;
                    WantUpdateSize = false;

                    ArtTexture texture = (ArtTexture)Texture;

                    _originalSize.X = Width;
                    _originalSize.Y = Height;
                    _rect = texture.ImageRectangle;

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
                }


                public override bool Draw(UltimaBatcher2D batcher, int x, int y)
                {
                    Item item = World.Items.Get(LocalSerial);

                    if (item == null)
                    {
                        Dispose();
                    }

                    if (IsDisposed)
                        return false;

                    ResetHueVector();
                    ShaderHuesTraslator.GetHueVector(ref _hueVector, MouseIsOver && HighlightOnMouseOver ? 0x0035 : item.Hue, item.ItemData.IsPartialHue, 0, true);

                    return batcher.Draw2D(Texture, x + _point.X, y + _point.Y,
                                          _originalSize.X, _originalSize.Y,
                                          _rect.X, _rect.Y,
                                          _rect.Width, _rect.Height,
                                          ref _hueVector);
                }

                public override bool Contains(int x, int y)
                {
                    return true;
                }
            }
        }
    }
}
