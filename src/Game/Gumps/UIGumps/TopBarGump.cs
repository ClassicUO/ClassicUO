﻿#region license
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Gumps.UIGumps
{
    internal class TopBarGump : Gump
    {
        private readonly GameScene _scene;

        public TopBarGump(GameScene scene) : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;

            // maximized view
            AddChildren(new ResizePic(9200)
            {
                X = 0, Y = 0, Width = 610, Height = 27
            }, 1);

            AddChildren(new Button(0, 5540, 5542, 5541)
            {
                ButtonAction = ButtonAction.SwitchPage, ToPage = 2, X = 5, Y = 3
            }, 1);

            AddChildren(new Button((int) Buttons.Map, 2443, 2443, 0, "Map", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 30, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Paperdoll, 2445, 2445, 0, "Paperdoll", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 93, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Inventory, 2445, 2445, 0, "Inventory", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 201, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Journal, 2445, 2445, 0, "Journal", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 309, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Chat, 2443, 2443, 0, "Chat", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 417, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Help, 2443, 2443, 0, "Help", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 480, Y = 3, FontCenter = true
            }, 1);

            AddChildren(new Button((int) Buttons.Debug, 2443, 2443, 0, "Debug", 1, true, 0, 0x36)
            {
                ButtonAction = ButtonAction.Activate, X = 543, Y = 3, FontCenter = true
            }, 1);

            //minimized view
            AddChildren(new ResizePic(9200)
            {
                X = 0,
                Y = 0,
                Width = 30,
                Height = 27,
                IsVisible = false,
                IsEnabled = false
            }, 2);

            AddChildren(new Button(0, 5537, 5539, 5538)
            {
                ButtonAction = ButtonAction.SwitchPage, ToPage = 1, X = 5, Y = 3
            }, 2);

            //layer
            ControlInfo.Layer = UILayer.Over;
            _scene = scene;
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Right && (X != 0 || Y != 0))
            {
                X = 0;
                Y = 0;
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch ((Buttons) buttonID)
            {
                case Buttons.Map:
                    MiniMapGump.Toggle(_scene);

                    break;
                case Buttons.Paperdoll:

                    if (UIManager.GetByLocalSerial<PaperDollGump>(World.Player) == null)
                        GameActions.DoubleClick((Serial) (World.Player.Serial | int.MinValue));
                    else
                        UIManager.Remove<PaperDollGump>(World.Player);

                    break;
                case Buttons.Inventory:
                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];

                    if (backpack != null && !backpack.IsDisposed)
                    {
                        if (UIManager.GetByLocalSerial(backpack) == null)
                            GameActions.DoubleClick(backpack);
                        else
                            UIManager.Remove<Gump>(backpack);
                    }

                    break;
                case Buttons.Journal:

                    if (UIManager.GetByLocalSerial<JournalGump>() == null)
                        UIManager.Add(new JournalGump());
                    else
                        UIManager.Remove<JournalGump>();

                    break;
                case Buttons.Chat:
                    Log.Message(LogTypes.Warning, "Chat button pushed! Not implemented yet!");

                    break;
                case Buttons.Help:
                    GameActions.RequestHelp();

                    break;
                case Buttons.Debug:
                    Log.Message(LogTypes.Warning, "Debug button pushed! Not implemented yet!");

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