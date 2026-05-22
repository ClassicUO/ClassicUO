// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for World's items + containers subscribers.
    //
    // Fixture: build a fresh World which auto-wires its own subscribers via
    // SubscribeEvents(). We deliberately DO NOT call EventSink.ClearAll() in the
    // ctor because that would tear down the very subscribers under test. The
    // InGame guard inside several handlers is satisfied by SetInGameForTests(true)
    // — Player and Map can't be constructed in unit tests since their ctors hit
    // unmockable globals (Client.Game.UO.FileManager.*).
    //
    // ProfileManager.CurrentProfile is seeded with a default Profile so paths
    // that read it (UIManager static gump lookups, etc.) don't NRE.
    //
    // Collection serializes against other EventSink-touching test classes since
    // EventSink is a static, process-wide bus.
    //
    // Skipped handlers — one-line rationale each:
    //
    //   OnContainerItemAdded:  AddItemToContainer touches Client.Game.UO.GameCursor
    //                          on entry → NRE without a live GameController.
    //   OnItemMoveDenied:      same — first statement reads
    //                          Client.Game.UO.GameCursor.ItemHold.Serial.
    //   OnItemDragEnded:       directly mutates Client.Game.UO.GameCursor.ItemHold.
    //   OnItemDropAccepted:    same as OnItemDragEnded.
    //   OnItemDragAnimation:   calls SpawnEffect → EffectManager.CreateEffect →
    //                          GameEffect ctors require asset stack.
    //   OnDyeDataReceived:     dereferences Client.Game.UO.Gumps.GetGump(0x0906)
    //                          and constructs a UI gump → unobservable state.
    //   OnItemUpdated:         guards `if (Player == null) return;` and tests
    //                          cannot construct PlayerMobile.
    //   OnMobileUpdated:       same Player-null guard at top of handler.
    [Collection("EventSinkSerial")]
    public class WorldItemsContainersSubscriberTests : IDisposable
    {
        private readonly World _world;

        public WorldItemsContainersSubscriberTests()
        {
            ProfileManager.CurrentProfile = new Profile();
            _world = new World();
            _world.SetInGameForTests(true);
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        // ---------- OnItemEquipped ----------

        [Fact]
        public void ItemEquipped_Places_Item_At_Mobile_Layer()
        {
            uint mobileSerial = 0x0000_0501u;
            uint itemSerial = 0x4000_0501u;
            Mobile mobile = _world.GetOrCreateMobile(mobileSerial);
            _world.GetOrCreateItem(itemSerial);

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x13B9,
                Layer: Layer.OneHanded,
                ContainerSerial: mobileSerial,
                Hue: 0));

            Item equipped = mobile.FindItemByLayer(Layer.OneHanded);
            equipped.Should().NotBeNull();
            equipped.Serial.Should().Be(itemSerial);
            equipped.Graphic.Should().Be((ushort)0x13B9);
            equipped.Layer.Should().Be(Layer.OneHanded);
            equipped.Container.Should().Be(mobileSerial);
            equipped.Amount.Should().Be((ushort)1);
        }

        [Fact]
        public void ItemEquipped_Sets_Hue_Through_FixHue()
        {
            uint mobileSerial = 0x0000_0502u;
            uint itemSerial = 0x4000_0502u;
            _world.GetOrCreateMobile(mobileSerial);
            _world.GetOrCreateItem(itemSerial);

            // FixHue masks hue & 0x3FFF. 0x0042 stays 0x0042.
            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x1F03,
                Layer: Layer.Robe,
                ContainerSerial: mobileSerial,
                Hue: 0x0042));

            Item item = _world.Items.Get(itemSerial);
            item.Hue.Should().Be((ushort)0x0042);
        }

        [Fact]
        public void ItemEquipped_Multiple_Layers_Each_Findable()
        {
            uint mobileSerial = 0x0000_0503u;
            Mobile mobile = _world.GetOrCreateMobile(mobileSerial);

            uint swordSerial = 0x4000_0510u;
            uint robeSerial = 0x4000_0511u;
            _world.GetOrCreateItem(swordSerial);
            _world.GetOrCreateItem(robeSerial);

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: swordSerial,
                Graphic: 0x13B9,
                Layer: Layer.OneHanded,
                ContainerSerial: mobileSerial,
                Hue: 0));

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: robeSerial,
                Graphic: 0x1F03,
                Layer: Layer.Robe,
                ContainerSerial: mobileSerial,
                Hue: 0));

            mobile.FindItemByLayer(Layer.OneHanded).Should().NotBeNull()
                .And.Subject.As<Item>().Serial.Should().Be(swordSerial);
            mobile.FindItemByLayer(Layer.Robe).Should().NotBeNull()
                .And.Subject.As<Item>().Serial.Should().Be(robeSerial);
        }

        [Fact]
        public void ItemEquipped_GetOrCreate_Creates_Item_When_Missing()
        {
            uint mobileSerial = 0x0000_0504u;
            uint itemSerial = 0x4000_0520u;
            _world.GetOrCreateMobile(mobileSerial);

            // No pre-created item.
            _world.Items.Get(itemSerial).Should().BeNull();

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x1B72,
                Layer: Layer.Shirt,
                ContainerSerial: mobileSerial,
                Hue: 0));

            Item created = _world.Items.Get(itemSerial);
            created.Should().NotBeNull();
            created.Layer.Should().Be(Layer.Shirt);
            created.Container.Should().Be(mobileSerial);
        }

        [Fact]
        public void ItemEquipped_Ignored_When_Not_InGame()
        {
            _world.SetInGameForTests(false);

            uint mobileSerial = 0x0000_0505u;
            uint itemSerial = 0x4000_0530u;
            Mobile mobile = _world.GetOrCreateMobile(mobileSerial);

            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x13B9,
                Layer: Layer.OneHanded,
                ContainerSerial: mobileSerial,
                Hue: 0));

            // No item should have been created and the layer slot is empty.
            _world.Items.Get(itemSerial).Should().BeNull();
            mobile.FindItemByLayer(Layer.OneHanded).Should().BeNull();
        }

        [Fact]
        public void ItemEquipped_Relocates_Item_To_New_Mobile()
        {
            uint mobileA = 0x0000_0506u;
            uint mobileB = 0x0000_0507u;
            uint itemSerial = 0x4000_0540u;

            Mobile a = _world.GetOrCreateMobile(mobileA);
            Mobile b = _world.GetOrCreateMobile(mobileB);
            _world.GetOrCreateItem(itemSerial);

            // First equip on mobile A.
            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x13B9,
                Layer: Layer.OneHanded,
                ContainerSerial: mobileA,
                Hue: 0));

            a.FindItemByLayer(Layer.OneHanded).Should().NotBeNull();

            // Re-equip on mobile B at a different layer.
            EventSink.RaiseItemEquipped(new ItemEquippedArgs(
                Serial: itemSerial,
                Graphic: 0x13B9,
                Layer: Layer.TwoHanded,
                ContainerSerial: mobileB,
                Hue: 0));

            // Old mobile no longer holds the item; new mobile does.
            a.FindItemByLayer(Layer.OneHanded).Should().BeNull();

            Item moved = b.FindItemByLayer(Layer.TwoHanded);
            moved.Should().NotBeNull();
            moved.Serial.Should().Be(itemSerial);
            moved.Container.Should().Be(mobileB);
        }

        // ---------- OnContainerItemsReceived ----------
        //
        // NOTE: the non-corpse branch (and any Layer==0 child of a corpse) calls
        // World.RemoveItem → Item.Destroy → Entity.Destroy →
        // GameActions.SendCloseStatus which dereferences Client.Game.UO.Version
        // unconditionally — that's an NRE in unit tests with no live
        // GameController. EventSink.Invoke catches the exception so the rest of
        // the handler is silently aborted. Tests therefore exercise only the
        // corpse-with-Layer-!=-0 path which keeps items (no destruction) and
        // the no-op short-circuits.

        [Fact]
        public void ContainerItemsReceived_Corpse_Keeps_Equipped_Items()
        {
            // Corpse graphic (0x2006) triggers remove_unequipped=true: items with
            // Layer != 0 are preserved; the new linked-list head is rebuilt from
            // surviving items.
            uint corpseSerial = 0x4000_0610u;
            Item corpse = _world.GetOrCreateItem(corpseSerial);
            corpse.Graphic = 0x2006;

            uint shirtSerial = 0x4000_0611u;
            uint torsoSerial = 0x4000_0612u;
            Item shirt = _world.GetOrCreateItem(shirtSerial);
            shirt.Container = corpseSerial;
            shirt.Layer = Layer.Shirt;

            Item torso = _world.GetOrCreateItem(torsoSerial);
            torso.Container = corpseSerial;
            torso.Layer = Layer.Torso;

            corpse.PushToBack(shirt);
            corpse.PushToBack(torso);

            EventSink.RaiseContainerItemsReceived(new ContainerItemsReceivedArgs(
                ContainerSerial: corpseSerial,
                Count: 2));

            shirt.IsDestroyed.Should().BeFalse();
            torso.IsDestroyed.Should().BeFalse();
            corpse.FindItemByLayer(Layer.Shirt).Should().NotBeNull();
            corpse.FindItemByLayer(Layer.Torso).Should().NotBeNull();
            // The handler rebuilds container.Items from the first surviving entry.
            corpse.Items.Should().NotBeNull();
        }

        [Fact]
        public void ContainerItemsReceived_Corpse_With_No_Equipped_Items_Clears_List()
        {
            // A corpse with only Layer==0 children would hit the destroy path
            // and trip the SendCloseStatus NRE. Instead seed a corpse that's
            // already empty — the handler short-circuits via the IsEmpty
            // guard inside ClearContainerAndRemoveItems.
            uint corpseSerial = 0x4000_0615u;
            Item corpse = _world.GetOrCreateItem(corpseSerial);
            corpse.Graphic = 0x2006;

            corpse.Items.Should().BeNull();

            Action act = () => EventSink.RaiseContainerItemsReceived(
                new ContainerItemsReceivedArgs(
                    ContainerSerial: corpseSerial,
                    Count: 0));

            act.Should().NotThrow();
            corpse.Items.Should().BeNull();
        }

        [Fact]
        public void ContainerItemsReceived_Unknown_Container_Is_NoOp()
        {
            // No container in the world → handler bails before mutating anything.
            Action act = () => EventSink.RaiseContainerItemsReceived(
                new ContainerItemsReceivedArgs(
                    ContainerSerial: 0x4000_06FFu,
                    Count: 0));

            act.Should().NotThrow();
            _world.Items.Get(0x4000_06FFu).Should().BeNull();
        }

        [Fact]
        public void ContainerItemsReceived_Empty_Container_Is_NoOp()
        {
            uint containerSerial = 0x4000_0620u;
            Item container = _world.GetOrCreateItem(containerSerial);
            container.Graphic = 0x0E75;

            container.Items.Should().BeNull();

            Action act = () => EventSink.RaiseContainerItemsReceived(
                new ContainerItemsReceivedArgs(
                    ContainerSerial: containerSerial,
                    Count: 0));

            act.Should().NotThrow();
            container.Items.Should().BeNull();
        }

        [Fact]
        public void ContainerItemsReceived_Corpse_Items_Stay_In_World_Dictionary()
        {
            // Sibling assertion to the corpse-keeps-equipped path: surviving
            // (Layer != 0) items remain reachable through _world.Items, proving
            // the handler did not also evict them from the dictionary.
            uint corpseSerial = 0x4000_0620u;
            Item corpse = _world.GetOrCreateItem(corpseSerial);
            corpse.Graphic = 0x2006;

            uint shirtSerial = 0x4000_0621u;
            Item shirt = _world.GetOrCreateItem(shirtSerial);
            shirt.Container = corpseSerial;
            shirt.Layer = Layer.Shirt;
            corpse.PushToBack(shirt);

            EventSink.RaiseContainerItemsReceived(new ContainerItemsReceivedArgs(
                ContainerSerial: corpseSerial,
                Count: 1));

            _world.Items.Get(shirtSerial).Should().NotBeNull();
            _world.Items.Get(shirtSerial).Should().BeSameAs(shirt);
        }
    }
}
