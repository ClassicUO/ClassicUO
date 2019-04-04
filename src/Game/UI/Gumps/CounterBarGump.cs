using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class CounterBarGump : Gump
    {
        private readonly AlphaBlendControl _background;

        public CounterBarGump() : base(0, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            CanCloseWithRightClick = false;

            X = 0;
            Y = 0;
            Width = 200;
            Height = 34;

            WantUpdateSize = false;

            Add(_background = new AlphaBlendControl() { Width = Width, Height = Height });

            int x = 0;
            for (int i = 0; i < 50; i++)
            {
                Add(new CounterItem() { X = x });

                x += 20 + 10;
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Engine.WindowWidth != _background.Width)
            {
                Width = _background.Width = Engine.WindowWidth;
            }
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }


        class CounterItem : Control
        {
            private TextureControl _controlPic;
            private Graphic _graphic;
            private uint _time;
            private ushort _amount;

            public CounterItem()
            {
                AcceptMouseInput = true;
                WantUpdateSize = false;

                X = 300;
                Width = 20;
                Height = 34;
            }


            protected override void OnMouseUp(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                        return;

                    _graphic = gs.HeldItem.Graphic;

                    _controlPic?.Dispose();
                    _controlPic = new TextureControl()
                    {
                        ScaleTexture = true,
                        Texture = FileManager.Art.GetTexture(_graphic),
                        Hue = gs.HeldItem.Hue,
                        IsPartial = gs.HeldItem.IsPartialHue,
                        Width = Width, // 20x20
                        Height = Width,
                        AcceptMouseInput = false
                    };
                    Add(_controlPic);
                }
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);


                if (_time < Engine.Ticks)
                {
                    _time = (uint) Engine.Ticks + 100;

                   // _amount = 9000;
                    _amount = (ushort)World.Player.Equipment[(int)Layer.Backpack]?.Items?
                       .Where(s => s.Graphic == _graphic)?
                                           .Sum(s => s.Amount);
                }
            }

            public override bool Draw(Batcher2D batcher, int x, int y)
            {
              
                //batcher.Draw2D(CheckerTrans.TransparentTexture, new Rectangle(position.X, position.Y, Width, Width), ShaderHuesTraslator.GetHueVector(0, false, 0.5f, false));


                if (_graphic == 0)
                    return false;

                base.Draw(batcher, x, y);

                string text = _amount.ToString();

                if (_amount >= 1000)
                {
                    text = $"{text[0]}K+";
                }

                if (MouseIsOver)
                    batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Width, Vector3.Zero);


                batcher.DrawString(Fonts.Regular, text, X + 1, Width, Vector3.Zero);


                return true;
            }
        }
    }
}
