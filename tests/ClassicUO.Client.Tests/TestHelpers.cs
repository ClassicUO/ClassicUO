using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Network;
using NSubstitute;

namespace ClassicUO.Client.Tests
{
    internal static class TestHelpers
    {
        public static IProfileProvider CreateTestProfile()
        {
            var profile = Substitute.For<IProfileProvider>();
            profile.CurrentProfile.Returns(new Profile());
            return profile;
        }

        public static GameContext CreateTestContext(
            IGameController game = null,
            IUIManager ui = null,
            IProfileProvider profile = null,
            ISettingsProvider settings = null,
            INetworkClient network = null)
        {
            return new GameContext(
                game ?? Substitute.For<IGameController>(),
                ui ?? Substitute.For<IUIManager>(),
                profile ?? CreateTestProfile(),
                settings ?? Substitute.For<ISettingsProvider>(),
                network ?? Substitute.For<INetworkClient>()
            );
        }

        public static World CreateTestWorld(GameContext context = null)
        {
            return new World(context ?? CreateTestContext());
        }
    }
}
