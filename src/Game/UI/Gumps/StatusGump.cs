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
using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class StatusGumpBase : Gump
    {
        protected const ushort LOCK_UP_GRAPHIC = 0x0984;
        protected const ushort LOCK_DOWN_GRAPHIC = 0x0986;
        protected const ushort LOCK_LOCKED_GRAPHIC = 0x082C;


        protected StatusGumpBase() : base(0, 0)
        {
            // sanity check
            UIManager.GetGump<HealthBarGump>(World.Player)?.Dispose();

            CanCloseWithRightClick = true;
            CanMove = true;
        }

        public override GumpType GumpType => GumpType.StatusGump;
        protected Label[] _labels;
        protected readonly GumpPic[] _lockers = new GumpPic[3];
        protected Point _point;
        protected long _refreshTime;

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType) buttonID)
            {
                case ButtonType.BuffIcon:

                    BuffGump gump = UIManager.GetGump<BuffGump>();

                    if (gump == null)
                    {
                        UIManager.Add(new BuffGump(100, 100));
                    }
                    else
                    {
                        gump.SetInScreen();
                        gump.BringOnTop();
                    }

                    break;
            }
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.Target(World.Player);
                    Mouse.LastLeftButtonClickTime = 0;
                }
                else if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
                {
                    Point offset = Mouse.LDragOffset;

                    if (Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
                    {
                        UIManager.GetGump<BaseHealthBarGump>(World.Player)?.Dispose();

                        if (ProfileManager.CurrentProfile.CustomBarsToggled)
                        {
                            UIManager.Add(new HealthBarGumpCustom(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                        }
                        else
                        {
                            UIManager.Add(new HealthBarGump(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                        }

                        Dispose();
                    }
                }
            }
        }


        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (TargetManager.IsTargeting)
            {
                TargetManager.Target(World.Player);
                Mouse.LastLeftButtonClickTime = 0;
            }
        }

        public static StatusGumpBase GetStatusGump()
        {
            StatusGumpBase gump;

            if (!CUOEnviroment.IsOutlands)
            {
                if (ProfileManager.CurrentProfile.UseOldStatusGump)
                {
                    gump = UIManager.GetGump<StatusGumpOld>();
                }
                else
                {
                    gump = UIManager.GetGump<StatusGumpModern>();
                }
            }
            else
            {
                gump = UIManager.GetGump<StatusGumpOutlands>();
            }


            gump?.SetInScreen();

            return gump;
        }

        public static StatusGumpBase AddStatusGump(int x, int y)
        {
            StatusGumpBase gump;

            if (!CUOEnviroment.IsOutlands)
            {
                if (Client.Version < ClientVersion.CV_308Z || ProfileManager.CurrentProfile.UseOldStatusGump)
                {
                    gump = new StatusGumpOld();
                }
                else
                {
                    gump = new StatusGumpModern();
                }
            }
            else
            {
                gump = new StatusGumpOutlands();
            }

            gump.X = x;
            gump.Y = y;

            return gump;
        }

        protected static ushort GetStatLockGraphic(Lock lockStatus)
        {
            switch (lockStatus)
            {
                case Lock.Up: return LOCK_UP_GRAPHIC;

                case Lock.Down: return LOCK_DOWN_GRAPHIC;

                case Lock.Locked: return LOCK_LOCKED_GRAPHIC;

                default: return 0xFFFF;
            }
        }

        protected override void UpdateContents()
        {
            for (int i = 0; i < 3; i++)
            {
                Lock status = i == 0 ? World.Player.StrLock : i == 1 ? World.Player.DexLock : World.Player.IntLock;

                if (_lockers != null && _lockers[i] != null)
                {
                    _lockers[i].Graphic = GetStatLockGraphic(status);
                }
            }
        }

        protected enum ButtonType
        {
            BuffIcon,
            MinimizeMaximize
        }

        protected enum StatType
        {
            Str,
            Dex,
            Int
        }
    }

    internal class StatusGumpOld : StatusGumpBase
    {
        public StatusGumpOld()
        {
            Point p = Point.Zero;
            _labels = new Label[(int) MobileStats.NumStats];            

            Add(new GumpPic(0, 0, 0x0802, 0));
            p.X = 244;
            p.Y = 112;

            Label text = new Label(!string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty, false, 0x0386, font: 1)
            {
                X = 86,
                Y = 42
            };

            _labels[(int) MobileStats.Name] = text;
            Add(text);

            int xOffset = 0;

            if (Client.Version >= ClientVersion.CV_5020)
            {
                Add
                (
                    new Button((int)ButtonType.BuffIcon, 0x7538, 0x7539, 0x7539)
                    {
                        X = 20,
                        Y = 42,
                        ButtonAction = ButtonAction.Activate
                    }
                );
            }           
            
            Lock status = World.Player.StrLock;
            xOffset = Client.UseUOPGumps ? 28 : 40;
            ushort gumpID = GetStatLockGraphic(status);

            Add(_lockers[0] = new GumpPic(xOffset, 62, gumpID, 0));

            _lockers[0].MouseUp += (sender, e) =>
            {
                World.Player.StrLock = (Lock)(((byte)World.Player.StrLock + 1) % 3);
                GameActions.ChangeStatLock(0, World.Player.StrLock);
                ushort gumpid = GetStatLockGraphic(World.Player.StrLock);

                _lockers[0].Graphic = gumpid;
            };

            text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 62
            };

            _labels[(int) MobileStats.Strength] = text;
            Add(text);

            status = World.Player.DexLock;
            xOffset = Client.UseUOPGumps ? 28 : 40;
            gumpID = GetStatLockGraphic(status);

            Add(_lockers[1] = new GumpPic(xOffset, 74, gumpID, 0));

            _lockers[1].MouseUp += (sender, e) =>
            {
                World.Player.DexLock = (Lock)(((byte)World.Player.DexLock + 1) % 3);
                GameActions.ChangeStatLock(1, World.Player.DexLock);
                ushort gumpid = GetStatLockGraphic(World.Player.DexLock);

                _lockers[1].Graphic = gumpid;
            };

            text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 74
            };

            _labels[(int) MobileStats.Dexterity] = text;
            Add(text);

            status = World.Player.IntLock;
            xOffset = Client.UseUOPGumps ? 28 : 40;
            gumpID = GetStatLockGraphic(status);

            Add(_lockers[2] = new GumpPic(xOffset, 86, gumpID, 0));

            _lockers[2].MouseUp += (sender, e) =>
            {
                World.Player.IntLock = (Lock)(((byte)World.Player.IntLock + 1) % 3);
                GameActions.ChangeStatLock(2, World.Player.IntLock);
                ushort gumpid = GetStatLockGraphic(World.Player.IntLock);

                _lockers[2].Graphic = gumpid;
            };

            text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 86
            };

            _labels[(int) MobileStats.Intelligence] = text;
            Add(text);

            text = new Label(World.Player.IsFemale ? ResGumps.Female : ResGumps.Male, false, 0x0386, font: 1)
            {
                X = 86,
                Y = 98
            };

            _labels[(int) MobileStats.Sex] = text;
            Add(text);

            text = new Label(World.Player.PhysicalResistance.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 110
            };

            _labels[(int) MobileStats.AR] = text;
            Add(text);

            text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 62
            };

            _labels[(int) MobileStats.HealthCurrent] = text;
            Add(text);

            text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 74
            };

            _labels[(int) MobileStats.ManaCurrent] = text;
            Add(text);

            text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 86
            };

            _labels[(int) MobileStats.StaminaCurrent] = text;
            Add(text);

            text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
            {
                X = 171,
                Y = 98
            };

            _labels[(int) MobileStats.Gold] = text;
            Add(text);

            text = new Label($"{World.Player.Weight}/{World.Player.WeightMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 110
            };

            _labels[(int) MobileStats.WeightCurrent] = text;
            Add(text);


            Add
            (
                new HitBox
                (
                    86,
                    61,
                    34,
                    12,
                    ResGumps.Strength,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    86,
                    73,
                    34,
                    12,
                    ResGumps.Dex,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    86,
                    85,
                    34,
                    12,
                    ResGumps.Intelligence,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    86,
                    97,
                    34,
                    12,
                    ResGumps.Sex,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    86,
                    109,
                    34,
                    12,
                    ResGumps.Armor,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    171,
                    61,
                    66,
                    12,
                    ResGeneral.Hits,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    171,
                    73,
                    66,
                    12,
                    ResGeneral.Mana,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    171,
                    85,
                    66,
                    12,
                    ResGumps.Stamina,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    171,
                    97,
                    66,
                    12,
                    ResGumps.Gold,
                    0
                ) { CanMove = true }
            );

            Add
            (
                new HitBox
                (
                    171,
                    109,
                    66,
                    12,
                    ResGeneral.Weight,
                    0
                ) { CanMove = true }
            );

            _point = p;
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < totalTime)
            {
                _refreshTime = (long) totalTime + 250;

                _labels[(int) MobileStats.Name].Text = !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty;

                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();

                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();

                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

                _labels[(int) MobileStats.Sex].Text = World.Player.IsFemale ? ResGumps.Female : ResGumps.Male;

                _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistance.ToString();

                _labels[(int) MobileStats.HealthCurrent].Text = $"{World.Player.Hits}/{World.Player.HitsMax}";

                _labels[(int) MobileStats.ManaCurrent].Text = $"{World.Player.Mana}/{World.Player.ManaMax}";

                _labels[(int) MobileStats.StaminaCurrent].Text = $"{World.Player.Stamina}/{World.Player.StaminaMax}";

                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();

                _labels[(int) MobileStats.WeightCurrent].Text = $"{World.Player.Weight}/{World.Player.WeightMax}";
            }

            base.Update(totalTime, frameTime);
        }


        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            StaminaCurrent,
            ManaCurrent,
            WeightCurrent,
            Gold,
            AR,
            Sex,
            NumStats
        }
    }

    internal class StatusGumpModern : StatusGumpBase
    {
        public StatusGumpModern()
        {
            Point p = Point.Zero;
            int xOffset = 0;
            _labels = new Label[(int) MobileStats.NumStats];

            Add(new GumpPic(0, 0, 0x2A6C, 0));

            if (Client.Version >= ClientVersion.CV_308Z)
            {
                p.X = 389;
                p.Y = 152;


                AddStatTextLabel
                (
                    !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty,
                    MobileStats.Name,
                    Client.UseUOPGumps ? 90 : 58,
                    50,
                    320,
                    0x0386,
                    TEXT_ALIGN_TYPE.TS_CENTER
                );


                if (Client.Version >= ClientVersion.CV_5020)
                {
                    Add
                    (
                        new Button((int) ButtonType.BuffIcon, 0x7538, 0x7539, 0x7539)
                        {
                            X = 40,
                            Y = 50,
                            ButtonAction = ButtonAction.Activate
                        }
                    );
                }

                Lock status = World.Player.StrLock;
                xOffset = Client.UseUOPGumps ? 28 : 40;
                ushort gumpID = GetStatLockGraphic(status);

                Add(_lockers[0] = new GumpPic(xOffset, 76, gumpID, 0));

                _lockers[0].MouseUp += (sender, e) =>
                {
                    World.Player.StrLock = (Lock) (((byte) World.Player.StrLock + 1) % 3);
                    GameActions.ChangeStatLock(0, World.Player.StrLock);
                    ushort gumpid = GetStatLockGraphic(World.Player.StrLock);

                    _lockers[0].Graphic = gumpid;
                };

                //AddChildren(_lockers[0] = new Button((int)ButtonType.LockerStr, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 76,
                //    ButtonAction = ButtonAction.Activate,
                //});
                status = World.Player.DexLock;
                xOffset = Client.UseUOPGumps ? 28 : 40;
                gumpID = GetStatLockGraphic(status);

                Add(_lockers[1] = new GumpPic(xOffset, 102, gumpID, 0));

                _lockers[1].MouseUp += (sender, e) =>
                {
                    World.Player.DexLock = (Lock) (((byte) World.Player.DexLock + 1) % 3);
                    GameActions.ChangeStatLock(1, World.Player.DexLock);
                    ushort gumpid = GetStatLockGraphic(World.Player.DexLock);

                    _lockers[1].Graphic = gumpid;
                };

                //AddChildren(_lockers[1] = new Button((int)ButtonType.LockerDex, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 102,
                //    ButtonAction = ButtonAction.Activate
                //});
                status = World.Player.IntLock;
                xOffset = Client.UseUOPGumps ? 28 : 40;
                gumpID = GetStatLockGraphic(status);

                Add(_lockers[2] = new GumpPic(xOffset, 132, gumpID, 0));

                _lockers[2].MouseUp += (sender, e) =>
                {
                    World.Player.IntLock = (Lock) (((byte) World.Player.IntLock + 1) % 3);
                    GameActions.ChangeStatLock(2, World.Player.IntLock);
                    ushort gumpid = GetStatLockGraphic(World.Player.IntLock);

                    _lockers[2].Graphic = gumpid;
                };
                //AddChildren(_lockers[2] = new Button((int)ButtonType.LockerInt, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 132,
                //    ButtonAction = ButtonAction.Activate
                //});

                if (Client.UseUOPGumps)
                {
                    xOffset = 80;
                    AddStatTextLabel(World.Player.HitChanceIncrease.ToString(), MobileStats.HitChanceInc, xOffset, 161);

                    Add
                    (
                        new HitBox
                        (
                            58,
                            154,
                            59,
                            24,
                            ResGumps.HitChanceIncrease,
                            0
                        ) { CanMove = true }
                    );
                }
                else
                {
                    xOffset = 88;
                }


                AddStatTextLabel(World.Player.Strength.ToString(), MobileStats.Strength, xOffset, 77);
                AddStatTextLabel(World.Player.Dexterity.ToString(), MobileStats.Dexterity, xOffset, 105);
                AddStatTextLabel(World.Player.Intelligence.ToString(), MobileStats.Intelligence, xOffset, 133);


                Add
                (
                    new HitBox
                    (
                        58,
                        70,
                        59,
                        24,
                        ResGumps.Strength,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        58,
                        98,
                        59,
                        24,
                        ResGumps.Dexterity,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        58,
                        126,
                        59,
                        24,
                        ResGumps.Intelligence,
                        0
                    ) { CanMove = true }
                );

                int textWidth = 40;

                if (Client.UseUOPGumps)
                {
                    xOffset = 150;

                    AddStatTextLabel($"{World.Player.DefenseChanceIncrease}/{World.Player.MaxDefenseChanceIncrease}", MobileStats.DefenseChanceInc, xOffset, 161);

                    Add
                    (
                        new HitBox
                        (
                            124,
                            154,
                            59,
                            24,
                            ResGumps.DefenseChanceIncrease,
                            0
                        ) { CanMove = true }
                    );
                }
                else
                {
                    xOffset = 146;
                }


                xOffset -= 5;

                AddStatTextLabel
                (
                    World.Player.Hits.ToString(),
                    MobileStats.HealthCurrent,
                    xOffset,
                    70,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                AddStatTextLabel
                (
                    World.Player.HitsMax.ToString(),
                    MobileStats.HealthMax,
                    xOffset,
                    83,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                AddStatTextLabel
                (
                    World.Player.Stamina.ToString(),
                    MobileStats.StaminaCurrent,
                    xOffset,
                    98,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                AddStatTextLabel
                (
                    World.Player.StaminaMax.ToString(),
                    MobileStats.StaminaMax,
                    xOffset,
                    111,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                AddStatTextLabel
                (
                    World.Player.Mana.ToString(),
                    MobileStats.ManaCurrent,
                    xOffset,
                    126,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                AddStatTextLabel
                (
                    World.Player.ManaMax.ToString(),
                    MobileStats.ManaMax,
                    xOffset,
                    139,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                xOffset += 5;

                Add
                (
                    new Line
                    (
                        xOffset,
                        138,
                        Math.Abs(xOffset - 185),
                        1,
                        0xFF383838
                    )
                );

                Add
                (
                    new Line
                    (
                        xOffset,
                        110,
                        Math.Abs(xOffset - 185),
                        1,
                        0xFF383838
                    )
                );

                Add
                (
                    new Line
                    (
                        xOffset,
                        82,
                        Math.Abs(xOffset - 185),
                        1,
                        0xFF383838
                    )
                );

                Add
                (
                    new HitBox
                    (
                        124,
                        70,
                        59,
                        24,
                        ResGumps.HitPoints,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        124,
                        98,
                        59,
                        24,
                        ResGumps.Stamina,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        124,
                        126,
                        59,
                        24,
                        ResGeneral.Mana,
                        0
                    ) { CanMove = true }
                );

                if (Client.UseUOPGumps)
                {
                    xOffset = 240;

                    AddStatTextLabel(World.Player.LowerManaCost.ToString(), MobileStats.LowerManaCost, xOffset, 162);

                    Add
                    (
                        new HitBox
                        (
                            205,
                            154,
                            65,
                            24,
                            ResGumps.LowerManaCost,
                            0
                        ) { CanMove = true }
                    );
                }
                else
                {
                    xOffset = 220;
                }

                AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, xOffset, 77);
                AddStatTextLabel(World.Player.Luck.ToString(), MobileStats.Luck, xOffset, 105);

                xOffset -= 10;

                AddStatTextLabel
                (
                    World.Player.Weight.ToString(),
                    MobileStats.WeightCurrent,
                    xOffset,
                    126,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                int lineX = Client.UseUOPGumps ? 236 : 216;

                Add
                (
                    new Line
                    (
                        lineX,
                        138,
                        Math.Abs(lineX - (Client.UseUOPGumps ? 270 : 250)),
                        1,
                        0xFF383838
                    )
                );

                AddStatTextLabel
                (
                    World.Player.WeightMax.ToString(),
                    MobileStats.WeightMax,
                    xOffset,
                    139,
                    textWidth,
                    alignment: TEXT_ALIGN_TYPE.TS_CENTER
                );

                xOffset = Client.UseUOPGumps ? 205 : 188;

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        70,
                        65,
                        24,
                        ResGumps.MaximumStats,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        98,
                        65,
                        24,
                        ResGumps.Luck,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        126,
                        65,
                        24,
                        ResGeneral.Weight,
                        0
                    ) { CanMove = true }
                );

                if (Client.UseUOPGumps)
                {
                    xOffset = 320;

                    AddStatTextLabel(World.Player.DamageIncrease.ToString(), MobileStats.DamageChanceInc, xOffset, 105);

                    AddStatTextLabel(World.Player.SwingSpeedIncrease.ToString(), MobileStats.SwingSpeedInc, xOffset, 161);

                    Add
                    (
                        new HitBox
                        (
                            285,
                            98,
                            69,
                            24,
                            ResGumps.WeaponDamageIncrease,
                            0
                        ) { CanMove = true }
                    );

                    Add
                    (
                        new HitBox
                        (
                            285,
                            154,
                            69,
                            24,
                            ResGumps.SwingSpeedIncrease,
                            0
                        ) { CanMove = true }
                    );
                }
                else
                {
                    xOffset = 280;

                    AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, xOffset, 105);

                    Add
                    (
                        new HitBox
                        (
                            260,
                            98,
                            69,
                            24,
                            ResGumps.Gold,
                            0
                        ) { CanMove = true }
                    );
                }

                AddStatTextLabel($"{World.Player.DamageMin}-{World.Player.DamageMax}", MobileStats.Damage, xOffset, 77);

                AddStatTextLabel($"{World.Player.Followers}-{World.Player.FollowersMax}", MobileStats.Followers, xOffset, 133);

                xOffset = Client.UseUOPGumps ? 285 : 260;

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        70,
                        69,
                        24,
                        ResGumps.Damage,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        126,
                        69,
                        24,
                        ResGumps.Followers,
                        0
                    ) { CanMove = true }
                );

                if (Client.UseUOPGumps)
                {
                    xOffset = 400;

                    AddStatTextLabel(World.Player.LowerReagentCost.ToString(), MobileStats.LowerReagentCost, xOffset, 77);

                    AddStatTextLabel(World.Player.SpellDamageIncrease.ToString(), MobileStats.SpellDamageInc, xOffset, 105);

                    AddStatTextLabel(World.Player.FasterCasting.ToString(), MobileStats.FasterCasting, xOffset, 133);

                    AddStatTextLabel(World.Player.FasterCastRecovery.ToString(), MobileStats.FasterCastRecovery, xOffset, 161);

                    xOffset = 365;

                    Add
                    (
                        new HitBox
                        (
                            xOffset,
                            70,
                            55,
                            24,
                            ResGumps.LowerReagentCost,
                            0
                        ) { CanMove = true }
                    );

                    Add
                    (
                        new HitBox
                        (
                            xOffset,
                            98,
                            55,
                            24,
                            ResGumps.SpellDamageIncrease,
                            0
                        ) { CanMove = true }
                    );

                    Add
                    (
                        new HitBox
                        (
                            xOffset,
                            126,
                            55,
                            24,
                            ResGumps.FasterCasting,
                            0
                        ) { CanMove = true }
                    );

                    Add
                    (
                        new HitBox
                        (
                            xOffset,
                            154,
                            55,
                            24,
                            ResGumps.FasterCastRecovery,
                            0
                        ) { CanMove = true }
                    );

                    xOffset = 480;

                    AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, xOffset, 161);

                    Add
                    (
                        new HitBox
                        (
                            445,
                            154,
                            55,
                            24,
                            ResGumps.Gold,
                            0
                        ) { CanMove = true }
                    );

                    xOffset = 475;

                    AddStatTextLabel($"{World.Player.PhysicalResistance}/{World.Player.MaxPhysicResistence}", MobileStats.AR, xOffset, 74);

                    AddStatTextLabel($"{World.Player.FireResistance}/{World.Player.MaxFireResistence}", MobileStats.RF, xOffset, 92);

                    AddStatTextLabel($"{World.Player.ColdResistance}/{World.Player.MaxColdResistence}", MobileStats.RC, xOffset, 106);

                    AddStatTextLabel($"{World.Player.PoisonResistance}/{World.Player.MaxPoisonResistence}", MobileStats.RP, xOffset, 120);

                    AddStatTextLabel($"{World.Player.EnergyResistance}/{World.Player.MaxEnergyResistence}", MobileStats.RE, xOffset, 134);
                }
                else
                {
                    xOffset = 354;

                    AddStatTextLabel(World.Player.PhysicalResistance.ToString(), MobileStats.AR, xOffset, 76);
                    AddStatTextLabel(World.Player.FireResistance.ToString(), MobileStats.RF, xOffset, 92);
                    AddStatTextLabel(World.Player.ColdResistance.ToString(), MobileStats.RC, xOffset, 106);
                    AddStatTextLabel(World.Player.PoisonResistance.ToString(), MobileStats.RP, xOffset, 120);
                    AddStatTextLabel(World.Player.EnergyResistance.ToString(), MobileStats.RE, xOffset, 134);
                }


                xOffset = Client.UseUOPGumps ? 445 : 334;

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        76,
                        40,
                        14,
                        ResGumps.PhysicalResistance,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        92,
                        40,
                        14,
                        ResGumps.FireResistance,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        106,
                        40,
                        14,
                        ResGumps.ColdResistance,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        120,
                        40,
                        14,
                        ResGumps.PoisonResistance,
                        0
                    ) { CanMove = true }
                );

                Add
                (
                    new HitBox
                    (
                        xOffset,
                        134,
                        40,
                        14,
                        ResGumps.EnergyResistance,
                        0
                    ) { CanMove = true }
                );
            }
            else
            {
                if (Client.Version == ClientVersion.CV_308D)
                {
                    AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, 171, 124);

                    Add
                    (
                        new HitBox
                        (
                            171,
                            124,
                            34,
                            12,
                            ResGumps.MaxStats,
                            0
                        ) { CanMove = true }
                    );
                }
                else if (Client.Version == ClientVersion.CV_308J)
                {
                    AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, 180, 131);

                    AddStatTextLabel($"{World.Player.Followers}/{World.Player.FollowersMax}", MobileStats.Followers, 180, 144);

                    Add
                    (
                        new HitBox
                        (
                            180,
                            131,
                            34,
                            12,
                            ResGumps.MaxStats,
                            0
                        ) { CanMove = true }
                    );

                    Add
                    (
                        new HitBox
                        (
                            171,
                            144,
                            34,
                            12,
                            ResGumps.Followers,
                            0
                        ) { CanMove = true }
                    );
                }
            }

            if (Client.UseUOPGumps)
            {
                p.X = 540;
                p.Y = 180;
            }


            Add
            (
                new HitBox
                (
                    p.X,
                    p.Y,
                    16,
                    16,
                    ResGumps.Minimize,
                    0
                ) { CanMove = true }
            );

            _point = p;
        }


        private void AddStatTextLabel
        (
            string text,
            MobileStats stat,
            int x,
            int y,
            int maxWidth = 0,
            ushort hue = 0x0386,
            TEXT_ALIGN_TYPE alignment = TEXT_ALIGN_TYPE.TS_LEFT
        )
        {
            Label label = new Label
            (
                text,
                false,
                hue,
                maxWidth,
                align: alignment,
                font: 1
            )
            {
                X = x,
                Y = y
            };

            _labels[(int) stat] = label;
            Add(label);
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < totalTime)
            {
                _refreshTime = (long) totalTime + 250;

                _labels[(int) MobileStats.Name].Text = !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty;

                if (Client.UseUOPGumps)
                {
                    _labels[(int) MobileStats.HitChanceInc].Text = World.Player.HitChanceIncrease.ToString();
                }

                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();

                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();

                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

                if (Client.UseUOPGumps)
                {
                    _labels[(int) MobileStats.DefenseChanceInc].Text = $"{World.Player.DefenseChanceIncrease}/{World.Player.MaxDefenseChanceIncrease}";
                }

                _labels[(int) MobileStats.HealthCurrent].Text = World.Player.Hits.ToString();

                _labels[(int) MobileStats.HealthMax].Text = World.Player.HitsMax.ToString();

                _labels[(int) MobileStats.StaminaCurrent].Text = World.Player.Stamina.ToString();

                _labels[(int) MobileStats.StaminaMax].Text = World.Player.StaminaMax.ToString();

                _labels[(int) MobileStats.ManaCurrent].Text = World.Player.Mana.ToString();

                _labels[(int) MobileStats.ManaMax].Text = World.Player.ManaMax.ToString();

                if (Client.UseUOPGumps)
                {
                    _labels[(int) MobileStats.LowerManaCost].Text = World.Player.LowerManaCost.ToString();
                }

                _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();

                _labels[(int) MobileStats.Luck].Text = World.Player.Luck.ToString();

                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();

                _labels[(int) MobileStats.WeightMax].Text = World.Player.WeightMax.ToString();

                if (Client.UseUOPGumps)
                {
                    _labels[(int) MobileStats.DamageChanceInc].Text = World.Player.DamageIncrease.ToString();

                    _labels[(int) MobileStats.SwingSpeedInc].Text = World.Player.SwingSpeedIncrease.ToString();
                }

                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();

                _labels[(int) MobileStats.Damage].Text = $"{World.Player.DamageMin}-{World.Player.DamageMax}";

                _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";

                if (Client.UseUOPGumps)
                {
                    _labels[(int) MobileStats.LowerReagentCost].Text = World.Player.LowerReagentCost.ToString();

                    _labels[(int) MobileStats.SpellDamageInc].Text = World.Player.SpellDamageIncrease.ToString();

                    _labels[(int) MobileStats.FasterCasting].Text = World.Player.FasterCasting.ToString();

                    _labels[(int) MobileStats.FasterCastRecovery].Text = World.Player.FasterCastRecovery.ToString();

                    _labels[(int) MobileStats.AR].Text = $"{World.Player.PhysicalResistance}/{World.Player.MaxPhysicResistence}";

                    _labels[(int) MobileStats.RF].Text = $"{World.Player.FireResistance}/{World.Player.MaxFireResistence}";

                    _labels[(int) MobileStats.RC].Text = $"{World.Player.ColdResistance}/{World.Player.MaxColdResistence}";

                    _labels[(int) MobileStats.RP].Text = $"{World.Player.PoisonResistance}/{World.Player.MaxPoisonResistence}";

                    _labels[(int) MobileStats.RE].Text = $"{World.Player.EnergyResistance}/{World.Player.MaxEnergyResistence}";
                }
                else
                {
                    _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistance.ToString();

                    _labels[(int) MobileStats.RF].Text = World.Player.FireResistance.ToString();

                    _labels[(int) MobileStats.RC].Text = World.Player.ColdResistance.ToString();

                    _labels[(int) MobileStats.RP].Text = World.Player.PoisonResistance.ToString();

                    _labels[(int) MobileStats.RE].Text = World.Player.EnergyResistance.ToString();
                }
            }

            base.Update(totalTime, frameTime);
        }


        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            HealthMax,
            StaminaCurrent,
            StaminaMax,
            ManaCurrent,
            ManaMax,
            WeightMax,
            Followers,
            WeightCurrent,
            LowerReagentCost,
            SpellDamageInc,
            FasterCasting,
            FasterCastRecovery,
            StatCap,
            HitChanceInc,
            DefenseChanceInc,
            LowerManaCost,
            DamageChanceInc,
            SwingSpeedInc,
            Luck,
            Gold,
            AR,
            RF,
            RC,
            RP,
            RE,
            Damage,
            Sex,
            NumStats
        }
    }

    internal class StatusGumpOutlands : StatusGumpBase
    {
        private readonly GumpPicWithWidth[] _fillBars = new GumpPicWithWidth[3];

        public StatusGumpOutlands()
        {
            Point pos = Point.Zero;
            _labels = new Label[(int) MobileStats.Max];

            Add(new GumpPic(0, 0, 0x2A6C, 0));
            Add(new GumpPic(34, 12, 0x0805, 0)); // Health bar
            Add(new GumpPic(34, 25, 0x0805, 0)); // Mana bar
            Add(new GumpPic(34, 38, 0x0805, 0)); // Stamina bar

            if (Client.Version >= ClientVersion.CV_5020)
            {
                Add
                (
                    new Button((int) ButtonType.BuffIcon, 0x837, 0x838, 0x838)
                    {
                        X = 159,
                        Y = 40,
                        ButtonAction = ButtonAction.Activate
                    }
                );

                Add
                (
                    new Label
                    (
                        ResGumps.Buffs,
                        false,
                        0x0386,
                        60,
                        1
                    ) { X = 174, Y = 40 }
                );
            }

            ushort gumpIdHp = 0x0806;

            if (World.Player.IsPoisoned)
            {
                gumpIdHp = 0x0808;
            }
            else if (World.Player.IsYellowHits)
            {
                gumpIdHp = 0x0809;
            }

            _fillBars[(int) FillStats.Hits] = new GumpPicWithWidth
            (
                34,
                12,
                gumpIdHp,
                0,
                0
            );

            _fillBars[(int) FillStats.Mana] = new GumpPicWithWidth
            (
                34,
                25,
                0x0806,
                0,
                0
            );

            _fillBars[(int) FillStats.Stam] = new GumpPicWithWidth
            (
                34,
                38,
                0x0806,
                0,
                0
            );

            Add(_fillBars[(int) FillStats.Hits]);
            Add(_fillBars[(int) FillStats.Mana]);
            Add(_fillBars[(int) FillStats.Stam]);

            UpdateStatusFillBar(FillStats.Hits, World.Player.Hits, World.Player.HitsMax);
            UpdateStatusFillBar(FillStats.Mana, World.Player.Mana, World.Player.ManaMax);
            UpdateStatusFillBar(FillStats.Stam, World.Player.Stamina, World.Player.StaminaMax);

            // Name
            Label text = new Label
            (
                !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty,
                false,
                0x0386,
                320,
                1,
                align: TEXT_ALIGN_TYPE.TS_CENTER
            )
            {
                X = 108,
                Y = 12
            };

            _labels[(int) MobileStats.Name] = text;
            Add(text);


            // Stat locks
            Add(_lockers[(int) StatType.Str] = new GumpPic(10, 73, GetStatLockGraphic(World.Player.StrLock), 0));

            Add(_lockers[(int) StatType.Dex] = new GumpPic(10, 102, GetStatLockGraphic(World.Player.DexLock), 0));

            Add(_lockers[(int) StatType.Int] = new GumpPic(10, 130, GetStatLockGraphic(World.Player.IntLock), 0));

            _lockers[(int) StatType.Str].MouseUp += (sender, e) =>
            {
                World.Player.StrLock = (Lock) (((byte) World.Player.StrLock + 1) % 3);
                GameActions.ChangeStatLock(0, World.Player.StrLock);

                _lockers[(int) StatType.Str].Graphic = GetStatLockGraphic(World.Player.StrLock);
            };

            _lockers[(int) StatType.Dex].MouseUp += (sender, e) =>
            {
                World.Player.DexLock = (Lock) (((byte) World.Player.DexLock + 1) % 3);
                GameActions.ChangeStatLock(1, World.Player.DexLock);

                _lockers[(int) StatType.Dex].Graphic = GetStatLockGraphic(World.Player.DexLock);
            };

            _lockers[(int) StatType.Int].MouseUp += (sender, e) =>
            {
                World.Player.IntLock = (Lock) (((byte) World.Player.IntLock + 1) % 3);
                GameActions.ChangeStatLock(2, World.Player.IntLock);

                _lockers[(int) StatType.Int].Graphic = GetStatLockGraphic(World.Player.IntLock);
            };

            // Str/dex/int text labels
            int xOffset = 60;
            AddStatTextLabel(World.Player.Strength.ToString(), MobileStats.Strength, xOffset, 73);
            AddStatTextLabel(World.Player.Dexterity.ToString(), MobileStats.Dexterity, xOffset, 102);
            AddStatTextLabel(World.Player.Intelligence.ToString(), MobileStats.Intelligence, xOffset, 130);

            // Hits/stam/mana

            AddStatTextLabel
            (
                World.Player.Hits.ToString(),
                MobileStats.HealthCurrent,
                117,
                66,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.HitsMax.ToString(),
                MobileStats.HealthMax,
                117,
                79,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.Stamina.ToString(),
                MobileStats.StaminaCurrent,
                117,
                95,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.StaminaMax.ToString(),
                MobileStats.StaminaMax,
                117,
                108,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.Mana.ToString(),
                MobileStats.ManaCurrent,
                117,
                124,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.ManaMax.ToString(),
                MobileStats.ManaMax,
                117,
                137,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            // CurrentProfile over max lines
            Add
            (
                new Line
                (
                    118,
                    79,
                    30,
                    1,
                    0xFF383838
                )
            );

            Add
            (
                new Line
                (
                    118,
                    108,
                    30,
                    1,
                    0xFF383838
                )
            );

            Add
            (
                new Line
                (
                    118,
                    137,
                    30,
                    1,
                    0xFF383838
                )
            );

            // Followers / max followers

            AddStatTextLabel
            (
                $"{World.Player.Followers}/{World.Player.FollowersMax}",
                MobileStats.Followers,
                192,
                73,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );


            // Armor, weight / max weight
            AddStatTextLabel
            (
                World.Player.PhysicalResistance.ToString(),
                MobileStats.AR,
                196,
                102,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.Weight.ToString(),
                MobileStats.WeightCurrent,
                185,
                124,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            AddStatTextLabel
            (
                World.Player.WeightMax.ToString(),
                MobileStats.WeightMax,
                185,
                137,
                40,
                alignment: TEXT_ALIGN_TYPE.TS_CENTER
            );

            Add
            (
                new Line
                (
                    186,
                    137,
                    30,
                    1,
                    0xFF383838
                )
            );

            // Hunger satisfaction, murder count, damage, gold

            AddStatTextLabel
            (
                World.Player.Luck.ToString(), // FIXME: packet handling
                MobileStats.HungerSatisfactionMinutes,
                282,
                44
            );

            AddStatTextLabel
            (
                World.Player.StatsCap.ToString(), // FIXME: packet handling
                MobileStats.MurderCount,
                260,
                73
            );

            AddStatTextLabel($"{World.Player.DamageMin}-{World.Player.DamageMax}", MobileStats.Damage, 260, 102);

            AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, 254, 132);

            // Timers

            AddStatTextLabel
            (
                World.Player.ColdResistance.ToString(), // FIXME: packet handling
                MobileStats.CriminalTimerSeconds,
                354,
                44
            );

            AddStatTextLabel
            (
                World.Player.FireResistance.ToString(), // FIXME: packet handling
                MobileStats.MurderCountDecayHours,
                354,
                73
            );

            AddStatTextLabel
            (
                World.Player.PoisonResistance.ToString(), // FIXME: packet handling
                MobileStats.PvpCooldownSeconds,
                354,
                102
            );

            AddStatTextLabel
            (
                World.Player.EnergyResistance.ToString(), // FIXME: packet handling
                MobileStats.BandageTimerSeconds,
                354,
                131
            );
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_refreshTime < totalTime)
            {
                _refreshTime = (long) totalTime + 250;

                UpdateStatusFillBar(FillStats.Hits, World.Player.Hits, World.Player.HitsMax);
                UpdateStatusFillBar(FillStats.Mana, World.Player.Mana, World.Player.ManaMax);
                UpdateStatusFillBar(FillStats.Stam, World.Player.Stamina, World.Player.StaminaMax);

                _labels[(int) MobileStats.Name].Text = !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty;

                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();

                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();

                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

                _labels[(int) MobileStats.HealthCurrent].Text = World.Player.Hits.ToString();

                _labels[(int) MobileStats.HealthMax].Text = World.Player.HitsMax.ToString();

                _labels[(int) MobileStats.StaminaCurrent].Text = World.Player.Stamina.ToString();

                _labels[(int) MobileStats.StaminaMax].Text = World.Player.StaminaMax.ToString();

                _labels[(int) MobileStats.ManaCurrent].Text = World.Player.Mana.ToString();

                _labels[(int) MobileStats.ManaMax].Text = World.Player.ManaMax.ToString();

                _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";

                _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistance.ToString();

                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();

                _labels[(int) MobileStats.WeightMax].Text = World.Player.WeightMax.ToString();

                _labels[(int) MobileStats.Damage].Text = $"{World.Player.DamageMin}-{World.Player.DamageMax}";

                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();

                _labels[(int) MobileStats.HungerSatisfactionMinutes].Text = World.Player.Luck.ToString(); // FIXME: packet handling

                _labels[(int) MobileStats.MurderCount].Text = World.Player.StatsCap.ToString(); // FIXME: packet handling

                _labels[(int) MobileStats.MurderCountDecayHours].Text = World.Player.FireResistance.ToString(); // FIXME: packet handling

                _labels[(int) MobileStats.CriminalTimerSeconds].Text = World.Player.ColdResistance.ToString(); // FIXME: packet handling

                _labels[(int) MobileStats.PvpCooldownSeconds].Text = World.Player.PoisonResistance.ToString(); // FIXME: packet handling

                _labels[(int) MobileStats.BandageTimerSeconds].Text = World.Player.EnergyResistance.ToString(); // FIXME: packet handling
            }

            base.Update(totalTime, frameTime);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.Target(World.Player);
                    Mouse.LastLeftButtonClickTime = 0;
                }
                else
                {
                    Point p = new Point(x, y);
                    Rectangle rect = new Rectangle(Bounds.Width - 42, Bounds.Height - 25, Bounds.Width, Bounds.Height);

                    if (rect.Contains(p))
                    {
                        UIManager.GetGump<BaseHealthBarGump>(World.Player)?.Dispose();

                        //TCH whole if else
                        if (ProfileManager.CurrentProfile.CustomBarsToggled)
                        {
                            UIManager.Add(new HealthBarGumpCustom(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                        }
                        else
                        {
                            UIManager.Add(new HealthBarGump(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                        }

                        Dispose();
                    }
                }
            }
        }

        private static int CalculatePercents(int max, int current, int maxValue)
        {
            if (max > 0)
            {
                max = current * 100 / max;

                if (max > 100)
                {
                    max = 100;
                }

                if (max > 1)
                {
                    max = maxValue * max / 100;
                }
            }

            return max;
        }

        private void UpdateStatusFillBar(FillStats id, int current, int max)
        {
            ushort gumpId = 0x0806;

            if (id == FillStats.Hits)
            {
                if (World.Player.IsPoisoned)
                {
                    gumpId = 0x0808;
                }
                else if (World.Player.IsYellowHits)
                {
                    gumpId = 0x0809;
                }
            }

            if (max > 0)
            {
                _fillBars[(int) id].Graphic = gumpId;

                _fillBars[(int) id].Percent = CalculatePercents(max, current, 109);
            }
        }

        // TODO: move to base class?
        private void AddStatTextLabel
        (
            string text,
            MobileStats stat,
            int x,
            int y,
            int maxWidth = 0,
            ushort hue = 0x0386,
            TEXT_ALIGN_TYPE alignment = TEXT_ALIGN_TYPE.TS_LEFT
        )
        {
            Label label = new Label
            (
                text,
                false,
                hue,
                maxWidth,
                align: alignment,
                font: 1
            )
            {
                X = x - 5,
                Y = y
            };

            _labels[(int) stat] = label;
            Add(label);
        }


        private enum MobileStats
        {
            Name,
            Strength,
            Dexterity,
            Intelligence,
            HealthCurrent,
            HealthMax,
            StaminaCurrent,
            StaminaMax,
            ManaCurrent,
            ManaMax,
            WeightCurrent,
            WeightMax,
            Followers,
            FollowersMax,
            Gold,
            AR,
            Damage,
            HungerSatisfactionMinutes,
            MurderCount,
            MurderCountDecayHours,
            CriminalTimerSeconds,
            PvpCooldownSeconds,
            BandageTimerSeconds,
            Max
        }

        private enum FillStats
        {
            Hits,
            Mana,
            Stam
        }
    }
}