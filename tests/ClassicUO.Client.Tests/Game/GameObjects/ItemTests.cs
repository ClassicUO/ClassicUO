using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.GameObjects
{
    public class ItemTests
    {
        private readonly World _world;

        public ItemTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Create_SetsSerial()
        {
            var item = Item.Create(_world, 0x40000001);

            item.Serial.Should().Be(0x40000001u);
        }

        [Fact]
        public void Graphic_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x0EEA;

            item.Graphic.Should().Be(0x0EEA);
        }

        [Fact]
        public void Hue_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Hue = 0x0035;

            item.Hue.Should().Be(0x0035);
        }

        [Fact]
        public void Amount_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Amount = 100;

            item.Amount.Should().Be(100);
        }

        [Fact]
        public void Layer_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Layer = Layer.Ring;

            item.Layer.Should().Be(Layer.Ring);
        }

        [Fact]
        public void Container_DefaultIs0xFFFFFFFF()
        {
            var item = Item.Create(_world, 0x40000001);

            item.Container.Should().Be(0xFFFFFFFF);
        }

        [Fact]
        public void OnGround_TrueWhenContainerIsInvalid()
        {
            var item = Item.Create(_world, 0x40000001);
            // Default container is 0xFFFFFFFF which is not valid

            item.OnGround.Should().BeTrue();
        }

        [Fact]
        public void OnGround_FalseWhenContainerIsValidSerial()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Container = 0x0001; // valid mobile serial

            item.OnGround.Should().BeFalse();
        }

        [Fact]
        public void OnGround_FalseWhenContainerIsValidItemSerial()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Container = 0x40000002;

            item.OnGround.Should().BeFalse();
        }

        [Fact]
        public void IsCorpse_TrueWhenGraphicIs0x2006()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x2006;

            item.IsCorpse.Should().BeTrue();
        }

        [Fact]
        public void IsCorpse_FalseForOtherGraphics()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x0001;

            item.IsCorpse.Should().BeFalse();
        }

        [Theory]
        [InlineData(0x0EEA, true)]
        [InlineData(0x0EED, true)]
        [InlineData(0x0EF0, true)]
        public void IsCoin_TrueForCoinGraphics(ushort graphic, bool expected)
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = graphic;

            item.IsCoin.Should().Be(expected);
        }

        [Theory]
        [InlineData(0x0001)]
        [InlineData(0x0EEB)]
        [InlineData(0x0EEF)]
        [InlineData(0x0EF1)]
        public void IsCoin_FalseForNonCoinGraphics(ushort graphic)
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = graphic;

            item.IsCoin.Should().BeFalse();
        }

        [Fact]
        public void Opened_DefaultIsFalse()
        {
            var item = Item.Create(_world, 0x40000001);

            item.Opened.Should().BeFalse();
        }

        [Fact]
        public void Opened_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Opened = true;

            item.Opened.Should().BeTrue();
        }

        [Fact]
        public void Price_DefaultIsZero()
        {
            var item = Item.Create(_world, 0x40000001);

            item.Price.Should().Be(0u);
        }

        [Fact]
        public void Price_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Price = 5000;

            item.Price.Should().Be(5000u);
        }

        [Fact]
        public void IsMulti_DefaultIsFalse()
        {
            var item = Item.Create(_world, 0x40000001);

            item.IsMulti.Should().BeFalse();
        }

        [Fact]
        public void IsMulti_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.IsMulti = true;

            item.IsMulti.Should().BeTrue();
        }

        [Fact]
        public void IsMulti_WhenSetToFalse_ClearsMultiDistanceBonus()
        {
            var item = Item.Create(_world, 0x40000001);
            item.IsMulti = true;
            item.IsMulti = false;

            item.MultiDistanceBonus.Should().Be(0);
        }

        [Fact]
        public void IsDamageable_DefaultIsFalse()
        {
            var item = Item.Create(_world, 0x40000001);

            item.IsDamageable.Should().BeFalse();
        }

        [Fact]
        public void IsDamageable_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.IsDamageable = true;

            item.IsDamageable.Should().BeTrue();
        }

        [Fact]
        public void Container_CanBeSetAndRead()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Container = 0x40000002;

            item.Container.Should().Be(0x40000002u);
        }

        [Fact]
        public void OnGround_TrueWhenContainerIsZero()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Container = 0;

            item.OnGround.Should().BeTrue();
        }

        [Fact]
        public void WantUpdateMulti_DefaultIsTrue()
        {
            var item = Item.Create(_world, 0x40000001);

            item.WantUpdateMulti.Should().BeTrue();
        }

        [Fact]
        public void LightID_DefaultIsZero()
        {
            var item = Item.Create(_world, 0x40000001);

            item.LightID.Should().Be(0);
        }

        [Fact]
        public void DisplayedGraphic_ReturnsGraphic_WhenNotCoinAndNotMulti()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x1234;

            item.DisplayedGraphic.Should().Be(0x1234);
        }

        [Fact]
        public void DisplayedGraphic_ReturnsCoinGraphicPlusTwo_WhenAmountGreaterThanFive()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x0EEA;
            item.Amount = 10;

            item.DisplayedGraphic.Should().Be((ushort)(0x0EEA + 2));
        }

        [Fact]
        public void DisplayedGraphic_ReturnsCoinGraphicPlusOne_WhenAmountBetweenTwoAndFive()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x0EEA;
            item.Amount = 3;

            item.DisplayedGraphic.Should().Be((ushort)(0x0EEA + 1));
        }

        [Fact]
        public void DisplayedGraphic_ReturnsCoinGraphic_WhenAmountIsOne()
        {
            var item = Item.Create(_world, 0x40000001);
            item.Graphic = 0x0EEA;
            item.Amount = 1;

            item.DisplayedGraphic.Should().Be(0x0EEA);
        }
    }
}
