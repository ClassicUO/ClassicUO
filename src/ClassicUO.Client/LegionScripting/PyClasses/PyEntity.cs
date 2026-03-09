using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses
{
    public class PyEntity : PyGameObject
    {
        public readonly uint Serial;
        public int Distance => GetEntity()?.Distance ?? 0;
        public string Name => GetEntity()?.Name ?? "";

        internal PyEntity(Entity entity) : base(entity)
        {
            if (entity == null) { Serial = 0; return; }
            Serial = entity.Serial;
            _entity = entity;
        }

        public override string ToString() => $"<{__class__} Serial=0x{Serial:X8} Graphic=0x{Graphic:X4} Hue=0x{Hue:X4} Pos=({X},{Y},{Z})>";
        public override string __class__ => "PyEntity";

        public static implicit operator uint(PyEntity entity) => entity?.Serial ?? 0;

        public void SetHue(ushort hue)
        {
            var e = GetEntity();
            if (e != null)
                e.Hue = Hue = hue;
        }

        protected Entity _entity;
        protected Entity GetEntity()
        {
            if (_entity != null && _entity.Serial == Serial) return _entity;
            var e = MainThreadQueue.InvokeOnMainThread(() => World.Get(Serial) as Entity);
            _entity = e;
            return e;
        }
    }
}
