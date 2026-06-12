using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the pure pockets of the health-bar Refresh loop: the bar-fill
// pixel math, the notoriety hue table, and the HP fill sprite/tint selection.
// These are the parity-sensitive bits (legacy CalculatePercents + bar graphics)
// pulled out of the CCN-46 system so they lock without a render context.
public class HealthBarTests
{
    [Theory]
    [InlineData(0, 50, 100, 0)]     // unknown max -> 0 width
    [InlineData(100, 100, 80, 80)]  // full -> full bar
    [InlineData(100, 50, 80, 40)]   // half -> half bar
    [InlineData(100, 1, 80, 1)]     // 1% rounds to a 1px sliver (>1 scale skipped)
    [InlineData(100, 150, 80, 80)]  // over-max clamps to full
    public void CalculatePercents_scales_current_over_max_into_pixels(int max, int current, int barW, int expected)
        => Assert.Equal(expected, HealthBarPlugin.CalculatePercents(max, current, barW));

    [Fact]
    public void NotorietyHue_maps_known_flags()
    {
        Assert.Equal(0x005A, HealthBarPlugin.NotorietyHue(NotorietyFlag.Innocent));
        Assert.Equal(0x0023, HealthBarPlugin.NotorietyHue(NotorietyFlag.Murderer));
        Assert.Equal(0x0031, HealthBarPlugin.NotorietyHue(NotorietyFlag.Enemy));
        Assert.Equal(0, HealthBarPlugin.NotorietyHue((NotorietyFlag)200));
    }

    [Fact]
    public void ResolveHpFillSprite_non_party_swaps_sprite_untinted()
    {
        Assert.Equal(((ushort)0x0806, Vector3.UnitZ), HealthBarPlugin.ResolveHpFillSprite(false, Flags.None));
        Assert.Equal(((ushort)0x0808, Vector3.UnitZ), HealthBarPlugin.ResolveHpFillSprite(false, Flags.Poisoned));
        Assert.Equal(((ushort)0x0809, Vector3.UnitZ), HealthBarPlugin.ResolveHpFillSprite(false, Flags.YellowBar));
    }

    [Fact]
    public void ResolveHpFillSprite_party_keeps_one_sprite_and_tints()
    {
        Assert.Equal(((ushort)0x0029, Vector3.UnitZ), HealthBarPlugin.ResolveHpFillSprite(true, Flags.None));
        Assert.Equal(((ushort)0x0029, new Vector3(63, 1f, 1f)), HealthBarPlugin.ResolveHpFillSprite(true, Flags.Poisoned));
        Assert.Equal(((ushort)0x0029, new Vector3(353, 1f, 1f)), HealthBarPlugin.ResolveHpFillSprite(true, Flags.YellowBar));
    }
}
