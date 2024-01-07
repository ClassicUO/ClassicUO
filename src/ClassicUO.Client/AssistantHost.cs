using ClassicUO.Configuration;
using ClassicUO.Network;
using StructPacker;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO
{
    sealed class AssistantHost : TcpClientRpc
    {
        protected override ArraySegment<byte> OnRequest(ArraySegment<byte> msg)
        {
            if (msg.Count <= 0)
                return ArraySegment<byte>.Empty;

            var cmd = (PluginCuoProtocol)msg.Array[0];

            switch (cmd)
            {
                case PluginCuoProtocol.OnPluginRecv:
                    {
                        var packetLen = BinaryPrimitives.ReadUInt16LittleEndian(msg.AsSpan(sizeof(byte), sizeof(ushort)));
                        
                        lock (PacketHandlers.Handler)
                        {
                            PacketHandlers.Handler.Append(msg.AsSpan(sizeof(byte) + sizeof(ushort), packetLen), true);
                        }
                    }
                    break;

                case PluginCuoProtocol.OnPluginSend:
                    {
                        var packetLen = BinaryPrimitives.ReadUInt16LittleEndian(msg.AsSpan(sizeof(byte), sizeof(ushort)));

                        if (NetClient.Socket.IsConnected)
                        {
                            NetClient.Socket.Send(msg.AsSpan(sizeof(byte) + sizeof(ushort), packetLen), true);
                        }
                    }
                    
                    break;

                case PluginCuoProtocol.OnPacketLength:
                    {
                        var req = new PluginPacketLengthRequest();
                        req.Unpack(msg.Array, msg.Offset);

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

        //[Pack]
        //internal struct PluginPacketRequestResponse
        //{
        //    public byte Cmd;
        //    public byte[] Packet;
        //}

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


        private readonly Dictionary<PluginCuoProtocol, byte[]> _simpleRequests = new Dictionary<PluginCuoProtocol, byte[]>();

        private void ReturnArray(ArraySegment<byte> segment)
        {
            if (segment.Array != null && segment.Array.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(segment.Array);
            }
        }

        private void MakeSimpleRequest(PluginCuoProtocol protocol)
        {
            if (!_simpleRequests.TryGetValue(protocol, out var buf))
            {
                var req = new PluginSimpleRequest()
                {
                    Cmd = (byte)protocol
                };

                buf = req.Pack();
                _simpleRequests.Add(protocol, buf);
            }

            var resp = Request(new ArraySegment<byte>(buf));

            ReturnArray(resp);
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
            ReturnArray(resp);
        }

        public void PluginTick()
        {
            MakeSimpleRequest(PluginCuoProtocol.OnTick);
        }

        public void PluginClosing()
        {
            MakeSimpleRequest(PluginCuoProtocol.OnClosing);
        }

        public void PluginFocusGained()
        {
            MakeSimpleRequest(PluginCuoProtocol.OnFocusGained);
        }

        public void PluginFocusLost()
        {
            MakeSimpleRequest(PluginCuoProtocol.OnFocusLost);
        }

        public void PluginConnected()
        {
            MakeSimpleRequest(PluginCuoProtocol.OnConnected);
        }

        public void PluginDisconnected() 
        {
            MakeSimpleRequest(PluginCuoProtocol.OnDisconnected);
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
            resp.Unpack(respMsg.Array, respMsg.Offset);

            ReturnArray(respMsg);

            return resp.Allowed;
        }

        public void PluginMouse(int button, int wheel)
        {
            var req = new PluginMouseRequest()
            {
                Cmd = (byte)PluginCuoProtocol.OnMouse,
                Button = button,
                Wheel = wheel
            };

            using var buf = req.PackToBuffer();
            var resp = Request(new ArraySegment<byte>(buf.Data, 0, buf.Size));
            ReturnArray(resp);
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
            //var buf = new byte[1];
            //buf[0] = (byte)PluginCuoProtocol.OnUpdatePlayerPos;
            //var resp = Request(buf);
        }

        public bool PluginPacketIn(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null && buffer.Count > 0)
            {
                var rentBuf = ArrayPool<byte>.Shared.Rent(sizeof(byte) + sizeof(ushort) + buffer.Count);

                try
                {
                    rentBuf[0] = (byte)PluginCuoProtocol.OnPacketIn;
                    BinaryPrimitives.WriteUInt16LittleEndian(rentBuf.AsSpan(sizeof(byte), sizeof(ushort)), (ushort) buffer.Count);

                    buffer.CopyTo(new ArraySegment<byte>(rentBuf, sizeof(byte) + sizeof(ushort), buffer.Count));

                    var respMsg = Request(new ArraySegment<byte>(rentBuf, 0, sizeof(byte) + sizeof(ushort) + buffer.Count));

                    var packetLen = BinaryPrimitives.ReadUInt16LittleEndian(respMsg.Array.AsSpan(sizeof(byte), sizeof(ushort)));

                    if (packetLen > 0)
                    {
                        respMsg.Array.AsSpan(sizeof(byte) + sizeof(ushort), packetLen).CopyTo(buffer.Array);
                        ReturnArray(respMsg);
                        return true;
                    }

                    ReturnArray(respMsg);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentBuf);
                }
            }

            return false;
        }

        public bool PluginPacketOut(Span<byte> buffer)
        {
            if (buffer.IsEmpty) return true;

            var rentBuf = ArrayPool<byte>.Shared.Rent(sizeof(byte) + sizeof(ushort) + buffer.Length);

            try
            {
                rentBuf[0] = (byte)PluginCuoProtocol.OnPacketOut;
                BinaryPrimitives.WriteUInt16LittleEndian(rentBuf.AsSpan(sizeof(byte), sizeof(ushort)), (ushort)buffer.Length);

                buffer.CopyTo(new ArraySegment<byte>(rentBuf, sizeof(byte) + sizeof(ushort), buffer.Length));

                var respMsg = Request(new ArraySegment<byte>(rentBuf, 0, sizeof(byte) + sizeof(ushort) + buffer.Length));

                var packetLen = BinaryPrimitives.ReadUInt16LittleEndian(respMsg.Array.AsSpan(sizeof(byte), sizeof(ushort)));

                if (packetLen > 0)
                {
                    respMsg.Array.AsSpan(sizeof(byte) + sizeof(ushort), packetLen).CopyTo(buffer);
                    ReturnArray(respMsg);
                    return true;
                }

                ReturnArray(respMsg);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentBuf);
            }

            return false;
        }
    }
}
