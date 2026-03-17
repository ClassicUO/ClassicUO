// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Network;

namespace ClassicUO.Game
{
    /// <summary>
    /// Ambient context that acts as a scoped service locator for all injectable
    /// dependencies. Production code receives the real delegation wrappers;
    /// test code can substitute any service with a mock.
    /// </summary>
    internal class GameContext
    {
        public IGameController Game { get; }
        public IUIManager UI { get; }
        public IProfileProvider Profile { get; }
        public ISettingsProvider Settings { get; }
        public INetworkClient Network { get; }

        public GameContext(
            IGameController game,
            IUIManager ui,
            IProfileProvider profile,
            ISettingsProvider settings,
            INetworkClient network)
        {
            Game = game;
            UI = ui;
            Profile = profile;
            Settings = settings;
            Network = network;
        }

        /// <summary>
        /// Creates a default production GameContext using delegation wrappers
        /// over the existing static singletons.
        /// </summary>
        public static GameContext CreateDefault()
        {
            var game = Client.Game;
            var settings = Configuration.Settings.GlobalSettings;
            var profile = new ProfileProviderInstance(settings);
            return new GameContext(
                game,
                new UIManagerInstance(game, profile),
                profile,
                settings,
                new NetClient()
            );
        }
    }
}
