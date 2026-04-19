// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterLoginHandlers(PacketHandlers h)
        {
            h.Add(0xA8, ServerListReceived);
            h.Add(0x8C, ReceiveServerRelay);
            h.Add(0x86, UpdateCharacterList);
            h.Add(0xA9, ReceiveCharacterList);
            h.Add(0xFD, LoginDelay);
            h.Add(0x82, ReceiveLoginRejection);
            h.Add(0x85, ReceiveLoginRejection);
            h.Add(0x53, ReceiveLoginRejection);
        }

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
