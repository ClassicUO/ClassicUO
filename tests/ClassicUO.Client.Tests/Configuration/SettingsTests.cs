using System.Text.Json;
using ClassicUO.Configuration;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Configuration
{
    public class SettingsTests
    {
        [Fact]
        public void DefaultSettings_HasExpectedDefaults()
        {
            var settings = new Settings();

            settings.FPS.Should().Be(60);
            settings.Port.Should().Be(2593);
            settings.IP.Should().Be("127.0.0.1");
        }

        [Fact]
        public void GlobalSettings_ReturnsInstance()
        {
            Settings.GlobalSettings.Should().NotBeNull();
            Settings.GlobalSettings.Should().BeOfType<Settings>();
        }

        [Fact]
        public void SETTINGS_FILENAME_IsSettingsJson()
        {
            Settings.SETTINGS_FILENAME.Should().Be("settings.json");
        }

        [Fact]
        public void AllProperties_HaveCorrectDefaultValues()
        {
            var settings = new Settings();

            settings.Username.Should().BeEmpty();
            settings.Password.Should().BeEmpty();
            settings.IP.Should().Be("127.0.0.1");
            settings.Port.Should().Be(2593);
            settings.IgnoreRelayIp.Should().BeFalse();
            settings.UltimaOnlineDirectory.Should().BeEmpty();
            settings.ProfilesPath.Should().BeEmpty();
            settings.ClientVersion.Should().BeEmpty();
            settings.Language.Should().BeEmpty();
            settings.LastServerNum.Should().Be(1);
            settings.LastServerName.Should().BeEmpty();
            settings.FPS.Should().Be(60);
            settings.ScreenScale.Should().Be(1f);
            settings.WindowPosition.Should().BeNull();
            settings.WindowSize.Should().BeNull();
            settings.IsWindowMaximized.Should().BeTrue();
            settings.SaveAccount.Should().BeFalse();
            settings.AutoLogin.Should().BeFalse();
            settings.Reconnect.Should().BeFalse();
            settings.ReconnectTime.Should().Be(1);
            settings.LoginMusic.Should().BeTrue();
            settings.LoginMusicVolume.Should().Be(70);
            settings.FixedTimeStep.Should().BeTrue();
            settings.RunMouseInASeparateThread.Should().BeTrue();
            settings.ForceDriver.Should().Be(0);
            settings.UseVerdata.Should().BeFalse();
            settings.MapsLayouts.Should().BeNull();
            settings.Encryption.Should().Be(0);
            settings.Plugins.Should().ContainSingle().Which.Should().Be(@"./Assistant/Razor.dll");
            settings.OverrideFile.Should().BeNull();
        }

        [Fact]
        public void FPS_GetSet_RoundTrips()
        {
            var settings = new Settings();

            settings.FPS = 120;

            settings.FPS.Should().Be(120);
        }

        [Fact]
        public void IP_GetSet_RoundTrips()
        {
            var settings = new Settings();

            settings.IP = "192.168.1.1";

            settings.IP.Should().Be("192.168.1.1");
        }

        [Fact]
        public void Port_GetSet_RoundTrips()
        {
            var settings = new Settings();

            settings.Port = 9999;

            settings.Port.Should().Be(9999);
        }

        [Fact]
        public void UltimaOnlineDirectory_GetSet_RoundTrips()
        {
            var settings = new Settings();

            settings.UltimaOnlineDirectory = "/path/to/uo";

            settings.UltimaOnlineDirectory.Should().Be("/path/to/uo");
        }

        [Fact]
        public void ClientVersion_GetSet_RoundTrips()
        {
            var settings = new Settings();

            settings.ClientVersion = "7.0.95.0";

            settings.ClientVersion.Should().Be("7.0.95.0");
        }

        [Fact]
        public void JsonSerialization_RoundTrip_PreservesProperties()
        {
            var original = new Settings
            {
                IP = "10.0.0.1",
                Port = 3000,
                FPS = 30,
                Username = "testuser",
                UltimaOnlineDirectory = "/uo/dir",
                ClientVersion = "7.0.90.0",
                LoginMusic = false,
                LoginMusicVolume = 50,
                LastServerNum = 5,
                ScreenScale = 2.0f
            };

            var json = JsonSerializer.Serialize(original, SettingsJsonContext.RealDefault.Settings);
            var deserialized = JsonSerializer.Deserialize(json, SettingsJsonContext.RealDefault.Settings);

            deserialized.Should().NotBeNull();
            deserialized.IP.Should().Be("10.0.0.1");
            deserialized.Port.Should().Be(3000);
            deserialized.FPS.Should().Be(30);
            deserialized.Username.Should().Be("testuser");
            deserialized.UltimaOnlineDirectory.Should().Be("/uo/dir");
            deserialized.ClientVersion.Should().Be("7.0.90.0");
            deserialized.LoginMusic.Should().BeFalse();
            deserialized.LoginMusicVolume.Should().Be(50);
            deserialized.LastServerNum.Should().Be(5);
            deserialized.ScreenScale.Should().Be(2.0f);
        }

        [Fact]
        public void JsonSerialization_DefaultSettings_RoundTripsCorrectly()
        {
            var original = new Settings();

            var json = JsonSerializer.Serialize(original, SettingsJsonContext.RealDefault.Settings);
            var deserialized = JsonSerializer.Deserialize(json, SettingsJsonContext.RealDefault.Settings);

            deserialized.Should().NotBeNull();
            deserialized.FPS.Should().Be(original.FPS);
            deserialized.IP.Should().Be(original.IP);
            deserialized.Port.Should().Be(original.Port);
            deserialized.IsWindowMaximized.Should().Be(original.IsWindowMaximized);
            deserialized.LoginMusic.Should().Be(original.LoginMusic);
            deserialized.LoginMusicVolume.Should().Be(original.LoginMusicVolume);
        }
    }
}
