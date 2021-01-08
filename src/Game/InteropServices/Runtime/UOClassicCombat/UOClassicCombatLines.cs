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
using ClassicUO.Game.Managers;
using System.Collections.Generic;

using ClassicUO.Renderer;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;

using Microsoft.Xna.Framework;

//-------------------------------NOTES / EXPERIMENTAL BELOW------------------------------------------------
//Displaying the list is turned off, as there is probably no use for it right now
//-------------------------------------------------------------------------------

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    internal class UCCLinesListEntry
    {
        public string Name { get; set; }
        public uint Serial { get; set; }
        public uint TimeAdded { get; set; }
    }
    internal class UOClassicCombatLines : Gump
    {
        //MAIN UI CONSTRUCT
        private readonly AlphaBlendControl _background;
        private readonly Label _title;

        //UI
        private Label _uiLabelcount;
        private Label[] _testentry = new Label[10]; //IF CHANGED, ALSO CHANGE LINE 233 AND 259
        private Label _uiLabelLasttarget;
        private Checkbox _uiCboxLT, _uiCboxHuntingMode, _uiCboxHMBlue, _uiCboxHMRed, _uiCboxHMOrange, _uiCboxHMCriminal;

        //CONSTANTS
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999, HUE_FONTS_YELLOW = 0x35, HUE_FONTS_RED = 0x26, HUE_FONTS_GREEN = 0x3F, HUE_FONTS_BLUE = 0x5D;
        //MAIN UI CONSTRUCT

        //OPTIONS TO VARS
        private bool UCCL_ToggleLastTarget = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget;
        private bool UCCL_ToggleHuntingMode = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode;
        private bool UCCL_ToggleHMBlue = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMBlue;
        private bool UCCL_ToggleHMRed = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMRed;
        private bool UCCL_ToggleHMOrange = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMOrange;
        private bool UCCL_ToggleHMCriminal = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMCriminal;
        //OPTIONS TO VARS END

        //VARS
        List<UCCLinesListEntry> entryList = new List<UCCLinesListEntry>();
        uint lastKick = Time.Ticks;
        uint _timeTillKick = 5000;

        public UOClassicCombatLines() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            //MAIN CONSTRUCT
            Width = 180;//180;
            Height = 85;//280;

            Add(_background = new AlphaBlendControl()
            {
                Alpha = 0.6f,
                Width = Width,
                Height = Height
            });
            Add(_background);

            _title = new Label("UCC -LINES-", true, HUE_FONTS_BLUE, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = 2
            };
            Add(_title);

            //MAIN CONSTRUCT
            /*
            _uiLabelcount = new Label("", true, HUE_FONT, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = _title.Bounds.Bottom
            };
            Add(_uiLabelcount);
            */

            //LAST TARGET BUTTON
            _uiCboxLT = new Checkbox(0x00D2, 0x00D3, "Last Target", FONT, HUE_FONT)
            {
                X = 2,
                Y = _title.Bounds.Bottom + 2,//_uiLabelcount.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget
            };
            _uiCboxLT.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget = _uiCboxLT.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxLT);

            _uiLabelLasttarget = new Label("", true, HUE_FONTS_YELLOW, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = _uiCboxLT.Bounds.Bottom
            };
            Add(_uiLabelLasttarget);

            //HUNT MODE BUTTONS
            _uiCboxHuntingMode = new Checkbox(0x00D2, 0x00D3, "HM", FONT, HUE_FONT)
            {
                X = 2,
                Y = _uiLabelLasttarget.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode
            };
            _uiCboxHuntingMode.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode = _uiCboxHuntingMode.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxHuntingMode);

            _uiCboxHMBlue = new Checkbox(0x00D2, 0x00D3, "B", FONT, HUE_FONT)
            {
                X = _uiCboxHuntingMode.Bounds.Right,
                Y = _uiLabelLasttarget.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMBlue
            };
            _uiCboxHMBlue.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMBlue = _uiCboxHMBlue.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxHMBlue);

            _uiCboxHMRed = new Checkbox(0x00D2, 0x00D3, "R", FONT, HUE_FONT)
            {
                X = _uiCboxHMBlue.Bounds.Right,
                Y = _uiLabelLasttarget.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMRed
            };
            _uiCboxHMRed.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMRed = _uiCboxHMRed.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxHMRed);

            _uiCboxHMOrange = new Checkbox(0x00D2, 0x00D3, "O", FONT, HUE_FONT)
            {
                X = _uiCboxHMRed.Bounds.Right,
                Y = _uiLabelLasttarget.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMOrange
            };
            _uiCboxHMOrange.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMOrange = _uiCboxHMOrange.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxHMOrange);

            _uiCboxHMCriminal = new Checkbox(0x00D2, 0x00D3, "C", FONT, HUE_FONT)
            {
                X = _uiCboxHMOrange.Bounds.Right,
                Y = _uiLabelLasttarget.Bounds.Bottom + 20,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMCriminal
            };
            _uiCboxHMCriminal.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMCriminal = _uiCboxHMCriminal.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxHMCriminal);

            //UPDATE VARS
            UpdateVars();

            //COPY PASTED
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

        }
        //MAIN
        public override void Update(double totalMS, double frameMS)
        {
            if (World.Player == null || World.Player.IsDestroyed)
                return;

            //UPDATE UI
            UpdateUI();

            //MAIN LOOP
            MainLoop();

            base.Update(totalMS, frameMS);
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatLinesLocation = Location;
        }
        public void UpdateVars()
        {
            //UPDATE VARS
            UCCL_ToggleLastTarget = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget;
            UCCL_ToggleHuntingMode = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode;
            UCCL_ToggleHMBlue = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMBlue;
            UCCL_ToggleHMRed = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMRed;
            UCCL_ToggleHMOrange = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMOrange;
            UCCL_ToggleHMCriminal = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMCriminal;

            //SET TOGGLE INCASE CHANGED BY MACRO
            _uiCboxLT.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget;
            _uiCboxHuntingMode.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode;
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        //MAIN LOOP METHODS
        private void UpdateUI()
        {
            //REFRESH THE COUNT
            //_uiLabelcount.Text = "Count: " + entryList.Count.ToString();

            //UPDATE LAST TARGET
            if (UCCL_ToggleLastTarget)
            {
                Entity _lasttarget = World.Get(TargetManager.LastTargetInfo.Serial);
                Mobile _lasttargetmobile = World.GetOrCreateMobile(TargetManager.LastTargetInfo.Serial);
                if (_lasttarget != null && _lasttargetmobile != null)
                {
                    _uiLabelLasttarget.Text = _lasttarget.Name;
                    _uiLabelLasttarget.Hue = Notoriety.GetHue(_lasttargetmobile.NotorietyFlag);
                }
            }
            else
            {
                _uiLabelLasttarget.Text = "";
            }
        }
        private void MainLoop()
        {
            //DO NOTHING EXCEPTIONS
            if (World.Player.IsDead)
                return;

            //KICK ENTRYS OLDER THAN 5S, CHECK EVERY SEC
            if (lastKick + 1000 <= Time.Ticks)
            {
                for (int i = entryList.Count - 1; i > -1; i--)
                {
                    if (entryList[i].TimeAdded + _timeTillKick <= Time.Ticks)
                    {
                        entryList.RemoveAt(i);
                        /*
                        if (i <= 9)                //MAX ARRAY SIZE //-------------------------------------------------------------------
                            Remove(_testentry[i]);
                        */
                    }
                }
                lastKick = Time.Ticks;
            }
        }
        //MACRO TRIGGERS
        public static void ClilocTriggerAddListEntryAll()
        {
            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();
            if (UOClassicCombatLines != null)
            {
                foreach (Mobile mobile in World.Mobiles)
                {
                    if (mobile == World.Player)
                        continue;

                    if (UOClassicCombatLines.entryList.Exists(mob => mob.Serial == mobile.Serial)) //NO DOUBLE ENTRYS, REFRESH TIME INSTEAD
                    {
                        UOClassicCombatLines.entryList.Find(mob => mob.Serial == mobile.Serial).TimeAdded = Time.Ticks;
                        continue;
                    }
                    else
                        UOClassicCombatLines.entryList.Add(new UCCLinesListEntry { Name = mobile.Name, Serial = mobile.Serial, TimeAdded = Time.Ticks });
                }
            }
        }
        public static void ClilocTriggerAddListEntryAllByNotoriety(NotorietyFlag flag)
        {
            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();
            if (UOClassicCombatLines != null)
            {
                foreach (Mobile mobile in World.Mobiles)
                {
                    if (mobile == World.Player)
                        continue;

                    if (mobile.NotorietyFlag != flag)
                        continue;

                    if (UOClassicCombatLines.entryList.Exists(mob => mob.Serial == mobile.Serial)) //NO DOUBLE ENTRYS, REFRESH TIME INSTEAD
                    {
                        UOClassicCombatLines.entryList.Find(mob => mob.Serial == mobile.Serial).TimeAdded = Time.Ticks;
                        continue;
                    }
                    else
                        UOClassicCombatLines.entryList.Add(new UCCLinesListEntry { Name = mobile.Name, Serial = mobile.Serial, TimeAdded = Time.Ticks });
                }
            }
        }
        public static void ClilocTriggerToggleLT()
        {
            ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget = !ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget;

            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();
            if (UOClassicCombatLines != null)
            {
                UOClassicCombatLines.UpdateVars();
            }

        }
        public static void ClilocTriggerToggleHM()
        {
            ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode = !ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode;

            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();
            if (UOClassicCombatLines != null)
            {
                UOClassicCombatLines.UpdateVars();
            }
        }
        //CLILOC TRIGGERS
        public static void ClilocTriggerAddListEntrySingle(uint serial, uint time)
        {
            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            Mobile mobtoadd = World.Mobiles.Get(serial);
            if (mobtoadd != null)
            {
                UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();

                if (UOClassicCombatLines != null)
                {
                    if (UOClassicCombatLines.entryList.Exists(mob => mob.Serial == mobtoadd.Serial)) //NO DOUBLE ENTRYS, REFRESH TIME INSTEAD
                    {
                        UOClassicCombatLines.entryList.Find(mob => mob.Serial == mobtoadd.Serial).TimeAdded = time;
                        return;
                    }
                    else
                        UOClassicCombatLines.entryList.Add(new UCCLinesListEntry { Name = mobtoadd.Name, Serial = serial, TimeAdded = time });
                }
            }
        }
        public void Draw(UltimaBatcher2D batcher)
        {
            //RETURN IF DISABLED
            if (!ProfileManager.CurrentProfile.UOClassicCombatLines)
                return;

            //POST
            Point p = World.Player.RealScreenPosition;
            p.X += (int)World.Player.Offset.X + 22;
            p.Y += (int)(World.Player.Offset.Y - World.Player.Offset.Z) + 22;
                       
            //GET LIST FROM UI
            UOClassicCombatLines UOClassicCombatLines = UIManager.GetGump<UOClassicCombatLines>();

            //GET LAST TARGET
            Entity _lasttarget = World.Get(TargetManager.LastTargetInfo.Serial);

            //SEARCH MOBS IN LIST
            foreach (Mobile mobile in World.Mobiles)
            {
                //VAR FOR MODES
                bool _draw = false;
                Color color = Color.White;

                if (!mobile.IsHuman)
                    continue;

                if (mobile == World.Player)
                    continue;

                switch (mobile.NotorietyFlag)
                {
                    case NotorietyFlag.Innocent:

                        if (ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode && ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMBlue && !World.Party.Contains(mobile.Serial))
                            _draw = true;

                        color = Color.Blue;
                        break;

                    case NotorietyFlag.Ally:

                        color = Color.Green;
                        break;

                    case NotorietyFlag.Criminal:

                        if (ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode && ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMCriminal && !World.Party.Contains(mobile.Serial))
                            _draw = true;

                        color = Color.Gray;
                        break;

                    case NotorietyFlag.Gray:

                        color = Color.Gray;
                        break;

                    case NotorietyFlag.Enemy:

                        if (ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode && ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMOrange && !World.Party.Contains(mobile.Serial))
                            _draw = true;

                        color = Color.Orange;
                        break;

                    case NotorietyFlag.Murderer:

                        if (ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHuntingMmode && ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleHMRed && !World.Party.Contains(mobile.Serial))
                            _draw = true;

                        color = Color.Red;
                        break;

                    case NotorietyFlag.Invulnerable:

                        color = Color.Yellow;
                        break;

                    default:

                        color = Color.White;
                        break;
                }

                if (ProfileManager.CurrentProfile.UOClassicCombatLines_ToggleLastTarget && _lasttarget != null)
                    if (mobile.Serial == _lasttarget.Serial)
                    {
                        _draw = true;
                        color = Color.White;
                    }
                        

                if (UOClassicCombatLines.entryList.Exists(mob => mob.Serial == mobile.Serial) || _draw)
                {
                    //CALC WHERE MOBILE IS
                    Point pm = mobile.RealScreenPosition;
                    pm.X += (int)mobile.Offset.X + 22;
                    pm.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 22;

                    if (!mobile.IsDead)
                    {
                        //CALC MIDDLE
                        int ox = (pm.X + p.X) / 2;
                        int oy = (pm.Y + p.Y) / 2;
                        batcher.DrawLine(SolidColorTextureCache.GetTexture(color), pm.X, pm.Y, p.X, p.Y, ox, oy);
                    }
                }
            }
        }
    }
}
