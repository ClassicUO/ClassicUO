using System;
using System.Security.Cryptography;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.QueuedPool
{
    public class GetOne
    {
        [Fact]
        public void When_GetOne_Called_Once_Remains_Should_Be_One()
        {
            var objectPool = new Utility.QueuedPool<DummyItem>(10);

            _ = objectPool.GetOne();

            objectPool.Remains.Should().Be(1);
        }

        [Fact]
        public void When_GetOne_Called_Twice_Remains_Should_Be_Two()
        {
            var objectPool = new Utility.QueuedPool<DummyItem>(10);

            _ = objectPool.GetOne();
            _ = objectPool.GetOne();

            objectPool.Remains.Should().Be(2);
        }
    }
}