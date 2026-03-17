using ClassicUO.Configuration;
using FluentAssertions;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Client.Tests.Configuration
{
    public class ProfileTests
    {
        [Fact]
        public void Profile_IsClass()
        {
            var profile = new Profile();

            profile.Should().NotBeNull();
            typeof(Profile).IsClass.Should().BeTrue();
        }

        [Fact]
        public void DefaultProfile_SoundDefaults()
        {
            var profile = new Profile();

            profile.EnableSound.Should().BeTrue();
            profile.SoundVolume.Should().Be(100);
            profile.EnableMusic.Should().BeTrue();
            profile.MusicVolume.Should().Be(100);
            profile.EnableFootstepsSound.Should().BeTrue();
            profile.EnableCombatMusic.Should().BeTrue();
            profile.ReproduceSoundsInBackground.Should().BeFalse();
        }

        [Fact]
        public void DefaultProfile_FontAndSpeechDefaults()
        {
            var profile = new Profile();

            profile.ChatFont.Should().Be(1);
            profile.SpeechDelay.Should().Be(100);
            profile.ScaleSpeechDelay.Should().BeTrue();
            profile.SaveJournalToFile.Should().BeTrue();
            profile.ForceUnicodeJournal.Should().BeFalse();
            profile.IgnoreAllianceMessages.Should().BeFalse();
            profile.IgnoreGuildMessages.Should().BeFalse();
        }

        [Fact]
        public void DefaultProfile_HueDefaults()
        {
            var profile = new Profile();

            profile.SpeechHue.Should().Be(0x02B2);
            profile.WhisperHue.Should().Be(0x0033);
            profile.EmoteHue.Should().Be(0x0021);
            profile.YellHue.Should().Be(0x0021);
            profile.PartyMessageHue.Should().Be(0x0044);
            profile.InnocentHue.Should().Be(0x005A);
            profile.MurdererHue.Should().Be(0x0023);
        }

        [Fact]
        public void DefaultProfile_VisualDefaults()
        {
            var profile = new Profile();

            profile.EnabledCriminalActionQuery.Should().BeTrue();
            profile.DrawRoofs.Should().BeTrue();
            profile.TreeToStumps.Should().BeFalse();
            profile.DefaultScale.Should().Be(1.0f);
            profile.EnableDeathScreen.Should().BeTrue();
            profile.EnableBlackWhiteEffect.Should().BeTrue();
            profile.VendorGumpHeight.Should().Be(60);
        }

        [Fact]
        public void DefaultProfile_TooltipDefaults()
        {
            var profile = new Profile();

            profile.UseTooltip.Should().BeTrue();
            profile.TooltipTextHue.Should().Be(0xFFFF);
            profile.TooltipDelayBeforeDisplay.Should().Be(250);
            profile.TooltipDisplayZoom.Should().Be(100);
            profile.TooltipBackgroundOpacity.Should().Be(70);
            profile.TooltipFont.Should().Be(1);
        }

        [Fact]
        public void DefaultProfile_MovementDefaults()
        {
            var profile = new Profile();

            profile.SmoothMovements.Should().BeTrue();
            profile.HoldDownKeyTab.Should().BeTrue();
            profile.AlwaysRun.Should().BeFalse();
            profile.EnablePathfind.Should().BeFalse();
            profile.HoldShiftForContext.Should().BeFalse();
            profile.HoldShiftToSplitStack.Should().BeFalse();
        }

        [Fact]
        public void DefaultProfile_GeneralDefaults()
        {
            var profile = new Profile();

            profile.WindowClientBounds.Should().Be(new Point(600, 480));
            profile.ContainerDefaultPosition.Should().Be(new Point(24, 24));
            profile.GameWindowPosition.Should().Be(new Point(10, 10));
            profile.GameWindowSize.Should().Be(new Point(600, 480));
            profile.GameWindowLock.Should().BeFalse();
            profile.WindowBorderless.Should().BeFalse();
            profile.UseColoredLights.Should().BeTrue();
            profile.UseObjectsFading.Should().BeTrue();
        }

        [Fact]
        public void DefaultProfile_JsonIgnoredProperties_AreNull()
        {
            var profile = new Profile();

            profile.Username.Should().BeNull();
            profile.ServerName.Should().BeNull();
            profile.CharacterName.Should().BeNull();
        }

        [Fact]
        public void Username_GetSet_RoundTrips()
        {
            var profile = new Profile();

            profile.Username = "testuser";

            profile.Username.Should().Be("testuser");
        }

        [Fact]
        public void SoundVolume_GetSet_RoundTrips()
        {
            var profile = new Profile();

            profile.SoundVolume = 50;

            profile.SoundVolume.Should().Be(50);
        }

        [Fact]
        public void SpeechHue_GetSet_RoundTrips()
        {
            var profile = new Profile();

            profile.SpeechHue = 0x1234;

            profile.SpeechHue.Should().Be(0x1234);
        }

        [Fact]
        public void DefaultScale_GetSet_RoundTrips()
        {
            var profile = new Profile();

            profile.DefaultScale = 2.5f;

            profile.DefaultScale.Should().Be(2.5f);
        }

        [Fact]
        public void WindowClientBounds_GetSet_RoundTrips()
        {
            var profile = new Profile();

            profile.WindowClientBounds = new Point(800, 600);

            profile.WindowClientBounds.Should().Be(new Point(800, 600));
        }

        [Fact]
        public void SpellDisplayFormat_HasExpectedDefault()
        {
            var profile = new Profile();

            profile.SpellDisplayFormat.Should().Be("{power} [{spell}]");
        }
    }
}
