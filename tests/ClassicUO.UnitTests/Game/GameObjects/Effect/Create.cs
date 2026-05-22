using ClassicUO.Game;
using System;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Effect
{
    public class Create
    {
        [Theory]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.FixedXYZ, typeof(ClassicUO.Game.Entities.FixedEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.FixedFrom, typeof(ClassicUO.Game.Entities.FixedEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.DragEffect, typeof(ClassicUO.Game.Entities.DragEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.Moving, typeof(ClassicUO.Game.Entities.MovingEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.Lightning, typeof(ClassicUO.Game.Entities.LightningEffect))]
        public void Create_Returns_Effect_Instance(int graphicEffectType, Type type)
        {
            var world = new World();
            var em = new ClassicUO.Game.Effects.EffectManager(world);

            em.CreateEffect((ClassicUO.Game.Data.GraphicEffectType) graphicEffectType, 0, 0, 1, 0,0, 0 , 0,0 ,0,0 ,0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);
            
            Assert.IsType(type, em.Items);

            em.Clear();
            world.Clear();
        }
    }
}
