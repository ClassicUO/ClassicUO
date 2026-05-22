// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for ObjectPropertiesListManager.
    //
    // OnMegaClilocReceived is gated on _world.InGame. PlayerMobile / Map ctors
    // read unmockable globals (Client.Game.UO.FileManager.*), so we satisfy the
    // gate via World.SetInGameForTests(true) — an internal seam that overrides
    // the InGame computation for unit tests.
    [Collection("EventSinkSerial")]
    public class ObjectPropertiesListManagerTests : System.IDisposable
    {
        private readonly World _world;
        private readonly ObjectPropertiesListManager _opl;

        public ObjectPropertiesListManagerTests()
        {
            _world = new World();
            _opl = _world.OPL;
            _world.SetInGameForTests(true);
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        [Fact]
        public void MegaClilocReceived_Caches_Name_Revision_And_Data()
        {
            uint serial = 0x4000_0050u; // item-range
            var args = new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 0x1234u,
                Name: "Sword of Testing",
                Properties: "Hardness 5\nWeight 10",
                NameCliloc: 1078872);

            EventSink.RaiseMegaClilocReceived(args);

            _opl.TryGetNameAndData(serial, out string name, out string data).Should().BeTrue();
            name.Should().Be("Sword of Testing");
            data.Should().Be("Hardness 5\nWeight 10");

            _opl.TryGetRevision(serial, out uint rev).Should().BeTrue();
            rev.Should().Be(0x1234u);

            _opl.GetNameCliloc(serial).Should().Be(1078872);
        }

        [Fact]
        public void MegaClilocReceived_IsRevisionEquals_Returns_True_For_Cached()
        {
            uint serial = 0x4000_0051u;
            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 0xABu,
                Name: "x",
                Properties: "y",
                NameCliloc: 0));

            _opl.IsRevisionEquals(serial, 0xABu).Should().BeTrue();
            _opl.IsRevisionEquals(serial, 0xCCu).Should().BeFalse();
        }

        [Fact]
        public void MegaClilocReceived_Second_Call_Overwrites_Cache_For_Same_Serial()
        {
            uint serial = 0x4000_0052u;

            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 1u,
                Name: "first",
                Properties: "props1",
                NameCliloc: 100));

            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 2u,
                Name: "second",
                Properties: "props2",
                NameCliloc: 200));

            _opl.TryGetNameAndData(serial, out string name, out string data).Should().BeTrue();
            name.Should().Be("second");
            data.Should().Be("props2");
            _opl.TryGetRevision(serial, out uint rev).Should().BeTrue();
            rev.Should().Be(2u);
            _opl.GetNameCliloc(serial).Should().Be(200);
        }

        [Fact]
        public void MegaClilocReceived_Ignored_When_Not_InGame()
        {
            _world.SetInGameForTests(false);
            _world.InGame.Should().BeFalse();

            uint serial = 0x4000_0053u;
            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 1u,
                Name: "ignored",
                Properties: "ignored",
                NameCliloc: 1));

            _opl.TryGetNameAndData(serial, out _, out _).Should().BeFalse();
            _opl.TryGetRevision(serial, out _).Should().BeFalse();
        }

        [Fact]
        public void OplInfoReceived_For_Unknown_Serial_IsRevisionEquals_Still_False()
        {
            // OnOplInfoReceived only requests via PacketHandlers.AddMegaClilocRequest
            // (network side-effect, not OPL state). The OPL cache remains empty.
            uint serial = 0x4000_0099u;
            EventSink.RaiseOplInfoReceived(new OplInfoReceivedArgs(serial, 0x55u));

            _opl.IsRevisionEquals(serial, 0x55u).Should().BeFalse();
            _opl.TryGetRevision(serial, out _).Should().BeFalse();
        }

        [Fact]
        public void MegaClilocReceived_Sets_Entity_Name_For_Item_With_Nonempty_Name()
        {
            uint serial = 0x4000_0060u;
            Item item = _world.GetOrCreateItem(serial);
            item.Name.Should().BeNullOrEmpty();

            EventSink.RaiseMegaClilocReceived(new MegaClilocReceivedArgs(
                Serial: serial,
                Revision: 1u,
                Name: "Magic Sword",
                Properties: "props",
                NameCliloc: 0));

            item.Name.Should().Be("Magic Sword");
        }
    }
}
