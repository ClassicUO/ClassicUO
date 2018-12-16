using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Views;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    public class MultiStatic : GameObject
    {
        public MultiStatic(Graphic graphic, Hue hue)
        {
            Graphic = graphic;
            Hue = hue;
        }

        public string Name => ItemData.Name;

        private StaticTiles? _itemData;

        public StaticTiles ItemData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_itemData.HasValue)
                    _itemData = TileData.StaticData[Graphic];
                return _itemData.Value;
            }
        }

        protected override View CreateView() => new MultiStaticView(this);
    }
}
