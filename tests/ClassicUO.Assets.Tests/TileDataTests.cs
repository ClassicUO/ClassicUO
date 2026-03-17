using ClassicUO.Assets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class StaticTilesTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var tile = new StaticTiles(
            flags: (ulong)(TileFlag.Wall | TileFlag.Impassable),
            weight: 10,
            layer: 2,
            count: 5,
            animId: 100,
            hue: 50,
            lightIndex: 3,
            height: 20,
            name: "Stone Wall"
        );

        tile.Flags.Should().Be(TileFlag.Wall | TileFlag.Impassable);
        tile.Weight.Should().Be(10);
        tile.Layer.Should().Be(2);
        tile.Count.Should().Be(5);
        tile.AnimID.Should().Be(100);
        tile.Hue.Should().Be(50);
        tile.LightIndex.Should().Be(3);
        tile.Height.Should().Be(20);
        tile.Name.Should().Be("Stone Wall");
    }

    [Theory]
    [InlineData(TileFlag.Animation, nameof(StaticTiles.IsAnimated))]
    [InlineData(TileFlag.Bridge, nameof(StaticTiles.IsBridge))]
    [InlineData(TileFlag.Impassable, nameof(StaticTiles.IsImpassable))]
    [InlineData(TileFlag.Surface, nameof(StaticTiles.IsSurface))]
    [InlineData(TileFlag.Wearable, nameof(StaticTiles.IsWearable))]
    [InlineData(TileFlag.Internal, nameof(StaticTiles.IsInternal))]
    [InlineData(TileFlag.Background, nameof(StaticTiles.IsBackground))]
    [InlineData(TileFlag.NoDiagonal, nameof(StaticTiles.IsNoDiagonal))]
    [InlineData(TileFlag.Wet, nameof(StaticTiles.IsWet))]
    [InlineData(TileFlag.Foliage, nameof(StaticTiles.IsFoliage))]
    [InlineData(TileFlag.Roof, nameof(StaticTiles.IsRoof))]
    [InlineData(TileFlag.Translucent, nameof(StaticTiles.IsTranslucent))]
    [InlineData(TileFlag.PartialHue, nameof(StaticTiles.IsPartialHue))]
    [InlineData(TileFlag.Generic, nameof(StaticTiles.IsStackable))]
    [InlineData(TileFlag.Transparent, nameof(StaticTiles.IsTransparent))]
    [InlineData(TileFlag.Container, nameof(StaticTiles.IsContainer))]
    [InlineData(TileFlag.Door, nameof(StaticTiles.IsDoor))]
    [InlineData(TileFlag.Wall, nameof(StaticTiles.IsWall))]
    [InlineData(TileFlag.LightSource, nameof(StaticTiles.IsLight))]
    [InlineData(TileFlag.NoShoot, nameof(StaticTiles.IsNoShoot))]
    [InlineData(TileFlag.Weapon, nameof(StaticTiles.IsWeapon))]
    [InlineData(TileFlag.MultiMovable, nameof(StaticTiles.IsMultiMovable))]
    [InlineData(TileFlag.Window, nameof(StaticTiles.IsWindow))]
    public void FlagProperty_ReturnsTrue_WhenFlagIsSet(TileFlag flag, string propertyName)
    {
        var tile = new StaticTiles((ulong)flag, 0, 0, 0, 0, 0, 0, 0, "");
        var prop = typeof(StaticTiles).GetProperty(propertyName);
        prop.Should().NotBeNull();

        var value = (bool)prop!.GetValue(tile)!;
        value.Should().BeTrue($"{propertyName} should be true when {flag} is set");
    }

    [Fact]
    public void FlagProperties_ReturnFalse_WhenNoFlagsSet()
    {
        var tile = new StaticTiles(0, 0, 0, 0, 0, 0, 0, 0, "");

        tile.IsAnimated.Should().BeFalse();
        tile.IsBridge.Should().BeFalse();
        tile.IsImpassable.Should().BeFalse();
        tile.IsSurface.Should().BeFalse();
        tile.IsWearable.Should().BeFalse();
        tile.IsWall.Should().BeFalse();
        tile.IsDoor.Should().BeFalse();
        tile.IsRoof.Should().BeFalse();
        tile.IsContainer.Should().BeFalse();
    }

    [Fact]
    public void Invalid_HasDefaultValues()
    {
        StaticTiles.Invalid.Flags.Should().Be(TileFlag.None);
        StaticTiles.Invalid.Weight.Should().Be(0);
        StaticTiles.Invalid.Height.Should().Be(0);
        StaticTiles.Invalid.Name.Should().BeNull();
    }
}

