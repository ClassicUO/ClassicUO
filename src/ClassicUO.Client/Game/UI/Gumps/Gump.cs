// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control
    {
        public Gump(World world, uint local, uint server)
        {
            World = world;
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public World World { get; }

        public bool CanBeSaved => GumpType != Gumps.GumpType.None;

        public virtual GumpType GumpType { get; }

        public bool InvalidateContents { get; set; }

        public uint MasterGumpSerial { get; set; }


        public override void Update()
        {
            if (InvalidateContents)
            {
                UpdateContents();
                InvalidateContents = false;
            }

            if (ActivePage == 0)
            {
                ActivePage = 1;
            }

            base.Update();
        }

        public override void Dispose()
        {
            Item it = World.Items.Get(LocalSerial);

            if (it != null && it.Opened)
            {
                it.Opened = false;
            }

            base.Dispose();
        }


        public virtual void Save(XmlTextWriter writer)
        {
            writer.WriteAttributeString("type", ((int) GumpType).ToString());
            writer.WriteAttributeString("x", X.ToString());
            writer.WriteAttributeString("y", Y.ToString());
            writer.WriteAttributeString("serial", LocalSerial.ToString());
        }

        public void SetInScreen()
        {
            Rectangle windowBounds = Client.Game.Window.ClientBounds;
            Rectangle bounds = Bounds;
            bounds.X += windowBounds.X;
            bounds.Y += windowBounds.Y;

            if (windowBounds.Intersects(bounds))
            {
                return;
            }

            X = 0;
            Y = 0;
        }

        public virtual void Restore(XmlElement xml)
        {
        }

        public void RequestUpdateContents()
        {
            InvalidateContents = true;
        }

        protected virtual void UpdateContents()
        {
        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width - (Width >> 2);
            int halfHeight = Height - (Height >> 2);

            if (X < -halfWidth)
            {
                position.X = -halfWidth;
            }

            if (Y < -halfHeight)
            {
                position.Y = -halfHeight;
            }

            if (X > Client.Game.Window.ClientBounds.Width - (Width - halfWidth))
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - halfWidth);
            }

            if (Y > Client.Game.Window.ClientBounds.Height - (Height - halfHeight))
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - halfHeight);
            }

            Location = position;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return IsVisible && base.Draw(batcher, x, y);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0)
            {
                List<uint> switches = new List<uint>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case StbTextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump
                (
                    LocalSerial,
                    // Seems like MasterGump serial does not work as expected.
                    /*MasterGumpSerial != 0 ? MasterGumpSerial :*/ ServerSerial,
                    buttonID,
                    switches.ToArray(),
                    entries.ToArray()
                );

                if (CanMove)
                {
                    UIManager.SavePosition(ServerSerial, Location);
                }
                else
                {
                    UIManager.RemovePosition(ServerSerial);
                }

                Dispose();
            }
        }

        protected override void CloseWithRightClick()
        {
            if (!CanCloseWithRightClick)
            {
                return;
            }

            if (ServerSerial != 0)
            {
                OnButtonClick(0);
            }

            base.CloseWithRightClick();
        }

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}