using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses
{
    public class PyItem : PyEntity
    {
        public int Amount => GetItem()?.Amount ?? 0;
        public bool IsCorpse => GetItem()?.IsCorpse ?? false;
        public bool Opened => GetItem()?.Opened ?? false;
        public uint Container => GetItem()?.Container ?? 0;

        internal PyItem(Item item) : base(item)
        {
            if (item != null)
                _item = item;
        }

        public static PyItem None => null;

        public override string __class__ => "PyItem";

        private Item _item;
        protected Item GetItem()
        {
            if (_item != null && _item.Serial == Serial) return _item;
            var i = MainThreadQueue.InvokeOnMainThread(() =>
            {
                if (World.Items.TryGetValue(Serial, out var it))
                    return it;
                return null;
            });
            _item = i;
            return i;
        }
    }
}
