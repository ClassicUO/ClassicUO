using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Utility.Collections;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests.Collections
{
    public class BagTests
    {
        [Fact]
        public void Constructor_DefaultCapacity_Is16()
        {
            var bag = new Bag<int>();

            bag.Capacity.Should().Be(16);
            bag.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_CustomCapacity_SetsCapacity()
        {
            var bag = new Bag<int>(32);

            bag.Capacity.Should().Be(32);
            bag.Count.Should().Be(0);
        }

        [Fact]
        public void Add_SingleElement_IncrementsCount()
        {
            var bag = new Bag<int>();

            bag.Add(42);

            bag.Count.Should().Be(1);
            bag[0].Should().Be(42);
        }

        [Fact]
        public void Add_MultipleElements_ContainsFindsAll()
        {
            var bag = new Bag<string>();

            bag.Add("alpha");
            bag.Add("beta");
            bag.Add("gamma");

            bag.Count.Should().Be(3);
            bag.Contains("alpha").Should().BeTrue();
            bag.Contains("beta").Should().BeTrue();
            bag.Contains("gamma").Should().BeTrue();
            bag.Contains("delta").Should().BeFalse();
        }

        [Fact]
        public void RemoveAt_SwapsWithLastElement()
        {
            var bag = new Bag<int>();
            bag.Add(10);
            bag.Add(20);
            bag.Add(30);

            var removed = bag.RemoveAt(0);

            removed.Should().Be(10);
            bag.Count.Should().Be(2);
            // The last element (30) should now be at index 0
            bag[0].Should().Be(30);
            bag[1].Should().Be(20);
        }

        [Fact]
        public void RemoveAt_LastElement_DecreasesCount()
        {
            var bag = new Bag<int>();
            bag.Add(10);
            bag.Add(20);

            var removed = bag.RemoveAt(1);

            removed.Should().Be(20);
            bag.Count.Should().Be(1);
            bag[0].Should().Be(10);
        }

        [Fact]
        public void Remove_ByValue_ReturnsTrueWhenFound()
        {
            var bag = new Bag<int>();
            bag.Add(10);
            bag.Add(20);
            bag.Add(30);

            bag.Remove(20).Should().BeTrue();

            bag.Count.Should().Be(2);
            bag.Contains(20).Should().BeFalse();
        }

        [Fact]
        public void Remove_ByValue_ReturnsFalseWhenNotFound()
        {
            var bag = new Bag<int>();
            bag.Add(10);

            bag.Remove(99).Should().BeFalse();
            bag.Count.Should().Be(1);
        }

        [Fact]
        public void RemoveAll_RemovesMatchingItemsFromAnotherBag()
        {
            var bag = new Bag<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);
            bag.Add(4);

            var toRemove = new Bag<int>();
            toRemove.Add(2);
            toRemove.Add(4);

            bag.RemoveAll(toRemove).Should().BeTrue();

            bag.Count.Should().Be(2);
            bag.Contains(2).Should().BeFalse();
            bag.Contains(4).Should().BeFalse();
            bag.Contains(1).Should().BeTrue();
            bag.Contains(3).Should().BeTrue();
        }

        [Fact]
        public void RemoveAll_ReturnsFlase_WhenNoMatchesFound()
        {
            var bag = new Bag<int>();
            bag.Add(1);

            var toRemove = new Bag<int>();
            toRemove.Add(99);

            bag.RemoveAll(toRemove).Should().BeFalse();
            bag.Count.Should().Be(1);
        }

        [Fact]
        public void Clear_ResetsCountToZero()
        {
            var bag = new Bag<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);

            bag.Clear();

            bag.Count.Should().Be(0);
            bag.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Clear_OnEmptyBag_DoesNotThrow()
        {
            var bag = new Bag<int>();

            var act = () => bag.Clear();

            act.Should().NotThrow();
            bag.Count.Should().Be(0);
        }

        [Fact]
        public void Indexer_Set_AutoExpandsCount()
        {
            var bag = new Bag<int>();

            bag[5] = 42;

            bag.Count.Should().Be(6);
            bag[5].Should().Be(42);
        }

        [Fact]
        public void Indexer_Set_WithinCount_DoesNotChangeCount()
        {
            var bag = new Bag<int>();
            bag.Add(10);
            bag.Add(20);

            bag[0] = 99;

            bag.Count.Should().Be(2);
            bag[0].Should().Be(99);
        }

        [Fact]
        public void IsEmpty_WhenEmpty_ReturnsTrue()
        {
            var bag = new Bag<int>();

            bag.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty_ReturnsFalse()
        {
            var bag = new Bag<int>();
            bag.Add(1);

            bag.IsEmpty.Should().BeFalse();
        }

        [Fact]
        public void Capacity_GrowsWhenExceeded()
        {
            var bag = new Bag<int>(4);

            for (int i = 0; i < 5; i++)
            {
                bag.Add(i);
            }

            bag.Count.Should().Be(5);
            // Capacity should grow to at least 1.5x the original (4 * 1.5 = 6)
            bag.Capacity.Should().BeGreaterThanOrEqualTo(5);
        }

        [Fact]
        public void Capacity_GrowsByOneAndAHalf()
        {
            // EnsureCapacity uses strict less-than, so capacity 4 can hold 3 items
            // Adding the 4th item triggers growth: Max(4 * 1.5, 4) = 6
            var bag = new Bag<int>(4);

            // Fill without triggering growth
            for (int i = 0; i < 3; i++)
                bag.Add(i);

            bag.Capacity.Should().Be(4);

            // Adding the 4th item triggers growth since EnsureCapacity(4) and 4 < 4 is false
            bag.Add(100);

            // 4 * 1.5 = 6
            bag.Capacity.Should().Be(6);
        }

        [Fact]
        public void Enumeration_YieldsAllItems()
        {
            var bag = new Bag<int>();
            bag.Add(10);
            bag.Add(20);
            bag.Add(30);

            var items = new List<int>();
            foreach (var item in bag)
            {
                items.Add(item);
            }

            items.Should().HaveCount(3);
            items.Should().Contain(10);
            items.Should().Contain(20);
            items.Should().Contain(30);
        }

        [Fact]
        public void Enumeration_EmptyBag_YieldsNoItems()
        {
            var bag = new Bag<int>();

            var items = new List<int>();
            foreach (var item in bag)
            {
                items.Add(item);
            }

            items.Should().BeEmpty();
        }

        [Fact]
        public void AddRange_FromAnotherBag_AddsAllElements()
        {
            var bag = new Bag<int>();
            bag.Add(1);

            var other = new Bag<int>();
            other.Add(2);
            other.Add(3);

            bag.AddRange(other);

            bag.Count.Should().Be(3);
            bag[0].Should().Be(1);
            bag[1].Should().Be(2);
            bag[2].Should().Be(3);
        }

        [Fact]
        public void AddRange_FromEmptyBag_DoesNotChangeCount()
        {
            var bag = new Bag<int>();
            bag.Add(1);

            var empty = new Bag<int>();
            bag.AddRange(empty);

            bag.Count.Should().Be(1);
        }

        [Fact]
        public void Indexer_Set_BeyondCapacity_GrowsCapacity()
        {
            var bag = new Bag<int>(4);

            bag[10] = 99;

            bag.Count.Should().Be(11);
            bag[10].Should().Be(99);
            bag.Capacity.Should().BeGreaterThanOrEqualTo(11);
        }

        [Fact]
        public void Contains_WithReferenceTypes()
        {
            var bag = new Bag<string>();
            bag.Add("hello");
            bag.Add("world");

            bag.Contains("hello").Should().BeTrue();
            bag.Contains("missing").Should().BeFalse();
        }

        [Fact]
        public void Clear_ReferenceTypes_ClearsReferences()
        {
            var bag = new Bag<string>();
            bag.Add("hello");
            bag.Add("world");

            bag.Clear();

            bag.Count.Should().Be(0);
            bag.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Enumeration_ViaLinq_Works()
        {
            var bag = new Bag<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);

            var result = bag.ToList();

            result.Should().BeEquivalentTo(new[] { 1, 2, 3 });
        }
    }
}
