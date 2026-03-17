using System.Linq;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class TextFileParserTests
    {
        [Fact]
        public void ReadTokens_WithDelimiters_SplitsTokensCorrectly()
        {
            var parser = new TextFileParser(
                "hello world foo",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "hello", "world", "foo" });
        }

        [Fact]
        public void ReadTokens_WithCommaDelimiter_SplitsCorrectly()
        {
            var parser = new TextFileParser(
                "alpha,beta,gamma",
                new[] { ',' },
                new char[0],
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "alpha", "beta", "gamma" });
        }

        [Fact]
        public void ReadTokens_CommentsAreSkipped()
        {
            var parser = new TextFileParser(
                "hello #this is a comment",
                new[] { ' ' },
                new[] { '#' },
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "hello" });
        }

        [Fact]
        public void ReadTokens_LineStartingWithComment_ReturnsEmpty()
        {
            var parser = new TextFileParser(
                "#this is all a comment",
                new[] { ' ' },
                new[] { '#' },
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEmpty();
        }

        [Fact]
        public void ReadTokens_QuotedStrings_PreservedAsSingleToken()
        {
            var parser = new TextFileParser(
                "hello \"world foo\" bar",
                new[] { ' ' },
                new char[0],
                new[] { '"', '"' });

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "hello", "world foo", "bar" });
        }

        [Fact]
        public void IsEOF_ReturnsFalse_WhenNotConsumed()
        {
            var parser = new TextFileParser(
                "hello world",
                new[] { ' ' },
                new char[0],
                new char[0]);

            parser.IsEOF().Should().BeFalse();
        }

        [Fact]
        public void IsEOF_ReturnsTrue_AfterAllTokensConsumed()
        {
            var parser = new TextFileParser(
                "hello",
                new[] { ' ' },
                new char[0],
                new char[0]);

            parser.ReadTokens();

            parser.IsEOF().Should().BeTrue();
        }

        [Fact]
        public void Restart_ResetsToBeginning()
        {
            var parser = new TextFileParser(
                "hello world",
                new[] { ' ' },
                new char[0],
                new char[0]);

            parser.ReadTokens();
            parser.IsEOF().Should().BeTrue();

            parser.Restart();

            parser.IsEOF().Should().BeFalse();
            var tokens = parser.ReadTokens();
            tokens.Should().BeEquivalentTo(new[] { "hello", "world" });
        }

        [Fact]
        public void GetTokens_ParsesInlineString()
        {
            var parser = new TextFileParser(
                "",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens = parser.GetTokens("one two three");

            tokens.Should().BeEquivalentTo(new[] { "one", "two", "three" });
        }

        [Fact]
        public void ReadTokens_EmptyString_ReturnsEmptyList()
        {
            var parser = new TextFileParser(
                "",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEmpty();
        }

        [Fact]
        public void ReadTokens_MultipleDelimitersBetweenTokens_SkipsAllDelimiters()
        {
            var parser = new TextFileParser(
                "hello   world",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "hello", "world" });
        }

        [Fact]
        public void ReadTokens_MultipleLines_ParsesLineByLine()
        {
            var parser = new TextFileParser(
                "line1 a b\nline2 c d",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens1 = parser.ReadTokens();
            tokens1.Should().BeEquivalentTo(new[] { "line1", "a", "b" });

            var tokens2 = parser.ReadTokens();
            tokens2.Should().BeEquivalentTo(new[] { "line2", "c", "d" });

            parser.IsEOF().Should().BeTrue();
        }

        [Fact]
        public void ReadTokens_MultipleDelimiterTypes_SplitsOnAll()
        {
            var parser = new TextFileParser(
                "a,b c;d",
                new[] { ',', ' ', ';' },
                new char[0],
                new char[0]);

            var tokens = parser.ReadTokens();

            tokens.Should().BeEquivalentTo(new[] { "a", "b", "c", "d" });
        }

        [Fact]
        public void ReadTokens_WithTrimDisabled_PreservesWhitespace()
        {
            var parser = new TextFileParser(
                "hello, world",
                new[] { ',' },
                new char[0],
                new char[0]);

            // trim=false, but ObtainUnquotedData does not include delimiters,
            // so only leading/trailing whitespace within tokens is affected
            var tokens = parser.ReadTokens(trim: false);

            tokens.Should().HaveCount(2);
            tokens[0].Should().Be("hello");
            tokens[1].Should().Be(" world");
        }

        [Fact]
        public void GetTokens_WithMultipleLines_ParsesAllLines()
        {
            var parser = new TextFileParser(
                "",
                new[] { ' ' },
                new char[0],
                new char[0]);

            var tokens = parser.GetTokens("first\nsecond\nthird");

            tokens.Should().BeEquivalentTo(new[] { "first", "second", "third" });
        }
    }
}
