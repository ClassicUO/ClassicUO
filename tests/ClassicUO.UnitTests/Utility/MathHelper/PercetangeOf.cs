using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class PercetangeOf
    {
        // Two-argument overload: PercetangeOf(int current, int max)

        [Theory]
        [InlineData(50, 100, 50)]
        [InlineData(100, 100, 100)]
        [InlineData(1, 3, 33)]
        [InlineData(25, 200, 12)]
        public void PercetangeOf_TwoArg_Should_Return_Percentage(int current, int max, int expected)
        {
            ClassicUO.Utility.MathHelper.PercetangeOf(current, max)
                .Should()
                .Be(expected);
        }

        [Fact]
        public void PercetangeOf_TwoArg_Zero_Current_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.PercetangeOf(0, 100)
                .Should()
                .Be(0);
        }

        [Fact]
        public void PercetangeOf_TwoArg_Zero_Max_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.PercetangeOf(100, 0)
                .Should()
                .Be(0);
        }

        [Theory]
        [InlineData(-1, 100)]
        [InlineData(100, -1)]
        [InlineData(-5, -10)]
        public void PercetangeOf_TwoArg_Negative_Input_Should_Return_Zero(int current, int max)
        {
            ClassicUO.Utility.MathHelper.PercetangeOf(current, max)
                .Should()
                .Be(0);
        }

        // Three-argument overload: PercetangeOf(int max, int current, int maxValue)

        [Fact]
        public void PercetangeOf_ThreeArg_Should_Scale_To_MaxValue()
        {
            // current=50, max=100 => percentage=50, then maxValue * 50 / 100
            ClassicUO.Utility.MathHelper.PercetangeOf(100, 50, 200)
                .Should()
                .Be(100);
        }

        [Fact]
        public void PercetangeOf_ThreeArg_Full_Percentage_Should_Return_MaxValue()
        {
            // current=100, max=100 => percentage=100, then maxValue * 100 / 100 = maxValue
            ClassicUO.Utility.MathHelper.PercetangeOf(100, 100, 500)
                .Should()
                .Be(500);
        }

        [Fact]
        public void PercetangeOf_ThreeArg_Over_100_Percent_Should_Cap_At_100()
        {
            // current=200, max=100 => percentage=200, capped to 100, then maxValue * 100 / 100
            ClassicUO.Utility.MathHelper.PercetangeOf(100, 200, 500)
                .Should()
                .Be(500);
        }

        [Fact]
        public void PercetangeOf_ThreeArg_Zero_Max_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.PercetangeOf(0, 50, 200)
                .Should()
                .Be(0);
        }

        [Fact]
        public void PercetangeOf_ThreeArg_Percentage_Of_One_Should_Return_Max_Parameter()
        {
            // current=1, max=100 => percentage=1, which is not > 1, so returns max param (1)
            ClassicUO.Utility.MathHelper.PercetangeOf(100, 1, 200)
                .Should()
                .Be(1);
        }
    }
}
