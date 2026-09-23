using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class RemoveUpperLowerChars
    {
        [Fact]
        public void RemoveUpperLowerChars_RemoveLower_True_Should_Keep_Uppercase_And_Spaces()
        {
            var result = ClassicUO.Utility.StringHelper.RemoveUpperLowerChars("Hello World", removelower: true);

            result
                .Should()
                .Be("H W");
        }

        [Fact]
        public void RemoveUpperLowerChars_RemoveLower_False_Should_Keep_Lowercase_And_Spaces()
        {
            var result = ClassicUO.Utility.StringHelper.RemoveUpperLowerChars("Hello World", removelower: false);

            result
                .Should()
                .Be("ello orld");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RemoveUpperLowerChars_No_Input_Should_Return_EmptyString(string input)
        {
            var result = ClassicUO.Utility.StringHelper.RemoveUpperLowerChars(input);

            result
                .Should()
                .BeEmpty();
        }
    }
}
