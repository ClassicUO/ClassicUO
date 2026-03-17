using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class ItemContainerTests
    {
        private readonly World _world;

        public ItemContainerTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        [Fact]
        public void ItemOnGround_DefaultContainer_IsOnGround()
        {
            var item = _world.GetOrCreateItem(0x40000001);

            // Default container is 0xFFFFFFFF which is invalid => OnGround
            item.Container.Should().Be(0xFFFFFFFF);
            item.OnGround.Should().BeTrue();
        }

        [Fact]
        public void ItemInContainer_OnGround_IsFalse()
        {
            var container = _world.GetOrCreateItem(0x40000010);
            container.Graphic = 0x0E75; // backpack graphic

            var child = _world.GetOrCreateItem(0x40000011);
            child.Container = container.Serial;

            child.OnGround.Should().BeFalse();
        }

        [Fact]
        public void ItemOnMobile_OnGround_IsFalse()
        {
            var mobile = _world.GetOrCreateMobile(0x0001);

            var item = _world.GetOrCreateItem(0x40000020);
            item.Container = mobile.Serial;
            item.Layer = Layer.OneHanded;

            item.OnGround.Should().BeFalse();
        }

        [Fact]
        public void RootContainer_SingleLevel_ReturnsContainerSerial()
        {
            // Item inside a container that is on the ground
            var container = _world.GetOrCreateItem(0x40000030);
            // container is on ground (default Container = 0xFFFFFFFF)

            var child = _world.GetOrCreateItem(0x40000031);
            child.Container = container.Serial;

            // RootContainer traverses up. Container's Container is invalid,
            // so it returns the container's serial itself.
            child.RootContainer.Should().Be(container.Serial);
        }

        [Fact]
        public void RootContainer_NestedItems_TraversesToRoot()
        {
            // Build a 3-level hierarchy: mobile -> backpack -> pouch -> potion
            var mobile = _world.GetOrCreateMobile(0x0002);

            var backpack = _world.GetOrCreateItem(0x40000040);
            backpack.Container = mobile.Serial;
            backpack.Layer = Layer.Backpack;

            var pouch = _world.GetOrCreateItem(0x40000041);
            pouch.Container = backpack.Serial;

            var potion = _world.GetOrCreateItem(0x40000042);
            potion.Container = pouch.Serial;

            // RootContainer should traverse: potion -> pouch -> backpack -> mobile
            // Since backpack.Container is a mobile serial, it returns mobile.Serial
            potion.RootContainer.Should().Be(mobile.Serial);
        }

        [Fact]
        public void RootContainer_ItemOnGround_ReturnsSelf()
        {
            var item = _world.GetOrCreateItem(0x40000050);
            // Default container 0xFFFFFFFF is not valid mobile or item serial
            // RootContainer returns item's own serial when container is not a mobile
            item.RootContainer.Should().Be(item.Serial);
        }

        [Fact]
        public void ItemWithLayer_SimulatesEquipment()
        {
            var mobile = _world.GetOrCreateMobile(0x0003);

            var sword = _world.GetOrCreateItem(0x40000060);
            sword.Container = mobile.Serial;
            sword.Layer = Layer.OneHanded;
            sword.Graphic = 0x0F5E;

            // Add to mobile's linked list so FindItemByLayer works
            mobile.PushToBack(sword);

            sword.Layer.Should().Be(Layer.OneHanded);
            sword.OnGround.Should().BeFalse();
            mobile.FindItemByLayer(Layer.OneHanded).Should().BeSameAs(sword);
        }

        [Fact]
        public void MultipleItemsInContainer_AllReferenceParent()
        {
            var bag = _world.GetOrCreateItem(0x40000070);

            var item1 = _world.GetOrCreateItem(0x40000071);
            item1.Container = bag.Serial;
            item1.Graphic = 0x0EEA; // gold

            var item2 = _world.GetOrCreateItem(0x40000072);
            item2.Container = bag.Serial;
            item2.Graphic = 0x0F0E; // a reagent

            var item3 = _world.GetOrCreateItem(0x40000073);
            item3.Container = bag.Serial;
            item3.Graphic = 0x0F09; // another reagent

            item1.OnGround.Should().BeFalse();
            item2.OnGround.Should().BeFalse();
            item3.OnGround.Should().BeFalse();

            item1.Container.Should().Be(bag.Serial);
            item2.Container.Should().Be(bag.Serial);
            item3.Container.Should().Be(bag.Serial);
        }

        [Fact]
        public void ContainerWithZeroSerial_IsOnGround()
        {
            var item = Item.Create(_world, 0x40000080);
            item.Container = 0;

            item.OnGround.Should().BeTrue();
        }

        [Fact]
        public void RootContainer_ChainBroken_ReturnsZero()
        {
            // If a container in the chain doesn't exist in World.Items,
            // RootContainer returns 0
            var child = _world.GetOrCreateItem(0x40000090);
            // Set container to an item serial that doesn't exist in world
            child.Container = 0x40000999;

            child.RootContainer.Should().Be(0u);
        }
    }
}
