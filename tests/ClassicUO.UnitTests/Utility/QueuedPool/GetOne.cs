using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.QueuedPool
{
    public class GetOne
    {
        [Fact]
        public void When_GetOne_Called_Once_Remains_Should_Be_One()
        {
            var objectPool = new ClassicUO.Utility.QueuedPool<DummyItem>(10);

            _ = objectPool.GetOne();

            objectPool.Remains.Should().Be(1);
        }

        [Fact]
        public void When_GetOne_Called_Twice_Remains_Should_Be_Two()
        {
            var objectPool = new ClassicUO.Utility.QueuedPool<DummyItem>(10);

            _ = objectPool.GetOne();
            _ = objectPool.GetOne();

            objectPool.Remains.Should().Be(2);
        }
    }
}