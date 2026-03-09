using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System.Xml;

namespace ClassicUO.LegionScripting
{
    internal class RunningScriptsGump : ResizableGump
    {
        private const int MIN_WIDTH = 280;
        private const int MIN_HEIGHT = 200;
        private AlphaBlendControl _bg;
        private ScrollArea _scroll;
        private UOLabel _title;
        private NiceButton _createBtn;
        private static int _lastX = -1;
        private static int _lastY = -1;
        private static int _lastWidth = 320;
        private static int _lastHeight = 350;

        public override GumpType GumpType => GumpType.RunningScripts;

        public RunningScriptsGump() : base(_lastWidth, _lastHeight, MIN_WIDTH, MIN_HEIGHT, 0, 0)
        {
            X = _lastX;
            Y = _lastY;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            CanMove = true;
            AnchorType = ANCHOR_TYPE.DISABLED;

            int border = BorderControl.BorderSize;
            int headerH = 40;

            Add(_bg = new AlphaBlendControl(0.77f) { X = border, Y = border });
            Add(_title = new UOLabel("Scripts Running", 1, UOLabelHue.Accent, Assets.TEXT_ALIGN_TYPE.TS_CENTER, Width, FontStyle.BlackBorder) { Y = border });
            Add(_createBtn = new NiceButton(border, headerH - 32, 80, 26, ButtonAction.Default, "Create New") { IsSelectable = false });
            _createBtn.MouseUp += OnCreate;
            Add(_scroll = new ScrollArea(border, headerH + border, Width - border * 2, Height - headerH - border * 2, true) { ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways });

            LegionScripting.ScriptStartedEvent += OnScriptChanged;
            LegionScripting.ScriptStoppedEvent += OnScriptChanged;
            BuildList();

            if (_lastX == -1 && _lastY == -1)
            {
                CenterXInViewPort();
                CenterYInViewPort();
            }
        }

        private void OnCreate(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtonType.Left) return;
            var studio = UIManager.GetGump<LegionScriptStudioGump>();
            if (studio != null)
                studio.SetInScreen();
            else
                UIManager.Add(new LegionScriptStudioGump());
        }

        private void OnScriptChanged(object sender, ScriptInfoEvent ev)
        {
            BuildList();
        }

        private void BuildList()
        {
            while (_scroll.Children.Count > 1)
                _scroll.Remove(_scroll.Children[1]);
            var running = LegionScripting.GetRunningScripts();
            int y = 0;
            int w = _scroll.Width - (_scroll.ScrollBarWidth() > 0 ? _scroll.ScrollBarWidth() : 14) - 8;
            if (running.Count == 0)
            {
                var lbl = new Label("No scripts running", true, 0x8080, w, font: 1) { X = 4, Y = y };
                _scroll.Add(lbl);
                return;
            }
            foreach (ScriptFile sf in running)
            {
                string display = string.IsNullOrEmpty(sf.SubGroup) ? sf.FileName : $"{sf.SubGroup}/{sf.FileName}";
                if (!string.IsNullOrEmpty(sf.Group) && sf.Group != ScriptManagerGump.NOGROUPTEXT)
                    display = $"{sf.Group}/{display}";
                var row = new RunningScriptRow(w, sf, display);
                row.Y = y;
                _scroll.Add(row);
                y += row.Height + 2;
            }
        }

        public override void OnResize()
        {
            base.OnResize();
            if (_bg == null) return;
            int border = BorderControl.BorderSize;
            int headerH = 40;
            _bg.Width = Width - border * 2;
            _bg.Height = Height - border * 2;
            _title.Width = Width - border * 2;
            _scroll.X = border;
            _scroll.Y = headerH + border;
            _scroll.Width = Width - border * 2;
            _scroll.Height = Height - headerH - border * 2;
            _scroll.UpdateScrollbarPosition();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);
            _lastX = X;
            _lastY = Y;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("rw", Width.ToString());
            writer.WriteAttributeString("rh", Height.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            if (int.TryParse(xml.GetAttribute("rw"), out int w) && w >= MIN_WIDTH &&
                int.TryParse(xml.GetAttribute("rh"), out int h) && h >= MIN_HEIGHT)
            {
                ResizeWindow(new Point(w, h));
                OnResize();
            }
        }

        public override void Dispose()
        {
            LegionScripting.ScriptStartedEvent -= OnScriptChanged;
            LegionScripting.ScriptStoppedEvent -= OnScriptChanged;
            base.Dispose();
        }

        private class RunningScriptRow : Control
        {
            private readonly ScriptFile _script;
            private readonly NiceButton _stopBtn;
            private readonly AlphaBlendControl _bg;

            public RunningScriptRow(int w, ScriptFile sf, string display)
            {
                _script = sf;
                Width = w;
                Height = 28;
                CanMove = true;
                Add(_bg = new AlphaBlendControl(0.35f) { Width = w, Height = Height, BaseColor = Color.DarkGreen });
                Add(new Label(display, true, 0xFFFF, w - 60, font: 1) { X = 6, Y = 4, AcceptMouseInput = false });
                Add(_stopBtn = new NiceButton(w - 55, 2, 50, 24, ButtonAction.Default, "Stop") { IsSelectable = false });
                _stopBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                        LegionScripting.StopScript(_script);
                };
            }
        }
    }
}
