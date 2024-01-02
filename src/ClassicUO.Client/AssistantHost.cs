using ClassicUO.Configuration;
using ClassicUO.Network;
using System;
using System.Buffers.Binary;
using System.Text;

namespace ClassicUO
{
    sealed class AssistantHost : TcpClientRpc
    {
        protected override void OnMessage(RpcMessage msg)
        {
            if (msg.Command == RpcCommand.Response)
                return;

            if (msg.Payload.Count <= 0)
                return;

            var cmd = (PluginCuoProtocol)msg.Payload.Array[0];

            switch (cmd)
            {
                case PluginCuoProtocol.OnPluginRecv:
                    lock (PacketHandlers.Handler)
                    {
                        PacketHandlers.Handler.Append(msg.Payload.Array.AsSpan(1, msg.Payload.Count - 1), true);
                    }

                    break;

                case PluginCuoProtocol.OnPluginSend:
                    if (NetClient.Socket.IsConnected)
                    {
                        NetClient.Socket.Send(msg.Payload.Array.AsSpan(1, msg.Payload.Count - 1), true);
                    }
                    break;
            }
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
        }

        private static readonly ArraySegment<byte> TickMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnTick });

        private static readonly ArraySegment<byte> ClosingMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnClosing });

        private static readonly ArraySegment<byte> FocusGainedMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnFocusGained });

        private static readonly ArraySegment<byte> FocusLostMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnFocusLost });

        private static readonly ArraySegment<byte> ConnectedMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnConnected });

        private static readonly ArraySegment<byte> DisconnectedMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnDisconnected });


        public void PluginInitialize(string pluginPath)
        {
            if (string.IsNullOrEmpty(pluginPath))
                return;

            var pluginPathLen = Encoding.UTF8.GetByteCount(pluginPath);
            var assetsPathLen = Encoding.UTF8.GetByteCount(Settings.GlobalSettings.UltimaOnlineDirectory);

            var buf = new byte[1 + sizeof(uint) + sizeof(ushort) * 2 + pluginPathLen + assetsPathLen];
            buf[0] = (byte)PluginCuoProtocol.OnInitialize;
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(1, sizeof(uint)), (uint)Client.Game.UO.Version);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(1 + sizeof(uint), sizeof(ushort)), (ushort)pluginPathLen);
            Encoding.UTF8.GetBytes(pluginPath, 0, pluginPathLen, buf, 1 + sizeof(uint) + sizeof(ushort));
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(1 + sizeof(uint) + sizeof(ushort) + pluginPathLen, sizeof(ushort)), (ushort)assetsPathLen);
            Encoding.UTF8.GetBytes(Settings.GlobalSettings.UltimaOnlineDirectory, 0, assetsPathLen, buf, 1 + sizeof(uint) + sizeof(ushort) * 2 + pluginPathLen);
            var resp = Request(buf);
        }

        public void PluginTick()
        {
            var resp = Request(TickMessage);
        }

        public void PluginClosing()
        {
            var resp = Request(ClosingMessage);
        }

        public void PluginFocusGained()
        {
            var resp = Request(FocusGainedMessage);
        }

        public void PluginFocusLost()
        {
            var resp = Request(FocusLostMessage);
        }

        public void PluginConnected()
        {
            var resp = Request(ConnectedMessage);
        }

        public void PluginDisconnected() 
        {
            var resp = Request(DisconnectedMessage);
        }

        public bool PluginHotkeys(int key, int mod, bool ispressed)
        {
            var buf = new byte[1 + sizeof(int) * 2 + sizeof(bool)];
            buf[0] = (byte)PluginCuoProtocol.OnHotkey;
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, sizeof(int)), key);
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1 + sizeof(int), sizeof(int)), mod);
            buf[1 + sizeof(int) * 2] = (byte) (ispressed ? 0x01 : 0x00);
           
            var resp = Request(buf);
            return true;
        }

        public void PluginMouse(int button, int wheel)
        {
            var buf = new byte[1 + sizeof(int) * 2];
            buf[0] = (byte)PluginCuoProtocol.OnMouse;
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1, sizeof(int)), button);
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(1 + sizeof(int), sizeof(int)), wheel);

            var resp = Request(buf);
        }

        public void PluginDrawCmdList()
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnCmdList;
            var resp = Request(buf);
        }

        public unsafe void PluginSdlEvent(SDL2.SDL.SDL_Event* ev)
        {
            //var buf = new byte[1];
            //buf[0] = (byte)PluginCuoProtocol.OnSdlEvent;
            //var response = RequestAsync(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
        }

        public void PluginUpdatePlayerPosition(int x, int y, int z)
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnUpdatePlayerPos;
            var resp = Request(buf);
        }

        public void PluginPacketIn(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null && buffer.Count> 0)
            {
                var buf = new byte[sizeof(byte) + buffer.Count];
                buf[0] = (byte)PluginCuoProtocol.OnPacketIn;

                Array.Copy(buffer.Array, buffer.Offset, buf, 1, buffer.Count);

                var resp = Request(buf);

                Array.Copy(resp.Payload.Array, resp.Payload.Offset + 1, buffer.Array, buffer.Offset, resp.Payload.Count - 1);
            }
        }

        public void PluginPacketOut(Span<byte> buffer)
        {
            if (buffer.Length > 0)
            {
                var buf = new byte[sizeof(byte) + buffer.Length];
                buf[0] = (byte)PluginCuoProtocol.OnPacketOut;

                buffer.CopyTo(buf.AsSpan(1));

                var resp = Request(buf);
                if (resp.Payload.Count > 0)
                    resp.Payload.AsSpan(resp.Payload.Offset + 1, resp.Payload.Count - 1).CopyTo(buffer);
            }
        }
    }
}