public class LandTilesTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var tile = new LandTiles(
            flags: (ulong)(TileFlag.Wet | TileFlag.Impassable),
            textId: 42,
            name: "Water"
        );

        tile.Flags.Should().Be(TileFlag.Wet | TileFlag.Impassable);
        tile.TexID.Should().Be(42);
        tile.Name.Should().Be("Water");
    }

    [Fact]
    public void IsWet_ReturnsTrue_WhenWetFlagSet()
    {
        var tile = new LandTiles((ulong)TileFlag.Wet, 0, "");
        tile.IsWet.Should().BeTrue();
    }

    [Fact]
    public void IsImpassable_ReturnsTrue_WhenImpassableFlagSet()
    {
        var tile = new LandTiles((ulong)TileFlag.Impassable, 0, "");
        tile.IsImpassable.Should().BeTrue();
    }

    [Fact]
    public void IsNoDiagonal_ReturnsTrue_WhenNoDiagonalFlagSet()
    {
        var tile = new LandTiles((ulong)TileFlag.NoDiagonal, 0, "");
        tile.IsNoDiagonal.Should().BeTrue();
    }

    [Fact]
    public void FlagProperties_ReturnFalse_WhenNoFlagsSet()
    {
        var tile = new LandTiles(0, 0, "");

        tile.IsWet.Should().BeFalse();
        tile.IsImpassable.Should().BeFalse();
        tile.IsNoDiagonal.Should().BeFalse();
    }
}

public class TileFlagTests
{
    [Fact]
    public void TileFlag_None_IsZero()
    {
        ((ulong)TileFlag.None).Should().Be(0);
    }

    [Fact]
    public void TileFlag_FlagsCanBeCombined()
    {
        var flags = TileFlag.Wall | TileFlag.Impassable | TileFlag.Door;

        flags.HasFlag(TileFlag.Wall).Should().BeTrue();
        flags.HasFlag(TileFlag.Impassable).Should().BeTrue();
        flags.HasFlag(TileFlag.Door).Should().BeTrue();
        flags.HasFlag(TileFlag.Wet).Should().BeFalse();
    }

    [Fact]
    public void TileFlag_HighBits_AreCorrect()
    {
        // These flags use bits above 32-bit range
        ((ulong)TileFlag.AlphaBlend).Should().Be(0x0100000000);
        ((ulong)TileFlag.UseNewArt).Should().Be(0x0200000000);
        ((ulong)TileFlag.ArtUsed).Should().Be(0x0400000000);
        ((ulong)TileFlag.NoShadow).Should().Be(0x1000000000);
        ((ulong)TileFlag.PixelBleed).Should().Be(0x2000000000);
        ((ulong)TileFlag.PlayAnimOnce).Should().Be(0x4000000000);
        ((ulong)TileFlag.MultiMovable).Should().Be(0x10000000000);
    }

    [Fact]
    public void TileFlag_StandardBits_AreCorrect()
    {
        ((ulong)TileFlag.Background).Should().Be(0x00000001);
        ((ulong)TileFlag.Weapon).Should().Be(0x00000002);
        ((ulong)TileFlag.Transparent).Should().Be(0x00000004);
        ((ulong)TileFlag.Wall).Should().Be(0x00000010);
        ((ulong)TileFlag.Impassable).Should().Be(0x00000040);
        ((ulong)TileFlag.Wet).Should().Be(0x00000080);
        ((ulong)TileFlag.Surface).Should().Be(0x00000200);
        ((ulong)TileFlag.Door).Should().Be(0x20000000);
        ((ulong)TileFlag.StairBack).Should().Be(0x40000000);
        ((ulong)TileFlag.StairRight).Should().Be(0x80000000);
    }
}
