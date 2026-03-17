using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.GameObjects
{
    public class MobileTests
    {
        private readonly World _world;

        public MobileTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Create_SetsSeriall()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.Serial.Should().Be(0x0001u);
        }

        [Fact]
        public void Create_AddsToWorldMobiles_WhenAddedManually()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            _world.Mobiles.Add(mobile);

            _world.Mobiles.Get(0x0001).Should().BeSameAs(mobile);
        }

        [Fact]
        public void Graphic_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x0190;

            mobile.Graphic.Should().Be(0x0190);
        }

        [Fact]
        public void Hue_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Hue = 0x0035;

            mobile.Hue.Should().Be(0x0035);
        }

        [Fact]
        public void Position_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.X = 100;
            mobile.Y = 200;
            mobile.Z = 10;

            mobile.X.Should().Be(100);
            mobile.Y.Should().Be(200);
            mobile.Z.Should().Be(10);
        }

        [Fact]
        public void Name_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Name = "Test Mobile";

            mobile.Name.Should().Be("Test Mobile");
        }

        [Fact]
        public void Hits_And_HitsMax_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Hits = 50;
            mobile.HitsMax = 100;

            mobile.Hits.Should().Be(50);
            mobile.HitsMax.Should().Be(100);
        }

        [Fact]
        public void NotorietyFlag_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.NotorietyFlag = NotorietyFlag.Murderer;

            mobile.NotorietyFlag.Should().Be(NotorietyFlag.Murderer);
        }

        [Fact]
        public void IsFemale_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.IsFemale = true;

            mobile.IsFemale.Should().BeTrue();
        }

        [Fact]
        public void IsRunning_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.IsRunning = true;

            mobile.IsRunning.Should().BeTrue();
        }

        [Fact]
        public void Direction_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Direction = Direction.South;

            mobile.Direction.Should().Be(Direction.South);
        }

        [Fact]
        public void IsDead_ReturnsTrueForDeadGraphic_0x0192()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x0192;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ReturnsTrueForDeadGraphic_0x0193()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x0193;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ReturnsTrueWhenSetExplicitly()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.IsDead = true;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ReturnsFalseForLivingGraphic()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x0190;

            mobile.IsDead.Should().BeFalse();
        }

        [Fact]
        public void IsHidden_ReturnsTrueWhenFlagsHasHidden()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Hidden;

            mobile.IsHidden.Should().BeTrue();
        }

        [Fact]
        public void IsHidden_ReturnsFalseWhenNoHiddenFlag()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.None;

            mobile.IsHidden.Should().BeFalse();
        }

        [Fact]
        public void IsParalyzed_ReturnsTrueWhenFlagsHasFrozen()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.Frozen;

            mobile.IsParalyzed.Should().BeTrue();
        }

        [Fact]
        public void IsParalyzed_ReturnsFalseWhenNoFrozenFlag()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.None;

            mobile.IsParalyzed.Should().BeFalse();
        }

        [Theory]
        [InlineData(0x0190, true)]  // human male
        [InlineData(0x0191, true)]  // human female
        [InlineData(0x0192, true)]  // dead male
        [InlineData(0x0193, true)]  // dead female
        [InlineData(0x00B7, true)]  // range 0x00B7-0x00BA
        [InlineData(0x00BA, true)]
        [InlineData(0x025D, true)]  // range 0x025D-0x0260
        [InlineData(0x0260, true)]
        [InlineData(0x029A, true)]  // gargoyle male
        [InlineData(0x029B, true)]  // gargoyle female
        [InlineData(0x02B6, true)]
        [InlineData(0x02B7, true)]
        [InlineData(0x03DB, true)]
        [InlineData(0x03DF, true)]
        [InlineData(0x03E2, true)]
        [InlineData(0x02E8, true)]
        [InlineData(0x02E9, true)]
        [InlineData(0x04E5, true)]
        [InlineData(0x0001, false)] // not human
        [InlineData(0x0000, false)]
        [InlineData(0x0100, false)]
        public void IsHuman_ReturnsExpected(ushort graphic, bool expected)
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = graphic;

            mobile.IsHuman.Should().Be(expected);
        }

        [Fact]
        public void ClearSteps_EmptiesStepsDeque()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            // Steps starts empty
            mobile.Steps.Count.Should().Be(0);

            mobile.ClearSteps();

            mobile.Steps.Count.Should().Be(0);
        }

        [Fact]
        public void Mana_And_ManaMax_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Mana = 25;
            mobile.ManaMax = 50;

            mobile.Mana.Should().Be(25);
            mobile.ManaMax.Should().Be(50);
        }

        [Fact]
        public void Stamina_And_StaminaMax_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Stamina = 30;
            mobile.StaminaMax = 60;

            mobile.Stamina.Should().Be(30);
            mobile.StaminaMax.Should().Be(60);
        }

        [Fact]
        public void Title_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Title = "Lord";

            mobile.Title.Should().Be("Lord");
        }

        [Fact]
        public void Race_CanBeSetAndRead()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Race = RaceType.ELF;

            mobile.Race.Should().Be(RaceType.ELF);
        }

        [Fact]
        public void GetSecureTradeBox_ReturnsNull_WhenNoItems()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.GetSecureTradeBox().Should().BeNull();
        }

        [Fact]
        public void InWarMode_ReturnsTrueWhenWarModeFlagSet()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.WarMode;

            mobile.InWarMode.Should().BeTrue();
        }

        [Fact]
        public void InWarMode_ReturnsFalseWhenNoWarModeFlag()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.None;

            mobile.InWarMode.Should().BeFalse();
        }

        [Fact]
        public void IsYellowHits_ReturnsTrueWhenYellowBarFlagSet()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Flags = Flags.YellowBar;

            mobile.IsYellowHits.Should().BeTrue();
        }

        [Fact]
        public void SpeedMode_DefaultIsNormal()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.SpeedMode.Should().Be(CharacterSpeedType.Normal);
        }

        [Fact]
        public void IsRenamable_DefaultIsFalse()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.IsRenamable.Should().BeFalse();
        }

        [Fact]
        public void Title_DefaultIsEmpty()
        {
            var mobile = Mobile.Create(_world, 0x0001);

            mobile.Title.Should().Be(string.Empty);
        }

        [Fact]
        public void IsDead_ForGraphic_0x025F_ReturnsTrue()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x025F;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ForGraphic_0x0260_ReturnsTrue()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x0260;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ForGraphic_0x02B6_ReturnsTrue()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x02B6;

            mobile.IsDead.Should().BeTrue();
        }

        [Fact]
        public void IsDead_ForGraphic_0x02B7_ReturnsTrue()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            mobile.Graphic = 0x02B7;

            mobile.IsDead.Should().BeTrue();
        }
    }
}
