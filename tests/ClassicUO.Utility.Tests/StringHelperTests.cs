using System.Linq;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class StringHelperTests
    {
        // CapitalizeFirstCharacter tests

        [Fact]
        public void CapitalizeFirstCharacter_Hello()
        {
            StringHelper.CapitalizeFirstCharacter("hello").Should().Be("Hello");
        }

        [Fact]
        public void CapitalizeFirstCharacter_EmptyString()
        {
            StringHelper.CapitalizeFirstCharacter("").Should().Be("");
        }

        [Fact]
        public void CapitalizeFirstCharacter_Null()
        {
            StringHelper.CapitalizeFirstCharacter(null).Should().Be("");
        }

        [Fact]
        public void CapitalizeFirstCharacter_SingleChar()
        {
            StringHelper.CapitalizeFirstCharacter("a").Should().Be("A");
        }

        [Fact]
        public void CapitalizeFirstCharacter_AlreadyCapitalized()
        {
            StringHelper.CapitalizeFirstCharacter("Hello").Should().Be("Hello");
        }

        // CapitalizeAllWords tests

        [Fact]
        public void CapitalizeAllWords_HelloWorld()
        {
            StringHelper.CapitalizeAllWords("hello world").Should().Be("Hello World");
        }

        [Fact]
        public void CapitalizeAllWords_EmptyString()
        {
            StringHelper.CapitalizeAllWords("").Should().Be("");
        }

        [Fact]
        public void CapitalizeAllWords_Null()
        {
            StringHelper.CapitalizeAllWords(null).Should().Be("");
        }

        [Fact]
        public void CapitalizeAllWords_SingleWord()
        {
            StringHelper.CapitalizeAllWords("hello").Should().Be("Hello");
        }

        [Fact]
        public void CapitalizeAllWords_MultipleSpaces()
        {
            // Multiple spaces: the algorithm capitalizes after whitespace
            string result = StringHelper.CapitalizeAllWords("hello  world");
            result.Should().Be("Hello  World");
        }

        // CapitalizeWordsByLimitator tests

        [Fact]
        public void CapitalizeWordsByLimitator_DotDelimiter()
        {
            StringHelper.CapitalizeWordsByLimitator("hello.world").Should().Be("Hello.World");
        }

        [Fact]
        public void CapitalizeWordsByLimitator_CommaDelimiter()
        {
            StringHelper.CapitalizeWordsByLimitator("hello,world").Should().Be("Hello,World");
        }

        [Fact]
        public void CapitalizeWordsByLimitator_SemicolonDelimiter()
        {
            StringHelper.CapitalizeWordsByLimitator("hello;world").Should().Be("Hello;World");
        }

        [Fact]
        public void CapitalizeWordsByLimitator_ExclamationDelimiter()
        {
            StringHelper.CapitalizeWordsByLimitator("hello!world").Should().Be("Hello!World");
        }

        [Fact]
        public void CapitalizeWordsByLimitator_EmptyString()
        {
            StringHelper.CapitalizeWordsByLimitator("").Should().Be("");
        }

        [Fact]
        public void CapitalizeWordsByLimitator_Null()
        {
            StringHelper.CapitalizeWordsByLimitator(null).Should().Be("");
        }

        // IsSafeChar tests

        [Fact]
        public void IsSafeChar_Space_ReturnsTrue()
        {
            StringHelper.IsSafeChar(0x20).Should().BeTrue();
        }

        [Fact]
        public void IsSafeChar_ControlChar_ReturnsFalse()
        {
            StringHelper.IsSafeChar(0x19).Should().BeFalse();
        }

        [Fact]
        public void IsSafeChar_0xFFFE_ReturnsFalse()
        {
            StringHelper.IsSafeChar(0xFFFE).Should().BeFalse();
        }

        [Fact]
        public void IsSafeChar_PrintableAscii_ReturnsTrue()
        {
            StringHelper.IsSafeChar('A').Should().BeTrue();
            StringHelper.IsSafeChar('z').Should().BeTrue();
            StringHelper.IsSafeChar('0').Should().BeTrue();
        }

        [Fact]
        public void IsSafeChar_Null_ReturnsFalse()
        {
            StringHelper.IsSafeChar(0).Should().BeFalse();
        }

        [Fact]
        public void IsSafeChar_MaxSafe_ReturnsTrue()
        {
            // 0xFFFD should be the last safe char
            StringHelper.IsSafeChar(0xFFFD).Should().BeTrue();
        }

        // IntToAbbreviatedString tests

        [Theory]
        [InlineData(999, "999")]
        [InlineData(0, "0")]
        [InlineData(1, "1")]
        [InlineData(-999, "-999")]
        public void IntToAbbreviatedString_SmallNumbers(int num, string expected)
        {
            StringHelper.IntToAbbreviatedString(num).Should().Be(expected);
        }

        [Fact]
        public void IntToAbbreviatedString_Thousands()
        {
            StringHelper.IntToAbbreviatedString(1000).Should().Be("1K+");
        }

        [Fact]
        public void IntToAbbreviatedString_HundredsOfThousands()
        {
            StringHelper.IntToAbbreviatedString(999999).Should().Be("999K+");
        }

        [Fact]
        public void IntToAbbreviatedString_Millions()
        {
            StringHelper.IntToAbbreviatedString(1000000).Should().Be("1M+");
        }

        [Fact]
        public void IntToAbbreviatedString_NegativeThousands()
        {
            StringHelper.IntToAbbreviatedString(-1000).Should().Be("-1K+");
        }

        [Fact]
        public void IntToAbbreviatedString_NegativeMillions()
        {
            StringHelper.IntToAbbreviatedString(-1000000).Should().Be("-1M+");
        }

        // RemoveUpperLowerChars tests

        [Fact]
        public void RemoveUpperLowerChars_RemoveLower_KeepsUppercaseAndSpaces()
        {
            StringHelper.RemoveUpperLowerChars("Hello World", removelower: true)
                .Should().Be("H W");
        }

        [Fact]
        public void RemoveUpperLowerChars_RemoveUpper_KeepsLowercaseAndSpaces()
        {
            StringHelper.RemoveUpperLowerChars("Hello World", removelower: false)
                .Should().Be("ello orld");
        }

        [Fact]
        public void RemoveUpperLowerChars_EmptyString()
        {
            StringHelper.RemoveUpperLowerChars("").Should().Be("");
        }

        [Fact]
        public void RemoveUpperLowerChars_Null()
        {
            StringHelper.RemoveUpperLowerChars(null).Should().Be("");
        }

        // AddSpaceBeforeCapital tests

        [Fact]
        public void AddSpaceBeforeCapital_HelloWorld()
        {
            StringHelper.AddSpaceBeforeCapital("HelloWorld").Should().Be("Hello World");
        }

        [Fact]
        public void AddSpaceBeforeCapital_SingleWord()
        {
            StringHelper.AddSpaceBeforeCapital("Hello").Should().Be("Hello");
        }

        [Fact]
        public void AddSpaceBeforeCapital_AllLowercase()
        {
            StringHelper.AddSpaceBeforeCapital("helloworld").Should().Be("helloworld");
        }

        [Fact]
        public void AddSpaceBeforeCapital_EmptyString()
        {
            StringHelper.AddSpaceBeforeCapital("").Should().Be("");
        }

        [Fact]
        public void AddSpaceBeforeCapital_Null()
        {
            StringHelper.AddSpaceBeforeCapital((string)null).Should().Be("");
        }

        [Fact]
        public void AddSpaceBeforeCapital_MultipleCapitals()
        {
            StringHelper.AddSpaceBeforeCapital("MyHTTPServer")
                .Should().Be("My HTTP Server");
        }

        // GetPluralAdjustedString tests

        [Fact]
        public void GetPluralAdjustedString_PluralTrue_AppendsPlural()
        {
            // "item%s/%" => parts[0]="item", parts[1]="s/"
            // parts[1] contains "/", pluralparts = ["s", ""]
            // plural=true => append "s"
            StringHelper.GetPluralAdjustedString("item%s/%", plural: true)
                .Should().Be("items");
        }

        [Fact]
        public void GetPluralAdjustedString_PluralFalse_AppendsSingular()
        {
            // plural=false => append pluralparts[1] = ""
            StringHelper.GetPluralAdjustedString("item%s/%", plural: false)
                .Should().Be("item");
        }

        [Fact]
        public void GetPluralAdjustedString_NoPercentSign_ReturnsOriginal()
        {
            StringHelper.GetPluralAdjustedString("item", plural: true)
                .Should().Be("item");
        }

        [Fact]
        public void GetPluralAdjustedString_WithSuffix()
        {
            // "box%es/%  of stuff" => split by % => ["box", "es/", "  of stuff"]
            // parts[1] = "es/", contains "/", pluralparts = ["es", ""]
            // plural=true => append "es", then append parts[2] = "  of stuff"
            StringHelper.GetPluralAdjustedString("box%es/%  of stuff", plural: true)
                .Should().Be("boxes  of stuff");
        }

        // StringToCp1252Bytes and Cp1252ToString tests

        [Fact]
        public void StringToCp1252Bytes_BasicAscii()
        {
            byte[] bytes = StringHelper.StringToCp1252Bytes("ABC").ToArray();
            bytes.Should().Equal(0x41, 0x42, 0x43);
        }

        [Fact]
        public void Cp1252ToString_BasicAscii()
        {
            byte[] bytes = { 0x41, 0x42, 0x43 };
            StringHelper.Cp1252ToString(bytes).Should().Be("ABC");
        }

        [Fact]
        public void StringToCp1252Bytes_ThenCp1252ToString_RoundTripsAscii()
        {
            string original = "Hello, World!";
            byte[] bytes = StringHelper.StringToCp1252Bytes(original).ToArray();
            string result = StringHelper.Cp1252ToString(bytes);

            result.Should().Be(original);
        }

        [Fact]
        public void StringToCp1252Bytes_EmptyString()
        {
            byte[] bytes = StringHelper.StringToCp1252Bytes("").ToArray();
            bytes.Should().BeEmpty();
        }

        [Fact]
        public void Cp1252ToString_EmptySpan()
        {
            StringHelper.Cp1252ToString(System.ReadOnlySpan<byte>.Empty).Should().Be("");
        }

        [Fact]
        public void StringToCp1252Bytes_WithLength()
        {
            byte[] bytes = StringHelper.StringToCp1252Bytes("ABCDEF", length: 3).ToArray();
            bytes.Should().Equal(0x41, 0x42, 0x43);
        }
    }
}
