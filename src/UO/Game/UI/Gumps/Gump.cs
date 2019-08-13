#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;

using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class Gump : Control           
    {
        public Gump(Serial local, Serial server)
        {
            LocalSerial = local;
            ServerSerial = server;
            AcceptMouseInput = false;
            AcceptKeyboardInput = false;
        }

        public bool BlockMovement { get; set; }

        public bool CloseIfClickOutside { get; set; }

        public bool CanBeSaved { get; protected set; }

        public override bool CanMove
        {
            get => !BlockMovement && base.CanMove;
            set => base.CanMove = value;
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (ActivePage == 0)
                ActivePage = 1;
            base.Update(totalMS, frameMS);
        }

        public virtual void Save(BinaryWriter writer)
        {
            // the header         
            Type type = GetType();
            ushort typeLen = (ushort) type.FullName.Length;
            writer.Write(typeLen);
            writer.WriteUTF8String(type.FullName);
            writer.Write(X);
            writer.Write(Y);
        }

        public virtual void Restore(BinaryReader reader)
        {

        }

        protected override void OnDragEnd(int x, int y)
        {
            Point position = Location;
            int halfWidth = Width >> 1;
            int halfHeight = Height >> 1;

            if (X < -halfWidth)
                position.X = -halfWidth;

            if (Y < -halfHeight)
                position.Y = -halfHeight;

            if (X > Engine.Batcher.GraphicsDevice.Viewport.Width - halfWidth)
                position.X = Engine.Batcher.GraphicsDevice.Viewport.Width - halfWidth;

            if (Y > Engine.Batcher.GraphicsDevice.Viewport.Height - halfHeight)
                position.Y = Engine.Batcher.GraphicsDevice.Viewport.Height - halfHeight;
            Location = position;
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            return IsVisible && base.Draw(batcher, position, hue);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (LocalSerial != 0)
            {
                List<Serial> switches = new List<Serial>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked: switches.Add(control.LocalSerial);

                            break;
                        case TextBox textBox: entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

                            break;
                    }
                }

                GameActions.ReplyGump(LocalSerial, ServerSerial, buttonID, switches.ToArray(), entries.ToArray());

                Engine.UI.SavePosition(ServerSerial, Location);
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

        public override void ChangePage(int pageIndex)
        {
            // For a gump, Page is the page that is drawing.
            ActivePage = pageIndex;
        }
    }
}