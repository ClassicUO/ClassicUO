// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Xml;

namespace ClassicUO.Game.UI.InfoBar
{
    /// <summary>
    /// Facade over the InfoBar collaborators. Keeps the existing public
    /// surface (<c>_world.InfoBars.X</c>) so consumers like
    /// <see cref="UI.Gumps.InfoBarGump"/> and
    /// <see cref="UI.Gumps.OptionsGump"/> are unchanged. The
    /// <see cref="InfoBarItem"/> list and <c>infobar.xml</c> persistence
    /// live in <see cref="IInfoBarStore"/>, and the stock item set in
    /// <see cref="IInfoBarDefaults"/>.
    /// </summary>
    internal sealed class InfoBarManager
    {
        private readonly IInfoBarStore _store;
        private readonly IInfoBarDefaults _defaults;

        /// <summary>Production composition root. Defaults to concrete collaborators. The <paramref name="world"/> parameter is retained for API compatibility; collaborators are world-independent.</summary>
        public InfoBarManager(World world)
            : this(new InfoBarStore(), new InfoBarDefaults())
        {
        }

        /// <summary>Full DI seam — inject the store and defaults collaborators.</summary>
        internal InfoBarManager(IInfoBarStore store, IInfoBarDefaults defaults)
        {
            _store = store;
            _defaults = defaults;
        }

        /// <summary>Backing list of info bar items, exposed for read-only iteration by gumps.</summary>
        public List<InfoBarItem> GetInfoBars() => _store.Items;

        /// <summary>Returns the names of all <see cref="InfoBarVars"/> values, used by the info bar builder UI.</summary>
        public static string[] GetVars()
        {
            return Enum.GetNames(typeof(InfoBarVars));
        }

        public void AddItem(InfoBarItem ibi) => _store.Add(ibi);

        public void RemoveItem(InfoBarItem item) => _store.Remove(item);

        public void Clear() => _store.Clear();

        public void Save() => _store.Save();

        public void Load() => _store.Load(_defaults);

        public void CreateDefault() => _defaults.CreateDefault(_store);
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
        NameNotoriety,
        TithingPoints
    }

    internal class InfoBarItem
    {
        public InfoBarItem(string label, InfoBarVars var, ushort labelColor)
        {
            this.label = label;
            this.var = var;
            hue = labelColor;
        }


        public InfoBarItem(XmlElement xml)
        {
            if (xml == null)
            {
                return;
            }

            label = xml.GetAttribute("text");
            var = (InfoBarVars) int.Parse(xml.GetAttribute("var"));
            hue = ushort.Parse(xml.GetAttribute("hue"));
        }

        public ushort hue;

        public string label;
        public InfoBarVars var;

        public void Save(XmlTextWriter writer)
        {
            writer.WriteStartElement("info");
            writer.WriteAttributeString("text", label);
            writer.WriteAttributeString("var", ((int) var).ToString());
            writer.WriteAttributeString("hue", hue.ToString());
            writer.WriteEndElement();
        }
    }
}
