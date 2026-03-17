using System.Collections.Generic;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface IHouseManager
    {
        IReadOnlyCollection<House> Houses { get; }

        void Add(uint serial, House revision);
        bool TryGetHouse(uint serial, out House house);
        bool TryToRemove(uint serial, int distance);
        bool IsHouseInRange(uint serial, int distance);
        bool EntityIntoHouse(uint house, GameObject obj);
        void Remove(uint serial);
        bool Exists(uint serial);
        void Clear();
        void RemoveMultiTargetHouse();
    }
}
