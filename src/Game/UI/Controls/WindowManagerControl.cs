using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Text;
using ClassicUO.Resources;

namespace ClassicUO.Game.UI.Gumps
{
    /// <summary>
    /// Window Manager
    /// - Reset Position of Gump on a screen
    /// </summary>
    internal sealed class WindowManagerControl: Control
    {
        private const ushort HUE_FONT = 0xFFFF;
        private const ushort BACKGROUND_COLOR = 999;
        private const ushort GUMP_WIDTH = 470;
        private const ushort GUMP_HEIGHT = 400;

        public WindowManagerControl(): base()
        {
            CanMove = true;

            Add
            (
                new AlphaBlendControl(0.05f)
                {
                    X = 1,
                    Y = 1,
                    Width = GUMP_WIDTH,
                    Height = GUMP_HEIGHT,
                    Hue = BACKGROUND_COLOR,
                    AcceptMouseInput = true,
                    CanMove = true,
                    CanCloseWithRightClick = true,
                }
            );

            #region Legend
            Add(new Label(ResGumps.UIManagerGumpName, true, HUE_FONT, 0, 255, Renderer.FontStyle.BlackBorder) { X = 5, Y = 10 });
            Add(new Label("X", true, HUE_FONT, 0, 255, Renderer.FontStyle.BlackBorder) { X = 300, Y = 10 });
            Add(new Label("Y", true, HUE_FONT, 0, 255, Renderer.FontStyle.BlackBorder) { X = 340, Y = 10 });
            Add(new Label(ResGumps.UIManagerGumpReset, true, HUE_FONT, 0, 255, Renderer.FontStyle.BlackBorder) { X = 390, Y = 10 });
            Add(new Line(0, 30, GUMP_WIDTH, 1, Color.Gray.PackedValue));
            #endregion

            DataBox box = new DataBox(10, 45, GUMP_WIDTH - 20, GUMP_HEIGHT - 60);
            box.WantUpdateSize = true;

            var y = 0;
            foreach (var gump in UIManager.Gumps.Where(x => x.CanMove && x.Parent == null && x.IsVisible))
            {
                box.Add(new WindowManagerEntryControl(gump) { Y = y });
                y += 20;
            }
            Add(box);

            box.ReArrangeChildren();

            Height = box.Bounds.Bottom;
        }

        private sealed class WindowManagerEntryControl : Control
        {
            private readonly Gump _gump;

            public WindowManagerEntryControl(Gump gump)
            {
                CanMove = true;
                AcceptMouseInput = false;

                _gump = gump;
                StringBuilder sb = new StringBuilder(gump.ToString().Split('.').Last());
                // If gump is Anchorable Append text
                if(_gump is AnchorableGump aGump)
                {
                    switch (aGump.AnchorType) {
                        case ANCHOR_TYPE.SPELL:
                            sb.Append($" [{aGump.Tooltip}]");
                            break;
                        case ANCHOR_TYPE.SKILL:
                            sb.Append($" [{(aGump as SkillButtonGump)?.SkillName}]");
                            break;
                        case ANCHOR_TYPE.HEALTHBAR:
                            if (aGump is HealthBarGump hb)
                            {
                                sb.Append($" [{hb.Name}]");
                            } else if (aGump is HealthBarGumpCustom hbc)
                            {
                                sb.Append($" [{hbc.Name}]");
                            }
                            break;
                        case ANCHOR_TYPE.MACRO:
                            sb.Append($" [{(aGump as MacroButtonGump)?._macro.Name}]");
                            break;
                    }
                }
                //Gump Name
                Label label;
                Add(label = new Label(sb.ToString(), true, HUE_FONT, 290) { X = 10 });
                //Gump X
                Add(new Label(_gump.X.ToString(), true, HUE_FONT, 250) { X = 290 });
                //Gump Y
                Add(new Label(_gump.Y.ToString(), true, HUE_FONT, 250) { X = 330 });
                //Gump Reset button
                Add(new Button(1, 0xFAB, 0xFAC) { X = 380, ButtonAction = ButtonAction.Activate });

                Height = label.Height;
            }

            public override void OnButtonClick(int buttonId)
            {
                //Center of Game Window
                var x = ProfileManager.CurrentProfile.GameWindowSize.X >> 1;
                var y = ProfileManager.CurrentProfile.GameWindowSize.Y >> 1;

                switch (buttonId)
                {
                    case 1:
                        if(_gump is AnchorableGump aGump)
                        {
                            // If AnchorableGump is anchored to another gump we need to Update Location of all anchored gumps
                            var aManager = UIManager.AnchorManager[aGump];
                            if (aManager != null)
                            {
                                aManager.UpdateLocation(this, -aGump.X + x, -aGump.Y + y);
                                return;
                            }
                        }

                        var deltaX = _gump.X - x;
                        var deltaY = _gump.Y = y;

                        _gump.X = x;
                        _gump.Y = y;

                        _gump.InvokeMove(deltaX, deltaY);;

                        break;
                }
            }
        }
    }
}
