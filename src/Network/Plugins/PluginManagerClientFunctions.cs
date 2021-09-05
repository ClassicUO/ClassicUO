using System;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;


namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
#if NETFRAMEWORK
       
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void dSocketAction(IntPtr buffer, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool dMovePlayer(byte dir, bool isRunning);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void dSetGameWindowTitle(IntPtr title);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr dGetCliloc(int clilocNum, IntPtr argsPtr, bool capitalize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr dGetUltimaOnlinePath();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int dGetPacketLength(ushort packetID);


        private static readonly IntPtr _sendPacketToClientPtr = Marshal.GetFunctionPointerForDelegate<dSocketAction>(SendPacketToClient);
        private static readonly IntPtr _sendPacketToServerPtr = Marshal.GetFunctionPointerForDelegate<dSocketAction>(SendPacketToServer);
        private static readonly IntPtr _movePlayerPtr = Marshal.GetFunctionPointerForDelegate<dMovePlayer>(MovePlayer);
        private static readonly IntPtr _setGameWindowTitlePtr = Marshal.GetFunctionPointerForDelegate<dSetGameWindowTitle>(SetGameWindowTitle);
        private static readonly IntPtr _getClilocPtr = Marshal.GetFunctionPointerForDelegate<dGetCliloc>(GetCliloc);
        private static readonly IntPtr _getUltimaOnlinePathPtr = Marshal.GetFunctionPointerForDelegate<dGetUltimaOnlinePath>(GetUOFilePath);
        private static readonly IntPtr _getPacketLengthPtr = Marshal.GetFunctionPointerForDelegate<dGetPacketLength>(GetPacketLength);

#else
        private static readonly IntPtr _sendPacketToClientPtr = (IntPtr)(delegate*<IntPtr, int, void>)(&SendPacketToClient);
        private static readonly IntPtr _sendPacketToServerPtr = (IntPtr)(delegate*<IntPtr, int, void>)(&SendPacketToServer);
        private static readonly IntPtr _movePlayerPtr = (IntPtr)(delegate*<byte, bool, bool>)(&MovePlayer);
        private static readonly IntPtr _setGameWindowTitlePtr = (IntPtr)(delegate*<IntPtr, void>)(&SetGameWindowTitle);
        private static readonly IntPtr _getClilocPtr = (IntPtr)(delegate*<int, IntPtr, bool, IntPtr>)(&GetCliloc);
        private static readonly IntPtr _getUltimaOnlinePathPtr = (IntPtr)(delegate* <IntPtr>)(&GetUOFilePath);
        private static readonly IntPtr _getPacketLengthPtr = (IntPtr)(delegate*<ushort, int>)(&GetPacketLength);
#endif







        private static void SendPacketToClient(IntPtr data, int length)
        {
            if (length > 0 && data != IntPtr.Zero)
            {
                // TODO: how can we avoid to waste memory?
                byte[] buffer = new byte[length];
                Marshal.Copy(data, buffer, 0, length);

                NetClient.EnqueuePacketFromPlugin(buffer, length);
            }       
        }

        private static void SendPacketToServer(IntPtr data, int length)
        {
            if (length > 0 && data != IntPtr.Zero)
            {
                StackDataWriter writer = new StackDataWriter(new Span<byte>((void*)data, length));

                if (NetClient.LoginSocket.IsDisposed && NetClient.Socket.IsConnected)
                {
                    NetClient.Socket.Send(writer.AllocatedBuffer, writer.BytesWritten, true);
                }
                else if (NetClient.Socket.IsDisposed && NetClient.LoginSocket.IsConnected)
                {
                    NetClient.LoginSocket.Send(writer.AllocatedBuffer, writer.BytesWritten, true);
                }

                writer.Dispose();
            }
        }

        private static bool MovePlayer(byte dir, bool isRunning)
        {
            if (!isRunning && (dir & (byte) Direction.Running) != 0)
            {
                isRunning = true;
            }

            return World.Player?.Walk((Direction) dir, isRunning) ?? false;
        }

        private static void SetGameWindowTitle(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Client.Game.SetWindowTitle(Marshal.PtrToStringAnsi(ptr));
            }
        }

        private static IntPtr GetCliloc(int clilocNum, IntPtr argsPtr, bool capitalize)
        {
            string args = argsPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(argsPtr) : string.Empty;

            string clilocStr = ClilocLoader.Instance.Translate(clilocNum, args, capitalize);

            if (!string.IsNullOrEmpty(clilocStr))
            {
                return Marshal.StringToHGlobalAnsi(clilocStr);
            }

            return IntPtr.Zero;
        }

        private static IntPtr GetUOFilePath()
        {
            return Marshal.StringToHGlobalAnsi(Settings.GlobalSettings.UltimaOnlineDirectory);
        }

        private static int GetPacketLength(ushort packetID)
        {
            return PacketsTable.GetPacketLength(packetID);
        }
    }
}
