using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game
{
    public class WorldTests
    {
        private readonly World _world;

        public WorldTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void Constructor_StoresProfile()
        {
            var profile = TestHelpers.CreateTestProfile();
            var world = new World(TestHelpers.CreateTestContext(profile: profile));

            world.Profile.Should().BeSameAs(profile);
        }

        [Fact]
        public void Items_StartsEmpty()
        {
            _world.Items.Should().BeEmpty();
        }

        [Fact]
        public void Mobiles_StartsEmpty()
        {
            _world.Mobiles.Should().BeEmpty();
        }

        [Fact]
        public void Player_InitiallyNull()
        {
            _world.Player.Should().BeNull();
        }

        [Fact]
        public void InGame_FalseWhenNoPlayer()
        {
            _world.InGame.Should().BeFalse();
        }

        [Fact]
        public void Contains_ReturnsFalse_ForInvalidSerial()
        {
            _world.Contains(0).Should().BeFalse();
        }

        [Fact]
        public void Contains_ReturnsFalse_ForNonExistentItemSerial()
        {
            // Item serial range: >= 0x40000000
            _world.Contains(0x40000001).Should().BeFalse();
        }

        [Fact]
        public void Contains_ReturnsFalse_ForNonExistentMobileSerial()
        {
            // Mobile serial range: > 0 and < 0x40000000
            _world.Contains(0x00000001).Should().BeFalse();
        }

        [Fact]
        public void Get_ReturnsNull_ForNonExistentSerial()
        {
            _world.Get(0x40000001).Should().BeNull();
        }

        [Fact]
        public void Get_ReturnsNull_ForZeroSerial()
        {
            _world.Get(0).Should().BeNull();
        }

        [Fact]
        public void GetOrCreateItem_CreatesAndAddsItem()
        {
            uint serial = 0x40000001;

            var item = _world.GetOrCreateItem(serial);

            item.Should().NotBeNull();
            item.Serial.Should().Be(serial);
            _world.Items.Should().ContainKey(serial);
        }

        [Fact]
        public void GetOrCreateMobile_CreatesAndAddsMobile()
        {
            uint serial = 0x00000001;

            var mob = _world.GetOrCreateMobile(serial);

            mob.Should().NotBeNull();
            mob.Serial.Should().Be(serial);
            _world.Mobiles.Should().ContainKey(serial);
        }

        [Fact]
        public void GetOrCreateItem_ReturnsSameForSameSerial()
        {
            uint serial = 0x40000001;

            var item1 = _world.GetOrCreateItem(serial);
            var item2 = _world.GetOrCreateItem(serial);

            item1.Should().BeSameAs(item2);
        }

        [Fact]
        public void GetOrCreateMobile_ReturnsSameForSameSerial()
        {
            uint serial = 0x00000001;

            var mob1 = _world.GetOrCreateMobile(serial);
            var mob2 = _world.GetOrCreateMobile(serial);

            mob1.Should().BeSameAs(mob2);
        }

        // Note: RemoveItem/RemoveMobile tests that call Destroy() are skipped because
        // Entity.Destroy() calls GameActions.SendCloseStatus which requires initialized
        // network/static state not available in unit tests.

        [Fact]
        public void RemoveItem_ReturnsFalse_ForNonExistent()
        {
            _world.RemoveItem(0x40000001).Should().BeFalse();
        }

        [Fact]
        public void RemoveMobile_ReturnsFalse_ForNonExistent()
        {
            _world.RemoveMobile(0x00000001).Should().BeFalse();
        }

        [Fact]
        public void Contains_ReturnsTrue_ForExistingItem()
        {
            uint serial = 0x40000001;
            _world.GetOrCreateItem(serial);

            _world.Contains(serial).Should().BeTrue();
        }

        [Fact]
        public void Contains_ReturnsTrue_ForExistingMobile()
        {
            uint serial = 0x00000001;
            _world.GetOrCreateMobile(serial);

            _world.Contains(serial).Should().BeTrue();
        }

        [Fact]
        public void Get_ReturnsEntity_ForExistingItem()
        {
            uint serial = 0x40000001;
            var item = _world.GetOrCreateItem(serial);

            var result = _world.Get(serial);

            result.Should().BeSameAs(item);
        }

        [Fact]
        public void Get_ReturnsEntity_ForExistingMobile()
        {
            uint serial = 0x00000001;
            var mob = _world.GetOrCreateMobile(serial);

            var result = _world.Get(serial);

            result.Should().BeSameAs(mob);
        }

        [Fact]
        public void Season_DefaultIsSummer()
        {
            _world.Season.Should().Be(Season.Summer);
        }

        [Fact]
        public void OldSeason_DefaultIsSummer()
        {
            _world.OldSeason.Should().Be(Season.Summer);
        }

        [Fact]
        public void ClientViewRange_DefaultIsMaxViewRange()
        {
            _world.ClientViewRange.Should().Be(Constants.MAX_VIEW_RANGE);
        }

        [Fact]
        public void ClientViewRange_CanBeSet()
        {
            _world.ClientViewRange = 10;

            _world.ClientViewRange.Should().Be(10);
        }

        [Fact]
        public void Context_IsNotNull()
        {
            var profile = TestHelpers.CreateTestProfile();
            var world = new World(TestHelpers.CreateTestContext(profile: profile));

            world.Context.Should().NotBeNull();
        }

        [Fact]
        public void MapIndex_DefaultIsNegativeOne()
        {
            _world.MapIndex.Should().Be(-1);
        }

        [Fact]
        public void SkillsRequested_DefaultFalse()
        {
            _world.SkillsRequested.Should().BeFalse();
        }

        [Fact]
        public void ServerName_DefaultIsUnderscore()
        {
            _world.ServerName.Should().Be("_");
        }

        // Note: Tests involving Destroy() (Get_ReturnsNull_ForDestroyedItem,
        // GetOrCreateItem_ReplacesDestroyedItem, RemoveItem_ReturnsTrue, etc.)
        // are omitted because Entity.Destroy() calls GameActions.SendCloseStatus
        // which requires initialized network/static state not available in unit tests.

        [Fact]
        public void Weather_IsNotNull()
        {
            _world.Weather.Should().NotBeNull();
        }

        [Fact]
        public void Party_IsNotNull()
        {
            _world.Party.Should().NotBeNull();
        }

        [Fact]
        public void Journal_IsNotNull()
        {
            _world.Journal.Should().NotBeNull();
        }

        [Fact]
        public void OPL_IsNotNull()
        {
            _world.OPL.Should().NotBeNull();
        }

        [Fact]
        public void MultipleItems_CanBeCreated()
        {
            _world.GetOrCreateItem(0x40000001);
            _world.GetOrCreateItem(0x40000002);
            _world.GetOrCreateItem(0x40000003);

            _world.Items.Count.Should().Be(3);
        }

        [Fact]
        public void MultipleMobiles_CanBeCreated()
        {
            _world.GetOrCreateMobile(0x00000001);
            _world.GetOrCreateMobile(0x00000002);
            _world.GetOrCreateMobile(0x00000003);

            _world.Mobiles.Count.Should().Be(3);
        }
    }
}
