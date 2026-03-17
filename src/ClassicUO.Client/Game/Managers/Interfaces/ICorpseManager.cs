using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal interface ICorpseManager
    {
        void Add(uint corpse, uint obj, Direction dir, bool run);
        void Remove(uint corpse, uint obj);
        bool Exists(uint corpse, uint obj);
        Item GetCorpseObject(uint serial);
        void Clear();
    }
}
