// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterLoginHandlers(PacketHandlers h)
        {
            h.Add(0x1B, EnterWorld);
            h.Add(0x55, LoginComplete);
            h.Add(0xBD, ClientVersion);
            h.Add(0xBE, AssistVersion);
            h.Add(0xD1, Logout);
            h.Add(0xE3, KREncryptionResponse);

            h.Add(0xA8, ServerListReceived);
            h.Add(0x8C, ReceiveServerRelay);
            h.Add(0x86, UpdateCharacterList);
            h.Add(0xA9, ReceiveCharacterList);
            h.Add(0xFD, LoginDelay);
            h.Add(0x82, ReceiveLoginRejection);
            h.Add(0x85, ReceiveLoginRejection);
            h.Add(0x53, ReceiveLoginRejection);
        }

        private static void EnterWorld(World world, ref StackDataReader p)
        {
            uint serial = p.ReadUInt32BE();

            world.CreatePlayer(serial);

            p.Skip(4);
            world.Player.Graphic = p.ReadUInt16BE();
            world.Player.CheckGraphicChange();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            sbyte z = (sbyte)p.ReadUInt16BE();

            if (world.Map == null)
            {
                world.MapIndex = 0;
            }

            world.Player.SetInWorldTile(x, y, z);
            world.Player.Direction = (Direction)(p.ReadUInt8() & 0x7);
            world.RangeSize.X = x;
            world.RangeSize.Y = y;

            if (
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.UseCustomLightLevel
            )
            {
                world.Light.Overall =
                    ProfileManager.CurrentProfile.LightLevelType == 1
                        ? Math.Min(world.Light.Overall, ProfileManager.CurrentProfile.LightLevel)
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

            GameActions.SingleClick(world, world.Player);
            NetClient.Socket.Send_SkillsRequest(world.Player.Serial);

            if (world.Player.IsDead)
            {
                world.ChangeSeason(Game.Managers.Season.Desolation, 42);
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

        private static void LoginComplete(World world, ref StackDataReader p)
        {
            if (world.Player != null && Client.Game.Scene is LoginScene)
            {
                var scene = new GameScene(world);
                Client.Game.SetScene(scene);

                //GameActions.OpenPaperdoll(world.Player);
                GameActions.RequestMobileStatus(world, world.Player);
                NetClient.Socket.Send_OpenChat("");

                NetClient.Socket.Send_SkillsRequest(world.Player);
                scene.DoubleClickDelayed(world.Player);

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_306E)
                {
                    NetClient.Socket.Send_ClientType();
                }

                if (Client.Game.UO.Version >= Utility.ClientVersion.CV_305D)
                {
                    NetClient.Socket.Send_ClientViewRange(world.ClientViewRange);
                }

                List<Gump> gumps = ProfileManager.CurrentProfile.ReadGumps(
                    world,
                    ProfileManager.ProfilePath
                );

                if (gumps != null)
                {
                    foreach (Gump gump in gumps)
                    {
                        UIManager.Add(gump);
                    }
                }
            }
        }

        private static void ClientVersion(World world, ref StackDataReader p)
        {
            NetClient.Socket.Send_ClientVersion(Settings.GlobalSettings.ClientVersion);
        }

        private static void AssistVersion(World world, ref StackDataReader p)
        {
            //uint version = p.ReadUInt32BE();

            //string[] parts = Service.GetByLocalSerial<Settings>().ClientVersion.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            //byte[] clientVersionBuffer =
            //    {byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3])};

            //NetClient.Socket.Send(new PAssistVersion(clientVersionBuffer, version));
        }

        private static void Logout(World world, ref StackDataReader p)
        {
            // http://docs.polserver.com/packets/index.php?Packet=0xD1

            if (
                Client.Game.GetScene<GameScene>().DisconnectionRequested
                && (
                    world.ClientFeatures.Flags
                    & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON
                ) != 0
            )
            {
                if (p.ReadBool())
                {
                    // client can disconnect
                    NetClient.Socket.Disconnect();
                    Client.Game.SetScene(new LoginScene(world));
                }
                else
                {
                    Log.Warn("0x1D - client asked to disconnect but server answered 'NO!'");
                }
            }
        }

        private static void KREncryptionResponse(World world, ref StackDataReader p) { }

        private static void ServerListReceived(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ServerListReceived(ref p);
            }
        }

        private static void ReceiveServerRelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleRelayServerPacket(ref p);
            }
        }

        private static void UpdateCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.UpdateCharacterList(ref p);
            }
        }

        private static void ReceiveCharacterList(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.ReceiveCharacterList(ref p);
            }
        }

        private static void LoginDelay(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleLoginDelayPacket(ref p);
            }
        }

        private static void ReceiveLoginRejection(World world, ref StackDataReader p)
        {
            if (world.InGame)
            {
                return;
            }

            LoginScene scene = Client.Game.GetScene<LoginScene>();

            if (scene != null)
            {
                scene.HandleErrorCode(ref p);
            }
        }
    }
}
