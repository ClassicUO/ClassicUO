using System;
using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Effect
{
    public class Create
    {
        [Theory]
        [InlineData((int) ClassicUO.Game.Data.GraphicEffectType.FixedXYZ, typeof(ClassicUO.Game.GameObjects.FixedEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.FixedFrom, typeof(ClassicUO.Game.GameObjects.FixedEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.DragEffect, typeof(ClassicUO.Game.GameObjects.DragEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.Moving, typeof(ClassicUO.Game.GameObjects.MovingEffect))]
        [InlineData((int)ClassicUO.Game.Data.GraphicEffectType.Lightning, typeof(ClassicUO.Game.GameObjects.LightningEffect))]
        public void Create_Returns_Effect_Instance(int graphicEffectType, Type type)
        {
            ClassicUO.Game.Managers.EffectManager em = new ClassicUO.Game.Managers.EffectManager();

            em.CreateEffect((ClassicUO.Game.Data.GraphicEffectType) graphicEffectType, 0, 0, 1, 0,0, 0 , 0,0 ,0,0 ,0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);
            
            Assert.IsType(type, em.Items);

            em.Clear();
        }
    }
}
