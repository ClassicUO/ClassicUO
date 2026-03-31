using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class IntToAbbreviatedString
    {
        [Theory]
        [InlineData(1000000, "1M+")]
        [InlineData(999999, "999K+")]
        [InlineData(1000, "1K+")]
        [InlineData(999, "999")]
        [InlineData(0, "0")]
        [InlineData(-1000, "-1K+")]
        [InlineData(-1000000, "-1M+")]
        public void IntToAbbreviatedString_Should_Return_Expected_Result(int input, string expected)
        {
            var result = ClassicUO.Utility.StringHelper.IntToAbbreviatedString(input);

            result
                .Should()
                .Be(expected);
        }
    }
}
