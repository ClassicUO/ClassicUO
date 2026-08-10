using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class GetPluralAdjustedString
    {
        [Theory]
        [InlineData("hello", true, "hello")]
        [InlineData("hello", false, "hello")]
        public void GetPluralAdjustedString_No_Percent_Should_Return_Input(string input, bool plural, string expected)
        {
            var result = ClassicUO.Utility.StringHelper.GetPluralAdjustedString(input, plural);

            result
                .Should()
                .Be(expected);
        }

        [Fact]
        public void GetPluralAdjustedString_With_Plural_Slash_Should_Return_Plural_Form()
        {
            var result = ClassicUO.Utility.StringHelper.GetPluralAdjustedString("boot%s/", plural: true);

            result
                .Should()
                .Be("boots");
        }

        [Fact]
        public void GetPluralAdjustedString_With_Singular_Slash_Should_Return_Singular_Form()
        {
            var result = ClassicUO.Utility.StringHelper.GetPluralAdjustedString("boot%s/", plural: false);

            result
                .Should()
                .Be("boot");
        }

        [Fact]
        public void GetPluralAdjustedString_Single_Part_After_Split_Should_Return_Input()
        {
            var result = ClassicUO.Utility.StringHelper.GetPluralAdjustedString("%onlypart", plural: true);

            result
                .Should()
                .Be("%onlypart");
        }
    }
}
