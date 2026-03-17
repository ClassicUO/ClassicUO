using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class DummyItem
    {
        public int Value;
    }

    public class QueuedPoolTests
    {
        [Fact]
        public void Constructor_SetsMaxSize()
        {
            var pool = new QueuedPool<DummyItem>(10);
            pool.MaxSize.Should().Be(10);
        }

        [Fact]
        public void Initially_RemainsIsZero()
        {
            var pool = new QueuedPool<DummyItem>(5);
            // Pool starts with MaxSize items, Remains = MaxSize - Count = 5 - 5 = 0
            pool.Remains.Should().Be(0);
        }

        [Fact]
        public void GetOne_ReturnsAnItem()
        {
            var pool = new QueuedPool<DummyItem>(5);
            var item = pool.GetOne();

            item.Should().NotBeNull();
        }

        [Fact]
        public void GetOne_IncreasesRemains()
        {
            var pool = new QueuedPool<DummyItem>(5);
            pool.Remains.Should().Be(0);

            pool.GetOne();
            pool.Remains.Should().Be(1);

            pool.GetOne();
            pool.Remains.Should().Be(2);
        }

        [Fact]
        public void ReturnOne_DecreasesRemains()
        {
            var pool = new QueuedPool<DummyItem>(5);
            var item = pool.GetOne();
            pool.Remains.Should().Be(1);

            pool.ReturnOne(item);
            pool.Remains.Should().Be(0);
        }

        [Fact]
        public void GetOne_WhenPoolEmpty_CreatesNewItem()
        {
            var pool = new QueuedPool<DummyItem>(2);

            // Drain the pool
            var item1 = pool.GetOne();
            var item2 = pool.GetOne();
            pool.Remains.Should().Be(2);

            // Pool is now empty, should create a new T() rather than throwing
            var item3 = pool.GetOne();
            item3.Should().NotBeNull();
            // Remains is still MaxSize - Count = 2 - 0 = 2 (new items don't affect MaxSize)
            pool.Remains.Should().Be(2);
        }

        [Fact]
        public void Clear_EmptiesPool()
        {
            var pool = new QueuedPool<DummyItem>(5);
            pool.Remains.Should().Be(0);

            pool.Clear();
            // After clear, internal stack is empty, Remains = MaxSize - 0 = MaxSize
            pool.Remains.Should().Be(5);
        }

        [Fact]
        public void OnPickup_CallbackIsInvokedOnGetOne()
        {
            int callbackCount = 0;
            DummyItem receivedItem = null;

            var pool = new QueuedPool<DummyItem>(3, item =>
            {
                callbackCount++;
                receivedItem = item;
            });

            var retrieved = pool.GetOne();

            callbackCount.Should().Be(1);
            receivedItem.Should().BeSameAs(retrieved);
        }

        [Fact]
        public void OnPickup_CallbackIsInvokedEachTime()
        {
            int callbackCount = 0;

            var pool = new QueuedPool<DummyItem>(3, _ => callbackCount++);

            pool.GetOne();
            pool.GetOne();
            pool.GetOne();

            callbackCount.Should().Be(3);
        }

        [Fact]
        public void ReturnOne_WithNull_DoesNotIncreaseCount()
        {
            var pool = new QueuedPool<DummyItem>(2);
            var item = pool.GetOne();
            pool.Remains.Should().Be(1);

            pool.ReturnOne(null);
            // Null is ignored, remains unchanged
            pool.Remains.Should().Be(1);
        }

        [Fact]
        public void GetOne_ThenReturnOne_ItemCanBeReused()
        {
            var pool = new QueuedPool<DummyItem>(1);

            var item = pool.GetOne();
            item.Value = 42;

            pool.ReturnOne(item);

            var reused = pool.GetOne();
            // Stack is LIFO, so we get back the same item
            reused.Value.Should().Be(42);
        }
    }
}
