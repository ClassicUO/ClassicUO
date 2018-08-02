using ClassicUO.Game.Renderer.Views;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class DeferredEntity : WorldObject, IPoolable
    {
        private readonly WorldObject _entity;
        private readonly Vector3 _position;

        public DeferredEntity(in WorldObject entity, in Vector3 position, in sbyte z) : base(World.Map)
        {
            _entity = entity;
            _position = position;
            Position = new Position(0xFFFF, 0xFFFF, z);
        }

        public IPoolable NextNode { get; set; }
        public IPoolable PreviousNode { get; set; }

        private ReturnToPoolDelegate _return;
        public void Initialize(ReturnToPoolDelegate returnDelegate)
        {
            _return = returnDelegate;
        }

        public void Return()
        {
            if (_return != null)
            {
                Reset();
                _return.Invoke(this);
                _return = null;
            }
        }

        private void Reset()
        {
            DisposeView();
            Map = null;
            Entity = null;
            AtPosition = Vector3.Zero;
            Z = sbyte.MinValue;
        }

        public DeferredEntity() : base(null)
        {

        }

        public WorldObject Entity { get; set; }
        public Vector3 AtPosition { get; set; }
        public sbyte Z { get; set; }


        protected override View CreateView()
        {
            return Entity == null ? null : new DeferredView(this, Entity.ViewObject, AtPosition);
        }

        public override void Dispose()
        {
            Return();
            base.Dispose();
        }
    }
}