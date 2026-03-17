using ClassicUO.Configuration;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ClassicUO.Client.Tests.Configuration
{
    public class ProfileManagerTests
    {
        private readonly IProfileProvider _profileProvider;

        public ProfileManagerTests()
        {
            var settings = Substitute.For<ISettingsProvider>();
            _profileProvider = new ProfileProviderInstance(settings);
        }

        [Fact]
        public void GlobalProfile_IsInitiallyNull()
        {
            _profileProvider.GlobalProfile.Should().BeNull();
        }

        [Fact]
        public void CurrentProfile_IsInitiallyNull()
        {
            _profileProvider.CurrentProfile.Should().BeNull();
        }

        [Fact]
        public void ProfilePath_IsInitiallyNull()
        {
            _profileProvider.ProfilePath.Should().BeNull();
        }

        [Fact]
        public void UnLoadProfile_SetsGlobalProfileToNull()
        {
            _profileProvider.UnLoadProfile();

            _profileProvider.GlobalProfile.Should().BeNull();
        }

        [Fact]
        public void UnLoadProfile_SetsCurrentProfileToNull()
        {
            _profileProvider.UnLoadProfile();

            _profileProvider.CurrentProfile.Should().BeNull();
        }
    }
}
