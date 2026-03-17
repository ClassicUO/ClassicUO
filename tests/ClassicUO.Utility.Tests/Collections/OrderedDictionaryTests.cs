using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests.Collections
{
    public class OrderedDictionaryTests
    {
        [Fact]
        public void Add_And_Count()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();

            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.Count.Should().Be(3);
        }

        [Fact]
        public void Remove_MaintainsOrder()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.Remove("b");

            dict.Count.Should().Be(2);
            dict[0].Should().Be(1);
            dict[1].Should().Be(3);
        }

        [Fact]
        public void Remove_ReturnsTrueWhenFound()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            dict.Remove("a").Should().BeTrue();
        }

        [Fact]
        public void Remove_ReturnsFalseWhenNotFound()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            dict.Remove("z").Should().BeFalse();
        }

        [Fact]
        public void IndexBasedAccess_GetByIntIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);
            dict.Add("b", 20);
            dict.Add("c", 30);

            dict[0].Should().Be(10);
            dict[1].Should().Be(20);
            dict[2].Should().Be(30);
        }

        [Fact]
        public void IndexBasedAccess_SetByIntIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);
            dict.Add("b", 20);

            dict[1] = 99;

            dict[1].Should().Be(99);
            dict["b"].Should().Be(99);
        }

        [Fact]
        public void KeyBasedAccess_GetByKey()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);
            dict.Add("b", 20);

            dict["a"].Should().Be(10);
            dict["b"].Should().Be(20);
        }

        [Fact]
        public void KeyBasedAccess_SetByKey_ExistingKey()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);

            dict["a"] = 99;

            dict["a"].Should().Be(99);
            dict.Count.Should().Be(1);
        }

        [Fact]
        public void KeyBasedAccess_SetByKey_NewKey_Adds()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);

            dict["b"] = 20;

            dict.Count.Should().Be(2);
            dict["b"].Should().Be(20);
        }

        [Fact]
        public void Insert_AtSpecificIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("c", 3);

            dict.Insert(1, "b", 2);

            dict.Count.Should().Be(3);
            dict[0].Should().Be(1);
            dict[1].Should().Be(2);
            dict[2].Should().Be(3);
            dict["b"].Should().Be(2);
        }

        [Fact]
        public void IndexOf_ByKey_ReturnsCorrectIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.IndexOf("b").Should().Be(1);
        }

        [Fact]
        public void IndexOf_ByKey_ReturnsNegativeOneWhenNotFound()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            dict.IndexOf("z").Should().Be(-1);
        }

        [Fact]
        public void ContainsKey_ReturnsCorrectResult()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            dict.ContainsKey("a").Should().BeTrue();
            dict.ContainsKey("z").Should().BeFalse();
        }

        [Fact]
        public void ContainsValue_ReturnsCorrectResult()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.ContainsValue(1).Should().BeTrue();
            dict.ContainsValue(99).Should().BeFalse();
        }

        [Fact]
        public void GetItem_ReturnsKeyValuePairByIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);
            dict.Add("b", 20);

            var item = dict.GetItem(1);

            item.Key.Should().Be("b");
            item.Value.Should().Be(20);
        }

        [Fact]
        public void GetItem_InvalidIndex_ThrowsArgumentException()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            Action act = () => dict.GetItem(5);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetItem_UpdatesValueByIndex()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 10);
            dict.Add("b", 20);

            dict.SetItem(0, 99);

            dict[0].Should().Be(99);
            dict["a"].Should().Be(99);
        }

        [Fact]
        public void SetItem_InvalidIndex_ThrowsArgumentException()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            Action act = () => dict.SetItem(5, 99);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetValue_ReturnsValueByKey()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("key", 42);

            dict.GetValue("key").Should().Be(42);
        }

        [Fact]
        public void GetValue_MissingKey_ThrowsArgumentException()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();

            Action act = () => dict.GetValue("missing");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetValue_ExistingKey_UpdatesValue()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("key", 10);

            dict.SetValue("key", 99);

            dict["key"].Should().Be(99);
            dict.Count.Should().Be(1);
        }

        [Fact]
        public void SetValue_NewKey_AddsEntry()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();

            dict.SetValue("new", 42);

            dict.Count.Should().Be(1);
            dict["new"].Should().Be(42);
        }

        [Fact]
        public void TryGetValue_ReturnsTrue_WhenKeyExists()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("key", 42);

            dict.TryGetValue("key", out var value).Should().BeTrue();
            value.Should().Be(42);
        }

        [Fact]
        public void TryGetValue_ReturnsFalse_WhenKeyDoesNotExist()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();

            dict.TryGetValue("missing", out var value).Should().BeFalse();
            value.Should().Be(default);
        }

        [Fact]
        public void RemoveAt_ByIndex_RemovesCorrectItem()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.RemoveAt(1);

            dict.Count.Should().Be(2);
            dict[0].Should().Be(1);
            dict[1].Should().Be(3);
            dict.ContainsKey("b").Should().BeFalse();
        }

        [Fact]
        public void RemoveAt_InvalidIndex_ThrowsArgumentException()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            Action act = () => dict.RemoveAt(5);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void RemoveAt_NegativeIndex_ThrowsArgumentException()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            Action act = () => dict.RemoveAt(-1);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SortKeys_SortsAlphabetically()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("c", 3);
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.SortKeys();

            dict.GetItem(0).Key.Should().Be("a");
            dict.GetItem(1).Key.Should().Be("b");
            dict.GetItem(2).Key.Should().Be("c");
        }

        [Fact]
        public void SortKeys_WithComparer()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("c", 3);
            dict.Add("b", 2);

            // Sort descending
            dict.SortKeys(Comparer<string>.Create((x, y) => string.Compare(y, x, StringComparison.Ordinal)));

            dict.GetItem(0).Key.Should().Be("c");
            dict.GetItem(1).Key.Should().Be("b");
            dict.GetItem(2).Key.Should().Be("a");
        }

        [Fact]
        public void SortValues_SortsNumerically()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("x", 30);
            dict.Add("y", 10);
            dict.Add("z", 20);

            dict.SortValues();

            dict[0].Should().Be(10);
            dict[1].Should().Be(20);
            dict[2].Should().Be(30);
        }

        [Fact]
        public void SortValues_WithComparer()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("x", 10);
            dict.Add("y", 30);
            dict.Add("z", 20);

            // Sort descending
            dict.SortValues(Comparer<int>.Create((a, b) => b.CompareTo(a)));

            dict[0].Should().Be(30);
            dict[1].Should().Be(20);
            dict[2].Should().Be(10);
        }

        [Fact]
        public void Enumeration_PreservesInsertionOrder()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("first", 1);
            dict.Add("second", 2);
            dict.Add("third", 3);

            var keys = new List<string>();
            var values = new List<int>();

            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }

            keys.Should().Equal("first", "second", "third");
            values.Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Enumeration_AfterRemoval_CorrectOrder()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.Remove("b");

            var keys = dict.Select(kvp => kvp.Key).ToList();
            keys.Should().Equal("a", "c");
        }

        [Fact]
        public void DuplicateKey_Throws()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            Action act = () => dict.Add("a", 2);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Clear_RemovesAllItems()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.Clear();

            dict.Count.Should().Be(0);
            dict.ContainsKey("a").Should().BeFalse();
        }

        [Fact]
        public void Keys_ReturnsAllKeysInOrder()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("b", 2);
            dict.Add("a", 1);
            dict.Add("c", 3);

            dict.Keys.Should().Equal("b", "a", "c");
        }

        [Fact]
        public void Values_ReturnsAllValuesInOrder()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("b", 20);
            dict.Add("a", 10);
            dict.Add("c", 30);

            dict.Values.Should().Equal(20, 10, 30);
        }

        [Fact]
        public void Constructor_WithComparer_UsesComparer()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            dict.Add("Key", 1);

            dict.ContainsKey("key").Should().BeTrue();
            dict.ContainsKey("KEY").Should().BeTrue();
        }

        [Fact]
        public void SortKeys_WithComparison()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("c", 3);
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.SortKeys((x, y) => string.Compare(x, y, StringComparison.Ordinal));

            dict.GetItem(0).Key.Should().Be("a");
            dict.GetItem(1).Key.Should().Be("b");
            dict.GetItem(2).Key.Should().Be("c");
        }

        [Fact]
        public void SortValues_WithComparison()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("x", 30);
            dict.Add("y", 10);
            dict.Add("z", 20);

            dict.SortValues((a, b) => a.CompareTo(b));

            dict[0].Should().Be(10);
            dict[1].Should().Be(20);
            dict[2].Should().Be(30);
        }

        [Fact]
        public void ContainsValue_WithComparer()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, string>();
            dict.Add("key", "Hello");

            dict.ContainsValue("hello", StringComparer.OrdinalIgnoreCase).Should().BeTrue();
            dict.ContainsValue("hello", StringComparer.Ordinal).Should().BeFalse();
        }

        [Fact]
        public void Insert_AtZero_ShiftsExistingItems()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("b", 2);
            dict.Add("c", 3);

            dict.Insert(0, "a", 1);

            dict.GetItem(0).Key.Should().Be("a");
            dict.GetItem(1).Key.Should().Be("b");
            dict.GetItem(2).Key.Should().Be("c");
        }

        [Fact]
        public void Insert_AtEnd_AppendsItem()
        {
            var dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("a", 1);

            dict.Insert(1, "b", 2);

            dict.GetItem(0).Key.Should().Be("a");
            dict.GetItem(1).Key.Should().Be("b");
        }

        [Fact]
        public void IDictionary_Interface_Add_And_Access()
        {
            IDictionary<string, int> dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();

            dict.Add("key", 42);

            dict["key"].Should().Be(42);
            dict.ContainsKey("key").Should().BeTrue();
        }

        [Fact]
        public void IDictionary_Interface_TryGetValue()
        {
            IDictionary<string, int> dict = new ClassicUO.Utility.Collections.OrderedDictionary<string, int>();
            dict.Add("key", 42);

            dict.TryGetValue("key", out var value).Should().BeTrue();
            value.Should().Be(42);

            dict.TryGetValue("missing", out _).Should().BeFalse();
        }
    }
}
