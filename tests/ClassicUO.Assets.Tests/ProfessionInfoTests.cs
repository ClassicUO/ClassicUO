using ClassicUO.Assets;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class ProfessionInfoTests
{
    [Fact]
    public void GetDefaults_BeforeCV70160_Returns50SkillValues()
    {
        var (skills, stats) = ProfessionInfo.GetDefaults(ClientVersion.CV_7000);

        // Before CV_70160: initialSkillValue=50, remainStatValue=10
        skills[0, 1].Should().Be(50);
        skills[1, 1].Should().Be(50);
        skills[2, 1].Should().Be(0); // third skill is 0 before CV_70160
        skills[3, 1].Should().Be(50);

        stats[0].Should().Be(60);
        stats[1].Should().Be(10);
        stats[2].Should().Be(10);
    }

    [Fact]
    public void GetDefaults_AtCV70160_Returns30SkillValues()
    {
        var (skills, stats) = ProfessionInfo.GetDefaults(ClientVersion.CV_70160);

        // At CV_70160: initialSkillValue=30, remainStatValue=15
        skills[0, 1].Should().Be(30);
        skills[1, 1].Should().Be(30);
        skills[2, 1].Should().Be(30); // third skill is 30 at CV_70160+
        skills[3, 1].Should().Be(30);

        stats[0].Should().Be(60);
        stats[1].Should().Be(15);
        stats[2].Should().Be(15);
    }

    [Fact]
    public void GetDefaults_AfterCV70160_Returns30SkillValues()
    {
        var (skills, stats) = ProfessionInfo.GetDefaults(ClientVersion.CV_70180);

        skills[0, 1].Should().Be(30);
        skills[1, 1].Should().Be(30);
        skills[2, 1].Should().Be(30);
        skills[3, 1].Should().Be(30);

        stats[0].Should().Be(60);
        stats[1].Should().Be(15);
        stats[2].Should().Be(15);
    }

    [Fact]
    public void GetDefaults_SkillIndices_StartAtZero()
    {
        var (skills, _) = ProfessionInfo.GetDefaults(ClientVersion.CV_7000);

        skills[0, 0].Should().Be(0);
        skills[1, 0].Should().Be(0);
        skills[2, 0].Should().Be(0);
        skills[3, 0].Should().Be(0);
    }

    [Fact]
    public void GetDefaults_Returns4Skills()
    {
        var (skills, _) = ProfessionInfo.GetDefaults(ClientVersion.CV_7000);

        skills.GetLength(0).Should().Be(4);
        skills.GetLength(1).Should().Be(2);
    }

    [Fact]
    public void GetDefaults_Returns3Stats()
    {
        var (_, stats) = ProfessionInfo.GetDefaults(ClientVersion.CV_7000);

        stats.Should().HaveCount(3);
    }

    [Fact]
    public void Constructor_InitializesFromClientVersion()
    {
        var info = new ProfessionInfo(ClientVersion.CV_70160);

        info.SkillDefVal.Should().NotBeNull();
        info.StatsVal.Should().NotBeNull();
        info.SkillDefVal[0, 1].Should().Be(30);
        info.StatsVal[0].Should().Be(60);
    }

    [Fact]
    public void ProfType_EnumValues()
    {
        ProfessionLoader.PROF_TYPE.NO_PROF.Should().Be(0);
        ((int)ProfessionLoader.PROF_TYPE.CATEGORY).Should().Be(1);
        ((int)ProfessionLoader.PROF_TYPE.PROFESSION).Should().Be(2);
    }
}
