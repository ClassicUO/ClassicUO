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

using ClassicUO.Game.Scenes;
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

        public void SetInScreen()
        {
            if (X >= Engine.Instance.Window.ClientBounds.Width || X < 0)
                X = 0;

            if (Y >= Engine.Instance.Window.ClientBounds.Height || Y < 0)
                Y = 0;
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return IsVisible && base.Draw(batcher, x, y);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (!IsDisposed && LocalSerial != 0 && !LocalSerial.IsValidLocalGumpSerial)
            {
                List<Serial> switches = new List<Serial>();
                List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                foreach (Control control in Children)
                {
                    switch (control)
                    {
                        case Checkbox checkbox when checkbox.IsChecked:
                            switches.Add(control.LocalSerial);

                            break;

                        case TextBox textBox:
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));

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

    internal abstract class MinimizableGump : TextContainerGump
    {
        internal bool IsMinimized
        {
            get => Iconized != null && IconizerArea != null && _MinimizedSave.TryGetValue(LocalSerial, out bool minimized) && minimized;
            private set
            {
                if (Iconized != null && IconizerArea != null && Iconized.IsVisible != value)
                {
                    _MinimizedSave[LocalSerial] = Iconized.IsVisible = value;
                }
            }
        }

        internal MinimizableGump(Serial local, Serial server) : base(local, server)
        {
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);
            if(!_IsUpdated)
            {
                _IsUpdated = true;
                AfterCreationCall();
            }
        }

        private void Iconized_MouseDoubleClick(object sender, Input.MouseDoubleClickEventArgs e)
        {
            if(e.Button == Input.MouseButton.Left)
            {
                IsMinimized = false;
            }
        }

        private void IconizerButton_MouseUp(object sender, Input.MouseEventArgs e)
        {
            if(e.Button == Input.MouseButton.Left)
            {
                IsMinimized = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsMinimized && Iconized != null)
                return Iconized.Draw(batcher, x + Iconized.X, y + Iconized.Y);
            else
                return base.Draw(batcher, x, y);
        }

        public override bool Contains(int x, int y)
        {
            return IsMinimized && Iconized != null ? Iconized.Contains(x, y) : base.Contains(x, y);
        }

        internal abstract GumpPic Iconized { get; }
        internal abstract HitBox IconizerArea { get; }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);
            writer.Write(LocalSerial);
            writer.Write(IsMinimized);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);
            if(Configuration.Profile.GumpsVersion > 1)
            {
                _MinimizedSave[reader.ReadUInt32()] = reader.ReadBoolean();
            }
        }

        //some gumps generate properties after it's main generation, this is here only to delay the generation of main properties AFTER the principal gump itself.
        private void AfterCreationCall()
        {
            if (IconizerArea != null && Iconized != null)
            {
                Iconized.Initialize();
                Iconized.IsVisible = false;
                Iconized.AcceptMouseInput = true;
                Iconized.CanMove = true;
                Add(Iconized);

                Add(IconizerArea);
                IconizerArea.MouseUp += IconizerButton_MouseUp;
                IconizerArea.Alpha = 0.95f;
                Iconized.MouseDoubleClick += Iconized_MouseDoubleClick;
                if (LocalSerial != Serial.INVALID)
                {
                    if (_MinimizedSave.TryGetValue(LocalSerial, out bool minimized))
                        _MinimizedSave[LocalSerial] = IsMinimized = minimized;
                    else
                        _MinimizedSave[LocalSerial] = IsMinimized;
                }
            }
        }

        protected override void CloseWithRightClick()
        {
            _MinimizedSave.Remove(LocalSerial);
            base.CloseWithRightClick();
        }

        private static readonly Dictionary<Serial, bool> _MinimizedSave = new Dictionary<Serial, bool>();
        private bool _IsUpdated = false;
    }
}