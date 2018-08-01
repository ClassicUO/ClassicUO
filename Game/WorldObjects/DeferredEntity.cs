using ClassicUO.Game.Renderer.Views;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class DeferredEntity : WorldObject
    {
        private readonly WorldObject _entity;
        private readonly Vector3 _position;

        public DeferredEntity(in WorldObject entity, in Vector3 position, in sbyte z) : base(World.Map)
        {
            _entity = entity;
            _position = position;
            Position = new Position(0xFFFF, 0xFFFF, z);
        }

        //public DeferredEntity() : base(null)
        //{

        //}

        //public WorldObject Entity { get; set; }
        //public Vector3 AtPosition { get; set; }
        //public sbyte Z { get; set; }


        protected override View CreateView()
        {
            return new DeferredView(this, _entity.ViewObject, _position);
        }

    }
}