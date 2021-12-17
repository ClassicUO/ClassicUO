#region license

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
        private HitBox _hitBox;
        private bool _isWarMode, _isMinimized;

        private PaperDollInteractable _paperDollInteractable;
        private GumpPic _partyManifestPic;

        private GumpPic _picBase;
        private GumpPic _profilePic;
        private readonly EquipmentSlot[] _slots = new EquipmentSlot[6];
        private Label _titleLabel;
        private GumpPic _virtueMenuPic;
        private Button _warModeBtn;


        public PaperDollGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;
        }

        public PaperDollGump(uint serial, bool canLift) : this()
        {
            LocalSerial = serial;
            CanLift = canLift;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.PaperDoll;

        public bool IsMinimized
        {
            get => _isMinimized;
            set
            {
                if (_isMinimized != value)
                {
                    _isMinimized = value;

                    _picBase.Graphic = value ? (ushort) 0x7EE : (ushort) (0x07d0 + (LocalSerial == World.Player ? 0 : 1));

                    foreach (Control c in Children)
                    {
                        c.IsVisible = !value;
                    }

                    _picBase.IsVisible = true;
                    WantUpdateSize = true;
                }
            }
        }

        public bool CanLift { get; set; }

        public override void Dispose()
        {
            UIManager.SavePosition(LocalSerial, Location);

            if (LocalSerial == World.Player)
            {
                if (_virtueMenuPic != null)
                {
                    _virtueMenuPic.MouseDoubleClick -= VirtueMenu_MouseDoubleClickEvent;
                }

                if (_partyManifestPic != null)
                {
                    _partyManifestPic.MouseDoubleClick -= PartyManifest_MouseDoubleClickEvent;
                }
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


        private void BuildGump()
        {
            _picBase?.Dispose();
            _hitBox?.Dispose();


            if (LocalSerial == World.Player)
            {
                Add(_picBase = new GumpPic(0, 0, 0x07d0, 0));
                _picBase.MouseDoubleClick += _picBase_MouseDoubleClick;

                //HELP BUTTON
                Add
                (
                    new Button((int) Buttons.Help, 0x07ef, 0x07f0, 0x07f1)
                    {
                        X = 185,
                        Y = 44 + 27 * 0,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                //OPTIONS BUTTON
                Add
                (
                    new Button((int) Buttons.Options, 0x07d6, 0x07d7, 0x07d8)
                    {
                        X = 185,
                        Y = 44 + 27 * 1,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // LOG OUT BUTTON
                Add
                (
                    new Button((int) Buttons.LogOut, 0x07d9, 0x07da, 0x07db)
                    {
                        X = 185,
                        Y = 44 + 27 * 2,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // QUESTS BUTTON
                Add
                (
                    new Button((int) Buttons.Quests, 0x57b5, 0x57b7, 0x57b6)
                    {
                        X = 185,
                        Y = 44 + 27 * 3,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // SKILLS BUTTON
                Add
                (
                    new Button((int) Buttons.Skills, 0x07df, 0x07e0, 0x07e1)
                    {
                        X = 185,
                        Y = 44 + 27 * 4,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // GUILD BUTTON
                Add
                (
                    new Button((int) Buttons.Guild, 0x57b2, 0x57b4, 0x57b3)
                    {
                        X = 185,
                        Y = 44 + 27 * 5,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                // TOGGLE PEACE/WAR BUTTON
                Mobile mobile = World.Mobiles.Get(LocalSerial);

                _isWarMode = mobile?.InWarMode ?? false;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;

                Add
                (
                    _warModeBtn = new Button((int) Buttons.PeaceWarToggle, btngumps[0], btngumps[1], btngumps[2])
                    {
                        X = 185,
                        Y = 44 + 27 * 6,
                        ButtonAction = ButtonAction.Activate
                    }
                );


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
                            if (UIManager.GetGump<RacialAbilitiesBookGump>() == null)
                            {
                                UIManager.Add(new RacialAbilitiesBookGump(100, 100));
                            }
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
            Add
            (
                new Button((int) Buttons.Status, 0x07eb, 0x07ec, 0x07ed)
                {
                    X = 185,
                    Y = 44 + 27 * 7,
                    ButtonAction = ButtonAction.Activate
                }
            );

            // Virtue menu
            Add(_virtueMenuPic = new GumpPic(80, 4, 0x0071, 0));
            _virtueMenuPic.MouseDoubleClick += VirtueMenu_MouseDoubleClickEvent;

            // Equipment slots for hat/earrings/neck/ring/bracelet
            Add
            (
                _slots[0] = new EquipmentSlot
                (
                    0,
                    2,
                    75,
                    Layer.Helmet,
                    this
                )
            );

            Add
            (
                _slots[1] = new EquipmentSlot
                (
                    0,
                    2,
                    75 + 21,
                    Layer.Earrings,
                    this
                )
            );

            Add
            (
                _slots[2] = new EquipmentSlot
                (
                    0,
                    2,
                    75 + 21 * 2,
                    Layer.Necklace,
                    this
                )
            );

            Add
            (
                _slots[3] = new EquipmentSlot
                (
                    0,
                    2,
                    75 + 21 * 3,
                    Layer.Ring,
                    this
                )
            );

            Add
            (
                _slots[4] = new EquipmentSlot
                (
                    0,
                    2,
                    75 + 21 * 4,
                    Layer.Bracelet,
                    this
                )
            );

            Add
            (
                _slots[5] = new EquipmentSlot
                (
                    0,
                    2,
                    75 + 21 * 5,
                    Layer.Tunic,
                    this
                )
            );

            // Paperdoll control!
            _paperDollInteractable = new PaperDollInteractable(8, 19, LocalSerial, this);
            //_paperDollInteractable.MouseOver += (sender, e) =>
            //{
            //    OnMouseOver(e.X, e.Y);
            //};
            Add(_paperDollInteractable);

            // Name and title
            _titleLabel = new Label("", false, 0x0386, 185)
            {
                X = 39,
                Y = 262
            };

            Add(_titleLabel);

            RequestUpdateContents();
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


        private void VirtueMenu_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
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
            }
        }

        private void Profile_MouseDoubleClickEvent(object o, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                GameActions.RequestProfile(LocalSerial);
            }
        }

        private void PartyManifest_MouseDoubleClickEvent(object sender, MouseDoubleClickEventArgs args)
        {
            if (args.Button == MouseButtonType.Left)
            {
                if (CUOEnviroment.IsOutlands)
                {
                    NetClient.Socket.Send_ASCIISpeechRequest("party", MessageType.Command, 0, 0);
                }
                else
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
                }
            }
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (mobile != null && mobile.IsDestroyed)
            {
                Dispose();

                return;
            }

            // This is to update the state of the war mode button.
            if (mobile != null && _isWarMode != mobile.InWarMode && LocalSerial == World.Player)
            {
                _isWarMode = mobile.InWarMode;
                ushort[] btngumps = _isWarMode ? WarModeBtnGumps : PeaceModeBtnGumps;
                _warModeBtn.ButtonGraphicNormal = btngumps[0];
                _warModeBtn.ButtonGraphicPressed = btngumps[1];
                _warModeBtn.ButtonGraphicOver = btngumps[2];
            }

            base.Update(totalTime, frameTime);


            if (_paperDollInteractable != null && (CanLift || LocalSerial == World.Player.Serial))
            {
                bool force_false = SelectedObject.Object is Item item && (item.Layer == Layer.Backpack || item.ItemData.IsContainer);

                if (_paperDollInteractable.HasFakeItem && !ItemHold.Enabled || force_false)
                {
                    _paperDollInteractable.SetFakeItem(false);
                }
                else if (!_paperDollInteractable.HasFakeItem && ItemHold.Enabled && !ItemHold.IsFixedPosition && UIManager.MouseOverControl?.RootParent == this)
                {
                    if (ItemHold.ItemData.AnimID != 0)
                    {
                        if (mobile != null && mobile.FindItemByLayer((Layer) ItemHold.ItemData.Layer) == null)
                        {
                            _paperDollInteractable.SetFakeItem(true);
                        }
                    }
                }
            }
        }


        protected override void OnMouseExit(int x, int y)
        {
            _paperDollInteractable?.SetFakeItem(false);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left && World.InGame)
            {
                Mobile container = World.Mobiles.Get(LocalSerial);

                if (ItemHold.Enabled)
                {
                    if (CanLift || LocalSerial == World.Player.Serial)
                    {
                        if (SelectedObject.Object is Item item && (item.Layer == Layer.Backpack || item.ItemData.IsContainer))
                        {
                            GameActions.DropItem
                            (
                                ItemHold.Serial,
                                0xFFFF,
                                0xFFFF,
                                0,
                                item.Serial
                            );

                            Mouse.CancelDoubleClick = true;
                        }
                        else
                        {
                            if (ItemHold.ItemData.IsWearable)
                            {
                                Item equipment = container.FindItemByLayer((Layer) ItemHold.ItemData.Layer);

                                if (equipment == null)
                                {
                                    GameActions.Equip(LocalSerial != World.Player ? container : World.Player);
                                    Mouse.CancelDoubleClick = true;
                                }
                            }
                        }
                    }
                }
                else if (SelectedObject.Object is Item item)
                {
                    if (TargetManager.IsTargeting)
                    {
                        TargetManager.Target(item.Serial);
                        Mouse.CancelDoubleClick = true;
                        Mouse.LastLeftButtonClickTime = 0;

                        if (TargetManager.TargetingState == CursorTarget.SetTargetClientSide)
                        {
                            UIManager.Add(new InspectorGump(item));
                        }
                    }
                    else if (!DelayedObjectClickManager.IsEnabled)
                    {
                        Point off = Mouse.LDragOffset;

                        DelayedObjectClickManager.Set(item.Serial, Mouse.Position.X - off.X - ScreenCoordinateX, Mouse.Position.Y - off.Y - ScreenCoordinateY, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                    }
                }
            }
            else
            {
                base.OnMouseUp(x, y, button);
            }
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
                BuildGump();

                //GameActions.DoubleClick(0x8000_0000 | LocalSerial);
                Client.Game.GetScene<GameScene>()?.DoubleClickDelayed(LocalSerial);

                IsMinimized = bool.Parse(xml.GetAttribute("isminimized"));
            }
            else
            {
                Dispose();
            }
        }


        protected override void UpdateContents()
        {
            Mobile mobile = World.Mobiles.Get(LocalSerial);

            if (mobile != null && mobile.Title != _titleLabel.Text)
            {
                UpdateTitle(mobile.Title);
            }

            _paperDollInteractable.Update();

            if (mobile != null)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    int idx = (int) _slots[i].Layer;

                    _slots[i].LocalSerial = mobile.FindItemByLayer((Layer) idx)?.Serial ?? 0;
                }
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            if (ItemHold.Enabled && !ItemHold.IsFixedPosition)
            {
                OnMouseUp(0, 0, MouseButtonType.Left);

                return;
            }

            switch ((Buttons) buttonID)
            {
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;

                case Buttons.Options:
                    GameActions.OpenSettings();

                    break;

                case Buttons.LogOut:
                    Client.Game.GetScene<GameScene>()?.RequestQuitGame();

                    break;

                case Buttons.Quests:
                    GameActions.RequestQuestMenu();

                    break;

                case Buttons.Skills:
                    GameActions.OpenSkills();

                    break;

                case Buttons.Guild:
                    GameActions.OpenGuildGump();

                    break;

                case Buttons.PeaceWarToggle:
                    GameActions.ToggleWarMode();

                    break;

                case Buttons.Status:

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
                            break;
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
            private ItemGumpFixed _itemGump;
            private readonly PaperDollGump _paperDollGump;

            public EquipmentSlot(uint serial, int x, int y, Layer layer, PaperDollGump paperDollGump)
            {
                X = x;
                Y = y;
                LocalSerial = serial;
                Width = 19;
                Height = 20;
                _paperDollGump = paperDollGump;
                Layer = layer;

                Add
                (
                    new GumpPicTiled
                    (
                        0,
                        0,
                        19,
                        20,
                        0x243A
                    )
                    {
                        AcceptMouseInput = false
                    }
                );

                Add
                (
                    new GumpPic(0, 0, 0x2344, 0)
                    {
                        AcceptMouseInput = false
                    }
                );

                AcceptMouseInput = true;

                WantUpdateSize = false;
            }

            public Layer Layer { get; }

            public override void Update(double totalTime, double frameTime)
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null || item.IsDestroyed)
                {
                    _itemGump?.Dispose();
                    _itemGump = null;
                }

                Mobile mobile = World.Mobiles.Get(_paperDollGump.LocalSerial);

                if (mobile != null)
                {
                    Item it_at_layer = mobile.FindItemByLayer(Layer);

                    if (item != it_at_layer || _itemGump == null)
                    {
                        if (_itemGump != null)
                        {
                            _itemGump.Dispose();
                            _itemGump = null;
                        }

                        item = it_at_layer;

                        if (item != null)
                        {
                            LocalSerial = it_at_layer.Serial;

                            Add
                            (
                                _itemGump = new ItemGumpFixed(item, 18, 18)
                                {
                                    X = 0,
                                    Y = 0,
                                    Width = 18,
                                    Height = 18,
                                    HighlightOnMouseOver = false,
                                    CanPickUp = World.InGame && (World.Player.Serial == _paperDollGump.LocalSerial || _paperDollGump.CanLift)
                                }
                            );
                        }
                    }
                }

                base.Update(totalTime, frameTime);
            }

            private class ItemGumpFixed : ItemGump
            {
                private readonly Point _originalSize;
                private readonly Point _point;
                private readonly Rectangle _rect;

                public ItemGumpFixed(Item item, int w, int h) : base
                (
                    item.Serial,
                    item.DisplayedGraphic,
                    item.Hue,
                    item.X,
                    item.Y
                )
                {
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
        }
    }
}