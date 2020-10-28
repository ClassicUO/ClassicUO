using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    internal static class AuraManager
    {
        /*private readonly BlendState _blend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha
        };*/
        private static Vector3 _auraHueVector = new Vector3(0, 13, 0);

        private static int _saveAuraUnderFeetType;

        public static bool IsEnabled
        {
            get
            {
                if (ProfileManager.CurrentProfile == null)
                {
                    return false;
                }

                switch (ProfileManager.CurrentProfile.AuraUnderFeetType)
                {
                    default:
                    case 0: return false;

                    case 1 when World.Player != null && World.Player.InWarMode: return true;
                    case 2 when Keyboard.Ctrl && Keyboard.Shift: return true;
                    case 3: return true;
                }
            }
        }

        public static Texture2D AuraTexture { get; private set; }

        public static void ToggleVisibility()
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!IsEnabled)
            {
                _saveAuraUnderFeetType = currentProfile.AuraUnderFeetType;
                currentProfile.AuraUnderFeetType = 3;
            }
            else
            {
                currentProfile.AuraUnderFeetType = _saveAuraUnderFeetType;
            }
        }

        public static void CreateAuraTexture(int radius = 30)
        {
            AuraTexture?.Dispose();

            short w = 0;
            short h = 0;
            uint[] data = CircleOfTransparency.CreateCircleTexture(radius, ref w, ref h);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort) (pixel << 3);

                    if (value > 0xFF)
                    {
                        value = 0xFF;
                    }

                    pixel = (uint) ((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            AuraTexture = new Texture2D(Client.Game.GraphicsDevice, w, h);
            AuraTexture.SetData(data);
        }

        public static void Draw(UltimaBatcher2D batcher, int x, int y, ushort hue)
        {
            x -= AuraTexture.Width >> 1;
            y -= AuraTexture.Height >> 1;

            _auraHueVector.X = hue;

            //batcher.SetBlendState(_blend);
            batcher.Draw2D(AuraTexture, x, y, ref _auraHueVector);
            //batcher.SetBlendState(null);
        }
    }
}