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
        private ushort _graphic;
        private Button _button;
        private GumpPic _background;
        private Point _startPoint;
        private GumpDirection _direction;

        public BuffGump() : base(0, 0)
        {
            X = 100;
            Y = 100;

            CanMove = true;
            CanCloseWithRightClick = true;

            _graphic = 0x7580;
            AddChildren(_background = new GumpPic(0, 0, _graphic, 0) { LocalSerial = 1});
            AddChildren(_button = new Button(0, 0x7585, 0x7589, 0x7589)
            {
                X = -2, Y = 36,
                ButtonAction = ButtonAction.Activate
            });
            _direction = GumpDirection.LEFT_HORIZONTAL;
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

            BuffIcon icon = new BuffIcon(graphic, CoreGame.Ticks + timer * 1000, text);
            BuffControlEntry entry = new BuffControlEntry(icon);


            AddChildren(entry);

            UpdateElements();
        }

        public void RemoveBuff(Graphic graphic)
        {
            RemoveChildren(Children.OfType<BuffControlEntry>().FirstOrDefault(s => s.Icon.Graphic == graphic));
            UpdateElements();
        }

        enum GumpDirection
        {
            LEFT_VERTICAL,
            LEFT_HORIZONTAL,
            RIGHT_VERTICAL,
            RIGHT_HORIZONTAL
        }
       

        private void UpdateElements()
        {
            BuffControlEntry[] list = GetControls<BuffControlEntry>();

            int offset = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var e = list[i];

                switch (_direction)
                {
                    case GumpDirection.LEFT_VERTICAL:
                        e.X = 26;
                        e.Y = 25 + offset;
                        offset += 31;
                        break;
                    case GumpDirection.LEFT_HORIZONTAL:
                        e.X = 26 + offset;
                        e.Y = 5;
                        offset += 31;
                        break;
                    case GumpDirection.RIGHT_VERTICAL:
                        e.X = 5;
                        e.Y =  48 + offset;
                        offset -= 31;
                        break;
                    case GumpDirection.RIGHT_HORIZONTAL:
                        e.X = 48 + offset;
                        e.Y = 5;

                        offset -= 31;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }

            }
        }

        public override void Update(double totalMS, double frameMS)
        {

    
            base.Update(totalMS, frameMS);
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0)
            {
                _graphic++;

                if (_graphic > 0x7582)
                    _graphic = 0x757F;


                switch (_graphic)
                {
                    case 0x7580:
                        _button.X = -2;
                        _button.Y = 36;
                        _startPoint = Point.Zero;
                        _direction = GumpDirection.LEFT_HORIZONTAL;
                        break;
                    
                    case 0x7581:
                        _button.X = 34;
                        _button.Y = 78;
                        _startPoint.X = 5;
                        _startPoint.Y = 48;
                        _direction = GumpDirection.RIGHT_VERTICAL;
                        break;
                    
                    case 0x7582:
                        _button.X = 76;
                        _button.Y = 36;
                        _startPoint.X = 48;
                        _startPoint.Y = 5;
                        _direction = GumpDirection.RIGHT_HORIZONTAL;
                        break;
                    
                    case 0x757F:
                    default:
                    
                        _button.X = 0;
                        _button.Y = 0;
                        _startPoint.X = 26;
                        _startPoint.Y = 25;
                        _direction = GumpDirection.LEFT_VERTICAL;
                        break;
                    
                }


                _background.Graphic = _graphic;
                _background.Texture = IO.Resources.Gumps.GetGumpTexture(_graphic);

                UpdateElements();
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
