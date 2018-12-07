#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.Configuration;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    public class Gump : GumpControl
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

        public virtual bool Save(out Dictionary<string, object> data)
        {
            //if (CanBeSaved && _save && !string.IsNullOrEmpty(_saveName))
            //{
            //    Service.Get<Settings>().AddGump(_saveName, Location);
            //}

            if (CanBeSaved)
            {
                data = new Dictionary<string, object>()
                {
                    { "location", Location }
                };

                return true;
            }
            data = null;
            return false;
        }

        public virtual bool Restore(Dictionary<string, object> data)
        {
            if (data.TryGetValue("location", out object point))
            {

                string[] s = point.ToString().Split(new[]
                {
                    ',', ' '
                }, StringSplitOptions.RemoveEmptyEntries);


                X = Convert.ToInt32(s[0]);
                Y = Convert.ToInt32(s[1]);

                return true;
            }

            return false;
        }

        private bool _save;
        private string _saveName;
        

        protected void SetNameAndPositionForSaving(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _save = true;
                _saveName = name;
            }
        }


        protected override void OnMove()
        {
            SpriteBatchUI sb = Service.Get<SpriteBatchUI>();
            Point position = Location;
            int halfWidth = Width >> 1;
            int halfHeight = Height >> 1;

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
                    GameActions.ReplyGump(LocalSerial, ServerSerial, buttonID);
                else
                {
                    List<Serial> switches = new List<Serial>();
                    List<Tuple<ushort, string>> entries = new List<Tuple<ushort, string>>();

                    foreach (GumpControl control in Children)
                    {
                        if (control is Checkbox checkbox && checkbox.IsChecked)
                            switches.Add(control.LocalSerial);
                        //else if (control is RadioButton radioButton && radioButton.IsChecked)
                        //    switches.Add(control.LocalSerial);
                        else if (control is TextBox textBox)
                            entries.Add(new Tuple<ushort, string>((ushort) textBox.LocalSerial, textBox.Text));
                    }

                    GameActions.ReplyGump(LocalSerial, ServerSerial, buttonID, switches.ToArray(), entries.ToArray());
                }

                UIManager.SavePosition(ServerSerial, Location);
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