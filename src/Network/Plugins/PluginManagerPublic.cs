using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
        public static int Initialize()
        {
            ClientFunctions clientFunc = new ClientFunctions();
            clientFunc.SendPacketToClient = _sendPacketToClientPtr;
            clientFunc.SendPacketToServer = _sendPacketToServerPtr;
            clientFunc.MovePlayer = _movePlayerPtr;
            clientFunc.SetGameWindowTitle = _setGameWindowTitlePtr;
            clientFunc.GetCliloc = _getClilocPtr;
            clientFunc.GetUltimaOnlinePath = _getUltimaOnlinePathPtr;
            clientFunc.GetPacketLength = _getPacketLengthPtr;

            return SendCommand(PluginEventType.Initialize, (IntPtr)Unsafe.AsPointer(ref clientFunc), out _);
        }

        public static int Close()
        {
            return SendCommand(PluginEventType.Close, IntPtr.Zero, out _);
        }

        public static int OnConnect()
        {
            return SendCommand(PluginEventType.Connect, IntPtr.Zero, out _);
        }

        public static int OnDisconnect()
        {
            return SendCommand(PluginEventType.Disconnect, IntPtr.Zero, out _);
        }

        public static int OnPacketSend(IntPtr buffer, ref int size, out bool filtered)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = buffer;
            packetInfo.LengthPtr = (IntPtr)Unsafe.AsPointer(ref size);

            return SendCommand(PluginEventType.PacketSend, (IntPtr)Unsafe.AsPointer(ref packetInfo), out filtered);
        }

        public static int OnPacketRecv(IntPtr buffer, ref int size, out bool filtered)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = buffer;
            packetInfo.LengthPtr = (IntPtr)Unsafe.AsPointer(ref size);

            return SendCommand(PluginEventType.PacketRecv, (IntPtr)Unsafe.AsPointer(ref packetInfo), out filtered);
        }

        public static int OnTick(uint ticks)
        {
            return SendCommand(PluginEventType.Tick, (IntPtr)Unsafe.AsPointer(ref ticks), out _);
        }

        public static int OnDraw(GraphicsDevice device)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = IntPtr.Zero;
            packetInfo.LengthPtr = IntPtr.Zero;

            int res = SendCommand(PluginEventType.Draw, (IntPtr)Unsafe.AsPointer(ref packetInfo), out _);

            if (res > 0)
            {
                if (packetInfo.Buffer != IntPtr.Zero && packetInfo.LengthPtr != IntPtr.Zero)
                {
                    HandleCmdList(device, packetInfo.Buffer, *(int*)packetInfo.LengthPtr, _resources);
                }
            }
            else
            {
                Console.WriteLine("draw returns {0}", res);
            }

            return res;
        }

        public static int OnPlayerPosition(int x, int y, int z)
        {
            PlayerPosition playerPosition = new PlayerPosition();
            playerPosition.X = x;
            playerPosition.Y = y;
            playerPosition.Z = z;

            return SendCommand(PluginEventType.SendPlayerPosition, (IntPtr)Unsafe.AsPointer(ref playerPosition), out _);
        }

    }
}
