#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGump : Gump
    {
        private readonly Label[] _labels = new Label[(int) MobileStats.Max];
        private readonly GumpPic[] _lockers = new GumpPic[3];
        private readonly bool _useUOPGumps;
        private double _refreshTime;

        private Point _point;

        public StatusGump() : base(0, 0)
        {
            CanMove = true;
            _useUOPGumps = FileManager.UseUOPGumps;
            bool oldStatus = Service.Get<Settings>().UseOldStatus;
            Point p = Point.Zero;

            if (FileManager.ClientVersion >= ClientVersions.CV_308D && !oldStatus)
                AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
            else
            {
                AddChildren(new GumpPic(0, 0, 0x0802, 0));
                p.X = 244;
                p.Y = 112;
            }

            int xOffset = 0;

            if (FileManager.ClientVersion >= ClientVersions.CV_308Z && !oldStatus)
            {
                p.X = 389;
                p.Y = 152;
                Label text;

                if (!string.IsNullOrEmpty(World.Player.Name))
                {
                    text = new Label(World.Player.Name, false, 0x0386, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        X = _useUOPGumps ? 90 : 58, Y = 50
                    };
                    _labels[(int) MobileStats.Name] = text;
                    AddChildren(text);
                }

                if (FileManager.ClientVersion >= ClientVersions.CV_5020)
                {
                    AddChildren(new Button((int) ButtonType.BuffIcon, 0x7538, 0x7538)
                    {
                        X = 40, Y = 50, ButtonAction = ButtonAction.Activate
                    });
                }

                Lock status = World.Player.StrLock;
                xOffset = _useUOPGumps ? 28 : 40;
                ushort gumpID = 0x0984; //Up

                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock
                AddChildren(_lockers[0] = new GumpPic(xOffset, 76, gumpID, 0));

                _lockers[0].MouseClick += (sender, e) =>
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
                    _lockers[0].Texture = IO.Resources.Gumps.GetGumpTexture(gumpid);
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
                AddChildren(_lockers[1] = new GumpPic(xOffset, 102, gumpID, 0));

                _lockers[1].MouseClick += (sender, e) =>
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
                    _lockers[1].Texture = IO.Resources.Gumps.GetGumpTexture(gumpid);
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
                AddChildren(_lockers[2] = new GumpPic(xOffset, 132, gumpID, 0));

                _lockers[2].MouseClick += (sender, e) =>
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
                    _lockers[2].Texture = IO.Resources.Gumps.GetGumpTexture(gumpid);
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

                    text = new Label(World.Player.HitChanceInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 161
                    };
                    _labels[(int) MobileStats.HitChanceInc] = text;
                    AddChildren(text);
                }
                else
                    xOffset = 88;

                text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 77
                };
                _labels[(int) MobileStats.Strength] = text;
                AddChildren(text);

                text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 105
                };
                _labels[(int) MobileStats.Dexterity] = text;
                AddChildren(text);

                text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 133
                };
                _labels[(int) MobileStats.Intelligence] = text;
                AddChildren(text);
                int textWidth = 40;

                if (_useUOPGumps)
                {
                    xOffset = 150;

                    text = new Label($"{World.Player.DefenseChanceInc}/{World.Player.MaxDefChance}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 161
                    };
                    _labels[(int) MobileStats.DefenseChanceInc] = text;
                    AddChildren(text);
                }
                else
                    xOffset = 146;

                text = new Label(World.Player.Hits.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 70
                };
                _labels[(int) MobileStats.HealthCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.HitsMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 83
                };
                _labels[(int) MobileStats.HealthMax] = text;
                AddChildren(text);

                text = new Label(World.Player.Stamina.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 98
                };
                _labels[(int) MobileStats.StaminaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.StaminaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 111
                };
                _labels[(int) MobileStats.StaminaMax] = text;
                AddChildren(text);

                text = new Label(World.Player.Mana.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 126
                };
                _labels[(int) MobileStats.ManaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.ManaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 139
                };
                _labels[(int) MobileStats.ManaMax] = text;
                AddChildren(text);
                AddChildren(new Line(xOffset, 138, Math.Abs(xOffset - 185), 1, 0xFF383838));
                AddChildren(new Line(xOffset, 110, Math.Abs(xOffset - 185), 1, 0xFF383838));
                AddChildren(new Line(xOffset, 82, Math.Abs(xOffset - 185), 1, 0xFF383838));

                if (_useUOPGumps)
                {
                    xOffset = 240;

                    text = new Label(World.Player.LowerManaCost.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 162
                    };
                    _labels[(int) MobileStats.LowerManaCost] = text;
                    AddChildren(text);
                }
                else
                    xOffset = 220;

                text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 77
                };
                _labels[(int) MobileStats.StatCap] = text;
                AddChildren(text);

                text = new Label(World.Player.Luck.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 105
                };
                _labels[(int) MobileStats.Luck] = text;
                AddChildren(text);

                text = new Label(World.Player.Weight.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 126
                };
                _labels[(int) MobileStats.WeightCurrent] = text;
                AddChildren(text);
                int lineX = _useUOPGumps ? 236 : 216;
                AddChildren(new Line(lineX, 138, Math.Abs(lineX - (_useUOPGumps ? 270 : 250)), 1, 0xFF383838));

                text = new Label(World.Player.WeightMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset, Y = 139
                };
                _labels[(int) MobileStats.WeightMax] = text;
                AddChildren(text);
                xOffset = _useUOPGumps ? 205 : 188;

                if (_useUOPGumps)
                {
                    xOffset = 320;

                    text = new Label(World.Player.DamageIncrease.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 105
                    };
                    _labels[(int) MobileStats.DamageChanceInc] = text;
                    AddChildren(text);

                    text = new Label(World.Player.SwingSpeedInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 161
                    };
                    _labels[(int) MobileStats.SwingSpeedInc] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 280;

                    text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 105
                    };
                    _labels[(int) MobileStats.Gold] = text;
                    AddChildren(text);
                }

                text = new Label($"{World.Player.DamageMin}-{World.Player.DamageMax}", false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 77
                };
                _labels[(int) MobileStats.Damage] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Followers}/{World.Player.FollowersMax}", false, 0x0386, font: 1)
                {
                    X = xOffset, Y = 133
                };
                _labels[(int) MobileStats.Followers] = text;
                AddChildren(text);
                xOffset = _useUOPGumps ? 285 : 260;

                if (_useUOPGumps)
                {
                    xOffset = 400;

                    text = new Label(World.Player.LowerReagentCost.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 77
                    };
                    _labels[(int) MobileStats.LowerReagentCost] = text;
                    AddChildren(text);

                    text = new Label(World.Player.SpellDamageInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 105
                    };
                    _labels[(int) MobileStats.SpellDamageInc] = text;
                    AddChildren(text);

                    text = new Label(World.Player.FasterCasting.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 133
                    };
                    _labels[(int) MobileStats.FasterCasting] = text;
                    AddChildren(text);

                    text = new Label(World.Player.FasterCastRecovery.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 161
                    };
                    _labels[(int) MobileStats.FasterCastRecovery] = text;
                    AddChildren(text);
                    xOffset = 480;

                    text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 161
                    };
                    _labels[(int) MobileStats.Gold] = text;
                    AddChildren(text);
                    xOffset = 475;

                    text = new Label($"{World.Player.ResistPhysical}/{World.Player.MaxPhysicRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 74
                    };
                    _labels[(int) MobileStats.AR] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistFire}/{World.Player.MaxFireRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 92
                    };
                    _labels[(int) MobileStats.RF] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistCold}/{World.Player.MaxColdRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 106
                    };
                    _labels[(int) MobileStats.RC] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistPoison}/{World.Player.MaxPoisonRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 120
                    };
                    _labels[(int) MobileStats.RP] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistEnergy}/{World.Player.MaxEnergyRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 134
                    };
                    _labels[(int) MobileStats.RE] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 354;

                    text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 76
                    };
                    _labels[(int) MobileStats.AR] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistFire.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 92
                    };
                    _labels[(int) MobileStats.RF] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistCold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 106
                    };
                    _labels[(int) MobileStats.RC] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistPoison.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 120
                    };
                    _labels[(int) MobileStats.RP] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistEnergy.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset, Y = 134
                    };
                    _labels[(int) MobileStats.RE] = text;
                    AddChildren(text);
                }

                xOffset = _useUOPGumps ? 445 : 334;
            }
            else
            {
                if (p.X == 0)
                {
                    p.X = 243;
                    p.Y = 150;
                }

                Label text;

                if (!string.IsNullOrEmpty(World.Player.Name))
                {
                    text = new Label(World.Player.Name, false, 0x0386, font: 1)
                    {
                        X = 86, Y = 42
                    };
                    _labels[(int) MobileStats.Name] = text;
                    AddChildren(text);
                }

                text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
                {
                    X = 86, Y = 61
                };
                _labels[(int) MobileStats.Strength] = text;
                AddChildren(text);

                text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
                {
                    X = 86, Y = 73
                };
                _labels[(int) MobileStats.Dexterity] = text;
                AddChildren(text);

                text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
                {
                    X = 86, Y = 85
                };
                _labels[(int) MobileStats.Intelligence] = text;
                AddChildren(text);

                text = new Label(World.Player.IsFemale ? "F" : "M", false, 0x0386, font: 1)
                {
                    X = 86, Y = 97
                };
                _labels[(int) MobileStats.Sex] = text;
                AddChildren(text);

                text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
                {
                    X = 86, Y = 109
                };
                _labels[(int) MobileStats.AR] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
                {
                    X = 171, Y = 61
                };
                _labels[(int) MobileStats.HealthCurrent] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
                {
                    X = 171, Y = 73
                };
                _labels[(int) MobileStats.ManaCurrent] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
                {
                    X = 171, Y = 85
                };
                _labels[(int) MobileStats.StaminaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                {
                    X = 171, Y = 97
                };
                _labels[(int) MobileStats.Gold] = text;
                AddChildren(text);

                text = new Label(World.Player.Weight.ToString(), false, 0x0386, font: 1)
                {
                    X = 171, Y = 109
                };
                _labels[(int) MobileStats.WeightCurrent] = text;
                AddChildren(text);

                if (!oldStatus)
                {
                    if (FileManager.ClientVersion == ClientVersions.CV_308D)
                    {
                        text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
                        {
                            X = 171, Y = 124
                        };
                        _labels[(int) MobileStats.StatCap] = text;
                        AddChildren(text);
                    }
                    else if (FileManager.ClientVersion == ClientVersions.CV_308J)
                    {
                        text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
                        {
                            X = 180, Y = 131
                        };
                        _labels[(int) MobileStats.StatCap] = text;
                        AddChildren(text);

                        text = new Label($"{World.Player.Followers}/{World.Player.FollowersMax}", false, 0x0386, font: 1)
                        {
                            X = 180, Y = 144
                        };
                        _labels[(int) MobileStats.Followers] = text;
                        AddChildren(text);
                    }
                }
            }


            if (!_useUOPGumps)
            {
               
            }
            else
            {
                p.X = 540;
                p.Y = 180;
            }


            _point = p;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (_useUOPGumps)
                {
                    if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
                    {
                        var dict = Service.Get<SceneManager>().GetScene<GameScene>().MobileGumpStack;
                        MobileHealthGump currentMobileHealthGump;
                        dict.Add(World.Player, World.Player);
                        UIManager.Add(currentMobileHealthGump = new MobileHealthGump(World.Player, ScreenCoordinateX, ScreenCoordinateY));

                        //if (dict.ContainsKey(World.Player))
                        //{
                        //    UIManager.Remove<MobileHealthGump>(World.Player);
                        //}

                        Dispose();

                    }
                }
                else
                {
                    if (x >= _point.X && x <= Width + 16 && y >= _point.Y && y <= Height + 16)
                    {
                        var dict = Service.Get<SceneManager>().GetScene<GameScene>().MobileGumpStack;
                        MobileHealthGump currentMobileHealthGump;
                        dict.Add(World.Player, World.Player);
                        UIManager.Add(currentMobileHealthGump = new MobileHealthGump(World.Player, ScreenCoordinateX, ScreenCoordinateY));


                        Dispose();
                    }
                }
            }
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            return base.Draw(spriteBatch, position, hue);
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime < totalMS)
            {
                _refreshTime = totalMS + 250;
                bool oldStatus = Service.Get<Settings>().UseOldStatus;

                if (FileManager.ClientVersion > ClientVersions.CV_308Z && !oldStatus)
                {
                    _labels[(int) MobileStats.Name].Text = World.Player.Name;

                    if (_useUOPGumps)
                        _labels[(int) MobileStats.HitChanceInc].Text = World.Player.HitChanceInc.ToString();
                    _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
                    _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
                    _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();

                    if (_useUOPGumps)
                        _labels[(int) MobileStats.DefenseChanceInc].Text = $"{World.Player.DefenseChanceInc}/{World.Player.MaxDefChance}";
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
                        _labels[(int) MobileStats.SwingSpeedInc].Text = World.Player.SwingSpeedInc.ToString();
                    }

                    _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
                    _labels[(int) MobileStats.Damage].Text = $"{World.Player.DamageMin}-{World.Player.DamageMax}";
                    _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";

                    if (_useUOPGumps)
                    {
                        _labels[(int) MobileStats.LowerReagentCost].Text = World.Player.LowerReagentCost.ToString();
                        _labels[(int) MobileStats.SpellDamageInc].Text = World.Player.SpellDamageInc.ToString();
                        _labels[(int) MobileStats.FasterCasting].Text = World.Player.FasterCasting.ToString();
                        _labels[(int) MobileStats.FasterCastRecovery].Text = World.Player.FasterCastRecovery.ToString();
                        _labels[(int) MobileStats.AR].Text = $"{World.Player.ResistPhysical}/{World.Player.MaxPhysicRes}";
                        _labels[(int) MobileStats.RF].Text = $"{World.Player.ResistFire}/{World.Player.MaxFireRes}";
                        _labels[(int) MobileStats.RC].Text = $"{World.Player.ResistCold}/{World.Player.MaxColdRes}";
                        _labels[(int) MobileStats.RP].Text = $"{World.Player.ResistPoison}/{World.Player.MaxPoisonRes}";
                        _labels[(int) MobileStats.RE].Text = $"{World.Player.ResistEnergy}/{World.Player.MaxEnergyRes}";
                    }
                    else
                    {
                        _labels[(int) MobileStats.AR].Text = World.Player.ResistPhysical.ToString();
                        _labels[(int) MobileStats.RF].Text = World.Player.ResistFire.ToString();
                        _labels[(int) MobileStats.RC].Text = World.Player.ResistCold.ToString();
                        _labels[(int) MobileStats.RP].Text = World.Player.ResistPoison.ToString();
                        _labels[(int) MobileStats.RE].Text = World.Player.ResistEnergy.ToString();
                    }
                }
                else
                {
                    _labels[(int) MobileStats.Name].Text = World.Player.Name;
                    _labels[(int) MobileStats.Strength].Text = World.Player.Strength.ToString();
                    _labels[(int) MobileStats.Dexterity].Text = World.Player.Dexterity.ToString();
                    _labels[(int) MobileStats.Intelligence].Text = World.Player.Intelligence.ToString();
                    _labels[(int) MobileStats.Sex].Text = World.Player.IsFemale ? "F" : "M";
                    _labels[(int) MobileStats.AR].Text = World.Player.ResistPhysical.ToString();
                    _labels[(int) MobileStats.HealthCurrent].Text = $"{World.Player.Hits}/{World.Player.HitsMax}";
                    _labels[(int) MobileStats.ManaCurrent].Text = $"{World.Player.Mana}/{World.Player.ManaMax}";
                    _labels[(int) MobileStats.StaminaCurrent].Text = $"{World.Player.Stamina}/{World.Player.StaminaMax}";
                    _labels[(int) MobileStats.Gold].Text = World.Player.Gold.ToString();
                    _labels[(int) MobileStats.WeightCurrent].Text = World.Player.Weight.ToString();

                    if (!oldStatus)
                    {
                        if (FileManager.ClientVersion == ClientVersions.CV_308D)
                            _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
                        else if (FileManager.ClientVersion == ClientVersions.CV_308J)
                        {
                            _labels[(int) MobileStats.StatCap].Text = World.Player.StatsCap.ToString();
                            _labels[(int) MobileStats.Followers].Text = $"{World.Player.Followers}/{World.Player.FollowersMax}";
                        }
                    }
                }
            }

            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((ButtonType) buttonID)
            {
                case ButtonType.BuffIcon:
                    BuffGump.Toggle();

                    break;
                //case ButtonType.LockerStr:
                //    World.Player.StrLock = (Lock)(((byte)World.Player.StrLock + 1) % 3);
                //    GameActions.ChangeStatLock(0, World.Player.StrLock);
                //    break;
                //case ButtonType.LockerDex:
                //    World.Player.DexLock = (Lock)(((byte)World.Player.DexLock + 1) % 3);
                //    GameActions.ChangeStatLock(1, World.Player.DexLock);
                //    break;
                //case ButtonType.LockerInt:
                //    World.Player.IntLock = (Lock)(((byte)World.Player.IntLock + 1) % 3);
                //    GameActions.ChangeStatLock(2, World.Player.IntLock);
                //    break;
                default:

                    throw new ArgumentOutOfRangeException(nameof(buttonID), buttonID, null);
            }
        }

        private enum ButtonType
        {
            BuffIcon,
            MininizeMaximize
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
            Max
        }
    }
}