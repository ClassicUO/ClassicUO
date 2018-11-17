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

using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.IO.Resources;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class StatusGumpOutlands : StatusGumpBase
    {
        public StatusGumpOutlands() : base()
        {
            Label text;
            Point pos = Point.Zero;
            _labels = new Label[(int)MobileStats.Max];

            AddChildren(new GumpPic(0, 0, 0x2A6C, 0));
            AddChildren(new GumpPic(34, 12, 0x0805, 0));    // Health bar
            AddChildren(new GumpPic(34, 25, 0x0805, 0));    // Mana bar
            AddChildren(new GumpPic(34, 38, 0x0805, 0));    // Stamina bar

            Graphic gumpIdHp = 0x0806;

            if (World.Player.IsPoisoned)
            {
                gumpIdHp = 0x0808;
            }
            else if (World.Player.IsYellowHits)
            {
                gumpIdHp = 0x0809;
            }

            FillStatusBar(34, 12, World.Player.Hits, World.Player.HitsMax, gumpIdHp);
            FillStatusBar(34, 25, World.Player.Mana, World.Player.ManaMax, 0x0806);
            FillStatusBar(34, 38, World.Player.Stamina, World.Player.StaminaMax, 0x0806);

            // Name
            if (!string.IsNullOrEmpty(World.Player.Name))
            {
                text = new Label(World.Player.Name, false, 320, 1, align: TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 100,
                    Y = 10
                };

                _labels[(int)MobileStats.Name] = text;
                AddChildren(text);
            }

            // Stat locks
            AddChildren(_lockers[(int)StatType.Str] = new GumpPic(LOCKER_COLUMN_X, ROW_1_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(World.Player.StrLock), 0));
            AddChildren(_lockers[(int)StatType.Dex] = new GumpPic(LOCKER_COLUMN_X, ROW_2_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(World.Player.DexLock), 0));
            AddChildren(_lockers[(int)StatType.Int] = new GumpPic(LOCKER_COLUMN_X, ROW_3_Y + ROW_HEIGHT - ROW_PADDING, GetStatLockGumpId(World.Player.IntLock), 0));

            _lockers[0].MouseClick += (sender, e) =>
            {
                World.Player.StrLock = (Lock)(((byte)World.Player.StrLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Str, World.Player.StrLock);
                _lockers[(int)StatType.Str].Graphic = GetStatLockGumpId(World.Player.StrLock);
                _lockers[(int)StatType.Str].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(World.Player.StrLock));
            };

            _lockers[1].MouseClick += (sender, e) =>
            {
                World.Player.DexLock = (Lock)(((byte)World.Player.DexLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Dex, World.Player.DexLock);
                _lockers[(int)StatType.Dex].Graphic = GetStatLockGumpId(World.Player.DexLock);
                _lockers[(int)StatType.Dex].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(World.Player.DexLock));
            };

            _lockers[2].MouseClick += (sender, e) =>
            {
                World.Player.IntLock = (Lock)(((byte)World.Player.IntLock + 1) % 3);
                GameActions.ChangeStatLock((byte)StatType.Int, World.Player.IntLock);
                _lockers[(int)StatType.Int].Graphic = GetStatLockGumpId(World.Player.IntLock);
                _lockers[(int)StatType.Int].Texture = IO.Resources.Gumps.GetGumpTexture(GetStatLockGumpId(World.Player.IntLock));
            };

            // Str/dex/int text labels
            int xOffset = COLUMN_1_X + COLUMN_1_ICON_WIDTH;
            AddStatTextLabel(World.Player.Strength.ToString(), MobileStats.Strength, xOffset, ROW_1_Y + ROW_HEIGHT - (3 * ROW_PADDING));
            AddStatTextLabel(World.Player.Dexterity.ToString(), MobileStats.Dexterity, xOffset, ROW_2_Y + ROW_HEIGHT - (3 * ROW_PADDING));
            AddStatTextLabel(World.Player.Intelligence.ToString(), MobileStats.Intelligence, xOffset, ROW_3_Y + ROW_HEIGHT - (3 * ROW_PADDING));

            // Hits/stam/mana
            xOffset = COLUMN_2_X + COLUMN_2_ICON_WIDTH;
            AddStatTextLabel(World.Player.Hits.ToString(), MobileStats.HealthCurrent, xOffset, ROW_1_Y + (ROW_HEIGHT / 2) - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.HitsMax.ToString(), MobileStats.HealthMax, xOffset, ROW_1_Y + ROW_HEIGHT - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.Stamina.ToString(), MobileStats.StaminaCurrent, xOffset, ROW_2_Y + (ROW_HEIGHT / 2) - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.StaminaMax.ToString(), MobileStats.StaminaMax, xOffset, ROW_2_Y + ROW_HEIGHT - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.Mana.ToString(), MobileStats.ManaCurrent, xOffset, ROW_3_Y + (ROW_HEIGHT / 2) - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.ManaMax.ToString(), MobileStats.ManaMax, xOffset, ROW_3_Y + ROW_HEIGHT - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);

            // Current over max lines
            AddChildren(new Line(xOffset, ROW_1_Y + 22, 30, 1, 0xFF383838));
            AddChildren(new Line(xOffset, ROW_2_Y + 22, 30, 1, 0xFF383838));
            AddChildren(new Line(xOffset, ROW_3_Y + 22, 30, 1, 0xFF383838));

            // Followers / max followers
            xOffset = COLUMN_3_X + COLUMN_3_ICON_WIDTH;
            AddStatTextLabel(World.Player.Followers.ToString(), MobileStats.Followers, xOffset, ROW_1_Y + (ROW_HEIGHT / 2) - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.FollowersMax.ToString(), MobileStats.FollowerMax, xOffset, ROW_1_Y + ROW_HEIGHT - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddChildren(new Line(xOffset, ROW_1_Y + 22, 30, 1, 0xFF383838));

            // Armor, weight / max weight
            AddStatTextLabel(World.Player.DefenseChanceInc.ToString(), MobileStats.DefenseChanceInc, xOffset, ROW_2_Y + ROW_HEIGHT - (3 * ROW_PADDING), alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.Weight.ToString(), MobileStats.WeightCurrent, xOffset, ROW_3_Y + (ROW_HEIGHT / 2) - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddStatTextLabel(World.Player.WeightMax.ToString(), MobileStats.WeightMax, xOffset, ROW_3_Y + ROW_HEIGHT - ROW_PADDING, maxWidth: 40, alignment: TEXT_ALIGN_TYPE.TS_CENTER);
            AddChildren(new Line(xOffset, ROW_3_Y + 22, 30, 1, 0xFF383838));

            // Damage, gold
            xOffset = COLUMN_4_X + COLUMN_4_ICON_WIDTH;
            AddStatTextLabel(String.Format("{0}-{1}", World.Player.DamageMin, World.Player.DamageMax), MobileStats.Damage, xOffset, ROW_2_Y + ROW_HEIGHT - (3 * ROW_PADDING));
            AddStatTextLabel(World.Player.Gold.ToString(), MobileStats.Gold, xOffset, ROW_3_Y + ROW_HEIGHT - (3 * ROW_PADDING));

            // TODO: Murder count
            // TODO: Hunger satisfaction minutes remaining

            xOffset = COLUMN_5_X + COLUMN_5_ICON_WIDTH;
            // TODO: Criminal timer seconds remaining
            // TODO: Murder count decay hours remaining
            // TODO: PvP cooldown seconds remaining
            // TODO: Bandage timer seconds remaining
            // TODO: Minimize hitbox
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

        // TODO: move to base class?
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

        // TODO: move to base class?
        private void AddStatTextLabel(string text, MobileStats stat, int x, int y, int maxWidth = 0, ushort hue = 0x0386, TEXT_ALIGN_TYPE alignment = TEXT_ALIGN_TYPE.TS_LEFT)
        {
            Label label = new Label(text, false, hue, maxwidth: maxWidth, align: alignment, font: 1)
            {
                X = x,
                Y = y
            };

            _labels[(int)stat] = label;
            AddChildren(label);
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
            FollowerMax,
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
