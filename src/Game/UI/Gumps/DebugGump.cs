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

using System.Text;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DebugGump : Gump
    {
        private const string DEBUG_STRING_0 = "- FPS: {0} (Min={1}, Max={2}), Scale: {3}, Total Objs: {4}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";

        private const string DEBUG_STRING_SMALL = "FPS: {0}";
        private readonly Label _label;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly AlphaBlendControl _trans;

        private bool _fullDisplayMode;

        public DebugGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _fullDisplayMode = !Engine.Profile.Current.DebugGumpIsMinimized;

            Width = 500;
            Height = 275;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width, Height = Height
            });

            Add(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10, Y = 10
            });

            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;
        }

        public bool FullDisplayMode
        {
            get => _fullDisplayMode;
            set
            {
                _fullDisplayMode = value;
                Engine.Profile.Current.DebugGumpIsMinimized = !_fullDisplayMode;
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                FullDisplayMode = !FullDisplayMode;

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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            _sb.Clear();
            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            if (FullDisplayMode && scene != null)
            {
                _sb.AppendFormat(DEBUG_STRING_0, Engine.CurrentFPS, Engine.FPSMin == int.MaxValue ? 0 : Engine.FPSMin, Engine.FPSMax, !World.InGame ? 1f : scene.Scale, scene.RenderedObjectsCount);
                _sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                _sb.AppendFormat(DEBUG_STRING_2, World.InGame ? World.Player.Position : Position.INVALID, Mouse.Position, (SelectedObject.Object as GameObject)?.Position ?? Position.INVALID);
                _sb.AppendFormat(DEBUG_STRING_3, ReadObject(SelectedObject.Object));
            }
            else
                _sb.AppendFormat(DEBUG_STRING_SMALL, Engine.CurrentFPS);

            _label.Text = _sb.ToString();

            return base.Draw(batcher, x, y);
        }

        private string ReadObject(BaseGameObject obj)
        {
            if (obj != null && FullDisplayMode)
            {
                switch (obj)
                {
                    case Mobile mob:

                        return $"Mobile ({mob.Serial})  graphic: {mob.Graphic}  flags: {mob.Flags}  noto: {mob.NotorietyFlag}";

                    case Item item:

                        return $"Item ({item.Serial})  graphic: {item.Graphic}  flags: {item.Flags}  amount: {item.Amount}";

                    case Static st:

                        return $"Static ({st.Graphic})  height: {st.ItemData.Height}  flags: {st.ItemData.Flags}  Alpha: {st.AlphaHue}";

                    case Multi multi:

                        return $"Multi ({multi.Graphic})  height: {multi.ItemData.Height}  flags: {multi.ItemData.Flags}";

                    case GameEffect effect:

                        if (effect.Source is Static s)
                            return $"Static ({s.Graphic})  height: {s.ItemData.Height}  flags: {s.ItemData.Flags}";

                        return "GameEffect";

                    case MessageInfo overhead:

                        return $"TextOverhead type: {overhead.Type}";

                    case Land land:

                        return $"Land ({land.Graphic})  flags: {land.TileData.Flags}";
                }
            }

            return string.Empty;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            Engine.Profile.Current.DebugGumpPosition = Location;
        }
    }
}