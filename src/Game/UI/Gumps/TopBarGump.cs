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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
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
                X = 0, Y = 0, Width = 610 + 63, Height = 27
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

            Add(new Button((int)Buttons.WorldMap, 2443, 2443, 0, "WorldMap", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate,
                X = 607,
                Y = 3,
                FontCenter = true
            }, 1);

            //minimized view
            Add(new ResizePic(9200)
            {
                X = 0,
                Y = 0,
                Width = 30,
                Height = 27
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

        public bool IsMinimized { get; private set; }

        //private static TopBarGump _gump;

        public static void Create()
        {
            TopBarGump gump = UIManager.GetGump<TopBarGump>();

            if (gump == null)
            {
                if (ProfileManager.Current.TopbarGumpPosition.X < 0 || ProfileManager.Current.TopbarGumpPosition.Y < 0)
                    ProfileManager.Current.TopbarGumpPosition = Point.Zero;

                UIManager.Add(gump = new TopBarGump
                {
                    X = ProfileManager.Current.TopbarGumpPosition.X,
                    Y = ProfileManager.Current.TopbarGumpPosition.Y
                });

                if (ProfileManager.Current.TopbarGumpIsMinimized)
                    gump.ChangePage(2);
            }
            else
                Log.Error( "TopBarGump already exists!!");
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Right && (X != 0 || Y != 0))
            {
                X = 0;
                Y = 0;

                ProfileManager.Current.TopbarGumpPosition = Location;
            }
        }

        public override void OnPageChanged()
        {
            ProfileManager.Current.TopbarGumpIsMinimized = IsMinimized = ActivePage == 2;
            WantUpdateSize = true;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.Current.TopbarGumpPosition = Location;
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Map:
                    MiniMapGump miniMapGump = UIManager.GetGump<MiniMapGump>();

                    if (miniMapGump == null)
                        UIManager.Add(new MiniMapGump());
                    else
                    {
                        miniMapGump.SetInScreen();
                        miniMapGump.BringOnTop();
                    }

                    break;

                case Buttons.Paperdoll:
                    PaperDollGump paperdollGump = UIManager.GetGump<PaperDollGump>(World.Player);

                    if (paperdollGump == null)
                        GameActions.OpenPaperdoll(World.Player);
                    else
                    {
                        paperdollGump.SetInScreen();
                        paperdollGump.BringOnTop();
                    }

                    break;

                case Buttons.Inventory:
                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];

                    ContainerGump backpackGump = UIManager.GetGump<ContainerGump>(backpack);

                    if (backpackGump == null)
                        GameActions.DoubleClick(backpack);
                    else
                    {
                        backpackGump.SetInScreen();
                        backpackGump.BringOnTop();
                    }

                    break;

                case Buttons.Journal:
                    JournalGump journalGump = UIManager.GetGump<JournalGump>();

                    if (journalGump == null)
                    {
                        UIManager.Add(new JournalGump
                                          {X = 64, Y = 64});
                    }
                    else
                    {
                        journalGump.SetInScreen();
                        journalGump.BringOnTop();
                    }

                    break;

                case Buttons.Chat:
                    Log.Warn( "Chat button pushed! Not implemented yet!");

                    break;

                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;

                case Buttons.Debug:

                    DebugGump debugGump = UIManager.GetGump<DebugGump>();

                    if (debugGump == null)
                    {
                        debugGump = new DebugGump
                        {
                            X = ProfileManager.Current.DebugGumpPosition.X,
                            Y = ProfileManager.Current.DebugGumpPosition.Y
                        };

                        UIManager.Add(debugGump);
                    }
                    else
                    {
                        debugGump.IsVisible = !debugGump.IsVisible;
                        debugGump.SetInScreen();
                    }

                    //Engine.DropFpsMinMaxValues();

                    break;
                case Buttons.WorldMap:

                    WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

                    if (worldMap == null || worldMap.IsDisposed)
                    {
                        worldMap = new WorldMapGump();
                        UIManager.Add(worldMap);
                    }
                    else
                    {
                        worldMap.BringOnTop();
                        worldMap.SetInScreen();
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
            Debug,
            WorldMap,
        }
    }
}