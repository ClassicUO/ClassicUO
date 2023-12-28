using ClassicUO.Network;
using System;
using System.Threading.Tasks;

namespace ClassicUO
{
    sealed class AssistantHost : TcpClientRpc
    {
        protected override void OnMessage(RpcMessage msg)
        {
            //Console.WriteLine("cmd {0} {1}", msg.Command, msg.ID);

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

        protected override void OnConnected()
        {
            PluginInitialize();

            base.OnConnected();
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


        private static readonly ArraySegment<byte> InitializeMessage = new ArraySegment<byte>(
            new byte[1] { (byte)PluginCuoProtocol.OnInitialize });

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


        public void PluginInitialize()
        {
            var response = Request(InitializeMessage).GetAwaiter().GetResult();
        }

        public void PluginTick()
        {
            var response = Request(TickMessage).GetAwaiter().GetResult();
        }

        public void PluginClosing()
        {
            var response = Request(ClosingMessage).GetAwaiter().GetResult();
        }

        public void PluginFocusGained()
        {
            var response = Request(FocusGainedMessage).GetAwaiter().GetResult();
        }

        public void PluginFocusLost()
        {
            var response = Request(FocusLostMessage).GetAwaiter().GetResult();
        }

        public void PluginConnected()
        {
            var response = Request(ConnectedMessage).GetAwaiter().GetResult();
        }

        public void PluginDisconnected() 
        {
            var response = Request(DisconnectedMessage).GetAwaiter().GetResult();
        }

        public bool PluginHotkeys(int key, int mod, bool ispressed)
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnHotkey;
            var response = Request(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
            return true;
        }

        public void PluginMouse(int button, int wheel)
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnMouse;
            var response = Request(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
        }

        public void PluginDrawCmdList()
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnCmdList;
            var response = Request(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
        }

        public unsafe void PluginSdlEvent(SDL2.SDL.SDL_Event* ev)
        {
            //var buf = new byte[1];
            //buf[0] = (byte)PluginCuoProtocol.OnSdlEvent;
            //var response = Request(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
        }

        public void PluginUpdatePlayerPosition(int x, int y, int z)
        {
            var buf = new byte[1];
            buf[0] = (byte)PluginCuoProtocol.OnUpdatePlayerPos;
            var response = Request(new ArraySegment<byte>(buf)).GetAwaiter().GetResult();
        }

        public void PluginPacketIn(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null && buffer.Count> 0)
            {
                var buf = new byte[sizeof(byte) + buffer.Count];
                buf[0] = (byte)PluginCuoProtocol.OnPacketIn;
                //BinaryPrimitives.WriteUInt16LittleEndian(buffer.Array.AsSpan(1, 2), (ushort)buffer.Count);

                Array.Copy(buffer.Array, buffer.Offset, buf, 1, buffer.Count);

                var resp = Request(buf).GetAwaiter().GetResult();

                Array.Copy(resp.Payload.Array, resp.Payload.Offset + 1, buffer.Array, buffer.Offset, resp.Payload.Count - 1);
            }
        }

        public void PluginPacketOut(Span<byte> buffer)
        {
            if (buffer.Length > 0)
            {
                var buf = new byte[sizeof(byte) + buffer.Length];
                buf[0] = (byte)PluginCuoProtocol.OnPacketOut;
                //BinaryPrimitives.WriteUInt16LittleEndian(buffer.Array.AsSpan(1, 2), (ushort)buffer.Count);

                buffer.CopyTo(buf.AsSpan(1));

                var resp = Request(buf).GetAwaiter().GetResult();
                if (resp.Payload.Count > 0)
                    resp.Payload.AsSpan(resp.Payload.Offset + 1, resp.Payload.Count - 1).CopyTo(buffer);
            }
        }
    }
}
