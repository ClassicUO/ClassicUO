using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.UnitTests.Utility
{
    public class TextFileParserTest
    {
        [Fact]
        public void Parse_Long_Text()
        {
            string s = " hello{a # quoted\tstring} a non quoted string    #ignoredstring  ";
            char[] delimiters = new char[] { ' ', '\t' };
            char[] comments = new char[] { '#' };
            char[] quotes = new char[] { '{', '}' };
            bool trim = true;


            ClassicUO.Utility.TextFileParser parser = new TextFileParser(s, delimiters, comments, quotes);

            List<string> tokens = parser.ReadTokens(trim);

            Assert.NotEmpty(tokens);
            Assert.Equal(6, tokens.Count);
            Assert.Equal("hello", tokens[0]);
            Assert.Equal("a # quoted\tstring", tokens[1]);
            Assert.Equal("a", tokens[2]);
            Assert.Equal("non", tokens[3]);
            Assert.Equal("quoted", tokens[4]);
            Assert.Equal("string", tokens[5]);
        }

        [Fact]
        public void Parse_Long_Text_SameQuotes()
        {
            string s = "1 2 3 4 @5 foo@6 bar@7@baz";
            char[] delimiters = new char[] { ' ', '\t' };
            char[] comments = new char[] { '#' };
            char[] quotes = new char[] { '@', '@' };
            bool trim = true;


            ClassicUO.Utility.TextFileParser parser = new TextFileParser(s, delimiters, comments, quotes);

            List<string> tokens = parser.ReadTokens(trim);

            Assert.NotEmpty(tokens);
            Assert.Equal(8, tokens.Count);
            Assert.Equal("1", tokens[0]);
            Assert.Equal("2", tokens[1]);
            Assert.Equal("3", tokens[2]);
            Assert.Equal("4", tokens[3]);
            Assert.Equal("5 foo", tokens[4]);
            Assert.Equal("6 bar", tokens[5]);
            Assert.Equal("7", tokens[6]);
            Assert.Equal("baz", tokens[7]);
        }

        [Fact]
        public void Parse_MultiLine_Text()
        {
            string s = "#comment must be skipped\n\n1 2 3 4\n 5 6 7 8\n@5 foo@6 bar@7@baz";
            char[] delimiters = new char[] { ' ', '\t' };
            char[] comments = new char[] { '#' };
            char[] quotes = new char[] { '@', '@' };
            bool trim = true;


            var parser = new TextFileParser(s, delimiters, comments, quotes);

            var tokens = parser.ReadTokens(trim);
            Assert.Empty(tokens);

            tokens = parser.ReadTokens(trim);
            Assert.Empty(tokens);

            tokens = parser.ReadTokens(trim);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(tokens[0..], ["1", "2", "3", "4"]);

            tokens = parser.ReadTokens(trim);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(tokens[0..], ["5", "6", "7", "8"]);

            tokens = parser.ReadTokens(trim);
            Assert.Equal(4, tokens.Count);
            Assert.Equal(tokens[0..], ["5 foo", "6 bar", "7", "baz"]);

            Assert.True(parser.IsEOF());
        }
    }
}
