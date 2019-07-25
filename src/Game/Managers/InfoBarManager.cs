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
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ClassicUO.Game.Managers
{
    internal class InfoBarManager
    {
        private List<InfoBarItem> infoBarItems;

        public InfoBarManager()
        {
            infoBarItems = new List<InfoBarItem>();

            if (Engine.Profile.Current.InfoBarItems != null)
            {
                infoBarItems = Engine.Profile.Current.InfoBarItems?.ToList<InfoBarItem>();
            }
        }

        public List<InfoBarItem> GetInfoBars()
        {
            return infoBarItems;
        }

        public static string[] GetVars(int shardtype)
        {
            if(shardtype != 2)
                return Enum.GetNames(typeof(InfoBarVars));
            else
                return Enum.GetNames(typeof(InfoBarVarsOutlands));
        }

        public void AddItem(InfoBarItem ibi)
        {
            infoBarItems.Add(ibi);
        }

        public void RemoveItem(InfoBarItem item)
        {
            infoBarItems.Remove(item);
        }

        public void Clear()
        {
            infoBarItems.Clear();
        }

    }

    internal enum InfoBarVars
    {
        HP = 0,
        Mana,
        Stamina,
        Weight,
        Followers,
        Gold,
        Damage,
        Armor,
        Luck,
        FireResist,
        ColdResist,
        PoisonResist,
        EnergyResist,
        LowerReagentCost,
        SpellDamageInc,
        FasterCasting,
        FasterCastRecovery,
        HitChanceInc,
        DefenseChanceInc,
        LowerManaCost,
        DamageChanceInc,
        SwingSpeedInc,
        StatsCap,
        NameNotoriety
    }

    internal enum InfoBarVarsOutlands
    {
        HP = 0,
        Mana,
        Stamina,
        Weight,
        Followers,
        Gold,
        Damage,
        Armor,
        FoodSatisfaction,
        MurderTimer,
        CriminalTimer,
        PvpCooldown,
        BandageTimer,
        LowerReagentCost,
        SpellDamageInc,
        FasterCasting,
        FasterCastRecovery,
        HitChanceInc,
        DefenseChanceInc,
        LowerManaCost,
        DamageChanceInc,
        SwingSpeedInc,
        MurderCount,
        NameNotoriety
    }

    [JsonObject]
    internal class InfoBarItem
    {
        [JsonProperty] public string label;
        [JsonProperty] public InfoBarVars var;
        [JsonProperty] public ushort hue;

        [JsonConstructor]
        public InfoBarItem(string _label, InfoBarVars _var, Hue _labelColor)
        {
            label = _label;
            var = _var;
            hue = _labelColor;
        }
    }
}
