using System;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Utility.MathHelper
{
    public class AngleBetweenVectors
    {
        [Fact]
        public void AngleBetweenVectors_Same_Point_Should_Return_Zero()
        {
            var point = new Vector2(5f, 5f);

            ClassicUO.Utility.MathHelper.AngleBetweenVectors(point, point)
                .Should()
                .BeApproximately(0f, 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_Directly_Right_Should_Return_Zero()
        {
            ClassicUO.Utility.MathHelper.AngleBetweenVectors(Vector2.Zero, new Vector2(1f, 0f))
                .Should()
                .BeApproximately(0f, 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_Directly_Up_Should_Return_HalfPi()
        {
            ClassicUO.Utility.MathHelper.AngleBetweenVectors(Vector2.Zero, new Vector2(0f, 1f))
                .Should()
                .BeApproximately((float)(Math.PI / 2), 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_Directly_Left_Should_Return_Pi()
        {
            ClassicUO.Utility.MathHelper.AngleBetweenVectors(Vector2.Zero, new Vector2(-1f, 0f))
                .Should()
                .BeApproximately((float)Math.PI, 0.0001f);
        }

        [Fact]
        public void AngleBetweenVectors_Directly_Down_Should_Return_NegativeHalfPi()
        {
            ClassicUO.Utility.MathHelper.AngleBetweenVectors(Vector2.Zero, new Vector2(0f, -1f))
                .Should()
                .BeApproximately((float)(-Math.PI / 2), 0.0001f);
        }
    }
}
