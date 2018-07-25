using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Game.Map;
using ClassicUO.Game.Renderer.Views;

namespace ClassicUO.Game.WorldObjects
{
    public class DeferredEntity : WorldObject
    {
        private readonly Entity _entity;

        public DeferredEntity(in Entity entity, in sbyte z, in Tile tile) : base(World.Map)
        {
            _entity = entity;      
            Position = new Position(entity.Position.X, entity.Position.Y, z);
            Tile = tile;
        }


        protected override View CreateView() => _entity.ViewObject;
    }
}
