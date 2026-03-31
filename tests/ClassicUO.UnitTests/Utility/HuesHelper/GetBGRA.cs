using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.HuesHelper
{
    public class GetBGRA
    {
        [Theory]
        [InlineData(0xAABBCCDDu, 0xDD, 0xCC, 0xBB, 0xAA)]
        [InlineData(0x00000000u, 0x00, 0x00, 0x00, 0x00)]
        [InlineData(0xFFFFFFFFu, 0xFF, 0xFF, 0xFF, 0xFF)]
        public void GetBGRA_Should_Extract_Correct_Components(
            uint input,
            byte expectedB,
            byte expectedG,
            byte expectedR,
            byte expectedA)
        {
            var (b, g, r, a) = ClassicUO.Utility.HuesHelper.GetBGRA(input);

            b.Should().Be(expectedB);
            g.Should().Be(expectedG);
            r.Should().Be(expectedR);
            a.Should().Be(expectedA);
        }
    }
}
