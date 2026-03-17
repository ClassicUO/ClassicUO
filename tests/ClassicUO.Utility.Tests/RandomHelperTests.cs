using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class RandomHelperTests
    {
        [Fact]
        public void GetValue_WithRange_ReturnsValueInInclusiveRange()
        {
            for (int i = 0; i < 100; i++)
            {
                int value = RandomHelper.GetValue(5, 10);
                value.Should().BeInRange(5, 10);
            }
        }

        [Fact]
        public void GetValue_WithEqualLowAndHigh_ReturnsThatValue()
        {
            for (int i = 0; i < 10; i++)
            {
                RandomHelper.GetValue(7, 7).Should().Be(7);
            }
        }

        [Fact]
        public void GetValue_Parameterless_ReturnsNonNegative()
        {
            for (int i = 0; i < 100; i++)
            {
                RandomHelper.GetValue().Should().BeGreaterOrEqualTo(0);
            }
        }

        [Fact]
        public void RandomList_ReturnsElementFromProvidedList()
        {
            int[] list = { 10, 20, 30, 40, 50 };

            for (int i = 0; i < 100; i++)
            {
                int value = RandomHelper.RandomList(list);
                list.Should().Contain(value);
            }
        }

        [Fact]
        public void RandomList_SingleElement_ReturnsThatElement()
        {
            RandomHelper.RandomList(42).Should().Be(42);
        }

        [Fact]
        public void RandomBool_ReturnsBothTrueAndFalse()
        {
            bool seenTrue = false;
            bool seenFalse = false;

            for (int i = 0; i < 1000; i++)
            {
                if (RandomHelper.RandomBool())
                    seenTrue = true;
                else
                    seenFalse = true;

                if (seenTrue && seenFalse)
                    break;
            }

            seenTrue.Should().BeTrue("RandomBool should eventually return true");
            seenFalse.Should().BeTrue("RandomBool should eventually return false");
        }

        [Fact]
        public void GetValue_WithNegativeRange_ReturnsValueInRange()
        {
            for (int i = 0; i < 100; i++)
            {
                int value = RandomHelper.GetValue(-10, -5);
                value.Should().BeInRange(-10, -5);
            }
        }

        [Fact]
        public void GetValue_WithLargeRange_ReturnsValueInRange()
        {
            for (int i = 0; i < 100; i++)
            {
                int value = RandomHelper.GetValue(0, 1000000);
                value.Should().BeInRange(0, 1000000);
            }
        }
    }
}
