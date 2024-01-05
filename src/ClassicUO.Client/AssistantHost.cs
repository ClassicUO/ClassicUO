using ClassicUO.Configuration;
using ClassicUO.Network;
using StructPacker;
using System;
using System.Linq;

namespace ClassicUO
{
    sealed class AssistantHost : TcpClientRpc
    {
        protected override ArraySegment<byte> OnRequest(RpcMessage msg)
        {
            if (msg.Payload.Count <= 0)
                return ArraySegment<byte>.Empty;

            var cmd = (PluginCuoProtocol)msg.Payload.Array[0];

            switch (cmd)
            {
                case PluginCuoProtocol.OnPluginRecv:
                    {
                        var req = new PluginPacketRequestResponse();
                        req.Unpack(msg.Payload.Array, msg.Payload.Offset);

                        lock (PacketHandlers.Handler)
                        {
                            PacketHandlers.Handler.Append(req.Packet, true);
                        }
                    }
                    break;

                case PluginCuoProtocol.OnPluginSend:
                    {
                        var req = new PluginPacketRequestResponse();
                        req.Unpack(msg.Payload.Array, msg.Payload.Offset);
                       
                        if (NetClient.Socket.IsConnected)
                        {
                            NetClient.Socket.Send(req.Packet, true);
                        }
                    }
                    
                    break;

                case PluginCuoProtocol.OnPacketLength:
                    {
                        var req = new PluginPacketLengthRequest();
                        req.Unpack(msg.Payload.Array, msg.Payload.Offset);

                        var resp = new PluginPacketLengthResponse()
                        {
                            Cmd = (byte)PluginCuoProtocol.OnPacketLength,
                            PacketLength = PacketsTable.GetPacketLength(req.ID)
                        };

                        using var buf = resp.PackToBuffer();

                        return new ArraySegment<byte>(buf.Data, 0, buf.Size);
                    }
            }

            return ArraySegment<byte>.Empty;
        }

        enum PluginCuoProtocol : byte
        {
            OnInitialize,
            OnTick,
            OnClosing,
            OnFocusGained,
            OnFocusLost,
            OnConnected,
            OnDisconnected,
            OnHotkey,
            OnMouse,
            OnCmdList,
            OnSdlEvent,
            OnUpdatePlayerPos,
            OnPacketIn,
            OnPacketOut,

            OnPluginRecv,
            OnPluginSend,
            OnPacketLength,
        }

        [Pack]
        internal struct PluginInitializeRequest
        {
            public byte Cmd;
            public uint ClientVersion;
            public string PluginPath;
            public string AssetsPath;
        }

        [Pack]
        internal struct PluginHotkeyRequest
        {
            public byte Cmd;
            public int Key;
            public int Mod;
            public bool IsPressed;
        }

        [Pack]
        internal struct PluginHotkeyResponse
        {
            public byte Cmd;
            public bool Allowed;
        }

        [Pack]
        internal struct PluginMouseRequest
        {
            public byte Cmd;
            public int Button;
            public int Wheel;
        }

        [Pack]
        internal struct PluginSimpleRequest
        {
            public byte Cmd;
        }

        [Pack]
        internal struct PluginPacketRequestResponse
        {
            public byte Cmd;
            public byte[] Packet;
        }

        //[Pack]
        //internal struct PluginSdlEvent
        //{
        //    public byte Cmd;
        //    public SDL2.SDL.SDL_Event
        //}

        [Pack]
        internal struct PluginPacketLengthRequest
        {
            public byte Cmd;
            public byte ID;
        }

        [Pack]
        internal struct PluginPacketLengthResponse
        {
            public byte Cmd;
            public short PacketLength;
        }


        private RpcMessage MakeSimpleRequest(PluginCuoProtocol protocol)
        {
            var req = new PluginSimpleRequest()
            {
                Cmd = (byte)protocol
            };

            using var buf = req.PackToBuffer();
            var resp = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));

            return resp;
        }

        public void PluginInitialize(string pluginPath)
        {
            if (string.IsNullOrEmpty(pluginPath))
                return;

            var req = new PluginInitializeRequest()
            {
                Cmd = (byte) PluginCuoProtocol.OnInitialize,
                ClientVersion = (uint)Client.Game.UO.Version,
                PluginPath = pluginPath,
                AssetsPath = Settings.GlobalSettings.UltimaOnlineDirectory
            };

            using var buf = req.PackToBuffer();
            var resp = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));
        }

        public void PluginTick()
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnTick);
        }

        public void PluginClosing()
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnClosing);
        }

        public void PluginFocusGained()
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnFocusGained);
        }

        public void PluginFocusLost()
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnFocusLost);
        }

        public void PluginConnected()
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnConnected);
        }

        public void PluginDisconnected() 
        {
            var resp = MakeSimpleRequest(PluginCuoProtocol.OnDisconnected);
        }

        public bool PluginHotkeys(int key, int mod, bool ispressed)
        {
            var req = new PluginHotkeyRequest()
            {
                Cmd = (byte)PluginCuoProtocol.OnHotkey,
                Key = key,
                Mod = mod,
                IsPressed = ispressed
            };

            using var buf = req.PackToBuffer();
            var respMsg = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));

            var resp = new PluginHotkeyResponse();
            resp.Unpack(respMsg.Payload.Array, respMsg.Payload.Offset);

            return resp.Allowed;
        }

        public void PluginMouse(int button, int wheel)
        {
            var req = new PluginMouseRequest()
            {
                Cmd = (byte)PluginCuoProtocol.OnHotkey,
                Button = button,
                Wheel = wheel
            };

            using var buf = req.PackToBuffer();
            var resp = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));
        }

        public void PluginDrawCmdList()
        {
            //var buf = new byte[1];
            //buf[0] = (byte)PluginCuoProtocol.OnCmdList;
            //var resp = Request(buf);
        }

        public unsafe int PluginSdlEvent(SDL2.SDL.SDL_Event* ev)
        {
            //var buf = new byte[1];
            //buf[0] = (byte)PluginCuoProtocol.OnSdlEvent;
            //var response = RequestAsync(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();

            return 0;
        }

        public void PluginUpdatePlayerPosition(int x, int y, int z)
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnUpdatePlayerPos;
            var resp = Request(buf);
        }

        public bool PluginPacketIn(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null && buffer.Count > 0)
            {
                var bufRef = buffer.ToArray(); // TODO: remove the allocation
                var req = new PluginPacketRequestResponse()
                {
                    Cmd = (byte)PluginCuoProtocol.OnPacketIn,
                    Packet = bufRef, 
                };

                using var buf = req.PackToBuffer();
                var respMsg = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));

                var resp = new PluginPacketRequestResponse();
                resp.Unpack(respMsg.Payload.Array, respMsg.Payload.Offset);

                if (resp.Packet.Length != 0)
                {
                    resp.Packet.CopyTo(buffer.Array, buffer.Offset);

                    return true;
                }
            }

            return false;
        }

        public bool PluginPacketOut(Span<byte> buffer)
        {
            if (buffer.IsEmpty) return true;

            var req = new PluginPacketRequestResponse()
            {
                Cmd = (byte)PluginCuoProtocol.OnPacketOut,
                Packet = buffer.ToArray(), // TODO: remove the allocation!
            };

            using var buf = req.PackToBuffer();
            var respMsg = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));

            var resp = new PluginPacketRequestResponse();
            resp.Unpack(respMsg.Payload.Array, respMsg.Payload.Offset);

            if (resp.Packet.Length != 0)
            {
                resp.Packet.CopyTo(buffer);

                return true;
            }

            return false;
        }
    }
}
