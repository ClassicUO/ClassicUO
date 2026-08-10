using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class AddSpaceBeforeCapital
    {
        [Theory]
        [InlineData("HelloWorld", "Hello World")]
        [InlineData("HTMLParser", "HTML Parser")]
        [InlineData("A", "A")]
        [InlineData("hello", "hello")]
        public void AddSpaceBeforeCapital_Should_Return_Expected_Result(string input, string expected)
        {
            var result = ClassicUO.Utility.StringHelper.AddSpaceBeforeCapital(input);

            result
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddSpaceBeforeCapital_No_Input_Should_Return_EmptyString(string input)
        {
            var result = ClassicUO.Utility.StringHelper.AddSpaceBeforeCapital(input);

            result
                .Should()
                .BeEmpty();
        }
    }
}
