using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class Hypotenuse
    {
        [Fact]
        public void Hypotenuse_3_4_Should_Return_5()
        {
            ClassicUO.Utility.MathHelper.Hypotenuse(3f, 4f)
                .Should()
                .BeApproximately(5.0, 0.0001);
        }

        [Fact]
        public void Hypotenuse_Zero_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.Hypotenuse(0f, 0f)
                .Should()
                .BeApproximately(0.0, 0.0001);
        }

        [Fact]
        public void Hypotenuse_One_Zero_Should_Return_One()
        {
            ClassicUO.Utility.MathHelper.Hypotenuse(1f, 0f)
                .Should()
                .BeApproximately(1.0, 0.0001);
        }

        [Fact]
        public void Hypotenuse_Zero_One_Should_Return_One()
        {
            ClassicUO.Utility.MathHelper.Hypotenuse(0f, 1f)
                .Should()
                .BeApproximately(1.0, 0.0001);
        }
    }
}
