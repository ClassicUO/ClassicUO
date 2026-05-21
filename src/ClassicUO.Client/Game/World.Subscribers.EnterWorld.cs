// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Game.Events;
using ClassicUO.Network;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeEnterWorldExtras()
        {
            EventSink.PlayerEnteredWorld += OnPlayerEnteredWorldExtras;
        }

        private void UnsubscribeEnterWorldExtras()
        {
            EventSink.PlayerEnteredWorld -= OnPlayerEnteredWorldExtras;
        }

        private void OnPlayerEnteredWorldExtras(PlayerEnteredWorldArgs e)
        {
            if (Player == null) return;

            if (
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.UseCustomLightLevel
            )
            {
                Light.Overall =
                    ProfileManager.CurrentProfile.LightLevelType == 1
                        ? Math.Min(Light.Overall, ProfileManager.CurrentProfile.LightLevel)
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

            GameActions.SingleClick(this, Player);
            NetClient.Socket.Send_SkillsRequest(Player.Serial);

            if (Player.IsDead)
            {
                ChangeSeason(Managers.Season.Desolation, 42);
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
