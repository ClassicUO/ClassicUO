using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class DebugGump : Gump
    {
        private readonly StringBuffer _sb = new StringBuffer();
        private readonly Label _label;
        private readonly CheckerTrans _trans;

        private const string DEBUG_STRING_0 = "- FPS: {0}\n";
        //private const string DEBUG_PROFILER = "- Draw: {0,2:P1}\n  Update: {1,2:P1}\n  Fixed: {2,2:P1}\n  AvgDraw: {3,2:G1}ms\n  Run slow: {4}\n";
        //private const string DEBUG_STRING_1 = "- Rendered:\n  mobiles:  {0}\n  items:   {1}\n  statics:  {2}\n  multies:  {3}\n  lands:   {4}\n  effects: {5}\n";
        //private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        //private const string DEBUG_STRING_3 = "- Selected: {0}";


        private const string DEBUG_PROFILER = "- Draw: {0,2:P1}  Update: {1,2:P1}  Fixed: {2,2:P1}  AvgDraw: {3}ms  Run slow: {4}\n";
        private const string DEBUG_STRING_1 = "- Mobiles: {0}   Items: {1}   Statics: {2}   Multi: {3}   Lands: {4}   Effects: {5}\n";
        private const string DEBUG_STRING_2 = "- CharPos: {0}\n- Mouse: {1}\n- InGamePos: {2}\n";
        private const string DEBUG_STRING_3 = "- Selected: {0}";

        public DebugGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            Width = 500;
            Height = 275;

            AddChildren(_trans = new CheckerTrans(.3f)
            {
                Width = Width , Height = Height
            });
            AddChildren(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10, Y = 10
            });

            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;


            StringBuffer.SetCustomFormatter<Position>((buffer, position, view) =>
            {
                buffer.AppendFormat("X:{0}, Y:{1}, Z:{2}", position.X, position.Y, position.Z);
            });

            StringBuffer.SetCustomFormatter<Point>((buffer, position, view) =>
            {
                buffer.AppendFormat("X:{0}, Y:{1}", position.X, position.Y);
            });

            StringBuffer.SetCustomFormatter<GameObject>((buffer, obj, view) =>
            {
                if (obj != null)
                {

                    switch (obj)
                    {
                        case Mobile mob:
                            buffer.AppendFormat("Mobile (0x{0:X8})  graphic: 0x{1:X4}  flags: {2}  noto: {3}", mob.Serial.Value, (ushort) mob.Graphic, mob.Flags.ToString(), mob.NotorietyFlag.ToString());
                            break;
                        case Item item:
                            buffer.AppendFormat("Item (0x{0:X8})  graphic: 0x{1:X4}  flags: {2}  amount: {3}", item.Serial.Value, (ushort)item.Graphic, item.Flags.ToString(), item.Amount);
                            break;
                        case Static st:
                            buffer.AppendFormat("Static (0x{0:X4})  height: {1}  flags: {2}", (ushort) st.Graphic, st.ItemData.Height, st.ItemData.Flags.ToString());
                            break;
                        case Multi multi:
                            buffer.AppendFormat("Multi (0x{0:X4})  height: {1}  flags: {2}", (ushort)multi.Graphic, multi.ItemData.Height, multi.ItemData.Flags.ToString());
                            break;
                        //case GameEffect effect:
                        //    break;
                        //case TextOverhead overhead:
                        //    break;
                        case Land land:
                            buffer.AppendFormat("Static (0x{0:X4})  flags: {1}", (ushort)land.Graphic, land.TileData.Flags.ToString());
                            break;
                    }
                }
               
            });
        }


        public override void Update(double totalMS, double frameMS)
        {
            _trans.Width = Width = _label.Width + 20;
            _trans.Height = Height = _label.Height + 20;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            //double timeDraw = Profiler.GetContext("RenderFrame").TimeInContext;
            //double timeUpdate = Profiler.GetContext("Update").TimeInContext;
            //double timeFixedUpdate = Profiler.GetContext("FixedUpdate").TimeInContext;
            //double timeOutOfContext = Profiler.GetContext("OutOfContext").TimeInContext;
            //double timeTotalCheck = timeOutOfContext + timeDraw + timeUpdate + timeFixedUpdate;
            //double timeTotal = Profiler.TrackedTime;
            //double avgDrawMs = Profiler.GetContext("RenderFrame").AverageTime * 10;
            
            _sb.Clear();
            GameScene scene = Engine.SceneManager.GetScene<GameScene>();

            _sb.AppendFormat(DEBUG_STRING_0, Engine.CurrentFPS);
            //_sb.AppendFormat(DEBUG_PROFILER, (timeDraw / timeTotal), (timeUpdate / timeTotal), (timeFixedUpdate / timeTotal), (int) avgDrawMs, Engine.IsRunningSlowly);
            _sb.AppendFormat(DEBUG_STRING_1, Engine.DebugInfo.MobilesRendered, Engine.DebugInfo.ItemsRendered, Engine.DebugInfo.StaticsRendered, Engine.DebugInfo.MultiRendered, Engine.DebugInfo.LandsRendered, Engine.DebugInfo.EffectsRendered);
            _sb.AppendFormat(DEBUG_STRING_2, World.InGame ? World.Player.Position : Position.Invalid, Mouse.Position, scene?.SelectedObject?.Position ?? Position.Invalid);
            _sb.AppendFormat(DEBUG_STRING_3, scene?.SelectedObject);

            _label.Text = _sb.ToString();

            return base.Draw(batcher, position, hue);
        }
    }
}
