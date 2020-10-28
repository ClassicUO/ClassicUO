using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Static : GameObject
    {
        private static readonly QueuedPool<Static> _pool = new QueuedPool<Static>
        (
            Constants.PREDICTABLE_STATICS, s =>
            {
                s.IsDestroyed = false;
                s.AlphaHue = 0;
                s.FoliageIndex = 0;
            }
        );

        public string Name => ItemData.Name;

        public ushort OriginalGraphic { get; private set; }

        public ref StaticTiles ItemData => ref TileDataLoader.Instance.StaticData[Graphic];

        public bool IsVegetation;


        public static Static Create(ushort graphic, ushort hue, int index)
        {
            Static s = _pool.GetOne();
            s.Graphic = s.OriginalGraphic = graphic;
            s.Hue = hue;
            s.UpdateGraphicBySeason();

            if (s.ItemData.Height > 5)
            {
                s._canBeTransparent = 1;
            }
            else if (s.ItemData.IsRoof || s.ItemData.IsSurface && s.ItemData.IsBackground || s.ItemData.IsWall)
            {
                s._canBeTransparent = 1;
            }
            else if (s.ItemData.Height == 5 && s.ItemData.IsSurface && !s.ItemData.IsBackground)
            {
                s._canBeTransparent = 1;
            }
            else
            {
                s._canBeTransparent = 0;
            }

            return s;
        }

        public void SetGraphic(ushort g)
        {
            Graphic = g;
        }

        public void RestoreOriginalGraphic()
        {
            Graphic = OriginalGraphic;
        }

        public override void UpdateGraphicBySeason()
        {
            SetGraphic(SeasonManager.GetSeasonGraphic(World.Season, OriginalGraphic));
            AllowedToDraw = !GameObjectHelper.IsNoDrawable(Graphic);
            IsVegetation = StaticFilters.IsVegetation(Graphic);
        }

        public override void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            base.Destroy();
            _pool.ReturnOne(this);
        }
    }
}