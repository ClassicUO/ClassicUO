using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ClassicUO.Input
{
    public class MouseManager : GameComponent
    {
        private MouseState _prevMouseState = Mouse.GetState();

        private readonly ushort[,] _cursorData = new ushort[2, 16]
        {
            {
                0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F,
                0x2070, 0x2071, 0x2072, 0x2073, 0x2074, 0x2075,
                0x2076, 0x2077, 0x2078, 0x2079
            },
            {
                0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058,
                0x2059, 0x205A, 0x205B, 0x205C, 0x205D, 0x205E,
                0x205F, 0x2060, 0x2061,
                0x2062
            }
        };

        private readonly int[,] _cursorOffset = new int[2, 16];

        public MouseManager(Game game) : base(game)
        {
           
        }


        private bool _updateTexture;
        private ushort _currentGraphic = 0x2073;
        public ushort CurrentGraphic
        {
            get => _currentGraphic;
            set
            {
                if (_currentGraphic != value)
                {
                    _currentGraphic = value;
                    _updateTexture = true;
                }
            }
        }
        public Texture2D Texture { get; private set; }

        public event EventHandler<MouseEventArgs> MouseDown, MouseUp, MouseMove;
        public event EventHandler<MouseWheelEventArgs> MouseWheel;


        public void LoadTextures()
        {
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    ushort id = _cursorData[i, j];
                    ushort[] pixels = AssetsLoader.Art.ReadStaticArt(id, out short width, out short height);
                    Texture2D texture = new Texture2D(this.Game.GraphicsDevice, width, height, false, SurfaceFormat.Bgra5551);
                    texture.SetData(pixels);

                    if (i == 0)
                    {
                        if (texture != null)
                        {
                            float offX = 0;
                            float offY = 0;

                            float dw = texture.Width;
                            float dh = texture.Height;

                            if (id == 0x206A)
                                offX = -4f;
                            else if (id == 0x206B)
                                offX = -dw + 3f;
                            else if (id == 0x206C)
                            {
                                offX = -dw + 3f;
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x206D)
                            {
                                offX = -dw;
                                offY = -dh;
                            }
                            else if (id == 0x206E)
                            {
                                offX = -(dw * 0.66f);
                                offY = -dh;
                            }
                            else if (id == 0x206F)
                                offY = ((-dh) + 4f);
                            else if (id == 0x2070)
                                offY = ((-dh) + 4f);
                            else if (id == 0x2075)
                                offY = -4f;
                            else if (id == 0x2076)
                            {
                                offX = -12f;
                                offY = -14f;
                            }
                            else if (id == 0x2077)
                            {
                                offX = -(dw / 2f);
                                offY = -(dh / 2f);
                            }
                            else if (id == 0x2078)
                                offY = -(dh * 0.66f);
                            else if (id == 0x2079)
                                offY = -(dh / 2f);

                            switch (id)
                            {
                                case 0x206B:
                                    offX = -29;
                                    offY = -1;
                                    break;
                                case 0x206C:
                                    offX = -41;
                                    offY = -9;
                                    break;
                                case 0x206D:
                                    offX = -36;
                                    offY = -25;
                                    break;
                                case 0x206E:
                                    offX = -14;
                                    offY = -33;
                                    break;
                                case 0x206F:
                                    offX = -2;
                                    offY = -26;
                                    break;
                                case 0x2070:
                                    offX = -3;
                                    offY = -8;
                                    break;
                                case 0x2071:
                                    offX = -1;
                                    offY = -1;
                                    break;
                                case 0x206A:
                                    offX = -4;
                                    offY = -2;
                                    break;
                                case 0x2075:
                                    offX = -2;
                                    offY = -10;
                                    break;
                                default:
                                    break;
                            }

                            _cursorOffset[0, j] = (int)offX;
                            _cursorOffset[1, j] = (int)offY;
                        }
                        else
                        {
                            _cursorOffset[0, j] = 0;
                            _cursorOffset[1, j] = 0;
                        }
                    }
                }
            }
        }

        private Texture2D _blackTexture;

        public void BeginDraw()
        {
            if (Texture == null || _updateTexture)
            {
                ushort[] pixels = AssetsLoader.Art.ReadStaticArt(CurrentGraphic, out var w, out var h);
                Texture = new Texture2D(this.Game.GraphicsDevice, w, h, false, SurfaceFormat.Bgra5551);
                Texture.SetData(pixels);
                _updateTexture = false;

                _blackTexture = new Texture2D(this.Game.GraphicsDevice, 1, 1);
                _blackTexture.SetData(new Color[] { Color.Black });
            }
        }

        public void Draw(in SpriteBatchUI sb)
        {
            ushort id = CurrentGraphic;

            if (id < 0x206A)
                id -= 0x2053;
            else
            {
                id -= 0x206A;
            }

            if (id < 16)
            {
                sb.Draw2D(Texture, 
                    new Vector3( _prevMouseState.X + _cursorOffset[0, id], _prevMouseState.Y + _cursorOffset[1, id], 0),
                    RenderExtentions.GetHueVector(0));

                // tooltip testing, very nice!
                //sb.Draw2D(_blackTexture, new Rectangle(_prevMouseState.X + _cursorOffset[0, id] - 100, _prevMouseState.Y + _cursorOffset[1, id] - 50, 100, 50), new Vector3(0, 0, 0.3f));
            }
        }


        public override void Update(GameTime gameTime)
        {
            MouseState current = Mouse.GetState();

            if (IsMouseButtonDown(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Left, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Right, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Middle, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton1, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }
            else if (IsMouseButtonDown(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton2, ButtonState.Pressed);
                MouseDown.Raise(arg);
            }


            if (IsMouseButtonUp(current.LeftButton, _prevMouseState.LeftButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Left, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.RightButton, _prevMouseState.RightButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Right, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.MiddleButton, _prevMouseState.MiddleButton))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.Middle, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton1, _prevMouseState.XButton1))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton1, ButtonState.Released);
                MouseUp.Raise(arg);
            }
            else if (IsMouseButtonUp(current.XButton2, _prevMouseState.XButton2))
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, MouseButton.XButton2, ButtonState.Released);
                MouseUp.Raise(arg);
            }


            if (current.ScrollWheelValue != _prevMouseState.ScrollWheelValue)
            {
                MouseWheelEventArgs arg = new MouseWheelEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y, current.ScrollWheelValue == 0 ? WheelDirection.None : current.ScrollWheelValue > 0 ? WheelDirection.Up : WheelDirection.Down);
                MouseWheel.Raise(arg);
            }

            if (current.X != _prevMouseState.X || current.Y != _prevMouseState.Y)
            {
                MouseEventArgs arg = new MouseEventArgs(current.X, current.Y, _prevMouseState.X, _prevMouseState.Y);
                MouseMove.Raise(arg);
                Log.Message(LogTypes.Trace, arg.Location.ToString());
            }

            _prevMouseState = current;

            base.Update(gameTime);
        }

       
        private bool IsMouseButtonDown(ButtonState current, ButtonState prev) => current == ButtonState.Pressed && prev == ButtonState.Released;
        private bool IsMouseButtonUp(ButtonState current, ButtonState prev) => current == ButtonState.Released && prev == ButtonState.Pressed;
    }
}
