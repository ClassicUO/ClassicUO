using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class MobileLifecycleTests
    {
        private readonly World _world;

        public MobileLifecycleTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void CreateMobile_AndAddToWorld_IsAccessibleViaMobiles()
        {
            var mobile = Mobile.Create(_world, 0x0001);
            _world.Mobiles.Add(mobile);

            _world.Mobiles.Get(0x0001).Should().NotBeNull();
            _world.Mobiles.Get(0x0001).Should().BeSameAs(mobile);
            _world.Contains(0x0001).Should().BeTrue();
        }

        [Fact]
        public void SetMobileProperties_AreAccessibleAfterWorldRegistration()
        {
            var mobile = _world.GetOrCreateMobile(0x0002);
            mobile.Name = "Test Warrior";
            mobile.Hits = 75;
            mobile.HitsMax = 100;
            mobile.X = 1000;
            mobile.Y = 2000;
            mobile.Z = 5;

            var retrieved = _world.Mobiles.Get(0x0002);
            retrieved.Name.Should().Be("Test Warrior");
            retrieved.Hits.Should().Be(75);
            retrieved.HitsMax.Should().Be(100);
            retrieved.X.Should().Be(1000);
            retrieved.Y.Should().Be(2000);
            retrieved.Z.Should().Be(5);
        }

        [Fact]
        public void AddEquipmentItem_FindItemByLayer_ReturnsCorrectItem()
        {
            var mobile = _world.GetOrCreateMobile(0x0003);

            var weapon = Item.Create(_world, 0x40000001);
            weapon.Layer = Layer.OneHanded;
            weapon.Container = mobile.Serial;
            weapon.Graphic = 0x0F5E; // a sword graphic

            // Add the item as a child of the mobile using linked list
            mobile.PushToBack(weapon);

            var found = mobile.FindItemByLayer(Layer.OneHanded);
            found.Should().NotBeNull();
            found.Should().BeSameAs(weapon);
            found.Graphic.Should().Be(0x0F5E);
        }

        [Fact]
        public void FindItemByLayer_ReturnsNull_ForUnequippedLayer()
        {
            var mobile = _world.GetOrCreateMobile(0x0004);

            var weapon = Item.Create(_world, 0x40000002);
            weapon.Layer = Layer.OneHanded;
            mobile.PushToBack(weapon);

            mobile.FindItemByLayer(Layer.TwoHanded).Should().BeNull();
        }

        [Fact]
        public void MultipleEquipmentItems_EachFoundByCorrectLayer()
        {
            var mobile = _world.GetOrCreateMobile(0x0005);

            var helmet = Item.Create(_world, 0x40000010);
            helmet.Layer = Layer.Helmet;
            helmet.Graphic = 0x1408;
            mobile.PushToBack(helmet);

            var shield = Item.Create(_world, 0x40000011);
            shield.Layer = Layer.TwoHanded;
            shield.Graphic = 0x1B76;
            mobile.PushToBack(shield);

            var ring = Item.Create(_world, 0x40000012);
            ring.Layer = Layer.Ring;
            ring.Graphic = 0x108A;
            mobile.PushToBack(ring);

            mobile.FindItemByLayer(Layer.Helmet).Should().BeSameAs(helmet);
            mobile.FindItemByLayer(Layer.TwoHanded).Should().BeSameAs(shield);
            mobile.FindItemByLayer(Layer.Ring).Should().BeSameAs(ring);
        }

        [Fact]
        public void CreateTwoMobiles_BothExistInWorld()
        {
            var mob1 = _world.GetOrCreateMobile(0x0010);
            mob1.Name = "Fighter";

            var mob2 = _world.GetOrCreateMobile(0x0011);
            mob2.Name = "Mage";

            _world.Mobiles.Count.Should().BeGreaterThanOrEqualTo(2);
            _world.Contains(0x0010).Should().BeTrue();
            _world.Contains(0x0011).Should().BeTrue();

            _world.Mobiles.Get(0x0010).Name.Should().Be("Fighter");
            _world.Mobiles.Get(0x0011).Name.Should().Be("Mage");
        }

        [Fact]
        public void RemoveMobile_ReturnsFalse_ForNonExistentSerial()
        {
            _world.RemoveMobile(0x9999).Should().BeFalse();
        }

        [Fact]
        public void GetReturnsNull_ForNonExistentMobile()
        {
            _world.Get(0x0099).Should().BeNull();
        }

        [Fact]
        public void MobileWithProperties_GetViaWorldGet_ReturnsSameObject()
        {
            var mobile = _world.GetOrCreateMobile(0x0020);
            mobile.Name = "Paladin";
            mobile.NotorietyFlag = NotorietyFlag.Ally;
            mobile.Hits = 100;
            mobile.HitsMax = 100;

            var retrieved = _world.Get(0x0020) as Mobile;
            retrieved.Should().NotBeNull();
            retrieved.Should().BeSameAs(mobile);
            retrieved.Name.Should().Be("Paladin");
            retrieved.NotorietyFlag.Should().Be(NotorietyFlag.Ally);
        }

        [Fact]
        public void MobileWithEquipment_PropertiesFlowThrough()
        {
            // Create mobile with stats
            var mobile = _world.GetOrCreateMobile(0x0030);
            mobile.Name = "Equipped Warrior";
            mobile.Hits = 50;
            mobile.HitsMax = 100;
            mobile.Direction = Direction.North;

            // Add equipment
            var armor = Item.Create(_world, 0x40000020);
            armor.Layer = Layer.Torso;
            armor.Graphic = 0x1415;
            armor.Hue = 0x0035;
            mobile.PushToBack(armor);

            // Verify the whole chain works: world -> mobile -> equipment
            var worldMobile = _world.Mobiles.Get(0x0030);
            worldMobile.Name.Should().Be("Equipped Warrior");

            var foundArmor = worldMobile.FindItemByLayer(Layer.Torso);
            foundArmor.Should().NotBeNull();
            foundArmor.Graphic.Should().Be(0x1415);
            foundArmor.Hue.Should().Be(0x0035);
        }
    }
}
