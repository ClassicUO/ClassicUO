using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Utility;
using FluentAssertions;
using System.IO;
using Xunit;

namespace ClassicUO.Assets.Tests;

public class UOFileManagerTests
{
    [Fact]
    public void Constructor_SetsVersionAndBasePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir);

            mgr.Version.Should().Be(ClientVersion.CV_7000);
            mgr.BasePath.Should().Be(tempDir);

            mgr.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetUOFilePath_ReturnsPathUnderBasePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir);
            var path = mgr.GetUOFilePath("tiledata.mul");

            path.Should().Be(Path.Combine(tempDir, "tiledata.mul"));

            mgr.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void GetUOFilePath_WithOverrideMap_ReturnsOverriddenPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var overridePath = Path.Combine(tempDir, "custom", "tiledata.mul");

        try
        {
            var overrideMap = new UOFilesOverrideMap();
            overrideMap["tiledata.mul"] = overridePath; // GetUOFilePath lowercases the key

            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir, overrideMap);
            var path = mgr.GetUOFilePath("tiledata.mul");

            path.Should().Be(overridePath);

            mgr.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsUOPInstallation_FalseWhenNoMainMiscUop()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir);

            mgr.IsUOPInstallation.Should().BeFalse();

            mgr.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Constructor_CreatesAllLoaders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir);

            mgr.Animations.Should().NotBeNull();
            mgr.AnimData.Should().NotBeNull();
            mgr.Arts.Should().NotBeNull();
            mgr.Maps.Should().NotBeNull();
            mgr.Clilocs.Should().NotBeNull();
            mgr.Gumps.Should().NotBeNull();
            mgr.Fonts.Should().NotBeNull();
            mgr.Hues.Should().NotBeNull();
            mgr.TileData.Should().NotBeNull();
            mgr.Multis.Should().NotBeNull();
            mgr.Skills.Should().NotBeNull();
            mgr.Texmaps.Should().NotBeNull();
            mgr.Speeches.Should().NotBeNull();
            mgr.Lights.Should().NotBeNull();
            mgr.Sounds.Should().NotBeNull();
            mgr.MultiMaps.Should().NotBeNull();
            mgr.Verdata.Should().NotBeNull();
            mgr.Professions.Should().NotBeNull();
            mgr.TileArt.Should().NotBeNull();
            mgr.StringDictionary.Should().NotBeNull();

            mgr.Dispose();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Dispose_SetsIsDisposedOnLoaders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cuo_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var mgr = new UOFileManager(ClientVersion.CV_7000, tempDir);
            mgr.Dispose();

            mgr.Clilocs.IsDisposed.Should().BeTrue();
            mgr.Skills.IsDisposed.Should().BeTrue();
            mgr.Speeches.IsDisposed.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
