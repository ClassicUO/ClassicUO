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

namespace ClassicUO.Game.Data
{
    /// <summary>
    ///     These are the features that are made available by Razor. Servers can explicitly disallow enabling these
    ///     features. However, because this client doesn't support Razor, for the most part, you can ignore these.
    /// </summary>
    [Flags]
    public enum AssistantFeatures : ulong
    {
        None = 0,
        FilterWeather = 1 << 0, // Weather Filter
        FilterLight = 1 << 1, // Light Filter
        SmartTarget = 1 << 2, // Smart Last Target
        RangedTarget = 1 << 3, // Range Check Last Target
        AutoOpenDoors = 1 << 4, // Automatically Open Doors
        DequipOnCast = 1 << 5, // Unequip Weapon on spell cast
        AutoPotionEquip = 1 << 6, // Un/re-equip weapon on potion use
        PoisonedChecks = 1 << 7, // Block heal If poisoned/Macro If Poisoned condition/Heal or Cure self
        LoopedMacros = 1 << 8, // Disallow looping or recursive macros
        UseOnceAgent = 1 << 9, // The use once agent
        RestockAgent = 1 << 10, // The restock agent
        SellAgent = 1 << 11, // The sell agent
        BuyAgent = 1 << 12, // The buy agent
        PotionHotkeys = 1 << 13, // All potion hotkeys
        RandomTargets = 1 << 14, // All random target hotkeys (not target next, last target, target self)
        ClosestTargets = 1 << 15, // All closest target hotkeys
        OverheadHealth = 1 << 16, // Health and Mana/Stam messages shown over player's heads
        AutolootAgent = 1 << 17, // The autoloot agent
        BoneCutterAgent = 1 << 18, // The bone cutter agent
        AdvancedMacros = 1 << 19, // Advanced macro engine
        AutoRemount = 1 << 20, // Auto remount after dismount
        AutoBandage = 1 << 21, // Auto bandage friends, self, last and mount option
        EnemyTargetShare = 1 << 22, // Enemy target share on guild, party or alliance chat
        FilterSeason = 1 << 23, // Season Filter
        SpellTargetShare = 1 << 24, // Spell target share on guild, party or alliance chat
        All = ulong.MaxValue
    }
}