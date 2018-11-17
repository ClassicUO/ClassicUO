#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//  This project is an alternative client for the game Ultima Online.
//  The goal of this is to develop a lightweight client considering 
//  new technologies.
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

<<<<<<< HEAD
using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;
=======
using ClassicUO.Game.Gumps.Controls;
>>>>>>> f7efc4b7791106650e9eb44210997702c265b782

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGumpOutlands : StatusGumpBase
    {
        public StatusGumpOutlands() : base()
        {
            Point pos = Point.Zero;
            _labels = new Label[(int)MobileStats.Max];

            AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
            AddChildren(new GumpPic(34, 12, 0x0805, 0));    // Health bar
            AddChildren(new GumpPic(34, 25, 0x0805, 0));    // Mana bar
            AddChildren(new GumpPic(34, 38, 0x0805, 0));    // Stamina bar

            Graphic gumpIdHp = 0x0806;

            if (_mobile.IsPoisoned)
            {
                gumpIdHp = 0x0808;
            }
            else if (_mobile.IsYellowHits)
            {
                gumpIdHp = 0x0809;
            }

            FillStatusBar(34, 12, _mobile.Hits, _mobile.HitsMax, gumpIdHp);
            FillStatusBar(34, 25, _mobile.Mana, _mobile.ManaMax, 0x0806);
            FillStatusBar(34, 38, _mobile.Stamina, _mobile.StaminaMax, 0x0806);

            // Name
            if (!string.IsNullOrEmpty(World.Player.Name))
            {
                Label text = new Label(World.Player.Name, false, 0x0386, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 100,
                    Y = 10
                };

                _labels[(int)MobileStats.Name] = text;
                AddChildren(text);
            }

            // Stat locks
            AddChildren(_lockers[(int)StatType.Str] = new GumpPic(LOCKER_COLUMN_X, ROW_1_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(_mobile.StrLock), 0));
            AddChildren(_lockers[(int)StatType.Dex] = new GumpPic(LOCKER_COLUMN_X, ROW_2_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(_mobile.DexLock), 0));
            AddChildren(_lockers[(int)StatType.Int] = new GumpPic(LOCKER_COLUMN_X, ROW_3_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(_mobile.IntLock), 0));

            _lockers[0].MouseClick += (sender, e) =>
            {
                _mobile.StrLock = (Lock)(((byte)_mobile.StrLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Str, _mobile.StrLock);
                _lockers[(int)StatType.Str].Graphic = GetStatLockGumpId(_mobile.StrLock);
                _lockers[(int)StatType.Str].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(_mobile.StrLock));
            };

            _lockers[1].MouseClick += (sender, e) =>
            {
                _mobile.DexLock = (Lock)(((byte)_mobile.DexLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Dex, _mobile.DexLock);
                _lockers[(int)StatType.Dex].Graphic = GetStatLockGumpId(_mobile.DexLock);
                _lockers[(int)StatType.Dex].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(_mobile.DexLock));
            };

            _lockers[2].MouseClick += (sender, e) =>
            {
                _mobile.IntLock = (Lock)(((byte)_mobile.IntLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Int, _mobile.IntLock);
                _lockers[(int)StatType.Int].Graphic = GetStatLockGumpId(_mobile.IntLock);
                _lockers[(int)StatType.Int].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(_mobile.IntLock));
            };

            // [1] Str/dex/int
            // [1] Str/stam/mana
            // [2] Hits / max hits
            // [2] Stam / max stam
            // [2] Mana / max mana
            // [3] Followers / max followers
            // [3] Armor
            // [3] Weight / max weight
            // [4] Hunger satisfaction minutes remaining
            // [4] Murder count
            // [4] Min damange - max damage
            // [4] Gold
            // [5] Criminal timer seconds remaining
            // [5] Murder count decay hours remaining
            // [5] PvP cooldown seconds remaining
            // [5] Bandage timer seconds remaining
            // [5] Minimize hitbox
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_refreshTime < totalMS)
            {
                _refreshTime = totalMS + 250;

                // TODO: Update labels (from World.Player)
            }

            base.Update(totalMS, frameMS);
        }

        private void FillStatusBar(int x, int y, int current, int max, Graphic gumpId)
        {
            // TODO: max 109?
            int percent = (current * 100) / max;
            percent = (int)Math.Ceiling(100.0);

            if (percent > 0)
            {
                AddChildren(new GumpPicTiled(x, y, percent, 10, gumpId));
            }
        }

        private Graphic GetStatLockGumpId(Lock lockStatus)
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
                    return Graphic.Invalid;
            }
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

        private const int ROW_0_Y = 26;
        private const int ROW_1_Y = 56;
        private const int ROW_2_Y = 86;
        private const int ROW_3_Y = 116;
        private const int ROW_HEIGHT = 24;
        private const int ROW_PADDING = 2;

        private const int LOCKER_COLUMN_X = 10;
        private const int LOCKER_COLUMN_WIDTH = 10;

        private const int COLUMN_1_X = 20;
        private const int COLUMN_1_WIDTH = 80;
        private const int COLUMN_1_ICON_WIDTH = 35;

        private const int COLUMN_2_X = 100;
        private const int COLUMN_2_WIDTH = 60;
        private const int COLUMN_2_ICON_WIDTH = 20;

        private const int COLUMN_3_X = 160;
        private const int COLUMN_3_WIDTH = 60;
        private const int COLUMN_3_ICON_WIDTH = 30;

        private const int COLUMN_4_X = 220;
        private const int COLUMN_4_WIDTH = 80;
        private const int COLUMN_4_ICON_WIDTH = 35;

        private const int COLUMN_5_X = 300;
        private const int COLUMN_5_WIDTH = 80;
        private const int COLUMN_5_ICON_WIDTH = 55;
    }
}
