using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.Easings
{
    public class BoundaryValues
    {
        private const float Precision = 1e-6f;
        private const float LoosePrecision = 0.01f;

        [Theory]
        [InlineData(0f)]
        [InlineData(0.25f)]
        [InlineData(0.5f)]
        [InlineData(0.75f)]
        [InlineData(1f)]
        public void Linear_Should_Return_Identity(float t)
        {
            ClassicUO.Utility.Easings.Linear(t)
                .Should()
                .Be(t);
        }

        [Fact]
        public void InQuad_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InQuad(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutQuad_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutQuad(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InCubic_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InCubic(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutCubic_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutCubic(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InQuart_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InQuart(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutQuart_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutQuart(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InQuint_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InQuint(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutQuint_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutQuint(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InSine_At_Zero_Should_Return_Negative_One()
        {
            ClassicUO.Utility.Easings.InSine(0f)
                .Should()
                .BeApproximately(-1f, Precision);
        }

        [Fact]
        public void OutSine_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutSine(0f)
                .Should()
                .BeApproximately(0f, Precision);
        }

        [Fact]
        public void InExpo_At_Zero_Should_Return_Approximately_Zero()
        {
            ClassicUO.Utility.Easings.InExpo(0f)
                .Should()
                .BeApproximately(0f, LoosePrecision);
        }

        [Fact]
        public void OutExpo_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutExpo(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InCirc_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InCirc(0f)
                .Should()
                .BeApproximately(0f, Precision);
        }

        [Fact]
        public void OutCirc_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutCirc(0f)
                .Should()
                .BeApproximately(0f, Precision);
        }

        [Fact]
        public void InElastic_At_Zero_Should_Return_Approximately_Zero()
        {
            ClassicUO.Utility.Easings.InElastic(0f)
                .Should()
                .BeApproximately(0f, LoosePrecision);
        }

        [Fact]
        public void OutElastic_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutElastic(0f)
                .Should()
                .BeApproximately(0f, Precision);
        }

        [Fact]
        public void InBack_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InBack(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutBack_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutBack(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InBounce_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.InBounce(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void OutBounce_At_Zero_Should_Return_Zero()
        {
            ClassicUO.Utility.Easings.OutBounce(0f)
                .Should()
                .Be(0f);
        }

        [Fact]
        public void InQuad_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InQuad(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void OutQuad_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutQuad(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void InCubic_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InCubic(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void OutCubic_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutCubic(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void InQuart_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InQuart(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void OutQuart_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutQuart(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void InQuint_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InQuint(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void OutQuint_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutQuint(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void InSine_At_One_Should_Return_Approximately_Zero()
        {
            ClassicUO.Utility.Easings.InSine(1f)
                .Should()
                .BeApproximately(0f, Precision);
        }

        [Fact]
        public void OutSine_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutSine(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void InExpo_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InExpo(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void OutExpo_At_One_Should_Return_Approximately_One()
        {
            ClassicUO.Utility.Easings.OutExpo(1f)
                .Should()
                .BeApproximately(1f, LoosePrecision);
        }

        [Fact]
        public void InCirc_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InCirc(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void OutCirc_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutCirc(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void InElastic_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InElastic(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void OutElastic_At_One_Should_Return_Approximately_One()
        {
            ClassicUO.Utility.Easings.OutElastic(1f)
                .Should()
                .BeApproximately(1f, LoosePrecision);
        }

        [Fact]
        public void InBack_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InBack(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void OutBack_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutBack(1f)
                .Should()
                .Be(1f);
        }

        [Fact]
        public void InBounce_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.InBounce(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void OutBounce_At_One_Should_Return_One()
        {
            ClassicUO.Utility.Easings.OutBounce(1f)
                .Should()
                .BeApproximately(1f, Precision);
        }

        [Fact]
        public void InOutQuad_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutQuad(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutCubic_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutCubic(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutQuart_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutQuart(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutQuint_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutQuint(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutSine_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutSine(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutExpo_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutExpo(0.5f)
                .Should()
                .BeApproximately(0.5f, LoosePrecision);
        }

        [Fact]
        public void InOutCirc_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutCirc(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutElastic_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutElastic(0.5f)
                .Should()
                .BeApproximately(0.5f, LoosePrecision);
        }

        [Fact]
        public void InOutBack_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutBack(0.5f)
                .Should()
                .BeApproximately(0.5f, Precision);
        }

        [Fact]
        public void InOutBounce_At_Half_Should_Return_Half()
        {
            ClassicUO.Utility.Easings.InOutBounce(0.5f)
                .Should()
                .BeApproximately(0.5f, LoosePrecision);
        }
    }
}
