using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class InRange
    {
        [Theory]
        [InlineData(5, 5, 10)]
        [InlineData(10, 5, 10)]
        public void InRange_Input_At_Boundary_Should_Return_True(int input, int low, int high)
        {
            ClassicUO.Utility.MathHelper.InRange(input, low, high)
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(7, 5, 10)]
        [InlineData(0, -1, 1)]
        public void InRange_Input_Within_Range_Should_Return_True(int input, int low, int high)
        {
            ClassicUO.Utility.MathHelper.InRange(input, low, high)
                .Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(4, 5, 10)]
        [InlineData(-6, -5, -1)]
        public void InRange_Input_Below_Range_Should_Return_False(int input, int low, int high)
        {
            ClassicUO.Utility.MathHelper.InRange(input, low, high)
                .Should()
                .BeFalse();
        }

        [Theory]
        [InlineData(11, 5, 10)]
        [InlineData(0, -5, -1)]
        public void InRange_Input_Above_Range_Should_Return_False(int input, int low, int high)
        {
            ClassicUO.Utility.MathHelper.InRange(input, low, high)
                .Should()
                .BeFalse();
        }

        [Theory]
        [InlineData(-3, -5, -1)]
        [InlineData(-5, -5, -1)]
        [InlineData(-1, -5, -1)]
        public void InRange_Negative_Range_Should_Return_True_When_In_Range(int input, int low, int high)
        {
            ClassicUO.Utility.MathHelper.InRange(input, low, high)
                .Should()
                .BeTrue();
        }
    }
}
