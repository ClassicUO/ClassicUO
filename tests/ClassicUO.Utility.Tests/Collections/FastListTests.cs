using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Utility.Collections;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests.Collections
{
    public class FastListTests
    {
        [Fact]
        public void DefaultConstructor_CreatesBufferOfSize5()
        {
            var list = new FastList<int>();

            list.Buffer.Length.Should().Be(5);
            list.Length.Should().Be(0);
        }

        [Fact]
        public void Constructor_CustomSize_CreatesBufferOfSpecifiedSize()
        {
            var list = new FastList<int>(10);

            list.Buffer.Length.Should().Be(10);
            list.Length.Should().Be(0);
        }

        [Fact]
        public void Add_IncrementsLength_StoresInBuffer()
        {
            var list = new FastList<int>();

            list.Add(42);

            list.Length.Should().Be(1);
            list.Buffer[0].Should().Be(42);
        }

        [Fact]
        public void Add_MultipleElements()
        {
            var list = new FastList<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Length.Should().Be(3);
            list.Buffer[0].Should().Be(1);
            list.Buffer[1].Should().Be(2);
            list.Buffer[2].Should().Be(3);
        }

        [Fact]
        public void Remove_RemovesFirstOccurrence_Ordered()
        {
            var list = new FastList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Remove(2);

            list.Length.Should().Be(2);
            list[0].Should().Be(1);
            list[1].Should().Be(3);
        }

        [Fact]
        public void Remove_ItemNotFound_DoesNothing()
        {
            var list = new FastList<int>();
            list.Add(1);
            list.Add(2);

            list.Remove(99);

            list.Length.Should().Be(2);
        }

        [Fact]
        public void RemoveAt_ShiftsElementsDown_Ordered()
        {
            var list = new FastList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            list.RemoveAt(0);

            list.Length.Should().Be(2);
            list[0].Should().Be(20);
            list[1].Should().Be(30);
        }

        [Fact]
        public void RemoveAt_LastElement()
        {
            var list = new FastList<int>();
            list.Add(10);
            list.Add(20);

            list.RemoveAt(1);

            list.Length.Should().Be(1);
            list[0].Should().Be(10);
        }

        [Fact]
        public void RemoveAtWithSwap_SwapsWithLast_Unordered()
        {
            var list = new FastList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            list.RemoveAtWithSwap(0);

            list.Length.Should().Be(2);
            list[0].Should().Be(30);
            list[1].Should().Be(20);
        }

        [Fact]
        public void RemoveAtWithSwap_LastElement()
        {
            var list = new FastList<int>();
            list.Add(10);
            list.Add(20);

            list.RemoveAtWithSwap(1);

            list.Length.Should().Be(1);
            list[0].Should().Be(10);
        }

        [Fact]
        public void Contains_ReturnsTrue_WhenItemExists()
        {
            var list = new FastList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Contains(2).Should().BeTrue();
        }

        [Fact]
        public void Contains_ReturnsFalse_WhenItemDoesNotExist()
        {
            var list = new FastList<int>();
            list.Add(1);

            list.Contains(99).Should().BeFalse();
        }

        [Fact]
        public void Contains_EmptyList_ReturnsFalse()
        {
            var list = new FastList<int>();

            list.Contains(1).Should().BeFalse();
        }

        [Fact]
        public void Clear_NullsOutBuffer_ResetsLength()
        {
            var list = new FastList<string>();
            list.Add("hello");
            list.Add("world");

            list.Clear();

            list.Length.Should().Be(0);
            // After Clear, buffer entries should be nulled for reference types
            list.Buffer[0].Should().BeNull();
            list.Buffer[1].Should().BeNull();
        }

        [Fact]
        public void Reset_ResetsLength_DoesNotClearBuffer()
        {
            var list = new FastList<int>();
            list.Add(42);
            list.Add(99);

            list.Reset();

            list.Length.Should().Be(0);
            // Buffer contents remain (not cleared)
            list.Buffer[0].Should().Be(42);
            list.Buffer[1].Should().Be(99);
        }

        [Fact]
        public void Indexer_ReturnsBufferAtIndex()
        {
            var list = new FastList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            list[0].Should().Be(10);
            list[1].Should().Be(20);
            list[2].Should().Be(30);
        }

        [Fact]
        public void EnsureCapacity_GrowsBuffer()
        {
            var list = new FastList<int>(2);
            list.Add(1);
            list.Add(2);

            list.EnsureCapacity(5);

            list.Buffer.Length.Should().BeGreaterThanOrEqualTo(7); // Length + additionalItemCount
            // Existing data preserved
            list[0].Should().Be(1);
            list[1].Should().Be(2);
        }

        [Fact]
        public void EnsureCapacity_DoesNotShrink()
        {
            var list = new FastList<int>(10);

            list.EnsureCapacity(1);

            list.Buffer.Length.Should().Be(10);
        }

        [Fact]
        public void AddRange_FromIEnumerable()
        {
            var list = new FastList<int>();
            list.Add(1);

            list.AddRange(new[] { 2, 3, 4 });

            list.Length.Should().Be(4);
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
            list[3].Should().Be(4);
        }

        [Fact]
        public void AddRange_EmptyEnumerable_DoesNothing()
        {
            var list = new FastList<int>();
            list.Add(1);

            list.AddRange(Array.Empty<int>());

            list.Length.Should().Be(1);
        }

        [Fact]
        public void Sort_DefaultComparer_SortsAscending()
        {
            var list = new FastList<int>();
            list.Add(3);
            list.Add(1);
            list.Add(2);

            list.Sort();

            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
        }

        [Fact]
        public void Sort_WithGenericComparer()
        {
            var list = new FastList<int>();
            list.Add(1);
            list.Add(3);
            list.Add(2);

            // Sort descending
            list.Sort((IComparer<int>)Comparer<int>.Create((a, b) => b.CompareTo(a)));

            list[0].Should().Be(3);
            list[1].Should().Be(2);
            list[2].Should().Be(1);
        }

        [Fact]
        public void Sort_WithNonGenericComparer()
        {
            var list = new FastList<int>();
            list.Add(3);
            list.Add(1);
            list.Add(2);

            IComparer nonGenericComparer = Comparer<int>.Default;
            list.Sort(nonGenericComparer);

            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
        }

        [Fact]
        public void AutoResize_WhenCapacityExceeded()
        {
            var list = new FastList<int>(2);

            list.Add(1);
            list.Add(2);
            list.Add(3); // Should trigger resize

            list.Length.Should().Be(3);
            list.Buffer.Length.Should().BeGreaterThanOrEqualTo(3);
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
        }

        [Fact]
        public void AutoResize_PreservesExistingData()
        {
            var list = new FastList<int>(2);
            list.Add(10);
            list.Add(20);

            // Fill beyond capacity
            for (int i = 0; i < 10; i++)
                list.Add(i + 100);

            list.Length.Should().Be(12);
            list[0].Should().Be(10);
            list[1].Should().Be(20);
            list[2].Should().Be(100);
        }

        [Fact]
        public void RemoveAt_MiddleElement_ShiftsCorrectly()
        {
            var list = new FastList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);
            list.Add(5);

            list.RemoveAt(2);

            list.Length.Should().Be(4);
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(4);
            list[3].Should().Be(5);
        }

        [Fact]
        public void Remove_ReferenceType()
        {
            var list = new FastList<string>();
            list.Add("alpha");
            list.Add("beta");
            list.Add("gamma");

            list.Remove("beta");

            list.Length.Should().Be(2);
            list[0].Should().Be("alpha");
            list[1].Should().Be("gamma");
        }

        [Fact]
        public void AddRange_FromList()
        {
            var list = new FastList<int>();
            list.AddRange(new List<int> { 1, 2, 3 });

            list.Length.Should().Be(3);
            list[0].Should().Be(1);
            list[1].Should().Be(2);
            list[2].Should().Be(3);
        }
    }
}
