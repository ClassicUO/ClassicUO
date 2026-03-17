using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface IIgnoreManager
    {
        HashSet<string> IgnoredCharsList { get; set; }

        void Initialize();
        void AddIgnoredTarget(Entity entity);
        void RemoveIgnoredTarget(string charName);
        void SaveIgnoreList();
    }
}
