using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using static SDL3.SDL;

namespace ClassicUO.LegionScripting
{
    internal class ScriptingInfoGump : ResizableGump
    {
        private ScrollArea _scrollArea;
        private Label _titleLabel;
        private static int _lastX = 200, _lastY = 200;
        private static int _lastWidth = 300, _lastHeight = 400;
        private const int MIN_WIDTH = 250;
        private const int MIN_HEIGHT = 200;
        private static Dictionary<string, object> _infoEntries = new Dictionary<string, object>();

        public static ScriptingInfoGump Instance { get; private set; }

        private ScriptingInfoGump() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            X = _lastX;
            Y = _lastY;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            BuildGump();
            UpdateUI();
        }

        private void BuildGump()
        {
            int border = BorderControl.BorderSize;
            _titleLabel = new Label("Scripting Info", true, 0x22, Width - border * 2 - 20, font: 1) { X = border, Y = border };
            Add(_titleLabel);
            int scrollY = _titleLabel.Y + _titleLabel.Height + 10;
            int scrollH = Height - scrollY - border;
            _scrollArea = new ScrollArea(border, scrollY, Width - 2 * border, scrollH, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView };
            Add(_scrollArea);
        }

        public static void Show()
        {
            Instance?.Dispose();
            UIManager.Add(Instance = new ScriptingInfoGump());
        }

        public static void AddOrUpdateInfo(string key, object value)
        {
            _infoEntries[key] = value;
            Instance?.UpdateUI();
        }

        public static void RemoveInfo(string key)
        {
            if (_infoEntries.Remove(key))
                Instance?.UpdateUI();
        }

        private void UpdateUI()
        {
            if (_scrollArea == null) return;
            _scrollArea.Clear();
            int w = _scrollArea.Width - (_scrollArea.ScrollBarWidth() > 0 ? _scrollArea.ScrollBarWidth() : 14) - 10;
            int y = 0;
            foreach (var kv in _infoEntries)
            {
                string text = $"{kv.Key}: {kv.Value?.ToString() ?? ""}";
                string copyVal = kv.Value?.ToString() ?? "";
                var label = new Label(text, true, 0xFFFF, w, font: 1) { X = 0, Y = y };
                if (!string.IsNullOrEmpty(copyVal))
                {
                    label.MouseUp += (s, ev) =>
                    {
                        try
                        {
                            SDL_SetClipboardText(copyVal);
                            GameActions.Print("Copied to clipboard!");
                        }
                        catch { }
                    };
                }
                _scrollArea.Add(label);
                y += label.Height + 2;
            }
            _scrollArea.ScrollMaxHeight = Math.Max(_scrollArea.Height, y);
        }

        public override void OnResize()
        {
            base.OnResize();
            _lastX = X; _lastY = Y; _lastWidth = Width; _lastHeight = Height;
            if (_titleLabel != null)
                _titleLabel.Width = Width - BorderControl.BorderSize * 2 - 20;
            if (_scrollArea != null)
            {
                int scrollY = _titleLabel.Y + _titleLabel.Height + 10;
                _scrollArea.Y = scrollY;
                _scrollArea.Width = Width - 2 * BorderControl.BorderSize;
                _scrollArea.Height = Height - scrollY - BorderControl.BorderSize;
            }
        }

        public override void Dispose()
        {
            Instance = null;
            base.Dispose();
        }
    }
}
