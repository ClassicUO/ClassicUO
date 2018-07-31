using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects
{
    public class DeferredEntity : WorldObject
    {
        private readonly WorldObject _entity;
        private readonly Vector3 _position;
        private readonly string _description;

        public DeferredEntity(in WorldObject entity, in Vector3 position, in sbyte z, in string des) : base(World.Map)
        {
            _entity = entity;
            _position = position;
            Position = new Position(0xFFFF, 0xFFFF, z);
            //Tile = tile;

            _description = des;
        }


        protected override View CreateView() => new DeferredView(this, _entity.ViewObject, _position);

        public override string ToString()
        {
            return _description;
        }
    }
}
