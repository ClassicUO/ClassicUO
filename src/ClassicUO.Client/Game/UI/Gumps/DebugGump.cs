#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class DebugGump : Gump
    {
        private const string DEBUG_STRING_0 = "- FPS: {0} (Min={1}, Max={2}), Zoom: {3:0.00}, Total Objs: {4}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";

        private const string DEBUG_STRING_SMALL = "FPS: {0}\nZoom: {1:0.00}";
        private const string DEBUG_STRING_SMALL_NO_ZOOM = "FPS: {0}";
        private static Point _last_position = new Point(-1, -1);

        private uint _timeToUpdate;
        private readonly AlphaBlendControl _alphaBlendControl;
        private string _cacheText = string.Empty;

        public DebugGump(World world, int x, int y) : base(world, 0, 0)
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

            Add
            (
                _alphaBlendControl = new AlphaBlendControl(.7f)
                {
                    Width = Width, Height = Height
                }
            );

            LayerOrder = UILayer.Over;

            WantUpdateSize = true;
        }

        public bool IsMinimized { get; set; }

        public override GumpType GumpType => GumpType.Debug;

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (Time.Ticks > _timeToUpdate)
            {
                _timeToUpdate = Time.Ticks + 100;

                GameScene scene = Client.Game.GetScene<GameScene>();
                Span<char> span = stackalloc char[256];
                ValueStringBuilder sb = new ValueStringBuilder(span);
           
                if (IsMinimized && scene != null)
                {
                    sb.Append
                    (string.Format(
                         DEBUG_STRING_0,
                         CUOEnviroment.CurrentRefreshRate,
                         0,
                         0,
                         !World.InGame ? 1f : scene.Camera.Zoom,
                         scene.RenderedObjectsCount
                         )
                     );

                    sb.Append($"- CUO version: {CUOEnviroment.Version}, Client version: {Settings.GlobalSettings.ClientVersion}\n");

                    //_sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
                    sb.Append(string.Format(DEBUG_STRING_2, World.InGame ? $"{World.Player.X}, {World.Player.Y}, {World.Player.Z}" : "0xFFFF, 0xFFFF, 0", Mouse.Position, SelectedObject.Object is GameObject gobj ? $"{gobj.X}, {gobj.Y}, {gobj.Z}" : "0xFFFF, 0xFFFF, 0"));

                    sb.Append(string.Format(DEBUG_STRING_3, ReadObject(SelectedObject.Object)));

                    if (Profiler.Enabled)
                    {
                        double timeDraw = Profiler.GetContext("RenderFrame").TimeInContext;

                        double timeUpdate = Profiler.GetContext("Update").TimeInContext;

                        double timeFixedUpdate = Profiler.GetContext("FixedUpdate").TimeInContext;

                        double timeOutOfContext = Profiler.GetContext("OutOfContext").TimeInContext;

                        //double timeTotalCheck = timeOutOfContext + timeDraw + timeUpdate;
                        double timeTotal = Profiler.TrackedTime;

                        double avgDrawMs = Profiler.GetContext("RenderFrame").AverageTime;

                        sb.Append("- Profiling\n");

                        sb.Append
                        (
                            string.Format
                            (
                                "    Draw:{0:0.0}% Update:{1:0.0}% FixedUpd:{2:0.0} AvgDraw:{3:0.0}ms {4}\n",
                                100d * (timeDraw / timeTotal),
                                100d * (timeUpdate / timeTotal),
                                100d * (timeFixedUpdate / timeTotal),
                                avgDrawMs,
                                CUOEnviroment.CurrentRefreshRate
                            )
                        );
                    }
                }
                else
                {
                    var cameraZoomCount = (int)((scene.Camera.ZoomMax - scene.Camera.ZoomMin) / scene.Camera.ZoomStep);
                    var cameraZoomIndex = cameraZoomCount - (int)((scene.Camera.ZoomMax - scene.Camera.Zoom) / scene.Camera.ZoomStep);

                    if (scene != null && cameraZoomIndex != 5)
                    {
                        sb.Append(string.Format(DEBUG_STRING_SMALL, CUOEnviroment.CurrentRefreshRate, !World.InGame ? 1f : scene.Camera.Zoom));
                    }
                    else
                    {
                        sb.Append(string.Format(DEBUG_STRING_SMALL_NO_ZOOM, CUOEnviroment.CurrentRefreshRate));
                    }
                }
                

                _cacheText = sb.ToString();

                sb.Dispose();

                Vector2 size = Fonts.Bold.MeasureString(_cacheText);

                _alphaBlendControl.Width = Width = (int) (size.X + 20);
                _alphaBlendControl.Height = Height = (int) (size.Y + 20);

                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawString
            (
                Fonts.Bold,
                _cacheText,
                x + 10,
                y + 10,
                hueVector
            );

            return true;
        }

        private string ReadObject(BaseGameObject obj)
        {
            if (obj != null && IsMinimized)
            {
                switch (obj)
                {
                    case Mobile mob: return $"Mobile (0x{mob.Serial:X8})  graphic: 0x{mob.Graphic:X4}  flags: {mob.Flags}  noto: {mob.NotorietyFlag}";

                    case Item item: return $"Item (0x{item.Serial:X8})  graphic: 0x{item.Graphic:X4}  flags: {item.Flags}  amount: {item.Amount} itemdata: {item.ItemData.Flags}";

                    case Static st: return $"Static (0x{st.Graphic:X4})  height: {st.ItemData.Height}  flags: {st.ItemData.Flags}  Alpha: {st.AlphaHue}";

                    case Multi multi: return $"Multi (0x{multi.Graphic:X4})  height: {multi.ItemData.Height}  flags: {multi.ItemData.Flags}";

                    case GameEffect effect: return "GameEffect";

                    case TextObject overhead: return $"TextOverhead type: {overhead.Type}  hue: 0x{overhead.Hue:X4}";

                    case Land land: return $"Land (0x{land.Graphic:X4})  flags: {land.TileData.Flags} stretched: {land.IsStretched}  avgZ: {land.AverageZ} minZ: {land.MinZ}";
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