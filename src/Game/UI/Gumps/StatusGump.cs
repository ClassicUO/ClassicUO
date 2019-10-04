#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal abstract class StatusGumpBase : Gump
    {
        protected readonly GumpPic[] _lockers = new GumpPic[3];
        protected readonly bool _useUOPGumps = FileManager.UseUOPGumps;

        protected Label[] _labels;
        protected Point _point;
        protected long _refreshTime;

        protected StatusGumpBase() : base(0, 0)
        {
            // sanity check
            Engine.UI.GetGump<HealthBarGump>(World.Player)?.Dispose();

            CanMove = true;
            CanBeSaved = true;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType) buttonID)
            {
                case ButtonType.BuffIcon:

                    BuffGump gump = Engine.UI.GetGump<BuffGump>();

                    if (gump == null)
                        Engine.UI.Add(new BuffGump(100, 100));
                    else
                    {
                        gump.SetInScreen();
                        gump.BringOnTop();
                    }

                    break;
            }
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.TargetGameObject(World.Player);
                    Mouse.LastLeftButtonClickTime = 0;
                }
                else if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
                {
                    var offset = Mouse.LDroppedOffset;

                    if (Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
                    {
                        Engine.UI.GetGump<HealthBarGump>(World.Player)?.Dispose();
                        Engine.UI.Add(new HealthBarGump(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
                        Dispose();
                    }
                   
                }
            }
        }


        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (TargetManager.IsTargeting)
            {
                TargetManager.TargetGameObject(World.Player);
                Mouse.LastLeftButtonClickTime = 0;
            }
        }

        public static StatusGumpBase GetStatusGump()
        {
            StatusGumpBase gump;


            switch (Engine.GlobalSettings.ShardType)
            {
                case 0: // modern

                    gump = Engine.UI.GetGump<StatusGumpModern>();

                    break;

                case 1: // old

                    gump = Engine.UI.GetGump<StatusGumpOld>();

                    break;

                case 2: // outlands

                    gump = Engine.UI.GetGump<StatusGumpOutlands>();

                    break;

                default:

                    gump = Engine.UI.Gumps.OfType<StatusGumpBase>().FirstOrDefault();

                    break;
            }

            gump?.SetInScreen();

            return gump;
        }

        public static void AddStatusGump(int x, int y)
        {
            switch (Engine.GlobalSettings.ShardType)
            {
                case 0: // modern

                    Engine.UI.Add(new StatusGumpModern
                                      {X = x, Y = y});

                    break;

                case 1: // old

                    Engine.UI.Add(new StatusGumpOld
                                      {X = x, Y = y});

                    break;

                case 2: // outlands

                    Engine.UI.Add(new StatusGumpOutlands
                                      {X = x, Y = y});

                    break;

                default:

                    throw new NotImplementedException();
            }
        }

        protected Graphic GetStatLockGraphic(Lock lockStatus)
        {
            switch (lockStatus)
            {
                case Lock.Up:

                    return 0x0984;

                case Lock.Down:

                    return 0x0986;

                case Lock.Locked:

                    return 0x082C;

                default:

                    return Graphic.INVALID;
            }
        }

        public void UpdateLocksAfterPacket()
        {
            if (Engine.GlobalSettings.ShardType != 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    Lock status = i == 0 ? World.Player.StrLock : i == 1 ? World.Player.DexLock : World.Player.IntLock;

                    ushort gumpID = 0x0984; //Up

                    if (status == Lock.Down)
                        gumpID = 0x0986; //Down
                    else if (status == Lock.Locked)
                        gumpID = 0x082C; //Lock
                    _lockers[i].Graphic = gumpID;
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

            if (p.X == 0)
            {
                p.X = 243;
                p.Y = 150;
            }


            Label text = new Label(!string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty, false, 0x0386, font: 1)
            {
                X = 86,
                Y = 42
            };
            _labels[(int) MobileStats.Name] = text;
            Add(text);


            text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 61
            };
            _labels[(int) MobileStats.Strength] = text;
            Add(text);

            text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 73
            };
            _labels[(int) MobileStats.Dexterity] = text;
            Add(text);

            text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 85
            };
            _labels[(int) MobileStats.Intelligence] = text;
            Add(text);

            text = new Label(!World.Player.IsMale ? "F" : "M", false, 0x0386, font: 1)
            {
                X = 86,
                Y = 97
            };
            _labels[(int) MobileStats.Sex] = text;
            Add(text);

            text = new Label(World.Player.PhysicalResistence.ToString(), false, 0x0386, font: 1)
            {
                X = 86,
                Y = 109
            };
            _labels[(int) MobileStats.AR] = text;
            Add(text);

            text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 61
            };
            _labels[(int) MobileStats.HealthCurrent] = text;
            Add(text);

            text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 73
            };
            _labels[(int) MobileStats.ManaCurrent] = text;
            Add(text);

            text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
            {
                X = 171,
                Y = 85
            };
            _labels[(int) MobileStats.StaminaCurrent] = text;
            Add(text);

            text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
            {
                X = 171,
                Y = 97
            };
            _labels[(int) MobileStats.Gold] = text;
            Add(text);

            text = new Label(World.Player.Weight.ToString(), false, 0x0386, font: 1)
            {
                X = 171,
                Y = 109
            };
            _labels[(int) MobileStats.WeightCurrent] = text;
            Add(text);

            _point = p;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_refreshTime < totalMS)
            {
                _refreshTime = (long) totalMS + 250;

                _labels[(int) MobileStats.Name].Text = !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty;
                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();
                _labels[(int) MobileStats.Sex].Text = !World.Player.IsMale ? "F" : "M";
                _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistence.ToString();
                _labels[(int) MobileStats.HealthCurrent].Text = $"{World.Player.Hits}/{World.Player.HitsMax}";
                _labels[(int) MobileStats.ManaCurrent].Text = $"{World.Player.Mana}/{World.Player.ManaMax}";
                _labels[(int) MobileStats.StaminaCurrent].Text = $"{World.Player.Stamina}/{World.Player.StaminaMax}";
                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();
            }

            base.Update(totalMS, frameMS);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            if (Engine.GlobalSettings.ShardType != 1)
                Dispose();
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

            if (FileManager.ClientVersion >= ClientVersions.CV_308Z)
            {
                p.X = 389;
                p.Y = 152;


                AddStatTextLabel(!string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty, MobileStats.Name, _useUOPGumps ? 90 : 58, 50, 320, 0x0386, TEXT_ALIGN_TYPE.TS_CENTER);


                if (FileManager.ClientVersion >= ClientVersions.CV_5020)
                {
                    Add(new Button((int) ButtonType.BuffIcon, 0x7538, 0x7539, 0x7539)
                    {
                        X = 40,
                        Y = 50,
                        ButtonAction = ButtonAction.Activate
                    });
                }

                Lock status = World.Player.StrLock;
                xOffset = _useUOPGumps ? 28 : 40;
                ushort gumpID = 0x0984; //Up

                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock
                Add(_lockers[0] = new GumpPic(xOffset, 76, gumpID, 0));

                _lockers[0].MouseUp += (sender, e) =>
                {
                    World.Player.StrLock = (Lock) (((byte) World.Player.StrLock + 1) % 3);
                    GameActions.ChangeStatLock(0, World.Player.StrLock);
                    Lock st = World.Player.StrLock;
                    ushort gumpid = 0x0984; //Up

                    if (st == Lock.Down)
                        gumpid = 0x0986; //Down
                    else if (st == Lock.Locked)
                        gumpid = 0x082C; //Lock
                    _lockers[0].Graphic = gumpid;
                    _lockers[0].Texture = FileManager.Gumps.GetTexture(gumpid);
                };

                //AddChildren(_lockers[0] = new Button((int)ButtonType.LockerStr, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 76,
                //    ButtonAction = ButtonAction.Activate,
                //});
                status = World.Player.DexLock;
                xOffset = _useUOPGumps ? 28 : 40;
                gumpID = 0x0984; //Up

                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock
                Add(_lockers[1] = new GumpPic(xOffset, 102, gumpID, 0));

                _lockers[1].MouseUp += (sender, e) =>
                {
                    World.Player.DexLock = (Lock) (((byte) World.Player.DexLock + 1) % 3);
                    GameActions.ChangeStatLock(1, World.Player.DexLock);
                    Lock st = World.Player.DexLock;
                    ushort gumpid = 0x0984; //Up

                    if (st == Lock.Down)
                        gumpid = 0x0986; //Down
                    else if (st == Lock.Locked)
                        gumpid = 0x082C; //Lock
                    _lockers[1].Graphic = gumpid;
                    _lockers[1].Texture = FileManager.Gumps.GetTexture(gumpid);
                };
                //AddChildren(_lockers[1] = new Button((int)ButtonType.LockerDex, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 102,
                //    ButtonAction = ButtonAction.Activate
                //});
                status = World.Player.IntLock;
                xOffset = _useUOPGumps ? 28 : 40;
                gumpID = 0x0984; //Up

                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock
                Add(_lockers[2] = new GumpPic(xOffset, 132, gumpID, 0));

                _lockers[2].MouseUp += (sender, e) =>
                {
                    World.Player.IntLock = (Lock) (((byte) World.Player.IntLock + 1) % 3);
                    GameActions.ChangeStatLock(2, World.Player.IntLock);
                    Lock st = World.Player.IntLock;
                    ushort gumpid = 0x0984; //Up

                    if (st == Lock.Down)
                        gumpid = 0x0986; //Down
                    else if (st == Lock.Locked)
                        gumpid = 0x082C; //Lock
                    _lockers[2].Graphic = gumpid;
                    _lockers[2].Texture = FileManager.Gumps.GetTexture(gumpid);
                };
                //AddChildren(_lockers[2] = new Button((int)ButtonType.LockerInt, gumpID, gumpID)
                //{
                //    X = xOffset,
                //    Y = 132,
                //    ButtonAction = ButtonAction.Activate
                //});

                if (_useUOPGumps)
                {
                    xOffset = 80;
                    AddStatTextLabel(World.Player.HitChanceIncrease.ToString(), MobileStats.HitChanceInc, xOffset, 161);
                }
                else
                    xOffset = 88;


                AddStatTextLabel(World.Player.Strength.ToString(), MobileStats.Strength, xOffset, 77);
                AddStatTextLabel(World.Player.Dexterity.ToString(), MobileStats.Dexterity, xOffset, 105);
                AddStatTextLabel(World.Player.Intelligence.ToString(), MobileStats.Intelligence, xOffset, 133);

                int textWidth = 40;

                if (_useUOPGumps)
                {
                    xOffset = 150;

                    AddStatTextLabel($"{World.Player.DefenseChanceIncrease}/{World.Player.MaxDefenseChanceIncrease}", MobileStats.DefenseChanceInc, xOffset, 161);
                }
                else
                    xOffset = 146;


                xOffset -= 5;

                AddStatTextLabel(World.Player.Hits.ToString(), MobileStats.HealthCurrent, xOffset, 70, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
                AddStatTextLabel(World.Player.HitsMax.ToString(), MobileStats.HealthMax, xOffset, 83, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

                AddStatTextLabel(World.Player.Stamina.ToString(), MobileStats.StaminaCurrent, xOffset, 98, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
                AddStatTextLabel(World.Player.StaminaMax.ToString(), MobileStats.StaminaMax, xOffset, 111, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

                AddStatTextLabel(World.Player.Mana.ToString(), MobileStats.ManaCurrent, xOffset, 126, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
                AddStatTextLabel(World.Player.ManaMax.ToString(), MobileStats.ManaMax, xOffset, 139, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

                xOffset += 5;

                Add(new Line(xOffset, 138, Math.Abs(xOffset - 185), 1, 0xFF383838));
                Add(new Line(xOffset, 110, Math.Abs(xOffset - 185), 1, 0xFF383838));
                Add(new Line(xOffset, 82, Math.Abs(xOffset - 185), 1, 0xFF383838));

                if (_useUOPGumps)
                {
                    xOffset = 240;

                    AddStatTextLabel(World.Player.LowerManaCost.ToString(), MobileStats.LowerManaCost, xOffset, 162);
                }
                else
                    xOffset = 220;

                AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, xOffset, 77);
                AddStatTextLabel(World.Player.Luck.ToString(), MobileStats.Luck, xOffset, 105);

                xOffset -= 10;
                AddStatTextLabel(World.Player.Weight.ToString(), MobileStats.WeightCurrent, xOffset, 126, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

                int lineX = _useUOPGumps ? 236 : 216;
                Add(new Line(lineX, 138, Math.Abs(lineX - (_useUOPGumps ? 270 : 250)), 1, 0xFF383838));

                AddStatTextLabel(World.Player.WeightMax.ToString(), MobileStats.WeightMax, xOffset, 139, textWidth, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

                xOffset = _useUOPGumps ? 205 : 188;

                if (_useUOPGumps)
                {
                    xOffset = 320;

                    AddStatTextLabel(World.Player.DamageIncrease.ToString(), MobileStats.DamageChanceInc, xOffset, 105);
                    AddStatTextLabel(World.Player.SwingSpeedIncrease.ToString(), MobileStats.SwingSpeedInc, xOffset, 161);
                }
                else
                {
                    xOffset = 280;

                    AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, xOffset, 105);
                }

                AddStatTextLabel($"{World.Player.DamageMin}-{World.Player.DamageMax}", MobileStats.Damage, xOffset, 77);
                AddStatTextLabel($"{World.Player.Followers}-{World.Player.FollowersMax}", MobileStats.Followers, xOffset, 133);


                xOffset = _useUOPGumps ? 285 : 260;

                if (_useUOPGumps)
                {
                    xOffset = 400;

                    AddStatTextLabel(World.Player.LowerReagentCost.ToString(), MobileStats.LowerReagentCost, xOffset, 77);
                    AddStatTextLabel(World.Player.SpellDamageIncrease.ToString(), MobileStats.SpellDamageInc, xOffset, 105);
                    AddStatTextLabel(World.Player.FasterCasting.ToString(), MobileStats.FasterCasting, xOffset, 133);
                    AddStatTextLabel(World.Player.FasterCastRecovery.ToString(), MobileStats.FasterCastRecovery, xOffset, 161);


                    xOffset = 480;

                    AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, xOffset, 161);

                    xOffset = 475;

                    AddStatTextLabel($"{World.Player.PhysicalResistence}/{World.Player.MaxPhysicResistence}", MobileStats.AR, xOffset, 74);
                    AddStatTextLabel($"{World.Player.FireResistance}/{World.Player.MaxFireResistence}", MobileStats.RF, xOffset, 92);
                    AddStatTextLabel($"{World.Player.ColdResistance}/{World.Player.MaxColdResistence}", MobileStats.RC, xOffset, 106);
                    AddStatTextLabel($"{World.Player.PoisonResistance}/{World.Player.MaxPoisonResistence}", MobileStats.RP, xOffset, 120);
                    AddStatTextLabel($"{World.Player.EnergyResistance}/{World.Player.MaxEnergyResistence}", MobileStats.RE, xOffset, 134);
                }
                else
                {
                    xOffset = 354;

                    AddStatTextLabel(World.Player.PhysicalResistence.ToString(), MobileStats.AR, xOffset, 76);
                    AddStatTextLabel(World.Player.FireResistance.ToString(), MobileStats.RF, xOffset, 92);
                    AddStatTextLabel(World.Player.ColdResistance.ToString(), MobileStats.RC, xOffset, 106);
                    AddStatTextLabel(World.Player.PoisonResistance.ToString(), MobileStats.RP, xOffset, 120);
                    AddStatTextLabel(World.Player.EnergyResistance.ToString(), MobileStats.RE, xOffset, 134);
                }
            }
            else
            {
                if (FileManager.ClientVersion == ClientVersions.CV_308D)
                    AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, 171, 124);
                else if (FileManager.ClientVersion == ClientVersions.CV_308J)
                {
                    AddStatTextLabel(World.Player.StatsCap.ToString(), MobileStats.StatCap, 180, 131);
                    AddStatTextLabel($"{World.Player.Followers}/{World.Player.FollowersMax}", MobileStats.Followers, 180, 144);
                }
            }

            xOffset = _useUOPGumps ? 445 : 334;

            if (_useUOPGumps)
            {
                p.X = 540;
                p.Y = 180;
            }

            _point = p;
        }


        private void AddStatTextLabel(string text, MobileStats stat, int x, int y, int maxWidth = 0, ushort hue = 0x0386, TEXT_ALIGN_TYPE alignment = TEXT_ALIGN_TYPE.TS_LEFT)
        {
            Label label = new Label(text, false, hue, maxWidth, align: alignment, font: 1)
            {
                X = x,
                Y = y
            };

            _labels[(int) stat] = label;
            Add(label);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_refreshTime < totalMS)
            {
                _refreshTime = (long) totalMS + 250;

                _labels[(int) MobileStats.Name].Text = !string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty;

                if (_useUOPGumps)
                    _labels[(int) MobileStats.HitChanceInc].Text = World.Player.HitChanceIncrease.ToString();
                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

                if (_useUOPGumps)
                    _labels[(int) MobileStats.DefenseChanceInc].Text = $"{World.Player.DefenseChanceIncrease}/{World.Player.MaxDefenseChanceIncrease}";
                _labels[(int) MobileStats.HealthCurrent].Text = World.Player.Hits.ToString();
                _labels[(int) MobileStats.HealthMax].Text = World.Player.HitsMax.ToString();
                _labels[(int) MobileStats.StaminaCurrent].Text = World.Player.Stamina.ToString();
                _labels[(int) MobileStats.StaminaMax].Text = World.Player.StaminaMax.ToString();
                _labels[(int) MobileStats.ManaCurrent].Text = World.Player.Mana.ToString();
                _labels[(int) MobileStats.ManaMax].Text = World.Player.ManaMax.ToString();

                if (_useUOPGumps)
                    _labels[(int) MobileStats.LowerManaCost].Text = World.Player.LowerManaCost.ToString();
                _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
                _labels[(int) MobileStats.Luck].Text = World.Player.Luck.ToString();
                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();
                _labels[(int) MobileStats.WeightMax].Text = World.Player.WeightMax.ToString();

                if (_useUOPGumps)
                {
                    _labels[(int) MobileStats.DamageChanceInc].Text = World.Player.DamageIncrease.ToString();
                    _labels[(int) MobileStats.SwingSpeedInc].Text = World.Player.SwingSpeedIncrease.ToString();
                }

                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
                _labels[(int) MobileStats.Damage].Text = $"{World.Player.DamageMin}-{World.Player.DamageMax}";
                _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";

                if (_useUOPGumps)
                {
                    _labels[(int) MobileStats.LowerReagentCost].Text = World.Player.LowerReagentCost.ToString();
                    _labels[(int) MobileStats.SpellDamageInc].Text = World.Player.SpellDamageIncrease.ToString();
                    _labels[(int) MobileStats.FasterCasting].Text = World.Player.FasterCasting.ToString();
                    _labels[(int) MobileStats.FasterCastRecovery].Text = World.Player.FasterCastRecovery.ToString();
                    _labels[(int) MobileStats.AR].Text = $"{World.Player.PhysicalResistence}/{World.Player.MaxPhysicResistence}";
                    _labels[(int) MobileStats.RF].Text = $"{World.Player.FireResistance}/{World.Player.MaxFireResistence}";
                    _labels[(int) MobileStats.RC].Text = $"{World.Player.ColdResistance}/{World.Player.MaxColdResistence}";
                    _labels[(int) MobileStats.RP].Text = $"{World.Player.PoisonResistance}/{World.Player.MaxPoisonResistence}";
                    _labels[(int) MobileStats.RE].Text = $"{World.Player.EnergyResistance}/{World.Player.MaxEnergyResistence}";
                }
                else
                {
                    _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistence.ToString();
                    _labels[(int) MobileStats.RF].Text = World.Player.FireResistance.ToString();
                    _labels[(int) MobileStats.RC].Text = World.Player.ColdResistance.ToString();
                    _labels[(int) MobileStats.RP].Text = World.Player.PoisonResistance.ToString();
                    _labels[(int) MobileStats.RE].Text = World.Player.EnergyResistance.ToString();
                }
            }

            base.Update(totalMS, frameMS);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            if (Engine.GlobalSettings.ShardType != 0)
                Dispose();
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

            if (FileManager.ClientVersion >= ClientVersions.CV_5020)
            {
                Add(new Button((int) ButtonType.BuffIcon, 0x837, 0x838, 0x838)
                {
                    X = 159,
                    Y = 40,
                    ButtonAction = ButtonAction.Activate
                });
                Add(new Label("Buffs", false, 0x0386, 60, 1) { X = 174, Y = 40 });
            }

            Graphic gumpIdHp = 0x0806;

            if (World.Player.IsPoisoned)
                gumpIdHp = 0x0808;
            else if (World.Player.IsYellowHits) gumpIdHp = 0x0809;

            _fillBars[(int) FillStats.Hits] = new GumpPicWithWidth(34, 12, gumpIdHp, 0, 0);
            _fillBars[(int) FillStats.Mana] = new GumpPicWithWidth(34, 25, 0x0806, 0, 0);
            _fillBars[(int) FillStats.Stam] = new GumpPicWithWidth(34, 38, 0x0806, 0, 0);

            Add(_fillBars[(int) FillStats.Hits]);
            Add(_fillBars[(int) FillStats.Mana]);
            Add(_fillBars[(int) FillStats.Stam]);

            UpdateStatusFillBar(FillStats.Hits, World.Player.Hits, World.Player.HitsMax);
            UpdateStatusFillBar(FillStats.Mana, World.Player.Mana, World.Player.ManaMax);
            UpdateStatusFillBar(FillStats.Stam, World.Player.Stamina, World.Player.StaminaMax);

            // Name
            Label text = new Label(!string.IsNullOrEmpty(World.Player.Name) ? World.Player.Name : string.Empty, false, 0x0386, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 108,
                Y = 12
            };

            _labels[(int) MobileStats.Name] = text;
            Add(text);


            // Stat locks
            Add(_lockers[(int) StatType.Str] = new GumpPic(
                                                           10, 73, GetStatLockGraphic(World.Player.StrLock), 0));

            Add(_lockers[(int) StatType.Dex] = new GumpPic(
                                                           10, 102, GetStatLockGraphic(World.Player.DexLock), 0));

            Add(_lockers[(int) StatType.Int] = new GumpPic(
                                                           10, 130, GetStatLockGraphic(World.Player.IntLock), 0));

            _lockers[(int) StatType.Str].MouseUp += (sender, e) =>
            {
                World.Player.StrLock = (Lock) (((byte) World.Player.StrLock + 1) % 3);
                GameActions.ChangeStatLock((byte) StatType.Str, World.Player.StrLock);
                _lockers[(int) StatType.Str].Graphic = GetStatLockGraphic(World.Player.StrLock);
                _lockers[(int) StatType.Str].Texture = FileManager.Gumps.GetTexture(GetStatLockGraphic(World.Player.StrLock));
            };

            _lockers[(int) StatType.Dex].MouseUp += (sender, e) =>
            {
                World.Player.DexLock = (Lock) (((byte) World.Player.DexLock + 1) % 3);
                GameActions.ChangeStatLock((byte) StatType.Dex, World.Player.DexLock);
                _lockers[(int) StatType.Dex].Graphic = GetStatLockGraphic(World.Player.DexLock);
                _lockers[(int) StatType.Dex].Texture = FileManager.Gumps.GetTexture(GetStatLockGraphic(World.Player.DexLock));
            };

            _lockers[(int) StatType.Int].MouseUp += (sender, e) =>
            {
                World.Player.IntLock = (Lock) (((byte) World.Player.IntLock + 1) % 3);
                GameActions.ChangeStatLock((byte) StatType.Int, World.Player.IntLock);
                _lockers[(int) StatType.Int].Graphic = GetStatLockGraphic(World.Player.IntLock);
                _lockers[(int) StatType.Int].Texture = FileManager.Gumps.GetTexture(GetStatLockGraphic(World.Player.IntLock));
            };

            // Str/dex/int text labels
            int xOffset = 60;
            AddStatTextLabel(World.Player.Strength.ToString(), MobileStats.Strength, xOffset, 73);
            AddStatTextLabel(World.Player.Dexterity.ToString(), MobileStats.Dexterity, xOffset, 102);
            AddStatTextLabel(World.Player.Intelligence.ToString(), MobileStats.Intelligence, xOffset, 130);

            // Hits/stam/mana

            AddStatTextLabel(
                             World.Player.Hits.ToString(),
                             MobileStats.HealthCurrent,
                             117, 66,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.HitsMax.ToString(),
                             MobileStats.HealthMax,
                             117, 79,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.Stamina.ToString(),
                             MobileStats.StaminaCurrent,
                             117, 95,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.StaminaMax.ToString(),
                             MobileStats.StaminaMax,
                             117, 108,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.Mana.ToString(),
                             MobileStats.ManaCurrent,
                             117, 124,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(World.Player.ManaMax.ToString(),
                             MobileStats.ManaMax,
                             117, 137,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            // Current over max lines
            Add(new Line(118, 79, 30, 1, 0xFF383838));
            Add(new Line(118, 108, 30, 1, 0xFF383838));
            Add(new Line(118, 137, 30, 1, 0xFF383838));

            // Followers / max followers

            AddStatTextLabel($"{World.Player.Followers}/{World.Player.FollowersMax}",
                             MobileStats.Followers,
                             192, 73,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);


            // Armor, weight / max weight
            AddStatTextLabel(
                             World.Player.PhysicalResistence.ToString(),
                             MobileStats.AR,
                             196, 102,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.Weight.ToString(),
                             MobileStats.WeightCurrent,
                             185, 124,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            AddStatTextLabel(
                             World.Player.WeightMax.ToString(),
                             MobileStats.WeightMax,
                             185, 137,
                             40,
                             alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            Add(new Line(186, 137, 30, 1, 0xFF383838));

            // Hunger satisfaction, murder count, damage, gold

            AddStatTextLabel(
                             World.Player.Luck.ToString(), // FIXME: packet handling
                             MobileStats.HungerSatisfactionMinutes,
                             282, 44);

            AddStatTextLabel(
                             World.Player.StatsCap.ToString(), // FIXME: packet handling
                             MobileStats.MurderCount,
                             260, 73);

            AddStatTextLabel(
                             $"{World.Player.DamageMin}-{World.Player.DamageMax}",
                             MobileStats.Damage,
                             260, 102);

            AddStatTextLabel(
                             World.Player.Gold.ToString(),
                             MobileStats.Gold,
                             254, 132);

            // Timers

            AddStatTextLabel(
                             World.Player.ColdResistance.ToString(), // FIXME: packet handling
                             MobileStats.CriminalTimerSeconds,
                             354, 44);

            AddStatTextLabel(
                             World.Player.FireResistance.ToString(), // FIXME: packet handling
                             MobileStats.MurderCountDecayHours,
                             354, 73);

            AddStatTextLabel(
                             World.Player.PoisonResistance.ToString(), // FIXME: packet handling
                             MobileStats.PvpCooldownSeconds,
                             354, 102);

            AddStatTextLabel(
                             World.Player.EnergyResistance.ToString(), // FIXME: packet handling
                             MobileStats.BandageTimerSeconds,
                             354, 131);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (IsDisposed)
                return;

            if (_refreshTime < totalMS)
            {
                _refreshTime = (long) totalMS + 250;

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
                _labels[(int) MobileStats.AR].Text = World.Player.PhysicalResistence.ToString();
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

            base.Update(totalMS, frameMS);
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.TargetGameObject(World.Player);
                    Mouse.LastLeftButtonClickTime = 0;
                }
                else
                {
                    Point p = new Point(x, y);
                    Rectangle rect = new Rectangle(Bounds.Width - 42, Bounds.Height - 25, Bounds.Width, Bounds.Height);

                    if (rect.Contains(p))
                    {
                        Engine.UI.GetGump<HealthBarGump>(World.Player)?.Dispose();
                        Engine.UI.Add(new HealthBarGump(World.Player) { X = ScreenCoordinateX, Y = ScreenCoordinateY });
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
                    max = 100;

                if (max > 1)
                    max = maxValue * max / 100;
            }

            return max;
        }

        private void UpdateStatusFillBar(FillStats id, int current, int max)
        {
            Graphic gumpId = 0x0806;

            if (id == FillStats.Hits)
            {
                if (World.Player.IsPoisoned)
                    gumpId = 0x0808;
                else if (World.Player.IsYellowHits) gumpId = 0x0809;
            }

            if (max > 0)
            {
                //int percent = (current * 100) / max;
                //percent = (int) Math.Ceiling(100.0);

                //if (percent > 1)
                //{
                //    // Adjust to actual width of fill bar (109)
                //    percent = (109 * percent) / 100;
                //}

                _fillBars[(int) id].Percent = CalculatePercents(max, current, 109);
                _fillBars[(int) id].Texture = FileManager.Gumps.GetTexture(gumpId);
            }
        }

        // TODO: move to base class?
        private void AddStatTextLabel(string text, MobileStats stat, int x, int y, int maxWidth = 0, ushort hue = 0x0386, TEXT_ALIGN_TYPE alignment = TEXT_ALIGN_TYPE.TS_LEFT)
        {
            Label label = new Label(text, false, hue, maxWidth, align: alignment, font: 1)
            {
                X = x - 5,
                Y = y
            };

            _labels[(int) stat] = label;
            Add(label);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            if (Engine.GlobalSettings.ShardType != 2)
                Dispose();
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


    //internal class StatusGump : Gump
    //{
    //    private readonly Label[] _labels = new Label[(int) MobileStats.Max];
    //    private readonly GumpPic[] _lockers = new GumpPic[3];
    //    private readonly bool _useUOPGumps;
    //    private Point _point;
    //    private double _refreshTime;

    //    public StatusGump() : base(0, 0)
    //    {
    //        Engine.UI.GetByLocalSerial<HealthBarGump>(World.Player)?.Dispose();

    //        CanBeSaved = true;
    //        CanMove = true;
    //        _useUOPGumps = FileManager.UseUOPGumps;

    //        BuildGump();
    //    }

    //    private void BuildGump()
    //    {
    //        Clear();

    //        bool oldStatus = Engine.Profile.Current.UseOldStatusGump;
    //        Point p = Point.Zero;

    //        if (FileManager.ClientVersion >= ClientVersions.CV_308D && !oldStatus)
    //            AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
    //        else
    //        {
    //            AddChildren(new GumpPic(0, 0, 0x0802, 0));
    //            p.X = 244;
    //            p.Y = 112;
    //        }

    //        int xOffset = 0;

    //        if (FileManager.ClientVersion >= ClientVersions.CV_308Z && !oldStatus)
    //        {
    //            p.X = 389;
    //            p.Y = 152;


    //            Label text = new Label(string.IsNullOrEmpty(World.Player.Name) ? string.Empty : World.Player.Name, false, 0x0386, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = _useUOPGumps ? 90 : 58,
    //                Y = 50
    //            };
    //            _labels[(int) MobileStats.Name] = text;
    //            AddChildren(text);


    //            if (FileManager.ClientVersion >= ClientVersions.CV_5020)
    //            {
    //                AddChildren(new Button((int) ButtonType.BuffIcon, 0x7538, 0x7538)
    //                {
    //                    X = 40,
    //                    Y = 50,
    //                    ButtonAction = ButtonAction.Activate
    //                });
    //            }

    //            Lock status = World.Player.StrLock;
    //            xOffset = _useUOPGumps ? 28 : 40;
    //            ushort gumpID = 0x0984; //Up

    //            if (status == Lock.Down)
    //                gumpID = 0x0986; //Down
    //            else if (status == Lock.Locked)
    //                gumpID = 0x082C; //Lock
    //            AddChildren(_lockers[0] = new GumpPic(xOffset, 76, gumpID, 0));

    //            _lockers[0].MouseClick += (sender, e) =>
    //            {
    //                World.Player.StrLock = (Lock) (((byte) World.Player.StrLock + 1) % 3);
    //                GameActions.ChangeStatLock(0, World.Player.StrLock);
    //                Lock st = World.Player.StrLock;
    //                ushort gumpid = 0x0984; //Up

    //                if (st == Lock.Down)
    //                    gumpid = 0x0986; //Down
    //                else if (st == Lock.Locked)
    //                    gumpid = 0x082C; //Lock
    //                _lockers[0].Graphic = gumpid;
    //                _lockers[0].Texture = FileManager.Gumps.GetTexture(gumpid);
    //            };

    //            //AddChildren(_lockers[0] = new Button((int)ButtonType.LockerStr, gumpID, gumpID)
    //            //{
    //            //    X = xOffset,
    //            //    Y = 76,
    //            //    ButtonAction = ButtonAction.Activate,                   
    //            //});
    //            status = World.Player.DexLock;
    //            xOffset = _useUOPGumps ? 28 : 40;
    //            gumpID = 0x0984; //Up

    //            if (status == Lock.Down)
    //                gumpID = 0x0986; //Down
    //            else if (status == Lock.Locked)
    //                gumpID = 0x082C; //Lock
    //            AddChildren(_lockers[1] = new GumpPic(xOffset, 102, gumpID, 0));

    //            _lockers[1].MouseClick += (sender, e) =>
    //            {
    //                World.Player.DexLock = (Lock) (((byte) World.Player.DexLock + 1) % 3);
    //                GameActions.ChangeStatLock(1, World.Player.DexLock);
    //                Lock st = World.Player.DexLock;
    //                ushort gumpid = 0x0984; //Up

    //                if (st == Lock.Down)
    //                    gumpid = 0x0986; //Down
    //                else if (st == Lock.Locked)
    //                    gumpid = 0x082C; //Lock
    //                _lockers[1].Graphic = gumpid;
    //                _lockers[1].Texture = FileManager.Gumps.GetTexture(gumpid);
    //            };
    //            //AddChildren(_lockers[1] = new Button((int)ButtonType.LockerDex, gumpID, gumpID)
    //            //{
    //            //    X = xOffset,
    //            //    Y = 102,
    //            //    ButtonAction = ButtonAction.Activate
    //            //});
    //            status = World.Player.IntLock;
    //            xOffset = _useUOPGumps ? 28 : 40;
    //            gumpID = 0x0984; //Up

    //            if (status == Lock.Down)
    //                gumpID = 0x0986; //Down
    //            else if (status == Lock.Locked)
    //                gumpID = 0x082C; //Lock
    //            AddChildren(_lockers[2] = new GumpPic(xOffset, 132, gumpID, 0));

    //            _lockers[2].MouseClick += (sender, e) =>
    //            {
    //                World.Player.IntLock = (Lock) (((byte) World.Player.IntLock + 1) % 3);
    //                GameActions.ChangeStatLock(2, World.Player.IntLock);
    //                Lock st = World.Player.IntLock;
    //                ushort gumpid = 0x0984; //Up

    //                if (st == Lock.Down)
    //                    gumpid = 0x0986; //Down
    //                else if (st == Lock.Locked)
    //                    gumpid = 0x082C; //Lock
    //                _lockers[2].Graphic = gumpid;
    //                _lockers[2].Texture = FileManager.Gumps.GetTexture(gumpid);
    //            };
    //            //AddChildren(_lockers[2] = new Button((int)ButtonType.LockerInt, gumpID, gumpID)
    //            //{
    //            //    X = xOffset,
    //            //    Y = 132,
    //            //    ButtonAction = ButtonAction.Activate
    //            //});

    //            if (_useUOPGumps)
    //            {
    //                xOffset = 80;

    //                text = new Label(World.Player.HitChanceInc.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 161
    //                };
    //                _labels[(int) MobileStats.HitChanceInc] = text;
    //                AddChildren(text);
    //            }
    //            else
    //                xOffset = 88;

    //            text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 77
    //            };
    //            _labels[(int) MobileStats.Strength] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 105
    //            };
    //            _labels[(int) MobileStats.Dexterity] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 133
    //            };
    //            _labels[(int) MobileStats.Intelligence] = text;
    //            AddChildren(text);
    //            int textWidth = 40;

    //            if (_useUOPGumps)
    //            {
    //                xOffset = 150;

    //                text = new Label($"{World.Player.DefenseChanceInc}/{World.Player.MaxDefChance}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 161
    //                };
    //                _labels[(int) MobileStats.DefenseChanceInc] = text;
    //                AddChildren(text);
    //            }
    //            else
    //                xOffset = 146;

    //            text = new Label(World.Player.Hits.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 70
    //            };
    //            _labels[(int) MobileStats.HealthCurrent] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.HitsMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 83
    //            };
    //            _labels[(int) MobileStats.HealthMax] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Stamina.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 98
    //            };
    //            _labels[(int) MobileStats.StaminaCurrent] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.StaminaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 111
    //            };
    //            _labels[(int) MobileStats.StaminaMax] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Mana.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 126
    //            };
    //            _labels[(int) MobileStats.ManaCurrent] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.ManaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 139
    //            };
    //            _labels[(int) MobileStats.ManaMax] = text;
    //            AddChildren(text);
    //            AddChildren(new Line(xOffset, 138, Math.Abs(xOffset - 185), 1, 0xFF383838));
    //            AddChildren(new Line(xOffset, 110, Math.Abs(xOffset - 185), 1, 0xFF383838));
    //            AddChildren(new Line(xOffset, 82, Math.Abs(xOffset - 185), 1, 0xFF383838));

    //            if (_useUOPGumps)
    //            {
    //                xOffset = 240;

    //                text = new Label(World.Player.LowerManaCost.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 162
    //                };
    //                _labels[(int) MobileStats.LowerManaCost] = text;
    //                AddChildren(text);
    //            }
    //            else
    //                xOffset = 220;

    //            text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 77
    //            };
    //            _labels[(int) MobileStats.StatCap] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Luck.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 105
    //            };
    //            _labels[(int) MobileStats.Luck] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Weight.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 126
    //            };
    //            _labels[(int) MobileStats.WeightCurrent] = text;
    //            AddChildren(text);
    //            int lineX = _useUOPGumps ? 236 : 216;
    //            AddChildren(new Line(lineX, 138, Math.Abs(lineX - (_useUOPGumps ? 270 : 250)), 1, 0xFF383838));

    //            text = new Label(World.Player.WeightMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
    //            {
    //                X = xOffset,
    //                Y = 139
    //            };
    //            _labels[(int) MobileStats.WeightMax] = text;
    //            AddChildren(text);
    //            xOffset = _useUOPGumps ? 205 : 188;

    //            if (_useUOPGumps)
    //            {
    //                xOffset = 320;

    //                text = new Label(World.Player.DamageIncrease.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 105
    //                };
    //                _labels[(int) MobileStats.DamageChanceInc] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.SwingSpeedInc.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 161
    //                };
    //                _labels[(int) MobileStats.SwingSpeedInc] = text;
    //                AddChildren(text);
    //            }
    //            else
    //            {
    //                xOffset = 280;

    //                text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 105
    //                };
    //                _labels[(int) MobileStats.Gold] = text;
    //                AddChildren(text);
    //            }

    //            text = new Label($"{World.Player.DamageMin}-{World.Player.DamageMax}", false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 77
    //            };
    //            _labels[(int) MobileStats.Damage] = text;
    //            AddChildren(text);

    //            text = new Label($"{World.Player.Followers}/{World.Player.FollowersMax}", false, 0x0386, font: 1)
    //            {
    //                X = xOffset,
    //                Y = 133
    //            };
    //            _labels[(int) MobileStats.Followers] = text;
    //            AddChildren(text);
    //            xOffset = _useUOPGumps ? 285 : 260;

    //            if (_useUOPGumps)
    //            {
    //                xOffset = 400;

    //                text = new Label(World.Player.LowerReagentCost.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 77
    //                };
    //                _labels[(int) MobileStats.LowerReagentCost] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.SpellDamageInc.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 105
    //                };
    //                _labels[(int) MobileStats.SpellDamageInc] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.FasterCasting.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 133
    //                };
    //                _labels[(int) MobileStats.FasterCasting] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.FasterCastRecovery.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 161
    //                };
    //                _labels[(int) MobileStats.FasterCastRecovery] = text;
    //                AddChildren(text);
    //                xOffset = 480;

    //                text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 161
    //                };
    //                _labels[(int) MobileStats.Gold] = text;
    //                AddChildren(text);
    //                xOffset = 475;

    //                text = new Label($"{World.Player.ResistPhysical}/{World.Player.MaxPhysicRes}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 74
    //                };
    //                _labels[(int) MobileStats.AR] = text;
    //                AddChildren(text);

    //                text = new Label($"{World.Player.ResistFire}/{World.Player.MaxFireRes}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 92
    //                };
    //                _labels[(int) MobileStats.RF] = text;
    //                AddChildren(text);

    //                text = new Label($"{World.Player.ResistCold}/{World.Player.MaxColdRes}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 106
    //                };
    //                _labels[(int) MobileStats.RC] = text;
    //                AddChildren(text);

    //                text = new Label($"{World.Player.ResistPoison}/{World.Player.MaxPoisonRes}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 120
    //                };
    //                _labels[(int) MobileStats.RP] = text;
    //                AddChildren(text);

    //                text = new Label($"{World.Player.ResistEnergy}/{World.Player.MaxEnergyRes}", false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 134
    //                };
    //                _labels[(int) MobileStats.RE] = text;
    //                AddChildren(text);
    //            }
    //            else
    //            {
    //                xOffset = 354;

    //                text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 76
    //                };
    //                _labels[(int) MobileStats.AR] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.ResistFire.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 92
    //                };
    //                _labels[(int) MobileStats.RF] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.ResistCold.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 106
    //                };
    //                _labels[(int) MobileStats.RC] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.ResistPoison.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 120
    //                };
    //                _labels[(int) MobileStats.RP] = text;
    //                AddChildren(text);

    //                text = new Label(World.Player.ResistEnergy.ToString(), false, 0x0386, font: 1)
    //                {
    //                    X = xOffset,
    //                    Y = 134
    //                };
    //                _labels[(int) MobileStats.RE] = text;
    //                AddChildren(text);
    //            }

    //            xOffset = _useUOPGumps ? 445 : 334;
    //        }
    //        else
    //        {
    //            if (p.X == 0)
    //            {
    //                p.X = 243;
    //                p.Y = 150;
    //            }


    //            Label text = new Label(string.IsNullOrEmpty(World.Player.Name) ? string.Empty : World.Player.Name, false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 42
    //            };
    //            _labels[(int) MobileStats.Name] = text;
    //            AddChildren(text);



    //            text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 61
    //            };
    //            _labels[(int) MobileStats.Strength] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 73
    //            };
    //            _labels[(int) MobileStats.Dexterity] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 85
    //            };
    //            _labels[(int) MobileStats.Intelligence] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.IsFemale ? "F" : "M", false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 97
    //            };
    //            _labels[(int) MobileStats.Sex] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 86,
    //                Y = 109
    //            };
    //            _labels[(int) MobileStats.AR] = text;
    //            AddChildren(text);

    //            text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
    //            {
    //                X = 171,
    //                Y = 61
    //            };
    //            _labels[(int) MobileStats.HealthCurrent] = text;
    //            AddChildren(text);

    //            text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
    //            {
    //                X = 171,
    //                Y = 73
    //            };
    //            _labels[(int) MobileStats.ManaCurrent] = text;
    //            AddChildren(text);

    //            text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
    //            {
    //                X = 171,
    //                Y = 85
    //            };
    //            _labels[(int) MobileStats.StaminaCurrent] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 171,
    //                Y = 97
    //            };
    //            _labels[(int) MobileStats.Gold] = text;
    //            AddChildren(text);

    //            text = new Label(World.Player.Weight.ToString(), false, 0x0386, font: 1)
    //            {
    //                X = 171,
    //                Y = 109
    //            };
    //            _labels[(int) MobileStats.WeightCurrent] = text;
    //            AddChildren(text);

    //            if (!oldStatus)
    //            {
    //                if (FileManager.ClientVersion == ClientVersions.CV_308D)
    //                {
    //                    text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
    //                    {
    //                        X = 171,
    //                        Y = 124
    //                    };
    //                    _labels[(int) MobileStats.StatCap] = text;
    //                    AddChildren(text);
    //                }
    //                else if (FileManager.ClientVersion == ClientVersions.CV_308J)
    //                {
    //                    text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
    //                    {
    //                        X = 180,
    //                        Y = 131
    //                    };
    //                    _labels[(int) MobileStats.StatCap] = text;
    //                    AddChildren(text);

    //                    text = new Label($"{World.Player.Followers}/{World.Player.FollowersMax}", false, 0x0386, font: 1)
    //                    {
    //                        X = 180,
    //                        Y = 144
    //                    };
    //                    _labels[(int) MobileStats.Followers] = text;
    //                    AddChildren(text);
    //                }
    //            }
    //        }

    //        if (!_useUOPGumps)
    //        {
    //        }
    //        else
    //        {
    //            p.X = 540;
    //            p.Y = 180;
    //        }

    //        _point = p;
    //    }

    //    protected override void OnMouseClick(int x, int y, MouseButton button)
    //    {
    //        if (button == MouseButton.Left)
    //        {
    //            if (_useUOPGumps)
    //            {
    //                if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
    //                {
    //                    //var list = Engine.SceneManager.GetScene<GameScene>().MobileGumpStack;
    //                    //list.Add(World.Player);
    //                    Engine.UI.Add(new HealthBarGump(World.Player) {X = ScreenCoordinateX, Y = ScreenCoordinateY});

    //                    //if (dict.ContainsKey(World.Player))
    //                    //{
    //                    //    Engine.UI.Remove<HealthBarGump>(World.Player);
    //                    //}
    //                    Dispose();
    //                }
    //            }
    //            else
    //            {
    //                if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
    //                {
    //                    //var list = Engine.SceneManager.GetScene<GameScene>().MobileGumpStack;
    //                    //list.Add(World.Player);
    //                    Engine.UI.Add(new HealthBarGump(World.Player) {X = ScreenCoordinateX, Y = ScreenCoordinateY});
    //                    Dispose();
    //                }
    //            }
    //        }
    //    }

    //    public override void Save(BinaryWriter writer)
    //    {
    //        base.Save(writer);
    //    }

    //    public override void Restore(BinaryReader reader)
    //    {
    //        base.Restore(reader);
    //    }

    //    public override void Update(double totalMS, double frameMS)
    //    {
    //        if (_refreshTime < totalMS)
    //        {
    //            _refreshTime = totalMS + 250;
    //            bool oldStatus = Engine.Profile.Current.UseOldStatusGump;

    //            if (FileManager.ClientVersion > ClientVersions.CV_308Z && !oldStatus)
    //            {
    //                _labels[(int) MobileStats.Name].Text = World.Player.Name;

    //                if (_useUOPGumps)
    //                    _labels[(int) MobileStats.HitChanceInc].Text = World.Player.HitChanceInc.ToString();
    //                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
    //                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
    //                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

    //                if (_useUOPGumps)
    //                    _labels[(int) MobileStats.DefenseChanceInc].Text = $"{World.Player.DefenseChanceInc}/{World.Player.MaxDefChance}";
    //                _labels[(int) MobileStats.HealthCurrent].Text = World.Player.Hits.ToString();
    //                _labels[(int) MobileStats.HealthMax].Text = World.Player.HitsMax.ToString();
    //                _labels[(int) MobileStats.StaminaCurrent].Text = World.Player.Stamina.ToString();
    //                _labels[(int) MobileStats.StaminaMax].Text = World.Player.StaminaMax.ToString();
    //                _labels[(int) MobileStats.ManaCurrent].Text = World.Player.Mana.ToString();
    //                _labels[(int) MobileStats.ManaMax].Text = World.Player.ManaMax.ToString();

    //                if (_useUOPGumps)
    //                    _labels[(int) MobileStats.LowerManaCost].Text = World.Player.LowerManaCost.ToString();
    //                _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
    //                _labels[(int) MobileStats.Luck].Text = World.Player.Luck.ToString();
    //                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();
    //                _labels[(int) MobileStats.WeightMax].Text = World.Player.WeightMax.ToString();

    //                if (_useUOPGumps)
    //                {
    //                    _labels[(int) MobileStats.DamageChanceInc].Text = World.Player.DamageIncrease.ToString();
    //                    _labels[(int) MobileStats.SwingSpeedInc].Text = World.Player.SwingSpeedInc.ToString();
    //                }

    //                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
    //                _labels[(int) MobileStats.Damage].Text = $"{World.Player.DamageMin}-{World.Player.DamageMax}";
    //                _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";

    //                if (_useUOPGumps)
    //                {
    //                    _labels[(int) MobileStats.LowerReagentCost].Text = World.Player.LowerReagentCost.ToString();
    //                    _labels[(int) MobileStats.SpellDamageInc].Text = World.Player.SpellDamageInc.ToString();
    //                    _labels[(int) MobileStats.FasterCasting].Text = World.Player.FasterCasting.ToString();
    //                    _labels[(int) MobileStats.FasterCastRecovery].Text = World.Player.FasterCastRecovery.ToString();
    //                    _labels[(int) MobileStats.AR].Text = $"{World.Player.ResistPhysical}/{World.Player.MaxPhysicRes}";
    //                    _labels[(int) MobileStats.RF].Text = $"{World.Player.ResistFire}/{World.Player.MaxFireRes}";
    //                    _labels[(int) MobileStats.RC].Text = $"{World.Player.ResistCold}/{World.Player.MaxColdRes}";
    //                    _labels[(int) MobileStats.RP].Text = $"{World.Player.ResistPoison}/{World.Player.MaxPoisonRes}";
    //                    _labels[(int) MobileStats.RE].Text = $"{World.Player.ResistEnergy}/{World.Player.MaxEnergyRes}";
    //                }
    //                else
    //                {
    //                    _labels[(int) MobileStats.AR].Text = World.Player.ResistPhysical.ToString();
    //                    _labels[(int) MobileStats.RF].Text = World.Player.ResistFire.ToString();
    //                    _labels[(int) MobileStats.RC].Text = World.Player.ResistCold.ToString();
    //                    _labels[(int) MobileStats.RP].Text = World.Player.ResistPoison.ToString();
    //                    _labels[(int) MobileStats.RE].Text = World.Player.ResistEnergy.ToString();
    //                }
    //            }
    //            else
    //            {
    //                _labels[(int) MobileStats.Name].Text = World.Player.Name;
    //                _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
    //                _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
    //                _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();
    //                _labels[(int) MobileStats.Sex].Text = World.Player.IsFemale ? "F" : "M";
    //                _labels[(int) MobileStats.AR].Text = World.Player.ResistPhysical.ToString();
    //                _labels[(int) MobileStats.HealthCurrent].Text = $"{World.Player.Hits}/{World.Player.HitsMax}";
    //                _labels[(int) MobileStats.ManaCurrent].Text = $"{World.Player.Mana}/{World.Player.ManaMax}";
    //                _labels[(int) MobileStats.StaminaCurrent].Text = $"{World.Player.Stamina}/{World.Player.StaminaMax}";
    //                _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
    //                _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();

    //                if (!oldStatus)
    //                {
    //                    if (FileManager.ClientVersion == ClientVersions.CV_308D)
    //                        _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
    //                    else if (FileManager.ClientVersion == ClientVersions.CV_308J)
    //                    {
    //                        _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
    //                        _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";
    //                    }
    //                }
    //            }
    //        }

    //        base.Update(totalMS, frameMS);
    //    }


    //    public override void OnButtonClick(int buttonID)
    //    {
    //        switch ((ButtonType) buttonID)
    //        {
    //            case ButtonType.BuffIcon:
    //                BuffGump.Toggle();

    //                break;
    //            //case ButtonType.LockerStr:
    //            //    World.Player.StrLock = (Lock)(((byte)World.Player.StrLock + 1) % 3);
    //            //    GameActions.ChangeStatLock(0, World.Player.StrLock);
    //            //    break;
    //            //case ButtonType.LockerDex:
    //            //    World.Player.DexLock = (Lock)(((byte)World.Player.DexLock + 1) % 3);
    //            //    GameActions.ChangeStatLock(1, World.Player.DexLock);
    //            //    break;
    //            //case ButtonType.LockerInt:
    //            //    World.Player.IntLock = (Lock)(((byte)World.Player.IntLock + 1) % 3);
    //            //    GameActions.ChangeStatLock(2, World.Player.IntLock);
    //            //    break;
    //            default:

    //                throw new ArgumentOutOfRangeException(nameof(buttonID), buttonID, null);
    //        }
    //    }

    //    private enum ButtonType
    //    {
    //        BuffIcon,
    //        MininizeMaximize
    //    }

    //    private enum MobileStats
    //    {
    //        Name,
    //        Strength,
    //        Dexterity,
    //        Intelligence,
    //        HealthCurrent,
    //        HealthMax,
    //        StaminaCurrent,
    //        StaminaMax,
    //        ManaCurrent,
    //        ManaMax,
    //        WeightMax,
    //        Followers,
    //        WeightCurrent,
    //        LowerReagentCost,
    //        SpellDamageInc,
    //        FasterCasting,
    //        FasterCastRecovery,
    //        StatCap,
    //        HitChanceInc,
    //        DefenseChanceInc,
    //        LowerManaCost,
    //        DamageChanceInc,
    //        SwingSpeedInc,
    //        Luck,
    //        Gold,
    //        AR,
    //        RF,
    //        RC,
    //        RP,
    //        RE,
    //        Damage,
    //        Sex,
    //        Max
    //    }
    //}
}