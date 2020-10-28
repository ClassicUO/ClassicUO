using System.Text;
using System.Xml;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NetworkStatsGump : Gump
    {
        private static Point _last_position = new Point(-1, -1);

        private uint _ping, _deltaBytesReceived, _deltaBytesSent;
        private readonly StringBuilder _sb = new StringBuilder();
        private uint _time_to_update;
        private readonly AlphaBlendControl _trans;

        public NetworkStatsGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _ping = _deltaBytesReceived = _deltaBytesSent = 0;

            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;
            Width = 100;
            Height = 30;

            Add
            (
                _trans = new AlphaBlendControl(.3f)
                {
                    Width = Width,
                    Height = Height
                }
            );


            LayerOrder = UILayer.Over;

            WantUpdateSize = false;
        }

        public override GumpType GumpType => GumpType.NetStats;

        public bool IsMinimized { get; set; }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            if (Time.Ticks > _time_to_update)
            {
                _sb.Clear();

                _time_to_update = Time.Ticks + 100;

                if (!NetClient.Socket.IsConnected)
                {
                    _ping = NetClient.LoginSocket.Statistics.Ping;
                    _deltaBytesReceived = NetClient.LoginSocket.Statistics.DeltaBytesReceived;
                    _deltaBytesSent = NetClient.LoginSocket.Statistics.DeltaBytesSent;
                }
                else if (!NetClient.Socket.IsDisposed)
                {
                    _ping = NetClient.Socket.Statistics.Ping;
                    _deltaBytesReceived = NetClient.Socket.Statistics.DeltaBytesReceived;
                    _deltaBytesSent = NetClient.Socket.Statistics.DeltaBytesSent;
                }

                if (IsMinimized)
                {
                    _sb.Append($"Ping: {_ping} ms");
                }
                else
                {
                    _sb.Append
                    (
                        $"Ping: {_ping} ms\n{"In:"} {NetStatistics.GetSizeAdaptive(_deltaBytesReceived),-6} {"Out:"} {NetStatistics.GetSizeAdaptive(_deltaBytesSent),-6}"
                    );
                }


                Vector2 size = Fonts.Bold.MeasureString(_sb.ToString());

                _trans.Width = Width = (int) (size.X + 20);
                _trans.Height = Height = (int) (size.Y + 20);
                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            ResetHueVector();

            if (_ping < 150)
            {
                HueVector.X = 0x44; // green
            }
            else if (_ping < 200)
            {
                HueVector.X = 0x34; // yellow
            }
            else if (_ping < 300)
            {
                HueVector.X = 0x31; // orange
            }
            else
            {
                HueVector.X = 0x20; // red
            }

            HueVector.Y = 1;

            batcher.DrawString(Fonts.Bold, _sb.ToString(), x + 10, y + 10, ref HueVector);

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }
    }
}