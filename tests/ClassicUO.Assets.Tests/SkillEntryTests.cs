using ClassicUO.Assets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class SkillEntryTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var entry = new SkillEntry(5, "Magery", true);

        entry.Index.Should().Be(5);
        entry.Name.Should().Be("Magery");
        entry.HasAction.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NoAction()
    {
        var entry = new SkillEntry(0, "Alchemy", false);

        entry.HasAction.Should().BeFalse();
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var entry = new SkillEntry(0, "Swordsmanship", true);

        entry.ToString().Should().Be("Swordsmanship");
    }

    [Fact]
    public void HardCodedName_EnumContainsExpectedSkills()
    {
        // Verify some well-known skill enum values and their indices
        ((int)SkillEntry.HardCodedName.Alchemy).Should().Be(0);
        ((int)SkillEntry.HardCodedName.Anatomy).Should().Be(1);
        ((int)SkillEntry.HardCodedName.Magery).Should().Be(25);
        ((int)SkillEntry.HardCodedName.Swordsmanship).Should().Be(40);
        ((int)SkillEntry.HardCodedName.Wrestling).Should().Be(43);
        ((int)SkillEntry.HardCodedName.Meditation).Should().Be(46);
    }

    [Fact]
    public void HardCodedName_HasExpectedCount()
    {
        var values = System.Enum.GetValues<SkillEntry.HardCodedName>();
        values.Should().HaveCount(58);
    }

    [Fact]
    public void Name_CanBeModified()
    {
        var entry = new SkillEntry(0, "Original", false);
        entry.Name = "Modified";

        entry.Name.Should().Be("Modified");
        entry.ToString().Should().Be("Modified");
    }

    [Fact]
    public void HasAction_CanBeModified()
    {
        var entry = new SkillEntry(0, "Test", false);
        entry.HasAction = true;

        entry.HasAction.Should().BeTrue();
    }
}
