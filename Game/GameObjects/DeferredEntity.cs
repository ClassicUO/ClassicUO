using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    public class DeferredEntity : GameObject
    {
        public DeferredEntity() : base(null)
        {
        }


        public GameObject Entity { get; set; }
        public Vector3 AtPosition { get; set; }
        public sbyte Z { get; set; }
        public Tile AssociatedTile { get; set; }


        public void Reset()
        {
            AssociatedTile.RemoveWorldObject(this);
            DisposeView();
            Map = null;
            Entity = null;
            AtPosition = Vector3.Zero;
            Z = sbyte.MinValue;
        }

        protected override View CreateView()
        {
            return Entity == null ? null : new DeferredView(this, Entity.View, AtPosition);
        }

        public override void Dispose()
        {
            Reset();
            base.Dispose();
        }
    }
}