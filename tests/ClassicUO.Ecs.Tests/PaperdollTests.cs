using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the paperdoll body-picker + scroll paint-order helpers — the
// pure mappings BuildBodyAndOverlays leans on. The body-spawn loop itself is
// screenshot-only, but these (legacy UpdateUI body picker + the
// behind-body child insert index) are parity-sensitive and pure.
public class PaperdollTests
{
    [Theory]
    [InlineData(0x0191, 0x000D)] // human female
    [InlineData(0x0193, 0x000D)]
    [InlineData(0x025D, 0x000E)] // elf male
    [InlineData(0x025E, 0x000F)]
    [InlineData(0x029A, 0x029A)] // gargoyle
    [InlineData(0x029B, 0x0299)]
    [InlineData(0x04E5, 0xC835)]
    [InlineData(0x03DB, 0x000C)]
    public void ResolveBodyGraphic_maps_known_bodies(int mobileGraphic, int expected)
        => Assert.Equal((ushort)expected, PaperdollPlugin.ResolveBodyGraphic((ushort)mobileGraphic, isFemale: false));

    [Fact]
    public void ResolveBodyGraphic_unknown_falls_back_by_sex()
    {
        Assert.Equal((ushort)0x000D, PaperdollPlugin.ResolveBodyGraphic(0xFFFF, isFemale: true));
        Assert.Equal((ushort)0x000C, PaperdollPlugin.ResolveBodyGraphic(0xFFFF, isFemale: false));
    }

    [Theory]
    [InlineData(0x0191, true)]
    [InlineData(0x0193, true)]
    [InlineData(0x025D, true)]
    [InlineData(0x029B, true)]
    [InlineData(0x02B7, true)]
    [InlineData(0x0190, false)] // male human
    [InlineData(0x025E, false)] // elf male
    public void ResolveIsFemale_flags_known_female_bodies(int mobileGraphic, bool expected)
        => Assert.Equal(expected, PaperdollPlugin.ResolveIsFemale((ushort)mobileGraphic));

    [Fact]
    public void BehindBodyScrollCount_other_mobile_has_profile_only()
    {
        var ctx = new GameContext { PlayerSerial = 0x1000 };
        Assert.Equal(1, PaperdollPlugin.BehindBodyScrollCount(ctx, 0x2000)); // not the player
    }

    [Fact]
    public void BehindBodyScrollCount_player_without_books_has_profile_plus_party()
    {
        var ctx = new GameContext { PlayerSerial = 0x1000, ClientFeatures = 0, ClientVersion = ClientVersion.CV_7000 };
        Assert.Equal(2, PaperdollPlugin.BehindBodyScrollCount(ctx, 0x1000));
    }

    [Fact]
    public void BehindBodyScrollCount_player_with_books_and_racial_counts_all_four()
    {
        var ctx = new GameContext
        {
            PlayerSerial = 0x1000,
            ClientFeatures = CharacterListFlags.CLF_PALADIN_NECROMANCER_TOOLTIPS,
            ClientVersion = ClientVersion.CV_7000,
        };
        Assert.Equal(4, PaperdollPlugin.BehindBodyScrollCount(ctx, 0x1000));
    }
}
