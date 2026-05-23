// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.Xml;
using ClassicUO.Resources;

namespace ClassicUO.Game.Skills
{
    internal sealed class SkillsGroup
    {
        private readonly byte[] _list = new byte[60];

        public SkillsGroup()
        {
            for (int i = 0; i < _list.Length; i++)
            {
                _list[i] = 0xFF;
            }
        }

        public SkillsGroup Left { get; set; }
        public SkillsGroup Right { get; set; }
        public int Count;
        public bool IsMaximized;
        public string Name = ResGeneral.NoName;

        public byte GetSkill(int index)
        {
            if (index < 0 || index >= Count)
            {
                return 0xFF;
            }

            return _list[index];
        }

        public void Add(byte item)
        {
            if (!Contains(item))
            {
                _list[Count++] = item;
            }
        }

        public void Remove(byte item)
        {
            bool removed = false;

            for (int i = 0; i < Count; i++)
            {
                if (_list[i] == item)
                {
                    removed = true;

                    for (; i < Count - 1; i++)
                    {
                        _list[i] = _list[i + 1];
                    }

                    break;
                }
            }

            if (removed)
            {
                Count--;

                if (Count < 0)
                {
                    Count = 0;
                }

                _list[Count] = 0xFF;
            }
        }

        public bool Contains(byte item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_list[i] == item)
                {
                    return true;
                }
            }

            return false;
        }

        public unsafe void Sort()
        {
            byte* table = stackalloc byte[60];
            int index = 0;

            int count = Client.Game.UO.FileManager.Skills.SkillsCount;

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < Count; j++)
                {
                    if (Client.Game.UO.FileManager.Skills.GetSortedIndex(i) == _list[j])
                    {
                        table[index++] = _list[j];

                        break;
                    }
                }
            }

            for (int j = 0; j < Count; j++)
            {
                _list[j] = table[j];
            }
        }

        public void TransferTo(SkillsGroup group)
        {
            for (int i = 0; i < Count; i++)
            {
                group.Add(_list[i]);
            }

            group.Sort();
        }

        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("group");
            xml.WriteAttributeString("name", Name);
            xml.WriteStartElement("skillids");

            for (int i = 0; i < Count; i++)
            {
                byte idx = GetSkill(i);

                if (idx != 0xFF)
                {
                    xml.WriteStartElement("skill");
                    xml.WriteAttributeString("id", idx.ToString());
                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();
            xml.WriteEndElement();
        }
    }

    /// <summary>
    /// Facade over the Skills group collaborators. Keeps the existing public
    /// surface (<c>_world.SkillsGroupManager.X</c>) so consumers like
    /// <see cref="UI.Gumps.StandardSkillsGump"/> and
    /// <see cref="Configuration.Profile"/> are unchanged. The
    /// <see cref="SkillsGroup"/> list and <c>skillsgroups.xml</c>
    /// persistence live in <see cref="ISkillsGroupStore"/>, default group
    /// production (MUL or fallback) in <see cref="ISkillsGroupDefaults"/>,
    /// and the legacy <c>skillgrp.mul</c> parser in
    /// <see cref="ISkillsGroupMulLoader"/>.
    /// </summary>
    internal sealed class SkillsGroupManager
    {
        private readonly ISkillsGroupStore _store;
        private readonly ISkillsGroupDefaults _defaults;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public SkillsGroupManager(World world)
            : this(new SkillsGroupStore(world), new SkillsGroupDefaults(new SkillsGroupMulLoader()))
        {
        }

        /// <summary>Full DI seam — inject the store and defaults collaborators.</summary>
        internal SkillsGroupManager(ISkillsGroupStore store, ISkillsGroupDefaults defaults)
        {
            _store = store;
            _defaults = defaults;
        }

        /// <summary>Backing list of groups. Exposed for read-only iteration and the <c>StandardSkillsGump</c> "reset to defaults" path that calls <see cref="List{T}.Clear"/> before <see cref="MakeDefault"/>.</summary>
        public List<SkillsGroup> Groups => _store.Groups;

        public void Add(SkillsGroup g) => _store.Add(g);

        public bool Remove(SkillsGroup g) => _store.Remove(g);

        public void Load() => _store.Load(_defaults);

        public void Save() => _store.Save();

        public void MakeDefault() => _defaults.MakeDefault(_store);
    }
}
