using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Utility.Collections;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests.Collections
{
    public class DequeTests
    {
        [Fact]
        public void DefaultConstructor_CreatesCapacity8()
        {
            var deque = new Deque<int>();

            deque.Capacity.Should().Be(8);
            deque.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCapacity_SetsCapacity()
        {
            var deque = new Deque<int>(16);

            deque.Capacity.Should().Be(16);
            deque.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithZeroCapacity_Succeeds()
        {
            var deque = new Deque<int>(0);

            deque.Capacity.Should().Be(0);
            deque.Count.Should().Be(0);
        }

        [Fact]
        public void Constructor_FromCollection_CopiesElements()
        {
            var source = new List<int> { 1, 2, 3 };
            var deque = new Deque<int>(source);

            deque.Count.Should().Be(3);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
            deque[2].Should().Be(3);
        }

        [Fact]
        public void Constructor_FromEmptyCollection_UsesDefaultCapacity()
        {
            var deque = new Deque<int>(new List<int>());

            deque.Count.Should().Be(0);
            deque.Capacity.Should().Be(8);
        }

        [Fact]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Action act = () => new Deque<int>(-1);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_NullCollection_ThrowsArgumentNullException()
        {
            Action act = () => new Deque<int>((IEnumerable<int>)null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddToBack_IncreasesCount()
        {
            var deque = new Deque<int>();

            deque.AddToBack(10);
            deque.AddToBack(20);

            deque.Count.Should().Be(2);
            deque[0].Should().Be(10);
            deque[1].Should().Be(20);
        }

        [Fact]
        public void AddToFront_IncreasesCount()
        {
            var deque = new Deque<int>();

            deque.AddToFront(10);
            deque.AddToFront(20);

            deque.Count.Should().Be(2);
            deque[0].Should().Be(20);
            deque[1].Should().Be(10);
        }

        [Fact]
        public void RemoveFromFront_ReturnsFirstItem()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            var result = deque.RemoveFromFront();

            result.Should().Be(1);
            deque.Count.Should().Be(2);
            deque[0].Should().Be(2);
        }

        [Fact]
        public void RemoveFromBack_ReturnsLastItem()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            var result = deque.RemoveFromBack();

            result.Should().Be(3);
            deque.Count.Should().Be(2);
            deque[1].Should().Be(2);
        }

        [Fact]
        public void RemoveFromFront_FIFO_Order()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveFromFront().Should().Be(1);
            deque.RemoveFromFront().Should().Be(2);
            deque.RemoveFromFront().Should().Be(3);
            deque.Count.Should().Be(0);
        }

        [Fact]
        public void RemoveFromBack_LIFO_Order()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveFromBack().Should().Be(3);
            deque.RemoveFromBack().Should().Be(2);
            deque.RemoveFromBack().Should().Be(1);
            deque.Count.Should().Be(0);
        }

        [Fact]
        public void RemoveFromFront_OnEmpty_ThrowsInvalidOperationException()
        {
            var deque = new Deque<int>();

            Action act = () => deque.RemoveFromFront();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RemoveFromBack_OnEmpty_ThrowsInvalidOperationException()
        {
            var deque = new Deque<int>();

            Action act = () => deque.RemoveFromBack();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Indexer_Get_ValidIndex_ReturnsCorrectItem()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);
            deque.AddToBack(30);

            deque[0].Should().Be(10);
            deque[1].Should().Be(20);
            deque[2].Should().Be(30);
        }

        [Fact]
        public void Indexer_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            Action act = () => { var _ = deque[5]; };

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Indexer_Get_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            Action act = () => { var _ = deque[-1]; };

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Indexer_Set_ValidIndex_UpdatesValue()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);

            deque[1] = 99;

            deque[1].Should().Be(99);
        }

        [Fact]
        public void Indexer_Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            Action act = () => deque[5] = 42;

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IndexOf_ReturnsCorrectIndex()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);
            deque.AddToBack(30);

            deque.IndexOf(20).Should().Be(1);
        }

        [Fact]
        public void IndexOf_ItemNotFound_ReturnsNegativeOne()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);

            deque.IndexOf(99).Should().Be(-1);
        }

        [Fact]
        public void Insert_AtFront()
        {
            var deque = new Deque<int>();
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.Insert(0, 1);

            deque.Count.Should().Be(3);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
            deque[2].Should().Be(3);
        }

        [Fact]
        public void Insert_AtMiddle()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(3);

            deque.Insert(1, 2);

            deque.Count.Should().Be(3);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
            deque[2].Should().Be(3);
        }

        [Fact]
        public void Insert_AtBack()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);

            deque.Insert(2, 3);

            deque.Count.Should().Be(3);
            deque[2].Should().Be(3);
        }

        [Fact]
        public void RemoveAt_Front()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveAt(0);

            deque.Count.Should().Be(2);
            deque[0].Should().Be(2);
            deque[1].Should().Be(3);
        }

        [Fact]
        public void RemoveAt_Middle()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveAt(1);

            deque.Count.Should().Be(2);
            deque[0].Should().Be(1);
            deque[1].Should().Be(3);
        }

        [Fact]
        public void RemoveAt_Back()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveAt(2);

            deque.Count.Should().Be(2);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
        }

        [Fact]
        public void InsertRange_InsertsElementsAtIndex()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(4);

            deque.InsertRange(1, new[] { 2, 3 });

            deque.Count.Should().Be(4);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
            deque[2].Should().Be(3);
            deque[3].Should().Be(4);
        }

        [Fact]
        public void InsertRange_AtFront()
        {
            var deque = new Deque<int>();
            deque.AddToBack(3);
            deque.AddToBack(4);

            deque.InsertRange(0, new[] { 1, 2 });

            deque.Count.Should().Be(4);
            deque.ToArray().Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void InsertRange_AtEnd()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);

            deque.InsertRange(2, new[] { 3, 4 });

            deque.Count.Should().Be(4);
            deque.ToArray().Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void RemoveRange_RemovesElementsFromMiddle()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);
            deque.AddToBack(4);
            deque.AddToBack(5);

            deque.RemoveRange(1, 2);

            deque.Count.Should().Be(3);
            deque[0].Should().Be(1);
            deque[1].Should().Be(4);
            deque[2].Should().Be(5);
        }

        [Fact]
        public void RemoveRange_FromFront()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveRange(0, 2);

            deque.Count.Should().Be(1);
            deque[0].Should().Be(3);
        }

        [Fact]
        public void RemoveRange_FromEnd()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.RemoveRange(1, 2);

            deque.Count.Should().Be(1);
            deque[0].Should().Be(1);
        }

        [Fact]
        public void Remove_ByValue_ReturnsTrue_WhenFound()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.Remove(2).Should().BeTrue();

            deque.Count.Should().Be(2);
            deque.ToArray().Should().Equal(1, 3);
        }

        [Fact]
        public void Remove_ByValue_ReturnsFalse_WhenNotFound()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            deque.Remove(99).Should().BeFalse();
            deque.Count.Should().Be(1);
        }

        [Fact]
        public void Clear_ResetsCountToZero()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            deque.Clear();

            deque.Count.Should().Be(0);
        }

        [Fact]
        public void ToArray_ReturnsContiguousArrayInOrder()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            var array = deque.ToArray();

            array.Should().Equal(1, 2, 3);
        }

        [Fact]
        public void ToArray_EmptyDeque_ReturnsEmptyArray()
        {
            var deque = new Deque<int>();

            deque.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void Capacity_Set_GrowsBuffer()
        {
            var deque = new Deque<int>(4);
            deque.AddToBack(1);
            deque.AddToBack(2);

            deque.Capacity = 16;

            deque.Capacity.Should().Be(16);
            deque.Count.Should().Be(2);
            deque[0].Should().Be(1);
            deque[1].Should().Be(2);
        }

        [Fact]
        public void Capacity_SetBelowCount_Throws()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            Action act = () => deque.Capacity = 2;

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Capacity_SetToSameValue_DoesNothing()
        {
            var deque = new Deque<int>(8);

            deque.Capacity = 8;

            deque.Capacity.Should().Be(8);
        }

        [Fact]
        public void Front_ReturnsRefToFirstElement()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);

            ref var front = ref deque.Front();

            front.Should().Be(10);

            front = 99;
            deque[0].Should().Be(99);
        }

        [Fact]
        public void Back_ReturnsRefToLastElement()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);

            ref var back = ref deque.Back();

            back.Should().Be(20);

            back = 99;
            deque[1].Should().Be(99);
        }

        [Fact]
        public void IList_Contains_ReturnsCorrectResult()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);

            var list = (ICollection<int>)deque;

            list.Contains(1).Should().BeTrue();
            list.Contains(99).Should().BeFalse();
        }

        [Fact]
        public void IList_Add_AddsToBack()
        {
            var deque = new Deque<int>();
            var collection = (ICollection<int>)deque;

            collection.Add(42);

            deque.Count.Should().Be(1);
            deque[0].Should().Be(42);
        }

        [Fact]
        public void IList_IsReadOnly_ReturnsFalse()
        {
            var deque = new Deque<int>();
            var collection = (ICollection<int>)deque;

            collection.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void IList_NonGeneric_Add_ReturnsIndex()
        {
            var deque = new Deque<int>();
            var list = (IList)deque;

            var index = list.Add(42);

            index.Should().Be(0);
            deque[0].Should().Be(42);
        }

        [Fact]
        public void IList_NonGeneric_Contains()
        {
            var deque = new Deque<int>();
            deque.AddToBack(42);
            var list = (IList)deque;

            list.Contains(42).Should().BeTrue();
            list.Contains(99).Should().BeFalse();
            list.Contains("not an int").Should().BeFalse();
        }

        [Fact]
        public void IList_NonGeneric_IndexOf()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);
            var list = (IList)deque;

            list.IndexOf(20).Should().Be(1);
            list.IndexOf("not an int").Should().Be(-1);
        }

        [Fact]
        public void IList_NonGeneric_Indexer_GetSet()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            var list = (IList)deque;

            ((int)list[0]).Should().Be(10);

            list[0] = 99;
            deque[0].Should().Be(99);
        }

        [Fact]
        public void WrapAround_AddToFront_CausesCircularBufferSplit()
        {
            var deque = new Deque<int>(4);

            // Add to front to cause wrap-around
            deque.AddToFront(1);
            deque.AddToFront(2);
            deque.AddToFront(3);
            deque.AddToFront(4);

            deque.Count.Should().Be(4);
            deque[0].Should().Be(4);
            deque[1].Should().Be(3);
            deque[2].Should().Be(2);
            deque[3].Should().Be(1);

            var array = deque.ToArray();
            array.Should().Equal(4, 3, 2, 1);
        }

        [Fact]
        public void WrapAround_MixedAddFrontAndBack()
        {
            var deque = new Deque<int>(4);

            deque.AddToBack(3);
            deque.AddToBack(4);
            deque.AddToFront(2);
            deque.AddToFront(1);

            deque.ToArray().Should().Equal(1, 2, 3, 4);
        }

        [Fact]
        public void WrapAround_AddRemovePattern_MaintainsOrder()
        {
            var deque = new Deque<int>(4);

            // Fill and partially drain to cause offset shift
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);
            deque.RemoveFromFront();
            deque.RemoveFromFront();

            // Now offset is shifted, add more
            deque.AddToBack(4);
            deque.AddToBack(5);
            deque.AddToBack(6);

            deque.Count.Should().Be(4);
            deque.ToArray().Should().Equal(3, 4, 5, 6);
        }

        [Fact]
        public void Enumeration_PreservesOrder()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            var items = deque.ToList();

            items.Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Enumeration_EmptyDeque_YieldsNothing()
        {
            var deque = new Deque<int>();

            deque.ToList().Should().BeEmpty();
        }

        [Fact]
        public void AutoGrow_WhenFull_DoublesCapacity()
        {
            var deque = new Deque<int>(2);
            deque.AddToBack(1);
            deque.AddToBack(2);

            deque.AddToBack(3);

            deque.Capacity.Should().Be(4);
            deque.Count.Should().Be(3);
            deque.ToArray().Should().Equal(1, 2, 3);
        }

        [Fact]
        public void GetAt_ReturnsRefToElement()
        {
            var deque = new Deque<int>();
            deque.AddToBack(10);
            deque.AddToBack(20);

            ref var item = ref deque.GetAt(1);

            item.Should().Be(20);

            item = 99;
            deque[1].Should().Be(99);
        }

        [Fact]
        public void CopyTo_CopiesToArray()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            deque.AddToBack(3);

            var array = new int[5];
            ((ICollection<int>)deque).CopyTo(array, 1);

            array.Should().Equal(0, 1, 2, 3, 0);
        }

        [Fact]
        public void Insert_InvalidIndex_Throws()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            Action act = () => deque.Insert(-1, 0);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void RemoveAt_InvalidIndex_Throws()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);

            Action act = () => deque.RemoveAt(5);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void IList_NonGeneric_Insert()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(3);
            var list = (IList)deque;

            list.Insert(1, 2);

            deque.Count.Should().Be(3);
            deque.ToArray().Should().Equal(1, 2, 3);
        }

        [Fact]
        public void IList_NonGeneric_Remove()
        {
            var deque = new Deque<int>();
            deque.AddToBack(1);
            deque.AddToBack(2);
            var list = (IList)deque;

            list.Remove(1);

            deque.Count.Should().Be(1);
            deque[0].Should().Be(2);
        }

        [Fact]
        public void IList_NonGeneric_IsFixedSize_ReturnsFalse()
        {
            var list = (IList)new Deque<int>();
            list.IsFixedSize.Should().BeFalse();
        }

        [Fact]
        public void IList_NonGeneric_IsReadOnly_ReturnsFalse()
        {
            var list = (IList)new Deque<int>();
            list.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void ICollection_NonGeneric_IsSynchronized_ReturnsFalse()
        {
            var collection = (ICollection)new Deque<int>();
            collection.IsSynchronized.Should().BeFalse();
        }

        [Fact]
        public void ICollection_NonGeneric_SyncRoot_IsNotNull()
        {
            var deque = new Deque<int>();
            var collection = (ICollection)deque;
            collection.SyncRoot.Should().BeSameAs(deque);
        }
    }
}
