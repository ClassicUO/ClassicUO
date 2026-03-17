using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal interface ISkillsGroupManager
    {
        List<SkillsGroup> Groups { get; }

        void Add(SkillsGroup g);
        bool Remove(SkillsGroup g);
        void Load();
        void Save();
        void MakeDefault();
    }
}
