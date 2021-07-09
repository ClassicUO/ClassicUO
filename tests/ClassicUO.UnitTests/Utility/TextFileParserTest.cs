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
            string s = " \"a # quoted\tstring\" a non quoted string    #ignoredstring  ";
            char[] delimiters = new char[] { ' ', '\t' };
            char[] comments = new char[] { '#' };
            char[] quotes = new char[] { '"', '"' };
            bool trim = true;


            ClassicUO.Utility.TextFileParser parser = new TextFileParser(s, delimiters, comments, quotes);

            List<string> tokens = parser.ReadTokens(trim);

            Assert.NotEmpty(tokens);
            Assert.Equal(5, tokens.Count);
            Assert.Equal("a # quoted\tstring", tokens[0]);
            Assert.Equal("a", tokens[1]);
            Assert.Equal("non", tokens[2]);
            Assert.Equal("quoted", tokens[3]);
            Assert.Equal("string", tokens[4]);
        }
    }
}
