using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.UI.Controls
{
    class HotkeyControl : Control
    {
        private readonly List<HotkeyBox> _hotkesBoxes = new List<HotkeyBox>();
        private readonly HotkeyAction _key;

        public HotkeyControl(string text, HotkeyAction key)
        {
            _key = key;
            CanMove = true;
            AcceptMouseInput = true;

            Add(new Label(text, true, 0, 150, 1));
           
            AddNew(key);
        }


        public void AddNew(HotkeyAction action)
        {
            HotkeyBox box = new HotkeyBox()
            {
                X = 150,
            };

            box.HotkeyChanged += (sender, e) =>
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();
                if (gs == null)
                    return;

                if (gs.Hotkeys.Bind(_key, box.Key, box.Mod))
                {

                }
                else // show a popup
                {
                    Engine.UI.Add(new MessageBoxGump(400, 200, "Key combination already exists.", null));
                }
            };
            box.HotkeyCancelled += (sender, e) =>
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();
                if (gs == null)
                    return;

                gs.Hotkeys.UnBind(_key);
            };

            if (_hotkesBoxes.Count != 0)
                box.Y = _hotkesBoxes.LastOrDefault().Bounds.Bottom;

            _hotkesBoxes.Add(box);

            Add(box);
        }
    }
}
