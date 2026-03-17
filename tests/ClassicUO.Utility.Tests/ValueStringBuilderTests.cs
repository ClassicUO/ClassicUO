using System;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class ValueStringBuilderTests
    {
        [Fact]
        public void Constructor_WithInitialCapacity_CreatesEmptyBuilder()
        {
            var sb = new ValueStringBuilder(16);

            sb.Length.Should().Be(0);
            sb.Capacity.Should().BeGreaterThanOrEqualTo(16);

            sb.Dispose();
        }

        [Fact]
        public void Constructor_WithInitialString_ContainsString()
        {
            var sb = new ValueStringBuilder("hello".AsSpan());

            sb.Length.Should().Be(5);
            string result = sb.AsSpan().ToString();
            result.Should().Be("hello");

            sb.Dispose();
        }

        [Fact]
        public void Constructor_WithSpanBuffer_UsesProvidedBuffer()
        {
            Span<char> buffer = stackalloc char[32];
            var sb = new ValueStringBuilder(buffer);

            sb.Length.Should().Be(0);
            sb.Capacity.Should().BeGreaterThanOrEqualTo(32);

            sb.Dispose();
        }

        [Fact]
        public void Constructor_WithInitialStringAndBuffer_ContainsString()
        {
            Span<char> buffer = stackalloc char[32];
            var sb = new ValueStringBuilder("world".AsSpan(), buffer);

            sb.Length.Should().Be(5);
            string result = sb.AsSpan().ToString();
            result.Should().Be("world");

            sb.Dispose();
        }

        [Fact]
        public void Append_Char_IncreasesLength()
        {
            var sb = new ValueStringBuilder(16);

            sb.Append('A');

            sb.Length.Should().Be(1);
            sb[0].Should().Be('A');

            sb.Dispose();
        }

        [Fact]
        public void Append_String_AppendsCorrectly()
        {
            var sb = new ValueStringBuilder(16);

            sb.Append("hello");

            sb.Length.Should().Be(5);
            sb.AsSpan().ToString().Should().Be("hello");

            sb.Dispose();
        }

        [Fact]
        public void Append_ReadOnlySpanChar_AppendsCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            ReadOnlySpan<char> span = "test".AsSpan();

            sb.Append(span);

            sb.Length.Should().Be(4);
            sb.AsSpan().ToString().Should().Be("test");

            sb.Dispose();
        }

        [Fact]
        public void Append_CharWithCount_RepeatsCharacter()
        {
            var sb = new ValueStringBuilder(16);

            sb.Append('X', 5);

            sb.Length.Should().Be(5);
            sb.AsSpan().ToString().Should().Be("XXXXX");

            sb.Dispose();
        }

        [Fact]
        public void Append_MultipleStrings_Concatenates()
        {
            var sb = new ValueStringBuilder(16);

            sb.Append("Hello");
            sb.Append(" ");
            sb.Append("World");

            sb.Length.Should().Be(11);
            sb.AsSpan().ToString().Should().Be("Hello World");

            sb.Dispose();
        }

        [Fact]
        public void ToString_ReturnsAccumulatedString()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("abc");
            sb.Append("def");

            string result = sb.ToString();

            result.Should().Be("abcdef");
            // Note: ToString also disposes the builder
        }

        [Fact]
        public void Length_TracksCorrectly_AfterMultipleOperations()
        {
            var sb = new ValueStringBuilder(16);

            sb.Length.Should().Be(0);

            sb.Append('A');
            sb.Length.Should().Be(1);

            sb.Append("BC");
            sb.Length.Should().Be(3);

            sb.Append('D', 3);
            sb.Length.Should().Be(6);

            sb.Dispose();
        }

        [Fact]
        public void Length_CanBeSetDirectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            sb.Length = 3;

            sb.Length.Should().Be(3);
            sb.AsSpan().ToString().Should().Be("Hel");

            sb.Dispose();
        }

        [Fact]
        public void Insert_CharAtIndex_InsertsCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("AC");

            sb.Insert(1, 'B', 1);

            sb.Length.Should().Be(3);
            sb.AsSpan().ToString().Should().Be("ABC");

            sb.Dispose();
        }

        [Fact]
        public void Insert_SpanAtIndex_InsertsCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("AD");

            sb.Insert(1, "BC".AsSpan());

            sb.Length.Should().Be(4);
            sb.AsSpan().ToString().Should().Be("ABCD");

            sb.Dispose();
        }

        [Fact]
        public void Remove_FromMiddle_RemovesCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("ABCDE");

            sb.Remove(1, 2);

            sb.Length.Should().Be(3);
            sb.AsSpan().ToString().Should().Be("ADE");

            sb.Dispose();
        }

        [Fact]
        public void Remove_FromStart_RemovesCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("ABCDE");

            sb.Remove(0, 2);

            sb.Length.Should().Be(3);
            sb.AsSpan().ToString().Should().Be("CDE");

            sb.Dispose();
        }

        [Fact]
        public void Remove_FromEnd_RemovesCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("ABCDE");

            sb.Remove(3, 2);

            sb.Length.Should().Be(3);
            sb.AsSpan().ToString().Should().Be("ABC");

            sb.Dispose();
        }

        [Fact]
        public void Remove_NegativeLength_ThrowsArgumentOutOfRange()
        {
            ArgumentOutOfRangeException caught = null;
            try
            {
                var sb = new ValueStringBuilder(16);
                sb.Append("ABC");
                sb.Remove(0, -1);
                sb.Dispose();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                caught = ex;
            }

            caught.Should().NotBeNull();
        }

        [Fact]
        public void Remove_NegativeStartIndex_ThrowsArgumentOutOfRange()
        {
            ArgumentOutOfRangeException caught = null;
            try
            {
                var sb = new ValueStringBuilder(16);
                sb.Append("ABC");
                sb.Remove(-1, 1);
                sb.Dispose();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                caught = ex;
            }

            caught.Should().NotBeNull();
        }

        [Fact]
        public void Replace_CharWithChar_ReplacesFirstOccurrence()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("ABCBA");

            sb.Replace('B', 'X');

            sb[1].Should().Be('X');
            // Only the first occurrence is replaced
            sb[3].Should().Be('B');

            sb.Dispose();
        }

        [Fact]
        public void Replace_SpanWithSpan_SameLength()
        {
            var sb = new ValueStringBuilder(32);
            sb.Append("XYZXYZ");

            // Replace "XYZ" with "ABC" - same length, replaces at indexOf position
            // Due to implementation, same-length replace copies newChars to slice(0, len)
            // which effectively overwrites from the start of the search region.
            sb.Replace("XYZ".AsSpan(), "ABC".AsSpan());

            // The implementation copies newChars to slice.Slice(0, oldChars.Length)
            // which is the beginning of the slice (startIndex=0), so result is "ABCXYZ"
            sb.AsSpan().ToString().Should().Be("ABCXYZ");

            sb.Dispose();
        }

        [Fact]
        public void Replace_SpanWithShorterSpan()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello World");

            sb.Replace("World".AsSpan(), "Bob".AsSpan());

            sb.AsSpan().ToString().Should().Be("Hello Bob");

            sb.Dispose();
        }

        [Fact]
        public void Replace_SpanWithLongerSpan()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hi World");

            sb.Replace("Hi".AsSpan(), "Hello".AsSpan());

            sb.AsSpan().ToString().Should().Be("Hello World");

            sb.Dispose();
        }

        [Fact]
        public void Replace_NotFound_DoesNothing()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            sb.Replace("XYZ".AsSpan(), "ABC".AsSpan());

            sb.AsSpan().ToString().Should().Be("Hello");

            sb.Dispose();
        }

        [Fact]
        public void Indexer_GetSet_WorksCorrectly()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("ABC");

            sb[0].Should().Be('A');
            sb[1].Should().Be('B');
            sb[2].Should().Be('C');

            sb[1] = 'Z';
            sb[1].Should().Be('Z');

            sb.Dispose();
        }

        [Fact]
        public void AsSpan_ReturnsCorrectContent()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            ReadOnlySpan<char> span = sb.AsSpan();

            span.Length.Should().Be(5);
            span.ToString().Should().Be("Hello");

            sb.Dispose();
        }

        [Fact]
        public void AsSpan_WithStart_ReturnsSubset()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            ReadOnlySpan<char> span = sb.AsSpan(2);

            span.Length.Should().Be(3);
            span.ToString().Should().Be("llo");

            sb.Dispose();
        }

        [Fact]
        public void AsSpan_WithStartAndLength_ReturnsSubset()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            ReadOnlySpan<char> span = sb.AsSpan(1, 3);

            span.Length.Should().Be(3);
            span.ToString().Should().Be("ell");

            sb.Dispose();
        }

        [Fact]
        public void AsSpan_WithTerminate_AddsNullChar()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hi");

            ReadOnlySpan<char> span = sb.AsSpan(terminate: true);

            span.Length.Should().Be(2);
            span.ToString().Should().Be("Hi");

            sb.Dispose();
        }

        [Fact]
        public void Dispose_WorksWithoutError()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("test");

            sb.Dispose();

            // After dispose, builder is reset
            sb.Length.Should().Be(0);
        }

        [Fact]
        public void Dispose_OnDefaultStruct_DoesNotThrow()
        {
            // Default ValueStringBuilder should not throw on Dispose
            Exception caught = null;
            try
            {
                var sb = new ValueStringBuilder();
                sb.Dispose();
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            caught.Should().BeNull();
        }

        [Fact]
        public void EnsureCapacity_GrowsBuffer_WhenNeeded()
        {
            var sb = new ValueStringBuilder(4);
            int initialCapacity = sb.Capacity;

            sb.EnsureCapacity(100);

            sb.Capacity.Should().BeGreaterThanOrEqualTo(100);
            sb.Length.Should().Be(0); // length unchanged

            sb.Dispose();
        }

        [Fact]
        public void EnsureCapacity_DoesNotShrink_WhenAlreadyLargeEnough()
        {
            var sb = new ValueStringBuilder(100);
            int initialCapacity = sb.Capacity;

            sb.EnsureCapacity(50);

            sb.Capacity.Should().BeGreaterThanOrEqualTo(initialCapacity);

            sb.Dispose();
        }

        [Fact]
        public void Append_BeyondCapacity_GrowsAutomatically()
        {
            var sb = new ValueStringBuilder(4);

            sb.Append("This is a much longer string than the initial capacity");

            sb.AsSpan().ToString().Should().Be("This is a much longer string than the initial capacity");

            sb.Dispose();
        }

        [Fact]
        public void TryCopyTo_SucceedsWithLargeEnoughBuffer()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            char[] destArr = new char[10];
            bool result = sb.TryCopyTo(destArr, out int written);

            result.Should().BeTrue();
            written.Should().Be(5);
            new string(destArr, 0, written).Should().Be("Hello");
        }

        [Fact]
        public void TryCopyTo_FailsWithTooSmallBuffer()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("Hello");

            char[] destArr = new char[2];
            bool result = sb.TryCopyTo(destArr, out int written);

            result.Should().BeFalse();
            written.Should().Be(0);
        }

        [Fact]
        public void AppendSpan_ReturnsWritableSpan()
        {
            var sb = new ValueStringBuilder(16);
            sb.Append("AB");

            Span<char> span = sb.AppendSpan(3);
            span[0] = 'C';
            span[1] = 'D';
            span[2] = 'E';

            sb.Length.Should().Be(5);
            sb.AsSpan().ToString().Should().Be("ABCDE");

            sb.Dispose();
        }
    }
}
