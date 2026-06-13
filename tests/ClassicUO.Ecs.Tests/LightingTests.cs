using ClassicUO.Configuration;
using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The ambient-level math + active-lights gating behind the world darkness pass.
// Pure (no GPU/assets): pins the legacy IsometricLight.Recalculate formula
// (level = max(personal, 32-overall) * 0.03125) and the custom-light-level /
// LightLevelType branches that GameScene.UseLights + the 0x4F handler encode.
public class LightingTests
{
    static (float level, bool useLights) Compute(int realOverall, int realPersonal, Profile p)
    {
        var light = new WorldLight { RealOverall = realOverall, RealPersonal = realPersonal };
        return LightingPlugin.Compute(light, p);
    }

    [Fact]
    public void Default_levels_are_bright_and_lights_off()
    {
        var (level, useLights) = Compute(9, 9, new Profile());
        Assert.Equal(0.71875, level, 5);   // max(9, 32-9) * 0.03125
        Assert.False(useLights);           // personal == overall → no darkening
    }

    [Fact]
    public void Full_day_is_full_bright()
    {
        var (level, useLights) = Compute(0, 0, new Profile());
        Assert.Equal(1.0, level, 5);       // 32 * 0.03125
        Assert.False(useLights);
    }

    [Fact]
    public void Night_darkens_and_enables_lights()
    {
        var (level, useLights) = Compute(28, 0, new Profile());
        Assert.Equal(0.125, level, 5);     // max(0, 32-28)=4 * 0.03125
        Assert.True(useLights);            // personal(0) < overall(28)
    }

    [Fact]
    public void Custom_absolute_overrides_server_level()
    {
        var p = new Profile { UseCustomLightLevel = true, LightLevelType = 0, LightLevel = 27 };
        var (level, useLights) = Compute(0, 0, p);   // server says full day
        Assert.Equal(0.15625, level, 5);             // max(0, 32-27)=5 * 0.03125 — stays dark
        Assert.True(useLights);                      // personal(0) < overall(27)
    }

    [Fact]
    public void Custom_minimum_caps_at_server_when_server_is_brighter()
    {
        var p = new Profile { UseCustomLightLevel = true, LightLevelType = 1, LightLevel = 10 };

        // Server darker than the cap → use the cap (10).
        var (darkLevel, _) = Compute(28, 0, p);
        Assert.Equal(0.6875, darkLevel, 5);          // min(28,10)=10 → 32-10=22 * 0.03125

        // Server brighter than the cap → use the server value (5).
        var (brightLevel, _) = Compute(5, 0, p);
        Assert.Equal(0.84375, brightLevel, 5);       // min(5,10)=5 → 32-5=27 * 0.03125
    }
}
