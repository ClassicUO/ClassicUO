#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DebugGump : Gump
    {
        private const string DEBUG_STRING_0 = "- FPS: {0} (Min={1}, Max={2}), Zoom: {3}, Total Objs: {4}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";

        private const string DEBUG_STRING_SMALL = "FPS: {0}\nZoom: {1}";
        private const string DEBUG_STRING_SMALL_NO_ZOOM = "FPS: {0}";

        private readonly StringBuilder _sb = new StringBuilder();
        private readonly AlphaBlendControl _trans;
        private uint _time_to_update;
        private static Point _last_position = new Point(-1, - 1);

        public DebugGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            Width = 100;
            Height = 50;
            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width, Height = Height
            });
            
            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = true;
        }

        public bool IsMinimized { get; set; }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_DEBUG;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Time.Ticks > _time_to_update)
            {
                _time_to_update = Time.Ticks + 100;

                _sb.Clear();
                GameScene scene = Client.Game.GetScene<GameScene>();

                if (IsMinimized && scene != null)
                {
                    _sb.AppendFormat(DEBUG_STRING_0, CUOEnviroment.CurrentRefreshRate, 0, 0, !World.InGame ? 1f : scene.Scale, scene.RenderedObjectsCount);
                    _sb.AppendLine($"- CUO version: {CUOEnviroment.Version}, Client version: {Settings.GlobalSettings.ClientVersion}");
                    //_sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                    _sb.AppendFormat(DEBUG_STRING_2, World.InGame ? $"{World.Player.X}, {World.Player.Y}, {World.Player.Z}" : "0xFFFF, 0xFFFF, 0", Mouse.Position, SelectedObject.Object is GameObject gobj ? $"{gobj.X}, {gobj.Y}, {gobj.Z}" : "0xFFFF, 0xFFFF, 0");
                    _sb.AppendFormat(DEBUG_STRING_3, ReadObject(SelectedObject.Object));
                }
                else if (scene != null && scene.ScalePos != 5)
                {
                    _sb.AppendFormat(DEBUG_STRING_SMALL, CUOEnviroment.CurrentRefreshRate, !World.InGame ? 1f : scene.Scale);
                }
                else
                {
                    _sb.AppendFormat(DEBUG_STRING_SMALL_NO_ZOOM, CUOEnviroment.CurrentRefreshRate);
                }


                var size = Fonts.Bold.MeasureString(_sb.ToString());

                _trans.Width = Width = (int) (size.X + 20);
                _trans.Height = Height = (int) (size.Y + 20);

                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
                return false;

            ResetHueVector();
            batcher.DrawString(Fonts.Bold, _sb.ToString(), x + 10, y + 10, ref _hueVector);

            return true;
        }

        private string ReadObject(BaseGameObject obj)
        {
            if (obj != null && IsMinimized)
            {
                switch (obj)
                {
                    case Mobile mob:

                        return $"Mobile (0x{mob.Serial:X8})  graphic: 0x{mob.Graphic:X4}  flags: {mob.Flags}  noto: {mob.NotorietyFlag}";

                    case Item item:

                        return $"Item (0x{item.Serial:X8})  graphic: 0x{item.Graphic:X4}  flags: {item.Flags}  amount: {item.Amount} itemdata: {item.ItemData.Flags}";

                    case Static st:

                        return $"Static (0x{st.Graphic:X4})  height: {st.ItemData.Height}  flags: {st.ItemData.Flags}  Alpha: {st.AlphaHue}";

                    case Multi multi:

                        return $"Multi (0x{multi.Graphic:X4})  height: {multi.ItemData.Height}  flags: {multi.ItemData.Flags}";

                    case GameEffect effect:
                        return "GameEffect";

                    case TextObject overhead:

                        return $"TextOverhead type: {overhead.Type}  hue: 0x{overhead.Hue:X4}";

                    case Land land:

                        return $"Land (0x{land.Graphic:X4})  flags: {land.TileData.Flags}";
                }
            }

            return string.Empty;
        }


        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }
    }
}