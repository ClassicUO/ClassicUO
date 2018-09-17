using System;
using System.Collections.Generic;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    public class Gump : GumpControl
    {
        public Gump(Serial local, Serial server) : base()
        {
            LocalSerial = local;
            ServerSerial = server;
        }

        public bool BlockMovement { get; set; }
        public override bool CanMove { get => !BlockMovement && base.CanMove; set => base.CanMove = value; }


        public override void Dispose()
        {
            base.Dispose();
        }


        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
        }

        protected override void OnMove()
        {
            SpriteBatchUI sb = Service.Get<SpriteBatchUI>();
            Point position = Location;

            int halfWidth = Width / 2;
            int halfHeight = Height / 2;

            if (X < -halfWidth)
                position.X = -halfWidth;
            if (Y < -halfHeight)
                position.Y = -halfHeight;
            if (X > sb.GraphicsDevice.Viewport.Width - halfWidth)
                position.X = sb.GraphicsDevice.Viewport.Width - halfWidth;
            if (Y > sb.GraphicsDevice.Viewport.Height - halfHeight)
                position.Y = sb.GraphicsDevice.Viewport.Height - halfHeight;

            Location = position;
        }

        public override void OnButtonClick(int buttonID)
        {
            if (LocalSerial != 0)
            {
                if (buttonID == 0) // cancel
                {
                    Network.NetClient.Socket.Send(new Network.PGumpResponse(LocalSerial, ServerSerial, buttonID, null, null));
                }
                else
                {
                    List<Serial> switches = new List<Serial>();
                    List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                    foreach (var control in Children)
                    {
                        if (control is Checkbox checkbox && checkbox.IsChecked)
                            switches.Add(control.LocalSerial);
                        else if (control is RadioButton radioButton && radioButton.IsChecked)
                            switches.Add(control.LocalSerial);
                        else if (control is TextBox textBox)
                            entries.Add(new Tuple<ushort, string>((ushort)LocalSerial, textBox.Text));
                    }

                    Network.NetClient.Socket.Send(new Network.PGumpResponse(LocalSerial, ServerSerial, buttonID, switches.ToArray(), entries.ToArray()));
                }
                Dispose();
            }
        }


        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
                return;

            if (ServerSerial != 0)
                OnButtonClick(0);
            base.CloseWithRightClick();
        }
    }
}
