using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.StringHelper
{
    public class CapitalizeAllWords
    {
        [Fact]
        public void CapitalizeAllWords_All_Words_Should_Be_Capitalized()
        {
            const string UNCAPITALIZED_WORDS = "hello fans of ultima online";
            const string EXPECTED_RESULT = "Hello Fans Of Ultima Online";

            var result = ClassicUO.Utility.StringHelper.CapitalizeAllWords(UNCAPITALIZED_WORDS);

            result
                .Should()
                .BeEquivalentTo(EXPECTED_RESULT);
        }

        [Fact]
        public void CapitalizeAllWords_If_Length_Is_1_Should_Be_Capitalized()
        {
            const string UNCAPITALIZED_WORDS = "h";
            const string EXPECTED_RESULT = "H";

            var result = ClassicUO.Utility.StringHelper.CapitalizeAllWords(UNCAPITALIZED_WORDS);

            result
                .Should()
                .BeEquivalentTo(EXPECTED_RESULT);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CapitalizeAllWords_No_Input_Should_Return_EmptyString(string input)
        {
            var result = ClassicUO.Utility.StringHelper.CapitalizeAllWords(input);

            result
                .Should()
                .BeEquivalentTo(string.Empty);
        }
    }
}