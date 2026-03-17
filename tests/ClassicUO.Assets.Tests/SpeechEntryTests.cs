using System;
using ClassicUO.Assets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class SpeechEntryTests
{
    [Fact]
    public void Constructor_SimpleKeyword_SetsPropertiesCorrectly()
    {
        var entry = new SpeechEntry(42, "bank");

        entry.KeywordID.Should().Be(42);
        entry.Keywords.Should().ContainSingle().Which.Should().Be("bank");
        entry.CheckStart.Should().BeFalse();
        entry.CheckEnd.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WildcardAtStart_SetsCheckStartTrue()
    {
        var entry = new SpeechEntry(1, "*bank");

        entry.CheckStart.Should().BeTrue();
        entry.CheckEnd.Should().BeFalse();
        entry.Keywords.Should().ContainSingle().Which.Should().Be("bank");
    }

    [Fact]
    public void Constructor_WildcardAtEnd_SetsCheckEndTrue()
    {
        var entry = new SpeechEntry(1, "bank*");

        entry.CheckStart.Should().BeFalse();
        entry.CheckEnd.Should().BeTrue();
        entry.Keywords.Should().ContainSingle().Which.Should().Be("bank");
    }

    [Fact]
    public void Constructor_WildcardBothEnds_SetsBothChecksTrue()
    {
        var entry = new SpeechEntry(1, "*bank*");

        entry.CheckStart.Should().BeTrue();
        entry.CheckEnd.Should().BeTrue();
        entry.Keywords.Should().ContainSingle().Which.Should().Be("bank");
    }

    [Fact]
    public void Constructor_MultipleKeywords_SplitByAsterisk()
    {
        var entry = new SpeechEntry(1, "*hello*world*");

        entry.CheckStart.Should().BeTrue();
        entry.CheckEnd.Should().BeTrue();
        entry.Keywords.Should().HaveCount(2);
        entry.Keywords[0].Should().Be("hello");
        entry.Keywords[1].Should().Be("world");
    }

    [Fact]
    public void Constructor_EmptyString_SetsNoKeywords()
    {
        var entry = new SpeechEntry(1, "");

        entry.Keywords.Should().BeEmpty();
        entry.CheckStart.Should().BeFalse();
        entry.CheckEnd.Should().BeFalse();
    }

    [Fact]
    public void Constructor_IdTruncatedToShort()
    {
        // SpeechEntry casts id to short
        var entry = new SpeechEntry(0x1234, "test");
        entry.KeywordID.Should().Be(0x1234);
    }

    [Fact]
    public void CompareTo_LesserId_ReturnsNegative()
    {
        var a = new SpeechEntry(1, "a");
        var b = new SpeechEntry(2, "b");

        a.CompareTo(b).Should().Be(-1);
    }

    [Fact]
    public void CompareTo_GreaterId_ReturnsPositive()
    {
        var a = new SpeechEntry(2, "a");
        var b = new SpeechEntry(1, "b");

        a.CompareTo(b).Should().Be(1);
    }

    [Fact]
    public void CompareTo_EqualId_ReturnsZero()
    {
        var a = new SpeechEntry(5, "a");
        var b = new SpeechEntry(5, "b");

        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void SpeechEntries_SortByKeywordId()
    {
        var entries = new[]
        {
            new SpeechEntry(3, "c"),
            new SpeechEntry(1, "a"),
            new SpeechEntry(2, "b"),
        };

        Array.Sort(entries);

        entries[0].KeywordID.Should().Be(1);
        entries[1].KeywordID.Should().Be(2);
        entries[2].KeywordID.Should().Be(3);
    }
}
