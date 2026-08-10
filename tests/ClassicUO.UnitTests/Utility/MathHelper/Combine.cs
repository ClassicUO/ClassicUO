using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class Combine
    {
        [Fact]
        public void Combine_Both_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.Combine(0, 0)
                .Should()
                .Be(0UL);
        }

        [Fact]
        public void Combine_Both_Zero_RoundTrip_Should_Extract_Zeros()
        {
            var combined = ClassicUO.Utility.MathHelper.Combine(0, 0);

            ClassicUO.Utility.MathHelper.GetNumbersFromCombine(combined, out int val1, out int val2);

            val1.Should().Be(0);
            val2.Should().Be(0);
        }

        /// <summary>
        /// Documents a bug: val2 &lt;&lt; 32 in uint context shifts by (32 mod 32) = 0,
        /// so Combine(val1, val2) actually computes val1 | val2 (bitwise OR).
        /// Combine(1, 2) = 1 | 2 = 3, not 1 | (2 &lt;&lt; 32).
        /// </summary>
        [Fact]
        public void Combine_Bug_Val2_Shift_Is_Noop_So_Result_Is_BitwiseOr()
        {
            ClassicUO.Utility.MathHelper.Combine(1, 2)
                .Should()
                .Be(3UL, "because val2 << 32 in uint context equals val2 << 0 = val2, so result is val1 | val2");
        }

        /// <summary>
        /// Documents that the round-trip fails for non-zero val2 due to the shift bug.
        /// The high 32 bits are never set, so GetNumbersFromCombine extracts val2 as 0.
        /// </summary>
        [Fact]
        public void Combine_Bug_RoundTrip_Fails_For_NonZero_Val2()
        {
            var combined = ClassicUO.Utility.MathHelper.Combine(1, 2);

            ClassicUO.Utility.MathHelper.GetNumbersFromCombine(combined, out int val1, out int val2);

            val1.Should().Be(3, "because combined = 3 and lower 32 bits of 3 = 3");
            val2.Should().Be(0, "because the high 32 bits are never set due to the shift bug");
        }

        [Fact]
        public void Combine_Only_Val1_Set_Should_Preserve_Val1_In_RoundTrip()
        {
            var combined = ClassicUO.Utility.MathHelper.Combine(42, 0);

            ClassicUO.Utility.MathHelper.GetNumbersFromCombine(combined, out int val1, out int val2);

            val1.Should().Be(42);
            val2.Should().Be(0);
        }

        [Fact]
        public void GetNumbersFromCombine_With_High_Bits_Set_Should_Extract_Both_Values()
        {
            // Manually construct a ulong with both halves set to verify GetNumbersFromCombine works correctly
            ulong combined = 42UL | (99UL << 32);

            ClassicUO.Utility.MathHelper.GetNumbersFromCombine(combined, out int val1, out int val2);

            val1.Should().Be(42);
            val2.Should().Be(99);
        }
    }
}
