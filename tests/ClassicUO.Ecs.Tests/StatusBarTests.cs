using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the status-bar field formatter — the CCN-54 switch that maps a
// StatusField to its display string. Pulled out of the Refresh system as a pure
// function (no ECS / render), so the cur/max vs single-value parity per field is
// locked here.
public class StatusBarTests
{
    [Fact]
    public void FormatField_single_value_fields_stringify_the_stat()
    {
        var d = new PlayerData { Str = 25, Dex = 18, Int = 40, Gold = 1234 };
        Assert.Equal("25", StatusBarPlugin.FormatField(StatusField.Strength, in d, default, default, default, ""));
        Assert.Equal("18", StatusBarPlugin.FormatField(StatusField.Dexterity, in d, default, default, default, ""));
        Assert.Equal("40", StatusBarPlugin.FormatField(StatusField.Intelligence, in d, default, default, default, ""));
        Assert.Equal("1234", StatusBarPlugin.FormatField(StatusField.Gold, in d, default, default, default, ""));
    }

    [Fact]
    public void FormatField_combined_fields_show_cur_slash_max()
    {
        var hits = new Hits { Value = 50, MaxValue = 100 };
        var mana = new Mana { Value = 12, MaxValue = 30 };
        var stam = new Stamina { Value = 7, MaxValue = 25 };
        PlayerData d = default;
        Assert.Equal("50/100", StatusBarPlugin.FormatField(StatusField.HitsCombined, in d, hits, mana, stam, ""));
        Assert.Equal("12/30", StatusBarPlugin.FormatField(StatusField.ManaCombined, in d, hits, mana, stam, ""));
        Assert.Equal("7/25", StatusBarPlugin.FormatField(StatusField.StaminaCombined, in d, hits, mana, stam, ""));
    }

    [Fact]
    public void FormatField_resist_fields_show_value_slash_max()
    {
        var d = new PlayerData { PhysicalRes = 5, MaxPhysicalRes = 70, FireRes = 10, MaxFireRes = 75 };
        Assert.Equal("5/70", StatusBarPlugin.FormatField(StatusField.Armor, in d, default, default, default, ""));
        Assert.Equal("10/75", StatusBarPlugin.FormatField(StatusField.FireRes, in d, default, default, default, ""));
    }

    [Fact]
    public void FormatField_damage_is_a_range()
    {
        var d = new PlayerData { DamageMin = 3, DamageMax = 9 };
        Assert.Equal("3-9", StatusBarPlugin.FormatField(StatusField.Damage, in d, default, default, default, ""));
    }

    [Fact]
    public void FormatField_sex_and_name()
    {
        var female = new PlayerData { IsFemale = true };
        var male = new PlayerData { IsFemale = false };
        Assert.Equal("Female", StatusBarPlugin.FormatField(StatusField.Sex, in female, default, default, default, ""));
        Assert.Equal("Male", StatusBarPlugin.FormatField(StatusField.Sex, in male, default, default, default, ""));
        Assert.Equal("Bob", StatusBarPlugin.FormatField(StatusField.Name, in male, default, default, default, "Bob"));
    }

    [Fact]
    public void FormatField_unknown_field_is_empty()
    {
        PlayerData d = default;
        Assert.Equal("", StatusBarPlugin.FormatField((StatusField)250, in d, default, default, default, ""));
    }
}
