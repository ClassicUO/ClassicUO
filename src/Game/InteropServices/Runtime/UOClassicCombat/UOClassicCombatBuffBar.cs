#region license

// Copyright (C) 2020 project dust765
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;

using System.IO;
using ClassicUO.Utility;
using System.Collections.Generic;

using ClassicUO.Renderer;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;

using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    internal class UCCBar : Control
    {
        private class LineCHB : Line
        {
            public LineCHB(int x, int y, int w, int h, uint color) : base(x, y, w, h, color)
            {
                LineWidth = w;
                LineColor = SolidColorTextureCache.GetTexture(new Color() { PackedValue = color });

                CanMove = true;
            }
            public int LineWidth { get; set; }
            public Texture2D LineColor { get; set; }
            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                ResetHueVector();
                ShaderHueTranslator.GetHueVector(ref HueVector, 0, false, Alpha);

                return batcher.Draw2D(LineColor, x, y, LineWidth, Height, ref HueVector);
            }
        }

        private Label _icon;
        private LineCHB _line;
        private Label _label;

        public ushort IconColor
        {
            get => _icon.Hue;
            set => _icon.Hue = value;
        }

        public int LineWidth
        {
            get => _line.LineWidth;
            set => _line.LineWidth = value;
        }
        
        public Texture2D LineColor
        {
            get => _line.LineColor;
            set => _line.LineColor = value;
        }

        public ushort TextColor
        {
            get => _label.Hue;
            set => _label.Hue = value;
        }
        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public UCCBar(int ix, int iy, string itext, ushort ihue, int lw, int lh, uint lcolor, string text, ushort tcolor)// : base(itexture, ix, iy, iw, ih, icolor, lw, lh, lcolor, text, tcolor)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            if (ProfileManager.CurrentProfile.UOClassicCombatBuffbar_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            _icon = new Label(itext, true, ihue, 0, 1, FontStyle.BlackBorder)
            {
                X = ix,
                Y = iy
            };
            _icon.Text = itext;
            _icon.Hue = ihue;
            _icon.Width = 40;
            _icon.Height = 20;
            Add(_icon);

            _line = new LineCHB(_icon.Width + 1, _icon.Y, lw, lh, lcolor)
            {

            };
            _line.Width = LineWidth;

            Add(_line);

            _label = new Label(text, true, tcolor, 0, 1, FontStyle.BlackBorder)
            {
                X = _icon.Width + 10,
                Y = _icon.Y,
            };
            _label.Text = Text;
            _label.Hue = TextColor;
            Add(_label);
        }
        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }
    }
    internal class UOClassicCombatBuffbar : Gump
    {
        //MAIN UI CONSTRUCT
        private readonly AlphaBlendControl _background;

        //UCCBBarS
        private UCCBar _uiUCCBBarSwing, _uiUCCBBarGotDisarmed, _uiUCCBBarGotHamstrung, _uiUCCBBarDoDisarm, _uiUCCBBarDoHamstring;

        //CONSTANTS UCCBBarS
        private int LINE_WIDTH = 0;
        private int LINE_HEIGHT = 20;

        //CONSTANTS
        private static Color LINE_COLOR_DRAW_RED = Color.Red;
        private static Color LINE_COLOR_DRAW_BLUE = Color.DodgerBlue;
        private static Color LINE_COLOR_DRAW_BLACK = Color.Black;

        private static readonly Texture2D LINE_COLOR_CHANGE_RED = SolidColorTextureCache.GetTexture(Color.Red);
        private static readonly Texture2D LINE_COLOR_CHANGE_ORANGE = SolidColorTextureCache.GetTexture(Color.Orange);

        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999, HUE_FONTS_YELLOW = 0x35, HUE_FONTS_RED = 0x26, HUE_FONTS_GREEN = 0x3F, HUE_FONTS_BLUE = 0x5D;
        //MAIN UI CONSTRUCT

        //SWING
        public bool _triggerSwing { get; set; }
        private uint _timerSwing;
        public uint _tickSwing { get; set; }

        //DISARMED
        public bool _triggerGotDisarmed { get; set; }
        private uint _timerGotDisarmed; //countdown timer
        public uint _tickGotDisarmed { get; set; }

        //HAMSTRUNG
        public bool _triggerGotHamstrung { get; set; }
        private uint _timerGotHamstrung; //countdown timer
        public uint _tickGotHamstrung { get; set; }
        //HAMSTRUNG

        //DO DISARM
        private bool _triggerDoDisarm { get; set; }
        private uint _timerDoDisarm; //countdown timer
        private uint _tickDoDisarmStriked { get; set; }
        private uint _tickDoDisarmFailed { get; set; }
        //DO DISARM

        //DO HAMSTRING
        private bool _triggerDoHamstring { get; set; }
        private uint _timerDoHamstring; //countdown timer
        private uint _tickDoHamstringStriked { get; set; }
        private uint _tickDoHamstringFailed { get; set; }
        //DO HAMSTRING

        //OPTIONS TO VARS
        private bool UCCS_SwingEnable = ProfileManager.CurrentProfile.UOClassicCombatBuffbar_SwingEnabled;
        private bool UCCS_DoDEnable = ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoDEnabled;
        private bool UCCS_GotDEnable = ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotDEnabled;
        private bool UCCS_DoHEnable = ProfileManager.CurrentProfile.UOClassicCombatBuffbar_DoHEnabled;
        private bool UCCS_GotHEnable = ProfileManager.CurrentProfile.UOClassicCombatBuffbar_GotHEnabled;
        private int _yModifier = 0;

        private uint UCCS_DisarmStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown;
        private uint UCCS_DisarmAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown;

        private uint UCCS_HamstringStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown;
        private uint UCCS_HamstringAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown;

        private uint UCCS_DisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown;
        private uint UCCS_HamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown;
        //OPTIONS TO VARS

        //SHARD TYPE
        private int _ShardType = Settings.GlobalSettings.ShardType; //Outlands = 2

        //WEAPONS LIST
        public static readonly List<ushort> WeaponsList = new List<ushort>();

        public UOClassicCombatBuffbar() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            if (ProfileManager.CurrentProfile.UOClassicCombatBuffbar_Locked)
            {
                CanMove = false;
                AcceptMouseInput = false;
            }

            //MAIN CONSTRUCT
            Width = 141;
            Height = 100;

            //ADJUST HEIGHT DEPENDING ON MENU
            if (!UCCS_SwingEnable)
                Height -= 20;

            if (!UCCS_DoDEnable)
                Height -= 20;

            if (!UCCS_GotDEnable)
                Height -= 20;

            if (_ShardType == 2)
            {
                if (!UCCS_DoHEnable)
                    Height -= 20;

                if (!UCCS_GotHEnable)
                    Height -= 20;
            }
            else
            {
                Height -= 40;
            }

            Add(_background = new AlphaBlendControl()
            {
                Alpha = 0.6f,
                Width = Width,
                Height = Height
            });

            Add(_background);

            //UCCBarS
            if (!UCCS_SwingEnable)
                _yModifier -= 20;
            else
                Add(_uiUCCBBarSwing = new UCCBar(0, 0, "Swing", HUE_FONTS_YELLOW, LINE_WIDTH, LINE_HEIGHT, LINE_COLOR_DRAW_RED.PackedValue, $"{_timerSwing}", HUE_FONTS_GREEN));

            if (!UCCS_DoDEnable)
                _yModifier -= 20;
            else
                Add(_uiUCCBBarDoDisarm = new UCCBar(0, 20 + _yModifier, "Do D", HUE_FONTS_YELLOW, LINE_WIDTH, LINE_HEIGHT, LINE_COLOR_DRAW_RED.PackedValue, $"{_timerDoDisarm}", HUE_FONTS_GREEN));

            if (!UCCS_GotDEnable)
                _yModifier -= 20;
            else
                Add(_uiUCCBBarGotDisarmed = new UCCBar(0, 40 + _yModifier, "Got D", HUE_FONTS_YELLOW, LINE_WIDTH, LINE_HEIGHT, LINE_COLOR_DRAW_RED.PackedValue, $"{_timerGotDisarmed}", HUE_FONTS_GREEN));

            if (_ShardType == 2)
            {
                if (!UCCS_DoHEnable)
                    _yModifier -= 20;
                else
                    Add(_uiUCCBBarDoHamstring = new UCCBar(0, 60 + _yModifier, "Do H", HUE_FONTS_YELLOW, LINE_WIDTH, LINE_HEIGHT, LINE_COLOR_DRAW_RED.PackedValue, $"{_timerDoHamstring}", HUE_FONTS_GREEN));

                if (!UCCS_GotHEnable)
                    _yModifier -= 20;
                else
                    Add(_uiUCCBBarGotHamstrung = new UCCBar(0, 80 + _yModifier, "Got H", HUE_FONTS_YELLOW, LINE_WIDTH, LINE_HEIGHT, LINE_COLOR_DRAW_RED.PackedValue, $"{_timerGotHamstrung}", HUE_FONTS_GREEN));

            }
            //UCCBarS

            //UPDATE VARS FROM PROFILE
            UpdateVars();

            //READ FILE
            LoadFile();

            //COPY PASTED
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

        }
        //MAIN
        public override void Update(double totalMS, double frameMS)
        {
            if (World.Player == null || World.Player.IsDestroyed /*|| World.Player.IsDead*/)
                return;

            //HUE TEXT
            if (UCCS_SwingEnable)
            {
                if (_timerSwing == 0)
                    _uiUCCBBarSwing.TextColor = HUE_FONTS_GREEN;
                else
                    _uiUCCBBarSwing.TextColor = HUE_FONTS_RED;
            }

            //DISARM
            if (UCCS_DoDEnable)
            {
                if (_timerDoDisarm == 0)
                    _uiUCCBBarDoDisarm.TextColor = HUE_FONTS_GREEN;
                else
                    _uiUCCBBarDoDisarm.TextColor = HUE_FONTS_RED;
            }
            if (UCCS_GotDEnable)
            {
                if (_timerGotDisarmed == 0)
                {
                    _uiUCCBBarGotDisarmed.TextColor = HUE_FONTS_GREEN;
                    _uiUCCBBarGotDisarmed.IconColor = HUE_FONTS_YELLOW;
                }
                else
                {
                    _uiUCCBBarGotDisarmed.TextColor = HUE_FONTS_RED;
                    _uiUCCBBarGotDisarmed.IconColor = HUE_FONTS_RED;
                }
            }

            //HAMSTRING
            if (_ShardType == 2)
            {
                if (UCCS_DoHEnable)
                {
                    if (_timerDoHamstring == 0)
                        _uiUCCBBarDoHamstring.TextColor = HUE_FONTS_GREEN;
                    else
                        _uiUCCBBarDoHamstring.TextColor = HUE_FONTS_RED;
                }
                if (UCCS_GotHEnable)
                {
                    if (_timerGotHamstrung == 0)
                    {
                        _uiUCCBBarGotHamstrung.TextColor = HUE_FONTS_GREEN;
                        _uiUCCBBarGotHamstrung.IconColor = HUE_FONTS_YELLOW;
                    }
                    else
                    {
                        _uiUCCBBarGotHamstrung.TextColor = HUE_FONTS_RED;
                        _uiUCCBBarGotHamstrung.IconColor = HUE_FONTS_RED;
                    }
                }
            }
            //END HUE TEXT

            //SWING
            if (_triggerSwing)
                Swing();

            //GOT DISARMED
            if (_triggerGotDisarmed)
                GotDisarmed();

            //DO DISARM
            if (_triggerDoDisarm)
                DoDisarm();

            if (_ShardType == 2)
            {
                //GOT HAMSTRUNG
                if (_triggerGotHamstrung)
                    GotHamstrung();

                //DO HAMSTRING
                if (_triggerDoHamstring)
                    DoHamstring();
            }

            base.Update(totalMS, frameMS);
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatBuffbarLocation = Location;
        }
        public void UpdateVars()
        {
            //NOTE, NOT UPDATING UCCS_SwingEnable and so on HERE. I WANT PEOPLE TO ENABLE / DISABLE BAR IN OPTION ON CHANGES
            //AS IT NEEDS A COMPLETE REBUILD AND NOT JUST SOME VARS CHANGED

            //UPDATE VARS
            UCCS_DisarmStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmStrikeCooldown;
            UCCS_DisarmAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmAttemptCooldown;

            UCCS_HamstringStrikeCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringStrikeCooldown;
            UCCS_HamstringAttemptCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstringAttemptCooldown;

            UCCS_DisarmedCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_DisarmedCooldown;
            UCCS_HamstrungCooldown = ProfileManager.CurrentProfile.UOClassicCombatSelf_HamstrungCooldown;
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        //MAIN METHODS
        public void Swing()
        {
            //CALC SWING TIME FOR CURR WEP
            uint SwingCooldown = 0;

            /*
            1290 //Kryss 0x1400 / 0x1401
            1340 //Katana 0x13FE / 0x13FF, Cutlass 0x1440 / 0x1441, Scimitar 0x13B5 / 0x13B6
            1390 //Club 0x13B3 / 0x13B4, Hammerpick 0x143C / 0x143D, War Axe 0x13AF / 0x13B0
            1500 //Warfork 0x1404 / 0x1405
            1560 //Longswords 0x13B7 / 0x13B8 / 0x0F60 / 0x0F61, Broadsword 0x0F5E / 0x0F5F, Viking Sword 0x13B9 / 0x13BA
            1630 //Mace 0x0F5C / 0x0F5D, Maul 0x143A / 0x143B, War Mace 0x1406 / 0x1407
            2210 //Bow 0x13B1 / 0x13B2
            1880 //Short Spear 0x1402 / 0x1403, Pitchfork 0x0E87 / 0x0E88
            1970 //Axe 0x0F49 / 0x0F4A, Battle Axe 0x0F47 / 0x0F48, Double Axe 0x0F4B / 0x0F4C, 
            Executioner's Axe 0x0F45 / 0x0F46, Large Battle Axe 0x13FA / 0x13FB, Two-Handed Axe 0x1442 / 0x1443
            2080 //Gnarled Staff 0x13F8 / 0x13F9, Black Staff 0x0DF0 / 0x0DF1, Quarters Staff 0x0E89 / 0x0E8A
            2680 //Crossbow 0x0F4F / 0x0F50
            2500 //Spear 0x0F62 / 0x0F63
            3000 //Halberd 0x143E / 0x143F, Bardiche 0x0F4D / 0x0F4E
            2780 //Warhammer 0x1438 / 0x1439
            3750 //Heavy Crossbow 0x13FC / 0x13FD
            */

            Item _tempItem = World.Player.FindItemByLayer(Layer.TwoHanded);
            if (_tempItem == null)
            {
                _tempItem = World.Player.FindItemByLayer(Layer.OneHanded);
            }

            //EMPTY HANDED CHECK
            if (_tempItem != null)
            {
                if (_ShardType == 2)
                {
                    switch (_tempItem.Graphic)
                    {
                        case 0x1400:
                        case 0x1401:
                            SwingCooldown = 1290;
                            break;

                        case 0x13FE:
                        case 0x13FF:
                        case 0x1440:
                        case 0x1441:
                        case 0x13B5:
                        case 0x13B6:
                            SwingCooldown = 1340;
                            break;
                        case 0x13B3:
                        case 0x13B4:
                        case 0x143C:
                        case 0x143D:
                        case 0x13AF:
                        case 0x13B0:
                            SwingCooldown = 1390;
                            break;
                        case 0x1404:
                        case 0x1405:
                            SwingCooldown = 1500;
                            break;
                        case 0x13B7:
                        case 0x13B8:
                        case 0x0F60:
                        case 0x0F61:
                        case 0x0F5E:
                        case 0x0F5F:
                        case 0x13B9:
                        case 0x13BA:
                            SwingCooldown = 1560;
                            break;
                        case 0x0F5C:
                        case 0x0F5D:
                        case 0x143A:
                        case 0x143B:
                        case 0x1406:
                        case 0x1407:
                            SwingCooldown = 1630;
                            break;
                        case 0x13B1:
                        case 0x13B2:
                            SwingCooldown = 2210;
                            break;
                        case 0x1402:
                        case 0x1403:
                        case 0x0E87:
                        case 0x0E88:
                            SwingCooldown = 1880;
                            break;
                        case 0x0F49:
                        case 0x0F4A:
                        case 0x0F47:
                        case 0x0F48:
                        case 0x0F4B:
                        case 0x0F4C:
                        case 0x0F45:
                        case 0x0F46:
                        case 0x13FA:
                        case 0x13FB:
                        case 0x1442:
                        case 0x1443:
                            SwingCooldown = 1970;
                            break;
                        case 0x13F8:
                        case 0x13F9:
                        case 0x0DF0:
                        case 0x0DF1:
                        case 0x0E89:
                        case 0x0E8A:
                            SwingCooldown = 2080;
                            break;
                        case 0x0F4F:
                        case 0x0F50:
                            SwingCooldown = 2680;
                            break;
                        case 0x0F62:
                        case 0x0F63:
                            SwingCooldown = 2500;
                            break;
                        case 0x143E:
                        case 0x143F:
                        case 0x0F4D:
                        case 0x0F4E:
                            SwingCooldown = 3000;
                            break;
                        case 0x1438:
                        case 0x1439:
                            SwingCooldown = 2780;
                            break;
                        case 0x13FC:
                        case 0x13FD:
                            SwingCooldown = 3750;
                            break;
                    }
                }
                else
                {
                    int index = WeaponsList.IndexOf(_tempItem.Graphic);
                    SwingCooldown = WeaponsList[index + 1];

                    //CALC =MIN(MAX((ROUNDDOWN(60000/((STAM+100)*SPEED),0))*0.25,1.25),10)
                    //source: http://forums.uosecondage.com/viewtopic.php?f=9&t=43574

                    double input = 60000 / ((World.Player.Stamina + 100) * SwingCooldown);
                    double final = Math.Min(Math.Max(Math.Floor(input) * 0.25, 1.25), 10) * 1000;
                    SwingCooldown = Convert.ToUInt32(final);
                }

            }
            //SET TIMER SWING AND UI
            if (_tickSwing != 0)
            {
                _timerSwing = (SwingCooldown / 100) - (Time.Ticks - _tickSwing) / 100;
            }
            if (_tickSwing != 0 && (_tickSwing + SwingCooldown) <= Time.Ticks)
            {
                _timerSwing = 0;
                _tickSwing = 0;
                _triggerSwing = false;

                //LINES
                _uiUCCBBarSwing.LineWidth = 0;
            }
            _uiUCCBBarSwing.Text = $"{_timerSwing}";

            //LINES
            if (_tickSwing != 0)
            {
                uint w = 100 / (SwingCooldown / 100) * _timerSwing;
                _uiUCCBBarSwing.LineWidth = Convert.ToInt32(w);
            }
        }
        public void DoDisarm()
        {
            //SET TIMER DO DISARM AND UI
            if (_tickDoDisarmFailed != 0)
            {
                _timerDoDisarm = (UCCS_DisarmAttemptCooldown / 1000) - (Time.Ticks - _tickDoDisarmFailed) / 1000;
                _uiUCCBBarDoDisarm.LineColor = LINE_COLOR_CHANGE_RED;
            }
            if (_tickDoDisarmStriked != 0)
            {
                _timerDoDisarm = (UCCS_DisarmStrikeCooldown / 1000) - (Time.Ticks - _tickDoDisarmStriked) / 1000;
                _uiUCCBBarDoDisarm.LineColor = LINE_COLOR_CHANGE_ORANGE;
            }

            if (_tickDoDisarmFailed != 0 && (_tickDoDisarmFailed + UCCS_DisarmAttemptCooldown) <= Time.Ticks || _tickDoDisarmStriked != 0 && (_tickDoDisarmStriked + UCCS_DisarmStrikeCooldown) <= Time.Ticks)
            {
                _timerDoDisarm = 0;
                _tickDoDisarmFailed = 0;
                _tickDoDisarmStriked = 0;
                _triggerDoDisarm = false;
                _uiUCCBBarDoDisarm.IconColor = HUE_FONTS_GREEN;

                //LINES
                _uiUCCBBarDoDisarm.LineWidth = 0;
                _uiUCCBBarDoDisarm.LineColor = LINE_COLOR_CHANGE_RED;
            }
            else
            {
                _uiUCCBBarDoDisarm.IconColor = HUE_FONTS_RED;
            }
            _uiUCCBBarDoDisarm.Text = $"{_timerDoDisarm}";

            //LINES
            if (_tickDoDisarmFailed != 0)
            {
                uint w = 100 / (UCCS_DisarmAttemptCooldown / 1000) * _timerDoDisarm;
                _uiUCCBBarDoDisarm.LineWidth = Convert.ToInt32(w);
            }
            if (_tickDoDisarmStriked != 0)
            {
                uint w = 100 / (UCCS_DisarmStrikeCooldown / 1000) * _timerDoDisarm;
                _uiUCCBBarDoDisarm.LineWidth = Convert.ToInt32(w);
            }
        }
        public void DoHamstring()
        {
            //SET TIMER DO HAMSTRING AND UI
            if (_tickDoHamstringFailed != 0)
            {
                _timerDoHamstring = (UCCS_HamstringAttemptCooldown / 1000) - (Time.Ticks - _tickDoHamstringFailed) / 1000;
                _uiUCCBBarDoHamstring.LineColor = LINE_COLOR_CHANGE_RED;
            }
            if (_tickDoHamstringStriked != 0)
            {
                _timerDoHamstring = (UCCS_HamstringStrikeCooldown / 1000) - (Time.Ticks - _tickDoHamstringStriked) / 1000;
                _uiUCCBBarDoHamstring.LineColor = LINE_COLOR_CHANGE_ORANGE;
            }

            if (_tickDoHamstringFailed != 0 && (_tickDoHamstringFailed + UCCS_HamstringAttemptCooldown) <= Time.Ticks || _tickDoHamstringStriked != 0 && (_tickDoHamstringStriked + UCCS_HamstringStrikeCooldown) <= Time.Ticks)
            {
                _timerDoHamstring = 0;
                _tickDoHamstringFailed = 0;
                _tickDoHamstringStriked = 0;
                _triggerDoHamstring = false;
                _uiUCCBBarDoHamstring.IconColor = HUE_FONTS_GREEN;

                //LINES
                _uiUCCBBarDoHamstring.LineWidth = 0;
                _uiUCCBBarDoHamstring.LineColor = LINE_COLOR_CHANGE_RED;
            }
            else
            {
                _uiUCCBBarDoHamstring.IconColor = HUE_FONTS_RED;
            }
            _uiUCCBBarDoHamstring.Text = $"{_timerDoHamstring}";

            //LINES
            if (_tickDoHamstringFailed != 0)
            {
                uint w = 100 / (UCCS_HamstringAttemptCooldown / 1000) * _timerDoHamstring;
                _uiUCCBBarDoHamstring.LineWidth = Convert.ToInt32(w);
            }
            if (_tickDoHamstringStriked != 0)
            {
                uint w = 100 / (UCCS_HamstringStrikeCooldown / 1000) * _timerDoHamstring;
                _uiUCCBBarDoHamstring.LineWidth = Convert.ToInt32(w);
            }

        }
        public void GotDisarmed()
        {
            //SET TIMER DISARM AND UI
            if (_tickGotDisarmed != 0)
            {
                _timerGotDisarmed = (UCCS_DisarmedCooldown / 1000) - (Time.Ticks - _tickGotDisarmed) / 1000;
            }
            if (_tickGotDisarmed != 0 && (_tickGotDisarmed + UCCS_DisarmedCooldown) <= Time.Ticks)
            {
                _timerGotDisarmed = 0;
                _tickGotDisarmed = 0;
                _triggerGotDisarmed = false;

                //LINES
                _uiUCCBBarGotDisarmed.LineWidth = 0;
            }
            _uiUCCBBarGotDisarmed.Text = $"{_timerGotDisarmed}";

            //LINES
            if (_tickGotDisarmed != 0)
            {
                uint w = 100 / (UCCS_DisarmedCooldown / 1000) * _timerGotDisarmed;
                _uiUCCBBarGotDisarmed.LineWidth = Convert.ToInt32(w);
            }
        }
        public void GotHamstrung()
        {
            //SET TIMER HAMSTRUNG AND UI
            if (_tickGotHamstrung != 0)
            {
                _timerGotHamstrung = (UCCS_HamstrungCooldown / 1000) - (Time.Ticks - _tickGotHamstrung) / 1000;
            }

            if (_tickGotHamstrung != 0 && (_tickGotHamstrung + UCCS_HamstrungCooldown) <= Time.Ticks)
            {
                _timerGotHamstrung = 0;
                _tickGotHamstrung = 0;
                _triggerGotHamstrung = false;

                //LINES
                _uiUCCBBarGotHamstrung.LineWidth = 0;
            }
            _uiUCCBBarGotHamstrung.Text = $"{_timerGotHamstrung}";

            //LINES
            if (_tickGotHamstrung != 0)
            {
                uint w = 100 / (UCCS_HamstrungCooldown / 1000) * _timerGotHamstrung;
                _uiUCCBBarGotHamstrung.LineWidth = Convert.ToInt32(w);
            }
        }
        //CLILOC TRIGGERS (DO AND ON / OFF)
        public void ClilocTriggerDisarmON()
        {
            if (!UCCS_DoDEnable)
                return;

            _uiUCCBBarDoDisarm.IconColor = HUE_FONTS_GREEN;
        }
        public void ClilocTriggerDisarmOFF()
        {
            if (!UCCS_DoDEnable)
                return;

            _uiUCCBBarDoDisarm.IconColor = HUE_FONTS_YELLOW;
            _triggerDoDisarm = false;
        }
        public void ClilocTriggerDisarmStriked()
        {
            if (!UCCS_DoDEnable)
                return;

            _triggerDoDisarm = true;
            _tickDoDisarmStriked = Time.Ticks;
        }
        public void ClilocTriggerDisarmFailed()
        {
            if (!UCCS_DoDEnable)
                return;

            _triggerDoDisarm = true;
            _tickDoDisarmFailed = Time.Ticks;
        }
        public void ClilocTriggerHamstringON()
        {
            if (_ShardType != 2 || !UCCS_DoHEnable)
                return;

            _uiUCCBBarDoHamstring.IconColor = HUE_FONTS_GREEN;
        }
        public void ClilocTriggerHamstringOFF()
        {
            if (_ShardType != 2 || !UCCS_DoHEnable)
                return;

            _uiUCCBBarDoHamstring.IconColor = HUE_FONTS_YELLOW;
            _triggerDoHamstring = false;
        }
        public void ClilocTriggerHamstringStriked()
        {
            if (_ShardType != 2 || !UCCS_DoHEnable)
                return;

            _triggerDoHamstring = true;
            _tickDoHamstringStriked = Time.Ticks;
        }
        public void ClilocTriggerHamstringFailed()
        {
            if (_ShardType != 2 || !UCCS_DoHEnable)
                return;

            _triggerDoHamstring = true;
            _tickDoHamstringFailed = Time.Ticks;
        }
        //ACION CLILOC TRIGGERS (SWING & GOT)
        public void ClilocTriggerSwing()
        {
            if (!UCCS_SwingEnable)
                return;

            _tickSwing = Time.Ticks;
            _triggerSwing = true;
        }
        public void ClilocTriggerGotDisarmed()
        {
            if (!UCCS_GotDEnable)
                return;

            _triggerGotDisarmed = true;
            _tickGotDisarmed = Time.Ticks;
        }
        public void ClilocTriggerGotHamstrung()
        {
            if (_ShardType != 2 || !UCCS_GotHEnable)
                return;

            _triggerGotHamstrung = true;
            _tickGotHamstrung = Time.Ticks;
        }
        //GET WEAPON SPEED FROM FILE OR CREATE IT
        public static void LoadFile()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string swingtimer = Path.Combine(path, "swingtimer.txt");

            if (!File.Exists(swingtimer))
            {
                using (StreamWriter writer = new StreamWriter(swingtimer))
                {
                    ushort[] weapons = {
                        0x1400, 0x1401, 0x13FE, 0x13FF, 0x1440, 0x1441, 0x13B5, 0x13B6,
                        0x13B3, 0x13B4, 0x143C, 0x143D, 0x13AF, 0x13B0, 0x1404, 0x1405,
                        0x13B7, 0x13B8, 0x0F60, 0x0F61, 0x0F5E, 0x0F5F, 0x13B9, 0x13BA,
                        0x0F5C, 0x0F5D, 0x143A, 0x143B, 0x1406, 0x1407, 0x13B1, 0x13B2,
                        0x1402, 0x1403, 0x0E87, 0x0E88, 0x0F49, 0x0F4A, 0x0F47, 0x0F48,
                        0x0F4B, 0x0F4C, 0x0F45, 0x0F46, 0x13FA, 0x13FB, 0x1442, 0x1443,
                        0x13F8, 0x13F9, 0x0DF0, 0x0DF1, 0x0E89, 0x0E8A, 0x0F4F, 0x0F50,
                        0x0F62, 0x0F63, 0x143E, 0x143F, 0x0F4D, 0x0F4E, 0x1438, 0x1439,
                        0x13FC, 0x13FD
                    };

                    for (int i = 0; i < weapons.Length; i++)
                    {
                        ushort graphic = weapons[i];
                        ushort flag = 5000;

                        switch (graphic)
                        {
                            case 0x1400:
                            case 0x1401:
                                flag = 53;
                                break;

                            case 0x13FE:
                            case 0x13FF:
                                flag = 58;
                                break;

                            case 0x1440:
                            case 0x1441:
                                flag = 45;
                                break;

                            case 0x13B5:
                            case 0x13B6:
                                flag = 43;
                                break;

                            case 0x13B3:
                            case 0x13B4:
                                flag = 40;
                                break;

                            case 0x143C:
                            case 0x143D:
                                flag = 30;
                                break;

                            case 0x13AF:
                            case 0x13B0:
                                flag = 40;
                                break;

                            case 0x1404:
                            case 0x1405:
                                flag = 45;
                                break;

                            case 0x13B7:
                            case 0x13B8:
                                flag = 35;
                                break;

                            case 0x0F60:
                            case 0x0F61:
                                flag = 35;
                                break;

                            case 0x0F5E:
                            case 0x0F5F:
                                flag = 45;
                                break;

                            case 0x13B9:
                            case 0x13BA:
                                flag = 30;
                                break;

                            case 0x0F5C:
                            case 0x0F5D:
                                flag = 30;
                                break;

                            case 0x143A:
                            case 0x143B:
                                flag = 30;
                                break;

                            case 0x1406:
                            case 0x1407:
                                flag = 32;
                                break;

                            case 0x13B1:
                            case 0x13B2:
                                flag = 20;
                                break;

                            case 0x1402:
                            case 0x1403:
                                flag = 50;
                                break;

                            case 0x0E87:
                            case 0x0E88:
                                flag = 45;
                                break;

                            case 0x0F49:
                            case 0x0F4A:
                                flag = 37;
                                break;

                            case 0x0F47:
                            case 0x0F48:
                                flag = 30;
                                break;

                            case 0x0F4B:
                            case 0x0F4C:
                                flag = 37;
                                break;

                            case 0x0F45:
                            case 0x0F46:
                                flag = 37;
                                break;

                            case 0x13FA:
                            case 0x13FB:
                                flag = 30;
                                break;

                            case 0x1442:
                            case 0x1443:
                                flag = 30;
                                break;

                            case 0x13F8:
                            case 0x13F9:
                                flag = 33;
                                break;

                            case 0x0DF0:
                            case 0x0DF1:
                                flag = 35;
                                break;

                            case 0x0E89:
                            case 0x0E8A:
                                flag = 48;
                                break;

                            case 0x0F4F:
                            case 0x0F50:
                                flag = 18;
                                break;

                            case 0x0F62:
                            case 0x0F63:
                                flag = 46;
                                break;

                            case 0x143E:
                            case 0x143F:
                                flag = 25;
                                break;

                            case 0x0F4D:
                            case 0x0F4E:
                                flag = 26;
                                break;

                            case 0x1438:
                            case 0x1439:
                                flag = 31;
                                break;

                            case 0x13FC:
                            case 0x13FD:
                                flag = 10;
                                break;
                        }

                        writer.WriteLine($"{graphic}={flag}");
                    }
                }
            }

            TextFileParser swingtimerParser = new TextFileParser(File.ReadAllText(swingtimer), new[] { ' ', '\t', ',', '=' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!swingtimerParser.IsEOF())
            {
                var ss = swingtimerParser.ReadTokens();
                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        WeaponsList.Add(graphic);
                    }

                    if (ushort.TryParse(ss[1], out ushort f))
                    {
                        WeaponsList.Add(f);
                    }
                }
            }
        }
    }
}