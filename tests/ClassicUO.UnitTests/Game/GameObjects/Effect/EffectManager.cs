﻿using Xunit;

namespace ClassicUO.UnitTests.Game.GameObjects.Effect
{
    public class EffectManager
    {
        [Fact]
        public void Create_EffectManager_Add_Single_Effect_Then_Clear_Contents()
        {
            ClassicUO.Game.Managers.EffectManager em = new ClassicUO.Game.Managers.EffectManager();

            em.CreateEffect(ClassicUO.Game.Data.GraphicEffectType.FixedFrom, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);

            Assert.NotNull(em.Items);

            em.Clear();

            Assert.Null(em.Next);
            Assert.Null(em.Previous);
            Assert.Null(em.Items);
        }

        [Fact]
        public void Create_EffectManager_Add_Multiple_Effects_Then_Clear_Contents()
        {
            ClassicUO.Game.Managers.EffectManager em = new ClassicUO.Game.Managers.EffectManager();

            em.CreateEffect(ClassicUO.Game.Data.GraphicEffectType.FixedFrom, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);
            em.CreateEffect(ClassicUO.Game.Data.GraphicEffectType.FixedFrom, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);
            em.CreateEffect(ClassicUO.Game.Data.GraphicEffectType.FixedFrom, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, false, ClassicUO.Game.Data.GraphicEffectBlendMode.Normal);

            Assert.NotNull(em.Items);

            em.Clear();

            Assert.Null(em.Next);
            Assert.Null(em.Previous);
            Assert.Null(em.Items);
        }
    }
}
