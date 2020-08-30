using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class CapitalizeFirstCharacter
    {
        [Fact]
        public void CapitalizeFirstCharacter_Only_FirstCharacter_Should_Be_Capitalized()
        {
            const string UNCAPITALIZED_WORDS = "hello fans of ultima online";
            const string EXPECTED_RESULT = "Hello fans of ultima online";

            var result = ClassicUO.Utility.StringHelper.CapitalizeFirstCharacter(UNCAPITALIZED_WORDS);

            result
                .Should()
                .BeEquivalentTo(EXPECTED_RESULT);
        }

        [Fact]
        public void CapitalizeFirstCharacter_If_Length_Is_1_Should_Be_Capitalized()
        {
            const string UNCAPITALIZED_WORDS = "h";
            const string EXPECTED_RESULT = "H";

            var result = ClassicUO.Utility.StringHelper.CapitalizeFirstCharacter(UNCAPITALIZED_WORDS);

            result
                .Should()
                .BeEquivalentTo(EXPECTED_RESULT);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CapitalizeFirstCharacter_No_Input_Should_Return_EmptyString(string input)
        {
            var result = ClassicUO.Utility.StringHelper.CapitalizeFirstCharacter(input);

            result
                .Should()
                .BeEquivalentTo(string.Empty);
        }
    }
}