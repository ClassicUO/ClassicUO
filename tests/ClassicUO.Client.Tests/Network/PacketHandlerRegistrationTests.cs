using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Network
{
    public class PacketHandlerRegistrationTests
    {
        [Fact]
        public void Handler_Singleton_Exists()
        {
            var handler = PacketHandlers.Handler;

            handler.Should().NotBeNull();
        }

        [Fact]
        public void Handler_Singleton_ReturnsSameInstance()
        {
            var handler1 = PacketHandlers.Handler;
            var handler2 = PacketHandlers.Handler;

            handler1.Should().BeSameAs(handler2);
        }

        [Fact]
        public void Add_HandlerForPacketId_DoesNotThrow()
        {
            var handler = PacketHandlers.Handler;

            // Register a no-op handler for an arbitrary packet ID
            var act = () => handler.Add(0xFE, (world, ref p) => { });

            act.Should().NotThrow();
        }

        [Fact]
        public void Add_AllPacketIds_DoesNotThrow()
        {
            var handler = PacketHandlers.Handler;

            for (int i = 0; i < 256; i++)
            {
                byte id = (byte)i;
                var act = () => handler.Add(id, (world, ref p) => { });
                act.Should().NotThrow($"adding handler for packet 0x{id:X2} should not throw");
            }

            // Re-register the real handlers since we just overwrote them on the static singleton
            PacketHandlers.RegisterHandlers();
        }
    }
}
