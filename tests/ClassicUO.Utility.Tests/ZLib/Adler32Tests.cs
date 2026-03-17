using System.Text;
using FluentAssertions;
using Xunit;
using ZLibNative;

namespace ClassicUO.Utility.Tests.ZLib
{
    public class Adler32Tests
    {
        [Fact]
        public void GetValue_EmptyInput_ReturnsInitialValue()
        {
            var adler = new Adler32();

            uint result = adler.GetValue();

            // Initial Adler-32 value is 1 (a=1, b=0 => (0 << 16) | 1 = 1)
            result.Should().Be(1);
        }

        [Fact]
        public void Update_SingleByte_ProducesCorrectChecksum()
        {
            var adler = new Adler32();

            adler.Update(0x01);

            uint result = adler.GetValue();
            // a = (1 + 1) % 65521 = 2
            // b = (0 + 2) % 65521 = 2
            // result = (2 << 16) | 2 = 0x00020002
            result.Should().Be(0x00020002);
        }

        [Fact]
        public void Update_ByteArray_ProducesCorrectChecksum()
        {
            var adler = new Adler32();
            byte[] data = Encoding.ASCII.GetBytes("Wikipedia");

            adler.Update(data);

            uint result = adler.GetValue();
            // Known Adler-32 of "Wikipedia" is 0x11E60398
            result.Should().Be(0x11E60398);
        }

        [Fact]
        public void Update_ByteArrayWithOffsetAndLength_ProducesCorrectChecksum()
        {
            var adler = new Adler32();
            byte[] data = Encoding.ASCII.GetBytes("xxWikipediayy");

            // Skip the "xx" prefix and "yy" suffix
            adler.Update(data, 2, 9);

            uint result = adler.GetValue();
            result.Should().Be(0x11E60398);
        }

        [Fact]
        public void Reset_ClearsState()
        {
            var adler = new Adler32();
            adler.Update(Encoding.ASCII.GetBytes("some data"));

            adler.Reset();

            uint result = adler.GetValue();
            result.Should().Be(1);
        }

        [Fact]
        public void Update_IncrementallyMatchesBulk()
        {
            var adlerBulk = new Adler32();
            var adlerIncremental = new Adler32();

            byte[] data = Encoding.ASCII.GetBytes("Hello, World!");

            adlerBulk.Update(data);

            foreach (byte b in data)
            {
                adlerIncremental.Update(b);
            }

            adlerIncremental.GetValue().Should().Be(adlerBulk.GetValue());
        }

        [Fact]
        public void Update_AllZeroBytes_ProducesCorrectChecksum()
        {
            var adler = new Adler32();
            byte[] data = new byte[10]; // all zeros

            adler.Update(data);

            uint result = adler.GetValue();
            // a = 1 + 0*10 = 1
            // b = 0 + 1*10 = 10
            // result = (10 << 16) | 1 = 0x000A0001
            result.Should().Be(0x000A0001);
        }

        [Fact]
        public void Update_SingleByteOfValue255()
        {
            var adler = new Adler32();

            adler.Update(0xFF);

            uint result = adler.GetValue();
            // a = (1 + 255) % 65521 = 256
            // b = (0 + 256) % 65521 = 256
            // result = (256 << 16) | 256 = 0x01000100
            result.Should().Be(0x01000100);
        }

        [Fact]
        public void Reset_AfterUpdate_AllowsReuse()
        {
            var adler = new Adler32();
            byte[] data = Encoding.ASCII.GetBytes("Wikipedia");

            adler.Update(data);
            adler.Reset();

            adler.Update(data);
            uint result = adler.GetValue();

            result.Should().Be(0x11E60398);
        }
    }
}
