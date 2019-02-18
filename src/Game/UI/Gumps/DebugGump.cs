using System.Text;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class DebugGump : Gump
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Label _label;
        private readonly AlphaBlendControl _trans;

        private bool _fullDisplayMode = true;

        private const string DEBUG_STRING_0 = "- FPS: {0}, Scale: {1}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";

        private const string DEBUG_STRING_SMALL = "FPS: {0}";

        public DebugGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            Width = 500;
            Height = 275;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width , Height = Height
            });
            Add(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10, Y = 10
            });

            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _fullDisplayMode = !_fullDisplayMode;
                return true;
            }

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            _trans.Width = Width = _label.Width + 20;
            _trans.Height = Height = _label.Height + 20;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            _sb.Clear();
            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            if (_fullDisplayMode)
            {
                _sb.AppendFormat(DEBUG_STRING_0, Engine.CurrentFPS, !World.InGame ? 1f : Engine.Profile.Current.ScaleZoom);
                _sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                _sb.AppendFormat(DEBUG_STRING_2, World.InGame ? World.Player.Position : Position.INVALID, Mouse.Position, scene?.SelectedObject?.Position ?? Position.INVALID);
                _sb.AppendFormat(DEBUG_STRING_3, ReadObject(scene?.SelectedObject));
            }
            else
                _sb.AppendFormat(DEBUG_STRING_SMALL, Engine.CurrentFPS);

            _label.Text = _sb.ToString();

            return base.Draw(batcher, position, hue);
        }

        private string ReadObject(GameObject obj)
        {
            if (obj != null && _fullDisplayMode)
            {
                switch (obj)
                {
                    case Mobile mob:
                        return string.Format("Mobile ({0})  graphic: {1}  flags: {2}  noto: {3}", mob.Serial, mob.Graphic, mob.Flags, mob.NotorietyFlag);
                    case Item item:
                        return string.Format("Item ({0})  graphic: {1}  flags: {2}  amount: {3}", item.Serial, item.Graphic, item.Flags, item.Amount);
                    case Static st:
                        return string.Format("Static ({0})  height: {1}  flags: {2}  Alpha: {3}", st.Graphic, st.ItemData.Height, st.ItemData.Flags, st.AlphaHue);
                    case Multi multi:
                        return string.Format("Multi ({0})  height: {1}  flags: {2}", multi.Graphic, multi.ItemData.Height, multi.ItemData.Flags);
                    case GameEffect effect:
                        if (effect.Source is Item i)
                            return string.Format("Item ({0})  graphic: {1}  flags: {2}  amount: {3}", i.Serial, i.Graphic, i.Flags, i.Amount);
                        else if (effect.Source is Static s)
                            return string.Format("Static ({0})  height: {1}  flags: {2}", s.Graphic, s.ItemData.Height, s.ItemData.Flags);
                        return string.Format("GameEffect");
                    case TextOverhead overhead:
                        return string.Format("TextOverhead hue: {0}", overhead.Hue);
                    case Land land:
                        return string.Format("Static ({0})  flags: {1}", land.Graphic, land.TileData.Flags);
                }
            }
            return string.Empty;
        }
    }
}
