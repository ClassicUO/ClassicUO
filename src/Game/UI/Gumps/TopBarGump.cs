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

using System.IO;
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class TopBarGump : Gump
    {
        private TopBarGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            // maximized view
            Add(new ResizePic(9200)
            {
                X = 0, Y = 0, Width = 610, Height = 27
            }, 1);

            Add(new Button(0, 5540, 5542, 5541)
            {
                ButtonAction = ButtonAction.SwitchPage, ToPage = 2, X = 5, Y = 3
            }, 1);

            Add(new Button((int) Buttons.Map, 2443, 2443, 0, "Map", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 30, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Paperdoll, 2445, 2445, 0, "Paperdoll", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 93, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Inventory, 2445, 2445, 0, "Inventory", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 201, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Journal, 2445, 2445, 0, "Journal", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 309, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Chat, 2443, 2443, 0, "Chat", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 417, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Help, 2443, 2443, 0, "Help", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 480, Y = 3, FontCenter = true
            }, 1);

            Add(new Button((int) Buttons.Debug, 2443, 2443, 0, "Debug", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 543, Y = 3, FontCenter = true
            }, 1);

            //minimized view
            Add(new ResizePic(9200)
            {
                X = 0,
                Y = 0,
                Width = 30,
                Height = 27,
            }, 2);

            Add(new Button(0, 5537, 5539, 5538)
            {
                ButtonAction = ButtonAction.SwitchPage,
                ToPage = 1,
                X = 5,
                Y = 3
            }, 2);

            //layer
            ControlInfo.Layer = UILayer.Over;
        }

        //private static TopBarGump _gump;

        public static void Create()
        {
            TopBarGump gump = Engine.UI.GetByLocalSerial<TopBarGump>();

            if (gump == null)
            {
                if (Engine.Profile.Current.TopbarGumpPosition.X < 0 || Engine.Profile.Current.TopbarGumpPosition.Y < 0)
                    Engine.Profile.Current.TopbarGumpPosition = Point.Zero;
                
                Engine.UI.Add(gump = new TopBarGump()
                {
                    X = Engine.Profile.Current.TopbarGumpPosition.X,
                    Y = Engine.Profile.Current.TopbarGumpPosition.Y,
                });

                if (Engine.Profile.Current.TopbarGumpIsMinimized)
                    gump.ChangePage(2);
            }
            else
            {
                Log.Message(LogTypes.Error, "TopBarGump already exists!!");
            }
        }


        public bool IsMinimized { get; private set; }
       
        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Right && (X != 0 || Y != 0))
            {
                X = 0;
                Y = 0;
            }
        }

        public override void OnPageChanged()
        {
            Engine.Profile.Current.TopbarGumpIsMinimized = IsMinimized = ActivePage == 2;
            WantUpdateSize = true;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            Engine.Profile.Current.TopbarGumpPosition = Location;
        }


        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {              
                case Buttons.Map:

                    MiniMapGump miniMapGump = Engine.UI.GetByLocalSerial<MiniMapGump>();
                    if (miniMapGump == null)
                        Engine.UI.Add(new MiniMapGump());
                    else
                        miniMapGump.BringOnTop();

                    break;
                case Buttons.Paperdoll:
                    PaperDollGump paperdollGump = Engine.UI.GetByLocalSerial<PaperDollGump>(World.Player);
                    if (paperdollGump == null)
                        GameActions.OpenPaperdoll(World.Player);
                    else
                        paperdollGump.BringOnTop();

                    break;
                case Buttons.Inventory:
                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];

                    ContainerGump backpackGump = Engine.UI.GetByLocalSerial<ContainerGump>(backpack);

                    if (backpackGump == null)
                    {
                        GameActions.DoubleClick(backpack);
                    }
                    else
                        backpackGump.BringOnTop();

                    break;
                case Buttons.Journal:
                    JournalGump journalGump = Engine.UI.GetByLocalSerial<JournalGump>();

                    if (journalGump == null)
                        Engine.UI.Add(new JournalGump() { X = 64, Y = 64});
                    else
                        journalGump.BringOnTop();

                    break;
                case Buttons.Chat:
                    Log.Message(LogTypes.Warning, "Chat button pushed! Not implemented yet!");

                    break;
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;
                case Buttons.Debug:

                    DebugGump debugGump = Engine.UI.GetByLocalSerial<DebugGump>();

                    if (debugGump == null)
                    {
                        // dont consider this case
                    }
                    else
                    {
                        debugGump.IsVisible = !debugGump.IsVisible;
                    }

                    break;
            }
        }

        private enum Buttons
        {
            Map,
            Paperdoll,
            Inventory,
            Journal,
            Chat,
            Help,
            Debug
        }
    }
}