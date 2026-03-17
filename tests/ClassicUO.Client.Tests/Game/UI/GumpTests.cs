using ClassicUO.Client.Tests;
using ClassicUO.Game;
using ClassicUO.Game.UI.Gumps;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.UI
{
    public class GumpTests
    {
        private readonly World _world;

        public GumpTests()
        {
            _world = TestHelpers.CreateTestWorld();
        }

        private Gump CreateGump(uint local = 0, uint server = 0)
        {
            return new Gump(_world, local, server);
        }

        [Fact]
        public void Constructor_SetsWorldReference()
        {
            var gump = CreateGump();

            gump.World.Should().BeSameAs(_world);
        }

        [Fact]
        public void Constructor_SetsLocalSerial()
        {
            var gump = CreateGump(local: 0x1234);

            gump.LocalSerial.Should().Be(0x1234u);
        }

        [Fact]
        public void Constructor_SetsServerSerial()
        {
            var gump = CreateGump(server: 0xABCD);

            gump.ServerSerial.Should().Be(0xABCDu);
        }

        [Fact]
        public void Constructor_SetsAcceptMouseInputFalse()
        {
            var gump = CreateGump();

            gump.AcceptMouseInput.Should().BeFalse();
        }

        [Fact]
        public void Constructor_SetsAcceptKeyboardInputFalse()
        {
            var gump = CreateGump();

            gump.AcceptKeyboardInput.Should().BeFalse();
        }

        [Fact]
        public void IsDisposed_DefaultFalse()
        {
            var gump = CreateGump();

            gump.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public void CanMove_DefaultFalse()
        {
            var gump = CreateGump();

            gump.CanMove.Should().BeFalse();
        }

        [Fact]
        public void CanMove_CanBeSetToTrue()
        {
            var gump = CreateGump();
            gump.CanMove = true;

            gump.CanMove.Should().BeTrue();
        }

        [Fact]
        public void CanCloseWithRightClick_DefaultTrue()
        {
            var gump = CreateGump();

            gump.CanCloseWithRightClick.Should().BeTrue();
        }

        [Fact]
        public void CanCloseWithRightClick_CanBeSetToFalse()
        {
            var gump = CreateGump();
            gump.CanCloseWithRightClick = false;

            gump.CanCloseWithRightClick.Should().BeFalse();
        }

        [Fact]
        public void RequestUpdateContents_SetsInvalidateContents()
        {
            var gump = CreateGump();

            gump.RequestUpdateContents();

            gump.InvalidateContents.Should().BeTrue();
        }

        [Fact]
        public void InvalidateContents_DefaultFalse()
        {
            var gump = CreateGump();

            gump.InvalidateContents.Should().BeFalse();
        }

        [Fact]
        public void GumpType_DefaultIsNone()
        {
            var gump = CreateGump();

            gump.GumpType.Should().Be(GumpType.None);
        }

        [Fact]
        public void CanBeSaved_FalseWhenGumpTypeNone()
        {
            var gump = CreateGump();

            gump.CanBeSaved.Should().BeFalse();
        }

        [Fact]
        public void MasterGumpSerial_DefaultZero()
        {
            var gump = CreateGump();

            gump.MasterGumpSerial.Should().Be(0u);
        }

        [Fact]
        public void MasterGumpSerial_CanBeSet()
        {
            var gump = CreateGump();
            gump.MasterGumpSerial = 0x5678;

            gump.MasterGumpSerial.Should().Be(0x5678u);
        }

        [Fact]
        public void Dispose_SetsIsDisposed()
        {
            var gump = CreateGump();

            gump.Dispose();

            gump.IsDisposed.Should().BeTrue();
        }

        [Fact]
        public void ChangePage_SetsActivePage()
        {
            var gump = CreateGump();

            gump.ChangePage(3);

            gump.ActivePage.Should().Be(3);
        }

        [Fact]
        public void IsVisible_DefaultTrue()
        {
            var gump = CreateGump();

            gump.IsVisible.Should().BeTrue();
        }

        [Fact]
        public void Width_Height_DefaultZero()
        {
            var gump = CreateGump();

            gump.Width.Should().Be(0);
            gump.Height.Should().Be(0);
        }

        [Fact]
        public void LocalSerial_CanBeModifiedAfterConstruction()
        {
            var gump = CreateGump(local: 1);
            gump.LocalSerial = 999;

            gump.LocalSerial.Should().Be(999u);
        }

        [Fact]
        public void ServerSerial_CanBeModifiedAfterConstruction()
        {
            var gump = CreateGump(server: 1);
            gump.ServerSerial = 888;

            gump.ServerSerial.Should().Be(888u);
        }
    }
}
