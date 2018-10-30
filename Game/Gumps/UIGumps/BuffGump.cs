using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class BuffGump : Gump
    {
        private Graphic _graphic;
        private Button _button;
        private GumpPic _background;

        public BuffGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = true;

            _graphic = 0x7580;
            AddChildren(_background = new GumpPic(0, 0, _graphic, 0) { LocalSerial = 1});
            AddChildren(_button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                X = -2, Y = 36,
                ButtonAction = ButtonAction.Activate
            });

        }


        private static BuffGump _gump;

        public static void Toggle()
        {
            UIManager ui = Service.Get<UIManager>();

            if (ui.GetByLocalSerial<BuffGump>() == null)
                ui.Add(_gump = new BuffGump());
            else
                _gump.Dispose();
        }

        public void AddBuff(Graphic graphic, ushort timer, string text)
        {
            BuffIcon icon = new BuffIcon(graphic, timer, text);

            var list = GetControls<BuffControlEntry>();

            BuffControlEntry entry = new BuffControlEntry(icon)
            {
                X = list.Length == 0 ? 20 : list[list.Length - 1].Bounds.Right,
                Y = 5
            };

            AddChildren(entry);
        }

        public void RemoveBuff(Graphic graphic)
        {
            RemoveChildren(Children.OfType<BuffControlEntry>().FirstOrDefault(s => s.Icon.Graphic == graphic));
        }

        public override void Update(double totalMS, double frameMS)
        {
            

            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                switch (_graphic)
                {
                    case 0x7580:
                        _button.X = -2;
                        _button.Y = 36;
                        _graphic = 0x7582;
                        break;
                    
                    case 0x7581:
                        _button.X = 34;
                        _button.Y = 78;
                        _graphic = 0x757F;
                        break;
                    
                    case 0x7582:
                        _button.X = 76;
                        _button.Y = 36;
                        _graphic = 0x7581;
                        break;
                    
                    case 0x757F:
                    default:
                    
                        _button.X = 0;
                        _button.Y = 0;
                        _graphic = 0x7580;
                        break;
                    
                }


                _background.Graphic = _graphic;
                _background.Texture = IO.Resources.Gumps.GetGumpTexture(_graphic);
            }
        }


        class BuffControlEntry : GumpPic
        {
            public BuffControlEntry(BuffIcon icon) : base(0, 0, icon.Graphic, 0)
            {
                Icon = icon;
                Texture = IO.Resources.Gumps.GetGumpTexture(icon.Graphic);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            public float Alpha { get; set; }
            public bool DecreaseAlpha { get; set; }
            public BuffIcon Icon { get; }


            public override void Update(double totalMS, double frameMS)
            {
                Texture.Ticks = (long) totalMS;

                base.Update(totalMS, frameMS);
            }

            //public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
            //{
            //    spriteBatch.Draw2D(Texture,)
            //}
        }
    }
}
