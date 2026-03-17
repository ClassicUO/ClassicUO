using ClassicUO.Network;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Network
{
    public class PacketsTableTests
    {
        [Fact]
        public void GetPacketLength_LoginSeed0x00_ReturnsFixedLength_OlderVersion()
        {
            var table = new PacketsTable(ClientVersion.CV_7000);

            // Before CV_70180, packet 0x00 should be 0x68
            table.GetPacketLength(0x00).Should().Be(0x68);
        }

        [Fact]
        public void GetPacketLength_LoginSeed0x00_ReturnsFixedLength_NewerVersion()
        {
            var table = new PacketsTable(ClientVersion.CV_70180);

            // At CV_70180+, packet 0x00 should be 0x6A
            table.GetPacketLength(0x00).Should().Be(0x6A);
        }

        [Fact]
        public void GetPacketLength_Packet0x03_ReturnsVariableLength()
        {
            var table = new PacketsTable(ClientVersion.CV_500A);

            table.GetPacketLength(0x03).Should().Be(-1);
        }

        [Fact]
        public void GetPacketLength_FixedSizePacket0x02_Returns7()
        {
            var table = new PacketsTable(ClientVersion.CV_500A);

            table.GetPacketLength(0x02).Should().Be(0x07);
        }

        [Theory]
        [InlineData(ClientVersion.CV_OLD, 0x0B, 0x10A)]
        [InlineData(ClientVersion.CV_500A, 0x0B, 0x07)]
        public void GetPacketLength_Packet0x0B_DependsOnVersion(ClientVersion version, int packetId, short expected)
        {
            var table = new PacketsTable(version);

            table.GetPacketLength(packetId).Should().Be(expected);
        }

        [Theory]
        [InlineData(ClientVersion.CV_OLD, 0x16, 0x01)]
        [InlineData(ClientVersion.CV_500A, 0x16, -1)]
        public void GetPacketLength_Packet0x16_DependsOnVersion(ClientVersion version, int packetId, short expected)
        {
            var table = new PacketsTable(version);

            table.GetPacketLength(packetId).Should().Be(expected);
        }

        [Theory]
        [InlineData(ClientVersion.CV_6000, 0x08, 0x0E)]
        [InlineData(ClientVersion.CV_6017, 0x08, 0x0F)]
        public void GetPacketLength_Packet0x08_ChangesAtCV6017(ClientVersion version, int packetId, short expected)
        {
            var table = new PacketsTable(version);

            table.GetPacketLength(packetId).Should().Be(expected);
        }

        [Theory]
        [InlineData(ClientVersion.CV_6000, 0xB9, 0x03)]
        [InlineData(ClientVersion.CV_60142, 0xB9, 0x05)]
        public void GetPacketLength_Packet0xB9_ChangesAtCV60142(ClientVersion version, int packetId, short expected)
        {
            var table = new PacketsTable(version);

            table.GetPacketLength(packetId).Should().Be(expected);
        }

        [Fact]
        public void GetPacketLength_AllPacketIds_DoNotCrash()
        {
            var table = new PacketsTable(ClientVersion.CV_7090);

            for (int i = 0; i < 256; i++)
            {
                var act = () => table.GetPacketLength(i);
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void GetPacketLength_IdAbove0xFF_ReturnsNegativeOne()
        {
            var table = new PacketsTable(ClientVersion.CV_500A);

            table.GetPacketLength(0xFF).Should().Be(-1);
            table.GetPacketLength(0x100).Should().Be(-1);
        }

        [Fact]
        public void GetPacketLength_AllPacketIds_ReturnValidValues()
        {
            var table = new PacketsTable(ClientVersion.CV_7090);

            for (int i = 0; i < 255; i++)
            {
                short length = table.GetPacketLength(i);

                // Each packet should either be variable-length (-1) or have a positive length
                (length == -1 || length > 0).Should().BeTrue(
                    $"packet 0x{i:X2} should be variable (-1) or positive, but was {length}");
            }
        }

        [Fact]
        public void Constructor_MultipleVersions_DoNotThrow()
        {
            var versions = new[]
            {
                ClientVersion.CV_OLD,
                ClientVersion.CV_200,
                ClientVersion.CV_500A,
                ClientVersion.CV_5090,
                ClientVersion.CV_6013,
                ClientVersion.CV_6017,
                ClientVersion.CV_6060,
                ClientVersion.CV_60142,
                ClientVersion.CV_7000,
                ClientVersion.CV_7090,
                ClientVersion.CV_70180,
                ClientVersion.CV_706400,
                ClientVersion.CV_7010400,
            };

            foreach (var version in versions)
            {
                var act = () => new PacketsTable(version);
                act.Should().NotThrow($"version {version} should construct without error");
            }
        }
    }
}
