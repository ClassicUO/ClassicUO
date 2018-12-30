using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using ClassicUO.Game;
using ClassicUO.IO;

using ClassicUO_API;

using SDL2;

namespace ClassicUO.Network
{
    internal static class Plugin
    {
        private static OnPacketSendRecv _recv, _send;
        private static OnGetPacketLength _getPacketLength;
        private static OnGetPlayerPosition _getPlayerPosition;
        private static OnCastSpell _castSpell;
        private static OnGetStaticImage _getStaticImage;

        private static readonly List<PluginHeader> _headers = new List<PluginHeader>();


        public static void Load()
        {
            _recv = OnPluginRecv;
            _send = OnPluginSend;
            _getPacketLength = PacketsTable.GetPacketLength;
            _getPlayerPosition = GetPlayerPosition;
            _castSpell = GameActions.CastSpell;
            _getStaticImage = GetStaticImage;

            PluginHeader header = new PluginHeader
            {
                ClientPath = FileManager.UoFolderPath,
                ClientVersion = (int) FileManager.ClientVersion,
                Recv = Marshal.GetFunctionPointerForDelegate(_recv),
                Send = Marshal.GetFunctionPointerForDelegate(_send),
                GetPacketLength = Marshal.GetFunctionPointerForDelegate(_getPacketLength),
                GetPlayerPosition = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition),
                CastSpell = Marshal.GetFunctionPointerForDelegate(_castSpell),
                GetStaticImage = Marshal.GetFunctionPointerForDelegate(_getStaticImage),
                HWND = SDL.SDL_GL_GetCurrentWindow()
            };

            object[] args = new object[1] {header};

            string path = Path.Combine(Engine.ExePath, "Data", "Plugins");

            DirectoryInfo directory = new DirectoryInfo(path);

            if (!directory.Exists)
                directory.Create();

            foreach (FileInfo file in directory.GetFiles("*.dll"))
            {
                try
                {
                    Assembly.LoadFile(file.FullName)
                            .GetTypes()
                            .Select(f =>
                             {
                                 MethodInfo b = f.GetMethod("Install", BindingFlags.Static | BindingFlags.Public);

                                 return b?.GetParameters()
                                          .FirstOrDefault(a => a.ParameterType.GetElementType() == typeof(PluginHeader) && a.ParameterType.IsByRef) != null
                                            ? b
                                            : null;
                             })
                            .FirstOrDefault(s => s != null)?
                            .Invoke(null, args);
                }
                catch
                {
                }
            }

            header = (PluginHeader) args[0];
            _headers.Add(header);

            header.OnInitialize?.Invoke();
        }


        private static void GetStaticImage(ushort g, ref ArtInfo info)
        {
            FileManager.Art.TryGetEntryInfo(g, out long address, out long size, out long compressedsize);
            info.Address = address;
            info.Size = size;
            info.CompressedSize = compressedsize;
        }

        private static bool GetPlayerPosition(out int x, out int y, out int z)
        {
            if (World.Player != null)
            {
                x = World.Player.X;
                y = World.Player.Y;
                z = World.Player.Z;

                return true;
            }

            x = y = z = 0;

            return false;
        }


        internal static bool ProcessRecvPacket(byte[] data, int length)
        {
            bool result = true;

            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];

                if (header.OnRecv != null && !header.OnRecv(data, length))
                    result = false;
            }

            return result;
        }


        internal static void OnClosing()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnClientClosing?.Invoke();

                _headers.RemoveAt(i--);
            }
        }

        internal static void OnFocusGained()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnFocusGained?.Invoke();
            }
        }

        internal static void OnFocusLost()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnFocusLost?.Invoke();
            }
        }


        internal static void OnConnected()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnConnected?.Invoke();
            }
        }

        internal static void OnDisconnected()
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                var header = _headers[i];
                header.OnDisconnected?.Invoke();
            }
        }

        internal static bool ProcessSendPacket(byte[] data, int length)
        {
            bool result = true;

            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];

                if (header.OnSend != null && !header.OnSend(data, length))
                    result = false;
            }

            return result;
        }

        internal static bool ProcessHotkeys(int key, int mod, bool ispressed)
        {
            bool result = true;

            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];

                if (header.OnHotkeyPressed != null && !header.OnHotkeyPressed(key, mod, ispressed))
                    result = false;
            }

            return result;
        }

        internal static void ProcessMouse(int button, int wheel)
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnMouse?.Invoke(button, wheel);
            }
        }

        internal static void UpdatePlayerPosition(int x, int y, int z)
        {
            for (int i = 0; i < _headers.Count; i++)
            {
                PluginHeader header = _headers[i];
                header.OnPlayerPositionChanged?.Invoke(x, y, z);
            }
        }

        private static bool OnPluginRecv(byte[] data, int length)
        {
            Packet p = new Packet(data, length);
            NetClient.EnqueuePacketFromPlugin(p);
            return true;
        }

        private static bool OnPluginSend(byte[] data, int length)
        {
            if (NetClient.LoginSocket.IsDisposed && NetClient.Socket.IsConnected)
            {
                NetClient.Socket.Send(data);
            }
            else if (NetClient.Socket.IsDisposed && NetClient.LoginSocket.IsConnected)
            {
                NetClient.LoginSocket.Send(data);
            }

            return true;
        }
    }
}