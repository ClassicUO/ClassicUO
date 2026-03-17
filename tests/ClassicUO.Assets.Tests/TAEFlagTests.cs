using ClassicUO.Assets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class TAEFlagTests
{
    [Fact]
    public void TAEFlag_None_IsZero()
    {
        ((ulong)TAEFlag.None).Should().Be(0);
    }

    [Fact]
    public void TAEFlag_StandardBits_MatchExpectedValues()
    {
        ((ulong)TAEFlag.Background).Should().Be(0x1);
        ((ulong)TAEFlag.Weapon).Should().Be(0x2);
        ((ulong)TAEFlag.Transparent).Should().Be(0x4);
        ((ulong)TAEFlag.Wall).Should().Be(0x10);
        ((ulong)TAEFlag.Impassable).Should().Be(0x40);
        ((ulong)TAEFlag.Wet).Should().Be(0x80);
        ((ulong)TAEFlag.Surface).Should().Be(0x200);
        ((ulong)TAEFlag.Container).Should().Be(0x200000);
        ((ulong)TAEFlag.Door).Should().Be(0x20000000);
    }

    [Fact]
    public void TAEFlag_ArticleThe_IsCombinationOfArticleAAndArticleAn()
    {
        TAEFlag.ArticleThe.Should().Be(TAEFlag.ArticleA | TAEFlag.ArticleAn);
    }

    [Fact]
    public void TAEFlag_HighBits_AreCorrect()
    {
        ((ulong)TAEFlag.NoHouse).Should().Be(0x100000000);
        ((ulong)TAEFlag.NoDraw).Should().Be(0x200000000);
        ((ulong)TAEFlag.AlphaBlend).Should().Be(0x800000000);
        ((ulong)TAEFlag.NoShadow).Should().Be(0x1000000000);
        ((ulong)TAEFlag.PixelBleed).Should().Be(0x2000000000);
        ((ulong)TAEFlag.PlayAnimOnce).Should().Be(0x8000000000);
        ((ulong)TAEFlag.MultiMovable).Should().Be(0x10000000000);
    }

    [Fact]
    public void TAEFlag_CanCombineFlags()
    {
        var flags = TAEFlag.Wall | TAEFlag.Impassable | TAEFlag.Door;

        flags.HasFlag(TAEFlag.Wall).Should().BeTrue();
        flags.HasFlag(TAEFlag.Impassable).Should().BeTrue();
        flags.HasFlag(TAEFlag.Door).Should().BeTrue();
        flags.HasFlag(TAEFlag.Wet).Should().BeFalse();
    }
}

public class TAEPropIDTests
{
    [Fact]
    public void TAEPropID_HasExpectedValues()
    {
        ((byte)TAEPropID.Weight).Should().Be(0);
        ((byte)TAEPropID.Quality).Should().Be(1);
        ((byte)TAEPropID.Quantity).Should().Be(2);
        ((byte)TAEPropID.Height).Should().Be(3);
        ((byte)TAEPropID.Value).Should().Be(4);
        ((byte)TAEPropID.Paperdoll).Should().Be(11);
    }

    [Fact]
    public void TAEPropID_HasExpectedCount()
    {
        var values = System.Enum.GetValues<TAEPropID>();
        values.Should().HaveCount(12);
    }
}
