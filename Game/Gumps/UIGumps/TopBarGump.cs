
using System;
using System.Collections.Generic;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class TopBarGump : Gump
    {
        private Scenes.GameScene _scene;
        //private ResizePic _maximizedBar, _minimizedBar;
        //private Button _maximize, _minimize, _map, _paperdoll, _inventory, _journal, _chat, _help, _debug;
        
        public TopBarGump(Scenes.GameScene scene) : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = false;
           

            // maximized view
            AddChildren(new ResizePic(9200) { X = 0, Y = 0, Width = 610, Height = 27 }, 1);
            AddChildren(new Button(0, 5540, 5542, 5541) { ButtonAction = ButtonAction.SwitchPage, ButtonParameter = 2, X = 5, Y = 3 }, 1);
            AddChildren(new Button((int)Buttons.Map, 2443, 2443, 0) { ButtonAction = ButtonAction.Activate, X = 30, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Map", }, 1);
            AddChildren(new Button((int)Buttons.Paperdoll, 2445, 2445, 0) { ButtonAction = ButtonAction.Activate, X = 93, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Paperdoll" }, 1);
            AddChildren(new Button((int)Buttons.Inventory, 2445, 2445, 0) { ButtonAction = ButtonAction.Activate, X = 201, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Inventory" }, 1);
            AddChildren(new Button((int)Buttons.Journal, 2445, 2445, 0) { ButtonAction = ButtonAction.Activate, X = 309, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Journal" }, 1);
            AddChildren(new Button((int)Buttons.Chat, 2443, 2443, 0) { ButtonAction = ButtonAction.Activate, X = 417, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Chat" }, 1);
            AddChildren(new Button((int)Buttons.Help, 2443, 2443, 0) { ButtonAction = ButtonAction.Activate, X = 480, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Help" }, 1);
            AddChildren( new Button((int)Buttons.Debug, 2443, 2443, 0) { ButtonAction = ButtonAction.Activate, X = 543, Y = 3, Font = 1, FontHue = 1, FontCenter = true, Text = "Debug" }, 1);

            //minimized view
            AddChildren(new ResizePic(9200) { X = 0, Y = 0, Width = 30, Height = 27, IsVisible = false, IsEnabled = false }, 2);
            AddChildren(new Button(0, 5537, 5539, 5538) { ButtonAction = ButtonAction.SwitchPage, ButtonParameter = 1, X = 5, Y = 3 }, 2);

            //layer
            //ControlInfo.Layer = UILayer.Over;
            _scene = scene;
        }


        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Right && (X != 0 || Y != 0))
            {
                X = 0; Y = 0;
            }
        }

        public override void OnButtonClick(int buttonID)
        {

            switch ((Buttons)buttonID)
            {
                case Buttons.Map:
                    Service.Get<Log>().Message(LogTypes.Warning, "Map button pushed! Not implemented yet!");
                    MiniMapGump.Toggle(_scene);
                    break;
                case Buttons.Paperdoll:
                    PaperDollGump.Toggle(World.Player, World.Player.Name);
                    break;
                case Buttons.Inventory:
                    Service.Get<Log>().Message(LogTypes.Warning, "Inventory button pushed! Not implemented yet!");
                    break;
                case Buttons.Journal:
                    Service.Get<Log>().Message(LogTypes.Warning, "Journal button pushed! Not implemented yet!");
                    break;
                case Buttons.Chat:
                    Service.Get<Log>().Message(LogTypes.Warning, "Chat button pushed! Not implemented yet!");
                    break;
                case Buttons.Help:
                    Service.Get<Log>().Message(LogTypes.Warning, "Help button pushed! Not implemented yet!");
                    break;
                case Buttons.Debug:
                    Service.Get<Log>().Message(LogTypes.Warning, "Debug button pushed! Not implemented yet!");
                    break;
                //case Buttons.Minimize:
                //    _maximizedBar.IsEnabled = _minimize.IsEnabled = _map.IsEnabled = _paperdoll.IsEnabled = _inventory.IsEnabled = _journal.IsEnabled = _chat.IsEnabled = _help.IsEnabled = _debug.IsEnabled = false;
                //    _maximizedBar.IsVisible = _minimize.IsVisible = _map.IsVisible = _paperdoll.IsVisible = _inventory.IsVisible = _journal.IsVisible = _chat.IsVisible = _help.IsVisible = _debug.IsVisible = false;
                //    _minimizedBar.IsVisible = _minimizedBar.IsEnabled = _maximize.IsEnabled = _maximize.IsVisible = true;
                //    break;
                //case Buttons.Maximize:
                //    _maximizedBar.IsEnabled = _minimize.IsEnabled = _map.IsEnabled = _paperdoll.IsEnabled = _inventory.IsEnabled = _journal.IsEnabled = _chat.IsEnabled = _help.IsEnabled = _debug.IsEnabled = true;
                //    _maximizedBar.IsVisible = _minimize.IsVisible = _map.IsVisible = _paperdoll.IsVisible = _inventory.IsVisible = _journal.IsVisible = _chat.IsVisible = _help.IsVisible = _debug.IsVisible = true;
                //    _minimizedBar.IsVisible = _minimizedBar.IsEnabled = _maximize.IsEnabled = _maximize.IsVisible = false;
                //    break;
            }
        }

        enum Buttons
        {
            Map,
            Paperdoll,
            Inventory,
            Journal,
            Chat,
            Help,
            Debug,
            //Minimize,
            //Maximize
        }
    }
}
