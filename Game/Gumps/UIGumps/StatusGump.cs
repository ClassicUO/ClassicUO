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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGump : Gump
    {
        private readonly Label[] _labels = new Label[(int) MobileStats.Max];
        //private readonly Texture2D _line;
        private readonly PlayerMobile _mobile = World.Player;
        private readonly int _offset;
        private readonly bool _useOldGump = false;
        private double _refreshTime;

        private bool _useUOPGumps = true;


        public StatusGump() : base(0, 0)
        {
            CanMove = true;
            //_line = new Texture2D(Service.Get<SpriteBatch3D>().GraphicsDevice, 1, 1);
            //_line.SetData(new[] {Color.Black});


            _offset = 82;

            Point p = Point.Zero;


            if (FileManager.ClientVersion >= ClientVersions.CV_308D)
            {
                AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
            }
            else
            {
                AddChildren(new GumpPic(0, 0, 0x0802, 0));

                p.X = 244;
                p.Y = 112;
            }

            int xOffset = 0;

            if (FileManager.ClientVersion >= ClientVersions.CV_308Z)
            {
                p.X = 389;
                p.Y = 152;

                Label text;
                if (!string.IsNullOrEmpty(World.Player.Name))
                {
                    text = new Label(World.Player.Name, false, 0x0386, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        X = _useUOPGumps ? 90 : 58,
                        Y = 50
                    };
                    _labels[(int) MobileStats.Name] = text;
                    AddChildren(text);
                }

                if (FileManager.ClientVersion >= ClientVersions.CV_5020)
                    AddChildren(new Button((int)ButtonType.BuffIcon, 0x7538, 0x7538)
                    {
                        X = 40,
                        Y = 50,
                        ButtonAction = ButtonAction.Activate
                    });


                Lock status = World.Player.StrLock;
                xOffset = _useUOPGumps ? 28 : 40;
                ushort gumpID = 0x0984; //Up
                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock

                AddChildren(new Button((int)ButtonType.LockerStr, gumpID, gumpID)
                {
                    X = xOffset,
                    Y = 76,
                    ButtonAction = ButtonAction.Activate
                });


                status = World.Player.DexLock;
                xOffset = _useUOPGumps ? 28 : 40;
                gumpID = 0x0984; //Up
                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock

                AddChildren(new Button((int)ButtonType.LockerDex, gumpID, gumpID)
                {
                    X = xOffset,
                    Y = 102,
                    ButtonAction = ButtonAction.Activate
                });


                status = World.Player.IntLock;
                xOffset = _useUOPGumps ? 28 : 40;
                gumpID = 0x0984; //Up
                if (status == Lock.Down)
                    gumpID = 0x0986; //Down
                else if (status == Lock.Locked)
                    gumpID = 0x082C; //Lock

                AddChildren(new Button((int)ButtonType.LockerInt, gumpID, gumpID)
                {
                    X = xOffset,
                    Y = 132,
                    ButtonAction = ButtonAction.Activate
                });


                if (_useUOPGumps)
                {
                    xOffset = 80;
                    text = new Label(World.Player.HitChanceInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 161
                    };
                    _labels[(int)MobileStats.HitChanceInc] = text;

                    AddChildren(text);
                }
                else
                {
                    xOffset = 88;
                }

                text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 77
                };
                _labels[(int)MobileStats.Strength] = text;
                AddChildren(text);

                text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 105
                };
                _labels[(int)MobileStats.Dexterity] = text;
                AddChildren(text);

                text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 133
                };
                _labels[(int)MobileStats.Intelligence] = text;
                AddChildren(text);


                int textWidth = 40;

                if (_useUOPGumps)
                {
                    xOffset = 150;

                    text = new Label($"{World.Player.DefenseChanceInc}/{World.Player.MaxDefChance}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 161
                    };
                    _labels[(int)MobileStats.DefenseChanceInc] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 146;
                }

                text = new Label(World.Player.Hits.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 70
                };
                _labels[(int)MobileStats.HealthCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.HitsMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 83
                };
                _labels[(int)MobileStats.HealthMax] = text;
                AddChildren(text);



                text = new Label(World.Player.Stamina.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 98
                };
                _labels[(int)MobileStats.StaminaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.StaminaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 111
                };
                _labels[(int)MobileStats.StaminaMax] = text;
                AddChildren(text);



                text = new Label(World.Player.Mana.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 126
                };
                _labels[(int)MobileStats.ManaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.ManaMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 139
                };
                _labels[(int)MobileStats.ManaMax] = text;
                AddChildren(text);


                AddChildren(new Line(xOffset, 138, Math.Abs(xOffset - 185), 1, 0xFF383838));
                AddChildren(new Line(xOffset, 110, Math.Abs(xOffset - 185), 1, 0xFF383838));
                AddChildren(new Line(xOffset, 82, Math.Abs(xOffset - 185), 1, 0xFF383838));


                if (_useUOPGumps)
                {
                    xOffset = 240;
                    text = new Label(World.Player.LowerManaCost.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 162
                    };
                    _labels[(int)MobileStats.LowerManaCost] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 220;
                }

                text = new Label(World.Player.StatsCap.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 77
                };
                _labels[(int)MobileStats.StatCap] = text;
                AddChildren(text);

                text = new Label(World.Player.Luck.ToString(), false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 105
                };
                _labels[(int)MobileStats.Luck] = text;
                AddChildren(text);

                text = new Label(World.Player.Weight.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 126
                };
                _labels[(int)MobileStats.WeightCurrent] = text;
                AddChildren(text);

                int lineX = _useUOPGumps ? 236 : 216;
                AddChildren(new Line(lineX, 138, Math.Abs( lineX - (_useUOPGumps ? 270 : 250)), 1, 0xFF383838));

                text = new Label(World.Player.WeightMax.ToString(), false, 0x0386, textWidth, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = xOffset,
                    Y = 139
                };
                _labels[(int)MobileStats.WeightMax] = text;
                AddChildren(text);

                xOffset = _useUOPGumps ? 205 : 188;

                if (_useUOPGumps)
                {
                    xOffset = 320;
                    text = new Label(World.Player.DamageChanceInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 105
                    };
                    _labels[(int)MobileStats.DamageChanceInc] = text;
                    AddChildren(text);

                    text = new Label(World.Player.SwingSpeedInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 161
                    };
                    _labels[(int)MobileStats.SwingSpeedInc] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 280;
                    text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 105
                    };
                    _labels[(int)MobileStats.Gold] = text;
                    AddChildren(text);
                }

                text = new Label($"{World.Player.DamageMin}-{World.Player.DamageMax}", false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 77
                };
                _labels[(int)MobileStats.Damage] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Followers}/{World.Player.FollowersMax}", false, 0x0386, font: 1)
                {
                    X = xOffset,
                    Y = 133
                };
                _labels[(int)MobileStats.Followers] = text;
                AddChildren(text);

                xOffset = _useUOPGumps ? 285 : 260;

                if (_useUOPGumps)
                {
                    xOffset = 400;
                    text = new Label(World.Player.LowerReagentCost.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 77
                    };
                    _labels[(int)MobileStats.LowerReagentCost] = text;
                    AddChildren(text);

                    text = new Label(World.Player.SpellDamageInc.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 105
                    };
                    _labels[(int)MobileStats.SpellDamageInc] = text;
                    AddChildren(text);

                    text = new Label(World.Player.FasterCasting.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 133
                    };
                    _labels[(int)MobileStats.FasterCasting] = text;
                    AddChildren(text);

                    text = new Label(World.Player.FasterCastRecovery.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 161
                    };
                    _labels[(int)MobileStats.FasterCastRecovery] = text;
                    AddChildren(text);


                    xOffset = 480;
                    text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 161
                    };
                    _labels[(int)MobileStats.Gold] = text;
                    AddChildren(text);

                    xOffset = 475;
                    text = new Label($"{World.Player.ResistPhysical}/{World.Player.MaxPhysicRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 74
                    };
                    _labels[(int)MobileStats.AR] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistFire}/{World.Player.MaxFireRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 92
                    };
                    _labels[(int)MobileStats.RF] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistCold}/{World.Player.MaxColdRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 106
                    };
                    _labels[(int)MobileStats.RC] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistPoison}/{World.Player.MaxPoisonRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 120
                    };
                    _labels[(int)MobileStats.RP] = text;
                    AddChildren(text);

                    text = new Label($"{World.Player.ResistEnergy}/{World.Player.MaxEnergyRes}", false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 134
                    };
                    _labels[(int)MobileStats.RE] = text;
                    AddChildren(text);
                }
                else
                {
                    xOffset = 354;
                    text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 76
                    };
                    _labels[(int)MobileStats.AR] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistFire.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 92
                    };
                    _labels[(int)MobileStats.RF] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistCold.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 106
                    };
                    _labels[(int)MobileStats.RC] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistPoison.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 120
                    };
                    _labels[(int)MobileStats.RP] = text;
                    AddChildren(text);

                    text = new Label(World.Player.ResistEnergy.ToString(), false, 0x0386, font: 1)
                    {
                        X = xOffset,
                        Y = 134
                    };
                    _labels[(int)MobileStats.RE] = text;
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
                        X = 86,
                        Y = 42
                    };
                    _labels[(int)MobileStats.Name] = text;
                    AddChildren(text);
                }

                text = new Label(World.Player.Strength.ToString(), false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 61
                };
                _labels[(int)MobileStats.Strength] = text;
                AddChildren(text);

                text = new Label(World.Player.Dexterity.ToString(), false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 73
                };
                _labels[(int)MobileStats.Dexterity] = text;
                AddChildren(text);

                text = new Label(World.Player.Intelligence.ToString(), false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 85
                };
                _labels[(int)MobileStats.Intelligence] = text;
                AddChildren(text);

                text = new Label(World.Player.IsFemale ? "F" : "M", false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 97
                };
                _labels[(int)MobileStats.Sex] = text;
                AddChildren(text);

                text = new Label(World.Player.ResistPhysical.ToString(), false, 0x0386, font: 1)
                {
                    X = 86,
                    Y = 109
                };
                _labels[(int)MobileStats.AR] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Hits}/{World.Player.HitsMax}", false, 0x0386, font: 1)
                {
                    X = 171,
                    Y = 61
                };
                _labels[(int)MobileStats.HealthCurrent] = text;
                AddChildren(text);

                text = new Label($"{World.Player.Mana}/{World.Player.ManaMax}", false, 0x0386, font: 1)
                {
                    X = 171,
                    Y = 73
                };
                AddChildren(text);

                text = new Label($"{World.Player.Stamina}/{World.Player.StaminaMax}", false, 0x0386, font: 1)
                {
                    X = 171,
                    Y = 85
                };
                _labels[(int)MobileStats.StaminaCurrent] = text;
                AddChildren(text);

                text = new Label(World.Player.Gold.ToString(), false, 0x0386, font: 1)
                {
                    X = 171,
                    Y = 97
                };
                _labels[(int)MobileStats.Gold] = text;
                AddChildren(text);

                text = new Label(World.Player.Weight.ToString(), false, 0x0386, font: 1)
                {
                    X = 171,
                    Y = 109
                };
                _labels[(int)MobileStats.WeightCurrent] = text;
                AddChildren(text);
            }


            //switch (FileManager.ClientVersion)
            //{
            //    case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && !_useOldGump: //ORIGINAL LARGE GUMP


            //        AddChildren(_labels[(int) MobileStats.Name] = new Label(_mobile.Name, false, 997, 400) {X = 260 - Width / 2, Y = 48});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.Strength] = new Label(_mobile.Name, false, 997, 400) {X = _offset - 4, Y = 76});
            //        AddChildren(_labels[(int) MobileStats.Dexterity] = new Label(_mobile.Dexterity.ToString(), false, 997, 400) {X = _offset - 4, Y = 104});
            //        AddChildren(_labels[(int) MobileStats.Intelligence] = new Label(_mobile.Intelligence.ToString(), false, 997, 400) {X = _offset - 4, Y = 132});
            //        AddChildren(_labels[(int) MobileStats.HitChanceInc] = new Label(_mobile.HitChanceInc.ToString(), false, 997, 400) {X = _offset - 4, Y = 160});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.HealthCurrent] = new Label(_mobile.Hits.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 69});
            //        AddChildren(_labels[(int) MobileStats.HealthMax] = new Label(_mobile.HitsMax.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 82});
            //        AddChildren(_labels[(int) MobileStats.StaminaCurrent] = new Label(_mobile.Stamina.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 97});
            //        AddChildren(_labels[(int) MobileStats.StaminaMax] = new Label(_mobile.StaminaMax.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 110});
            //        AddChildren(_labels[(int) MobileStats.ManaCurrent] = new Label(_mobile.Mana.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 125});
            //        AddChildren(_labels[(int) MobileStats.ManaMax] = new Label(_mobile.ManaMax.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 138});
            //        AddChildren(_labels[(int) MobileStats.DefenseChanceInc] = new Label(_mobile.DefenseChanceInc.ToString(), false, 997, 400) {X = 2 * _offset - 4, Y = 160});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.StatCap] = new Label(_mobile.StatsCap.ToString(), false, 997, 400) {X = 3 * _offset - 4, Y = 76});
            //        AddChildren(_labels[(int) MobileStats.Luck] = new Label(_mobile.Luck.ToString(), false, 997, 400) {X = 3 * _offset - 4, Y = 102});
            //        AddChildren(_labels[(int) MobileStats.WeightCurrent] = new Label(_mobile.Weight.ToString(), false, 997, 400) {X = 3 * _offset - 4, Y = 125});
            //        AddChildren(_labels[(int) MobileStats.WeightMax] = new Label(_mobile.WeightMax.ToString(), false, 997, 400) {X = 3 * _offset - 4, Y = 138});
            //        AddChildren(_labels[(int) MobileStats.LowerManaCost] = new Label(_mobile.LowerManaCost.ToString(), false, 997, 400) {X = 3 * _offset - 4, Y = 160});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.Damage] = new Label(ConcatCurrentMax(_mobile.DamageMin, _mobile.DamageMax), false, 997, 400) {X = 4 * _offset - 4, Y = 75});
            //        AddChildren(_labels[(int) MobileStats.DamageChanceInc] = new Label(_mobile.DamageChanceInc.ToString(), false, 997, 400) {X = 4 * _offset - 4, Y = 102});
            //        AddChildren(_labels[(int) MobileStats.Followers] = new Label(ConcatCurrentMax(_mobile.Followers, _mobile.FollowersMax), false, 997, 400) {X = 4 * _offset - 4, Y = 132});
            //        AddChildren(_labels[(int) MobileStats.SwingSpeedInc] = new Label(_mobile.SwingSpeedInc.ToString(), false, 997, 400) {X = 4 * _offset - 4, Y = 160});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.LowerReagentCost] = new Label(_mobile.LowerReagentCost.ToString(), false, 997, 400) {X = 5 * _offset - 4, Y = 76});
            //        AddChildren(_labels[(int) MobileStats.SpellDamageInc] = new Label(_mobile.SpellDamageInc.ToString(), false, 997, 400) {X = 5 * _offset - 4, Y = 102});
            //        AddChildren(_labels[(int) MobileStats.FasterCasting] = new Label(_mobile.FasterCasting.ToString(), false, 997, 400) {X = 5 * _offset - 4, Y = 132});
            //        AddChildren(_labels[(int) MobileStats.FasterCastRecovery] = new Label(_mobile.FasterCastRecovery.ToString(), false, 997, 400) {X = 5 * _offset - 4, Y = 160});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.AR] = new Label(ConcatCurrentMax(_mobile.ResistPhysical, _mobile.MaxPhysicRes), false, 997, 400) {X = 6 * _offset - 10, Y = 73});
            //        AddChildren(_labels[(int) MobileStats.RF] = new Label(ConcatCurrentMax(_mobile.ResistFire, _mobile.MaxFireRes), false, 997, 400) {X = 6 * _offset - 10, Y = 90});
            //        AddChildren(_labels[(int) MobileStats.RC] = new Label(ConcatCurrentMax(_mobile.ResistCold, _mobile.MaxColdRes), false, 997, 400) {X = 6 * _offset - 10, Y = 105});
            //        AddChildren(_labels[(int) MobileStats.RP] = new Label(ConcatCurrentMax(_mobile.ResistPoison, _mobile.MaxPoisonRes), false, 997, 400) {X = 6 * _offset - 10, Y = 119});
            //        AddChildren(_labels[(int) MobileStats.RE] = new Label(ConcatCurrentMax(_mobile.ResistEnergy, _mobile.MaxEnergyRes), false, 997, 400) {X = 6 * _offset - 10, Y = 135});
            //        AddChildren(_labels[(int) MobileStats.Gold] = new Label(_mobile.Gold.ToString(), false, 997, 400) {X = 6 * _offset - 4, Y = 160});

            //        break;
            //    case var expression when (FileManager.ClientVersion < ClientVersions.CV_308D) | _useOldGump: //OLD GUMP
            //        _offset = 84;
            //        AddChildren(new GumpPic(0, 0, 0x802, 0));
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.Name] = new Label(_mobile.Name, false, 997, 400) {X = _offset, Y = 42});
            //        AddChildren(_labels[(int) MobileStats.Strength] = new Label(_mobile.Strength.ToString(), false, 997, 400) {X = _offset, Y = 62});
            //        AddChildren(_labels[(int) MobileStats.Dexterity] = new Label(_mobile.Dexterity.ToString(), false, 997, 400) {X = _offset, Y = 74});
            //        AddChildren(_labels[(int) MobileStats.Intelligence] = new Label(_mobile.Intelligence.ToString(), false, 997, 400) {X = _offset, Y = 85});
            //        AddChildren(_labels[(int) MobileStats.Sex] = new Label((_mobile.Flags & Flags.Female) != 0 ? "F" : "M", false, 997, 400) {X = _offset, Y = 98});
            //        AddChildren(_labels[(int) MobileStats.AR] = new Label(ConcatCurrentMax(_mobile.ResistPhysical, _mobile.MaxPhysicRes), false, 997, 400) {X = _offset, Y = 110});
            //        //============================================================================================================================
            //        AddChildren(_labels[(int) MobileStats.HealthCurrent] = new Label(_mobile.Hits.ToString(), false, 997, 400) {X = 2 * _offset + 3, Y = 62});
            //        AddChildren(_labels[(int) MobileStats.ManaCurrent] = new Label(_mobile.Mana.ToString(), false, 997, 400) {X = 2 * _offset + 3, Y = 74});
            //        AddChildren(_labels[(int) MobileStats.StaminaCurrent] = new Label(_mobile.Stamina.ToString(), false, 997, 400) {X = 2 * _offset + 3, Y = 85});
            //        AddChildren(_labels[(int) MobileStats.Gold] = new Label(_mobile.Gold.ToString(), false, 997, 400) {X = 2 * _offset + 3, Y = 98});
            //        AddChildren(_labels[(int) MobileStats.WeightCurrent] = new Label(_mobile.Weight.ToString(), false, 997, 400) {X = 2 * _offset + 3, Y = 110});

            //        break;
            //}
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            base.Draw(spriteBatch, position, hue);

            //switch (FileManager.ClientVersion)
            //{
            //    case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && !_useOldGump:
            //        spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 2 * 82 - 4, (int) position.Y + 81, 20, 1), Vector3.Zero);
            //        spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 2 * 82 - 4, (int) position.Y + 109, 20, 1), Vector3.Zero);
            //        spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 2 * 82 - 4, (int) position.Y + 137, 20, 1), Vector3.Zero);
            //        spriteBatch.Draw2D(_line, new Rectangle((int) position.X + 3 * 82 - 4, (int) position.Y + 137, 20, 1), Vector3.Zero);

            //        break;
            //}

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime + 0.5d < totalMS)
            {
                _refreshTime = totalMS;

                switch (FileManager.ClientVersion)
                {
                    case var expression when FileManager.ClientVersion >= ClientVersions.CV_308D && _useUOPGumps:
                        _labels[(int) MobileStats.Name].Text = _mobile.Name;
                        _labels[(int) MobileStats.Strength].Text = _mobile.Strength.ToString();
                        _labels[(int) MobileStats.Dexterity].Text = _mobile.Dexterity.ToString();
                        _labels[(int) MobileStats.Intelligence].Text = _mobile.Intelligence.ToString();
                        _labels[(int) MobileStats.HealthCurrent].Text = _mobile.Hits.ToString();
                        _labels[(int) MobileStats.HealthMax].Text = _mobile.HitsMax.ToString();
                        _labels[(int) MobileStats.StaminaCurrent].Text = _mobile.Stamina.ToString();
                        _labels[(int) MobileStats.StaminaMax].Text = _mobile.StaminaMax.ToString();
                        _labels[(int) MobileStats.ManaCurrent].Text = _mobile.Mana.ToString();
                        _labels[(int) MobileStats.ManaMax].Text = _mobile.ManaMax.ToString();
                        _labels[(int) MobileStats.Followers].Text = ConcatCurrentMax(_mobile.Followers, _mobile.FollowersMax);
                        _labels[(int) MobileStats.WeightCurrent].Text = _mobile.Weight.ToString();
                        _labels[(int) MobileStats.WeightMax].Text = _mobile.WeightMax.ToString();
                        _labels[(int) MobileStats.StatCap].Text = _mobile.StatsCap.ToString();
                        _labels[(int) MobileStats.Luck].Text = _mobile.Luck.ToString();
                        _labels[(int) MobileStats.Gold].Text = _mobile.Gold.ToString();
                        _labels[(int) MobileStats.AR].Text = ConcatCurrentMax(_mobile.ResistPhysical, _mobile.MaxPhysicRes);
                        _labels[(int) MobileStats.RF].Text = ConcatCurrentMax(_mobile.ResistFire, _mobile.MaxFireRes);
                        _labels[(int) MobileStats.RC].Text = ConcatCurrentMax(_mobile.ResistCold, _mobile.MaxColdRes);
                        _labels[(int) MobileStats.RP].Text = ConcatCurrentMax(_mobile.ResistPoison, _mobile.MaxPoisonRes);
                        _labels[(int) MobileStats.RE].Text = ConcatCurrentMax(_mobile.ResistEnergy, _mobile.MaxEnergyRes);
                        _labels[(int) MobileStats.Damage].Text = ConcatCurrentMax(_mobile.DamageMin, _mobile.DamageMax);
                        _labels[(int) MobileStats.LowerReagentCost].Text = _mobile.LowerReagentCost.ToString();
                        _labels[(int) MobileStats.SpellDamageInc].Text = _mobile.SpellDamageInc.ToString();
                        _labels[(int) MobileStats.FasterCasting].Text = _mobile.FasterCasting.ToString();
                        _labels[(int) MobileStats.FasterCastRecovery].Text = _mobile.FasterCastRecovery.ToString();
                        _labels[(int) MobileStats.HitChanceInc].Text = _mobile.HitChanceInc.ToString();
                        _labels[(int) MobileStats.DefenseChanceInc].Text = _mobile.DefenseChanceInc.ToString();
                        _labels[(int) MobileStats.LowerManaCost].Text = _mobile.LowerManaCost.ToString();
                        _labels[(int) MobileStats.DamageChanceInc].Text = _mobile.DamageChanceInc.ToString();
                        _labels[(int) MobileStats.SwingSpeedInc].Text = _mobile.SwingSpeedInc.ToString();

                        break;
                    case var expression when (FileManager.ClientVersion < ClientVersions.CV_308D) && !_useUOPGumps: //OLD GUMP
                        _labels[(int) MobileStats.Name].Text = _mobile.Name;
                        _labels[(int) MobileStats.Strength].Text = _mobile.Strength.ToString();
                        _labels[(int) MobileStats.Dexterity].Text = _mobile.Dexterity.ToString();
                        _labels[(int) MobileStats.Intelligence].Text = _mobile.Intelligence.ToString();
                        _labels[(int) MobileStats.HealthCurrent].Text = ConcatCurrentMax(_mobile.Hits, _mobile.HitsMax);
                        _labels[(int) MobileStats.StaminaCurrent].Text = ConcatCurrentMax(_mobile.Stamina, _mobile.StaminaMax);
                        _labels[(int) MobileStats.ManaCurrent].Text = ConcatCurrentMax(_mobile.Mana, _mobile.ManaMax);
                        _labels[(int) MobileStats.WeightCurrent].Text = ConcatCurrentMax(_mobile.Weight, _mobile.WeightMax);
                        _labels[(int) MobileStats.Gold].Text = _mobile.Gold.ToString();
                        _labels[(int) MobileStats.AR].Text = _mobile.ResistPhysical.ToString();
                        _labels[(int) MobileStats.Sex].Text = (_mobile.Flags & Flags.Female) != 0 ? "F" : "M";

                        break;
                }
            }

            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {

        }

        private string ConcatCurrentMax(int min, int max) => $"{min}/{max}";



        private enum ButtonType
        {
            BuffIcon,
            LockerStr,
            LockerDex,
            LockerInt
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