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

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;

using ClassicUO.Network;
using static ClassicUO.Network.NetClient;

using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ClassicUO.Dust765.Dust765
{
    internal static class CombatCollection
    {
        //USED CONSTANTS
        // ## BEGIN - END ## // ART / HUE CHANGES
        public const ushort TREE_REPLACE_GRAPHIC = 0x0E59;
        public const ushort TREE_REPLACE_GRAPHIC_TILE = 0x07BD;
        public const ushort BLOCKER_REPLACE_GRAPHIC_TILE = 0x07BD;
        public const ushort BLOCKER_REPLACE_GRAPHIC_STUMP = 0x0E56;
        public const ushort BLOCKER_REPLACE_GRAPHIC_ROCK = 0x1775;
        public const ushort BRIGHT_WHITE_COLOR = 0x080A;
        public const ushort BRIGHT_PINK_COLOR = 0x0503;
        public const ushort BRIGHT_ICE_COLOR = 0x0480;
        public const ushort BRIGHT_FIRE_COLOR = 0x0496;
        public const ushort BRIGHT_POISON_COLOR = 0x0A0B;
        public const ushort BRIGHT_PARALYZE_COLOR = 0x0A13;
        // ## BEGIN - END ## // ART / HUE CHANGES
        
        // ## BEGIN - END ## // ART / HUE CHANGES
        //GAME\SCENES\GAMESCENEDRAWINGSORTING.CS
        public static Static GSDSFilters(Static st)
        {
            if (StaticFilters.IsTree(st.OriginalGraphic, out int index)) //INDEX?
            {
                if (ProfileManager.CurrentProfile.ColorTreeTile)
                    st.Hue = ProfileManager.CurrentProfile.TreeTileHue;
                else
                    st.RestoreOriginalHue();
            }
            if (IsBlockerTreeArt(st.OriginalGraphic) || IsBlockerStoneArt(st.OriginalGraphic))
            {
                if (ProfileManager.CurrentProfile.ColorBlockerTile)
                    st.Hue = ProfileManager.CurrentProfile.BlockerTileHue;
                else
                    st.RestoreOriginalHue();
            }
            return st;
        }

        //GAMEOBJECT\VIEWS\STATICVIEW.CS
        public static ushort ArtloaderFilters(ushort graphic)
        {
            if (StaticFilters.IsTree(graphic, out _))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TreeType == 1)
                {
                    graphic = TREE_REPLACE_GRAPHIC;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TreeType == 2)
                {
                    graphic = TREE_REPLACE_GRAPHIC_TILE;
                }
            }
            if (IsBlockerTreeArt(graphic))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 1)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_STUMP;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 2)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_TILE;
                }
            }
            if (IsBlockerStoneArt(graphic))
            {
                if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 1)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_ROCK;
                }
                else if (ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.BlockerType == 2)
                {
                    graphic = BLOCKER_REPLACE_GRAPHIC_TILE;
                }
            }
            return graphic;
        }

        //GAME\GAMEOBJECTS\VIEWS\ITEMVIEW.CS
        public static ushort StealthtHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.ColorStealth)
                hue = ProfileManager.CurrentProfile.StealthHue;

            if (ProfileManager.CurrentProfile.StealthNeonType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.StealthNeonType == 4)
                hue = BRIGHT_FIRE_COLOR;

            return hue;
        }

        [MethodImpl(256)]
        public static bool IsBlockerTreeArt(ushort g)
        {
            switch (g)
            {
                case 0x1772:
                case 0x177A:
                case 0xC2D:
                case 0xC99:
                case 0xC9B:
                case 0xC9C:
                case 0xC9D:
                case 0xCA6:
                case 0xCC4:

                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsBlockerStoneArt(ushort g)
        {
            switch (g)
            {
                case 0x1363:
                case 0x1364:
                case 0x1365:
                case 0x1366:
                case 0x1367:
                case 0x1368:
                case 0x1369:
                case 0x136A:
                case 0x136B:
                case 0x136C:
                case 0x136D:

                    return true;
            }

            return false;
        }

        [MethodImpl(256)]
        public static bool IsStealthArt(ushort g)
        {
            switch (g)
            {
                case 0x1E03:
                case 0x1E04:
                case 0x1E05:
                case 0x1E06:

                    return true;
            }

            return false;
        }
        // ## BEGIN - END ## // ART / HUE CHANGES

        public static ushort WeaponsHue(ushort hue)
        {
            if (ProfileManager.CurrentProfile.GlowingWeaponsType == 1)
                hue = BRIGHT_WHITE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 2)
                hue = BRIGHT_PINK_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 3)
                hue = BRIGHT_ICE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 4)
                hue = BRIGHT_FIRE_COLOR;
            else if (ProfileManager.CurrentProfile.GlowingWeaponsType == 5)
                hue = ProfileManager.CurrentProfile.HighlightGlowingWeaponsTypeHue;

            return hue;
        }

        //GAME\GAMECURSOR.CS
        public static void UpdateSpelltime()
        {
            GameCursor._spellTime = 30 - ((Time.Ticks - GameCursor._startSpellTime) / 1000); // count down

            // ## BEGIN - END ## // CURSOR
            GameCursor._spellTimeText?.Destroy();
            GameCursor._spellTimeText = RenderedText.Create(GameCursor._spellTime.ToString(), 0x0481, style: FontStyle.BlackBorder);
            // ## BEGIN - END ## // CURSOR
        }
        public static void StartSpelltime()
        {
            GameCursor._startSpellTime = Time.Ticks;
        }

        //NETWORK\PACKETHANDLERS.CS
        public static void SpellCastFromCliloc(string text)
        {
            if (SpellDefinition.WordToTargettype.TryGetValue(text, out SpellDefinition spell))
            {
                GameActions.LastSpellIndexCursor = spell.ID;
            }
            else
            {
                //THIS IS INCASE RAZOR OR ANOTHER ASSISTANT REWRITES THE STRING

                foreach (var key in SpellDefinition.WordToTargettype.Keys)
                {
                    if (text.Contains(key)) //SPELL FOUND
                    {
                        GameActions.LastSpellIndexCursor = SpellDefinition.WordToTargettype[key].ID;

                        //break; //DONT BREAK LOOP BECAUSE OF IN NOX / IN NOX GRAV
                    }
                }
            }
            // ## BEGIN - END ## // ONCASTINGGUMP
            if (ProfileManager.CurrentProfile.OnCastingGump)
            {
                if (!GameActions.iscasting)
                    World.Player.OnCasting.Start((uint) GameActions.LastSpellIndexCursor);
            }
            // ## BEGIN - END ## // ONCASTINGGUMP
        }
    }
}