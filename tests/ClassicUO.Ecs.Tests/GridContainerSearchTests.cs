using ClassicUO.Assets;
using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Regression tests for the grid-container search filter. The bug: the filter
// keyed the item name off EntityName, which is mobile-only and never set on
// container items, so every comparison saw null and the search "found nothing".
// Name now resolves OPL-then-tiledata (ResolveItemSearchName) and the match is a
// plain case-insensitive substring (MatchesSearch). Pure logic, no assets.
public class GridContainerSearchTests
{
    private static StaticTiles[] Tiles(ushort graphic, string name)
    {
        var t = new StaticTiles[0x4000];
        t[graphic] = new StaticTiles { Name = name };
        return t;
    }

    [Fact]
    public void ResolveItemSearchName_falls_back_to_tiledata_when_no_opl()
    {
        // The regression: with no OPL the name must NOT be null (it was, via
        // EntityName) — the tiledata name keeps search working immediately.
        var name = GridContainerGumpPlugin.ResolveItemSearchName(
            oplName: null, Tiles(0x13B9, "katana"), graphic: 0x13B9);
        Assert.Equal("katana", name);
    }

    [Fact]
    public void ResolveItemSearchName_prefers_the_opl_name_over_tiledata()
    {
        var name = GridContainerGumpPlugin.ResolveItemSearchName(
            oplName: "a vanquishing katana", Tiles(0x13B9, "katana"), graphic: 0x13B9);
        Assert.Equal("a vanquishing katana", name);
    }

    [Fact]
    public void ResolveItemSearchName_empty_opl_falls_back_too()
    {
        var name = GridContainerGumpPlugin.ResolveItemSearchName(
            oplName: "", Tiles(0x13B9, "katana"), graphic: 0x13B9);
        Assert.Equal("katana", name);
    }

    [Fact]
    public void ResolveItemSearchName_is_null_only_when_graphic_is_past_the_table()
    {
        var name = GridContainerGumpPlugin.ResolveItemSearchName(
            oplName: null, new StaticTiles[0x10], graphic: 0x500);
        Assert.Null(name);
    }

    [Fact]
    public void MatchesSearch_empty_query_shows_everything()
    {
        Assert.True(GridContainerGumpPlugin.MatchesSearch("katana", ""));
        Assert.True(GridContainerGumpPlugin.MatchesSearch(null, ""));
    }

    [Fact]
    public void MatchesSearch_is_a_case_insensitive_substring_match()
    {
        Assert.True(GridContainerGumpPlugin.MatchesSearch("a vanquishing katana", "KATANA"));
        Assert.True(GridContainerGumpPlugin.MatchesSearch("katana", "kat"));
    }

    [Fact]
    public void MatchesSearch_rejects_a_non_match_and_a_nameless_item()
    {
        Assert.False(GridContainerGumpPlugin.MatchesSearch("katana", "shield"));
        // A query against an item that resolved no name (graphic past the table)
        // is filtered out, not shown.
        Assert.False(GridContainerGumpPlugin.MatchesSearch(null, "kat"));
    }
}
