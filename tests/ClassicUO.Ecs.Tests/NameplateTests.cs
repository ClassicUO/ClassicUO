using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using Xunit;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the pure pockets of the nameplate update loop: the HP-bar
// colour thresholds, the notoriety hue table, and the gargoyle body check.
// UpdatePlates itself is camera/asset-driven (screenshot-only); these are the
// parity-sensitive value mappings pulled out of it.
public class NameplateTests
{
    [Theory]
    [InlineData(100, 0, 128, 0)]
    [InlineData(80, 0, 128, 0)]
    [InlineData(79, 154, 205, 50)]
    [InlineData(50, 154, 205, 50)]
    [InlineData(49, 255, 255, 0)]
    [InlineData(30, 255, 255, 0)]
    [InlineData(29, 255, 0, 0)]
    [InlineData(0, 255, 0, 0)]
    public void HpFillColor_picks_the_band_for_the_percentage(int pct, int r, int g, int b)
        => Assert.Equal(new ClayColor((byte)r, (byte)g, (byte)b, 255), NameplatePlugin.HpFillColor(pct));

    [Fact]
    public void NotorietyHue_maps_known_flags()
    {
        Assert.Equal(0x005A, NameplatePlugin.NotorietyHue(NotorietyFlag.Innocent));
        Assert.Equal(0x0044, NameplatePlugin.NotorietyHue(NotorietyFlag.Ally));
        Assert.Equal(0x0031, NameplatePlugin.NotorietyHue(NotorietyFlag.Enemy));
        Assert.Equal(0x0023, NameplatePlugin.NotorietyHue(NotorietyFlag.Murderer));
        Assert.Equal(0x0034, NameplatePlugin.NotorietyHue(NotorietyFlag.Invulnerable));
    }

    [Theory]
    [InlineData(0x029A, true)]
    [InlineData(0x029B, true)]
    [InlineData(0x02B6, true)]
    [InlineData(0x02B7, true)]
    [InlineData(0x0190, false)]
    [InlineData(0x0191, false)]
    public void IsGargoyle_matches_the_four_gargoyle_bodies(int graphic, bool expected)
        => Assert.Equal(expected, NameplatePlugin.IsGargoyle((ushort)graphic));
}
