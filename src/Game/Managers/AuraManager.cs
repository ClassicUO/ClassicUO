using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Input;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    class AuraManager
    {
        private Vector3 _auraHueVector = new Vector3(0, 1, 0);

        public bool IsEnabled
        {
            get
            {
                if (Engine.Profile.Current == null)
                    return false;

                switch (Engine.Profile.Current.AuraUnderFeetType)
                {
                    default:
                    case 0: return false;
                    case 1 when World.Player != null && World.Player.InWarMode: return true;
                    case 2 when Keyboard.Ctrl && Keyboard.Shift: return true;
                    case 3: return true;
                }
            }        
        }

        public Texture2D AuraTexture { get; private set; }


        public void CreateAuraTexture(int radius = 30)
        {
            AuraTexture?.Dispose();

            short w = 0;
            short h = 0;
            uint[] data = CircleOfTransparency.CreateTexture(radius, ref w, ref h);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort)(pixel << 3);

                    if (value > 0xFF)
                        value = 0xFF;

                    pixel = (uint)((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            AuraTexture = new Texture2D(Engine.Batcher.GraphicsDevice, w, h);
            AuraTexture.SetData(data);
        }

        public void Draw(Batcher2D batcher, int x, int y, Hue hue)
        {
            x -= AuraTexture.Width >> 1;
            y -= AuraTexture.Height >> 1;

            _auraHueVector.X = hue;

            batcher.SetBlendState(_blend);
            batcher.Draw2D(AuraTexture, x, y, _auraHueVector);
            batcher.SetBlendState(null);
        }

        private readonly BlendState _blend = new BlendState()
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha
        };


    }
}
