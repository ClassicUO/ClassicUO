// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Events;
using ClassicUO.Network;

namespace ClassicUO.Game.Entities.Players
{
    /// <summary>
    /// Handles the post-<see cref="EventSink.PlayerEnteredWorld"/> housekeeping
    /// (light-level, music volume, version-gated handshake packets, initial
    /// click + skill request, dead-player season override, plugin notifications).
    /// Replaces the legacy <c>World.Subscribers.EnterWorld</c> partial.
    /// </summary>
    internal sealed class EnterWorldExtrasHandler : IEventListener
    {
        private readonly World _world;

        public EnterWorldExtrasHandler(World world) => _world = world;

        public void Subscribe() => EventSink.PlayerEnteredWorld += OnPlayerEnteredWorldExtras;

        public void Unsubscribe() => EventSink.PlayerEnteredWorld -= OnPlayerEnteredWorldExtras;

        private void OnPlayerEnteredWorldExtras(PlayerEnteredWorldArgs e)
        {
            if (_world.Player == null) return;

            if (
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.UseCustomLightLevel
            )
            {
                _world.Light.Overall =
                    ProfileManager.CurrentProfile.LightLevelType == 1
                        ? Math.Min(_world.Light.Overall, ProfileManager.CurrentProfile.LightLevel)
                        : ProfileManager.CurrentProfile.LightLevel;
            }

            Client.Game.Audio.UpdateCurrentMusicVolume();

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_200)
            {
                if (ProfileManager.CurrentProfile != null)
                {
                    NetClient.Socket.Send_GameWindowSize(
                        (uint)Client.Game.Scene.Camera.Bounds.Width,
                        (uint)Client.Game.Scene.Camera.Bounds.Height
                    );
                }

                NetClient.Socket.Send_Language(Settings.GlobalSettings.Language);
            }

            NetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);

            GameActions.SingleClick(_world, _world.Player);
            NetClient.Socket.Send_SkillsRequest(_world.Player.Serial);

            if (_world.Player.IsDead)
            {
                _world.ChangeSeason(Seasons.Season.Desolation, 42);
            }

            if (
                Client.Game.UO.Version >= Utility.ClientVersion.CV_70796
                && ProfileManager.CurrentProfile != null
            )
            {
                NetClient.Socket.Send_ShowPublicHouseContent(
                    ProfileManager.CurrentProfile.ShowHouseContent
                );
            }

            NetClient.Socket.Send_ToPlugins_AllSkills();
            NetClient.Socket.Send_ToPlugins_AllSpells();
        }
    }
}
