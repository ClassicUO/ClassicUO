using ClassicUO.Client.Tests;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ClassicUO.Client.Tests.Integration
{
    public class GameContextTests
    {
        [Fact]
        public void CreateTestWorld_ReturnsNonNull_WithProfile()
        {
            var world = TestHelpers.CreateTestWorld();

            world.Should().NotBeNull();
            world.Profile.Should().NotBeNull();
            world.Profile.CurrentProfile.Should().NotBeNull();
        }

        [Fact]
        public void CustomContext_WithMockedServices_Works()
        {
            var ui = Substitute.For<IUIManager>();
            var profile = Substitute.For<IProfileProvider>();
            profile.CurrentProfile.Returns(new Profile());

            var context = TestHelpers.CreateTestContext(ui: ui, profile: profile);

            context.UI.Should().BeSameAs(ui);
            context.Profile.Should().BeSameAs(profile);
        }

        [Fact]
        public void WorldContext_IsNotNull()
        {
            var world = TestHelpers.CreateTestWorld();

            world.Context.Should().NotBeNull();
        }

        [Fact]
        public void WorldManagers_CanAccessContextThroughWorld()
        {
            var world = TestHelpers.CreateTestWorld();

            // Verify that creating managers through world works
            // and they can operate with the context
            world.TargetManager.Should().NotBeNull();
            world.ChatManager.Should().NotBeNull();
            world.Party.Should().NotBeNull();
            world.OPL.Should().NotBeNull();
            world.CorpseManager.Should().NotBeNull();
            world.Journal.Should().NotBeNull();
        }

        [Fact]
        public void TwoWorlds_WithDifferentContexts_AreIndependent()
        {
            var world1 = TestHelpers.CreateTestWorld();
            var world2 = TestHelpers.CreateTestWorld();

            // Add entities to world1
            world1.GetOrCreateMobile(0x0001);
            world1.GetOrCreateItem(0x40000001);

            // world2 should not have them
            world2.Contains(0x0001).Should().BeFalse();
            world2.Contains(0x40000001).Should().BeFalse();
        }

        [Fact]
        public void TwoWorlds_ManagersAreIndependent()
        {
            var world1 = TestHelpers.CreateTestWorld();
            var world2 = TestHelpers.CreateTestWorld();

            world1.OPL.Add(0x0001, 1u, "Sword", "Data", 0);
            world1.ChatManager.AddChannel("General", false);
            world1.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            // world2 managers should be unaffected
            world2.OPL.Contains(0x0001).Should().BeFalse();
            world2.ChatManager.Channels.Should().BeEmpty();
            world2.TargetManager.IsTargeting.Should().BeFalse();
        }

        [Fact]
        public void ProfileProvider_CurrentProfile_ReturnsProfile()
        {
            var profile = TestHelpers.CreateTestProfile();

            profile.CurrentProfile.Should().NotBeNull();
            profile.CurrentProfile.Should().BeOfType<Profile>();
        }

        [Fact]
        public void WorldCreatedWithContext_AllManagersOperational()
        {
            var world = TestHelpers.CreateTestWorld();

            // Test that all managers work end-to-end through the world
            var mobile = world.GetOrCreateMobile(0x0001);
            mobile.Name = "Test";

            world.OPL.Add(mobile.Serial, 1u, "Test Mobile", "HP: 100", 0);
            world.CorpseManager.Add(0x40000001, mobile.Serial, Direction.North, false);
            world.Party.Leader = mobile.Serial;
            world.Party.Members[0] = new PartyMember(world, mobile.Serial);
            world.ChatManager.AddChannel("General", false);
            world.TargetManager.SetTargeting(CursorTarget.Object, 1u, TargetType.Neutral);

            // Verify all state is coherent
            world.Contains(mobile.Serial).Should().BeTrue();
            world.OPL.Contains(mobile.Serial).Should().BeTrue();
            world.CorpseManager.Exists(0x40000001, 0u).Should().BeTrue();
            world.Party.Contains(mobile.Serial).Should().BeTrue();
            world.ChatManager.Channels.Should().ContainKey("General");
            world.TargetManager.IsTargeting.Should().BeTrue();
        }
    }
}
