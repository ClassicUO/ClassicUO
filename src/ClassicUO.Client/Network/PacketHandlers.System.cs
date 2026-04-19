// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.IO;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterSystemHandlers(PacketHandlers h)
        {
            h.Add(0x32, Unknown_0x32);
            h.Add(0x73, Ping);
            h.Add(0xB7, Help);
            h.Add(0xB9, EnableLockedFeatures);
            h.Add(0xC6, InvalidMapEnable);
            h.Add(0xC8, ClientViewRange);
            h.Add(0xCA, GetUserServerPingGodClientR);
            h.Add(0xCB, GlobalQueCount);
            h.Add(0xD0, ConfigurationFileR);
            h.Add(0xD7, GenericAOSCommandsR);
            h.Add(0xDB, CharacterTransferLog);
            h.Add(0xDC, OPLInfo);
            h.Add(0xF0, KrriosClientSpecial);
            h.Add(0xF1, FreeshardListR);
        }

        private static void Unknown_0x32(World world, ref StackDataReader p) { }

        private static void Ping(World world, ref StackDataReader p)
        {
            NetClient.Socket.Statistics.PingReceived(p.ReadUInt8());
        }

        private static void Help(World world, ref StackDataReader p) { }

        private static void InvalidMapEnable(World world, ref StackDataReader p) { }

        private static void GetUserServerPingGodClientR(World world, ref StackDataReader p) { }

        private static void GlobalQueCount(World world, ref StackDataReader p) { }

        private static void ConfigurationFileR(World world, ref StackDataReader p) { }

        private static void GenericAOSCommandsR(World world, ref StackDataReader p) { }

        private static void CharacterTransferLog(World world, ref StackDataReader p) { }

        private static void FreeshardListR(World world, ref StackDataReader p) { }

        private static void ClientViewRange(World world, ref StackDataReader p)
        {
            world.ClientViewRange = p.ReadUInt8();
        }

        private static void EnableLockedFeatures(World world, ref StackDataReader p)
        {
            LockedFeatureFlags flags = 0;

            if (Client.Game.UO.Version >= Utility.ClientVersion.CV_60142)
            {
                flags = (LockedFeatureFlags)p.ReadUInt32BE();
            }
            else
            {
                flags = (LockedFeatureFlags)p.ReadUInt16BE();
            }

            world.ClientLockedFeatures.SetFlags(flags);

            world.ChatManager.ChatIsEnabled = world.ClientLockedFeatures.Flags.HasFlag(
                LockedFeatureFlags.T2A
            )
                ? ChatStatus.Enabled
                : 0;

            BodyConvFlags bcFlags = 0;
            if (flags.HasFlag(LockedFeatureFlags.UOR))
                bcFlags |= BodyConvFlags.Anim1 | BodyConvFlags.Anim2;
            if (flags.HasFlag(LockedFeatureFlags.LBR))
                bcFlags |= BodyConvFlags.Anim1;
            if (flags.HasFlag(LockedFeatureFlags.AOS))
                bcFlags |= BodyConvFlags.Anim2;
            if (flags.HasFlag(LockedFeatureFlags.SE))
                bcFlags |= BodyConvFlags.Anim3;
            if (flags.HasFlag(LockedFeatureFlags.ML))
                bcFlags |= BodyConvFlags.Anim4;

            Client.Game.UO.Animations.UpdateAnimationTable(bcFlags);
        }

        private static void OPLInfo(World world, ref StackDataReader p)
        {
            if (world.ClientFeatures.TooltipsEnabled)
            {
                uint serial = p.ReadUInt32BE();
                uint revision = p.ReadUInt32BE();

                if (!world.OPL.IsRevisionEquals(serial, revision))
                {
                    AddMegaClilocRequest(serial);
                }
            }
        }

        private static void KrriosClientSpecial(World world, ref StackDataReader p)
        {
            byte type = p.ReadUInt8();

            switch (type)
            {
                case 0x00: // accepted
                    Log.Trace("Krrios special packet accepted");
                    world.WMapManager.SetACKReceived();
                    world.WMapManager.SetEnable(true);

                    break;

                case 0x01: // custom party info
                case 0x02: // guild track info
                    bool locations = type == 0x01 || p.ReadBool();

                    uint serial;

                    while ((serial = p.ReadUInt32BE()) != 0)
                    {
                        if (locations)
                        {
                            ushort x = p.ReadUInt16BE();
                            ushort y = p.ReadUInt16BE();
                            byte map = p.ReadUInt8();
                            int hits = type == 1 ? 0 : p.ReadUInt8();

                            world.WMapManager.AddOrUpdate(
                                serial,
                                x,
                                y,
                                hits,
                                map,
                                type == 0x02,
                                null,
                                true
                            );
                        }
                    }

                    world.WMapManager.RemoveUnupdatedWEntity();

                    break;

                case 0x03: // runebook contents
                    break;

                case 0x04: // guardline data
                    break;

                case 0xF0:
                    break;

                case 0xFE:

                    Client.Game.EnqueueAction(5000, () =>
                    {
                        Log.Info("Razor ACK sent");
                        NetClient.Socket.Send_RazorACK();
                    });

                    break;
            }
        }
    }
}
