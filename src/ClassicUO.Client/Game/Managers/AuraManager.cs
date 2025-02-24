// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    sealed class Aura : IDisposable
    {
        private static readonly Lazy<BlendState> _blend = new Lazy<BlendState>
        (
            () => new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha
            }
        );

        private Texture2D _texture;
        private readonly int _radius;

        public Aura(int radius)
        {
            _radius = radius;
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y, ushort hue, float depth)
        {
            if (_texture == null || _texture.IsDisposed)
            {
                var w = _radius * 2;
                var h = _radius * 2;
                var data = GenerateBlendedCircleColors(_radius);

                _texture?.Dispose();
                _texture = new Texture2D(batcher.GraphicsDevice, w, h, false, SurfaceFormat.Color);
                _texture.SetData(data);
            }

            x -= (_texture.Width >> 1);
            y -= (_texture.Height >> 1);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, 1);

            batcher.SetBlendState(_blend.Value);
            batcher.Draw(_texture, new Vector2(x, y), null, hueVec, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
            batcher.SetBlendState(null);
        }

        private Color[] GenerateBlendedCircleColors(int radius)
        {
            var width = radius * 2;
            var height = radius * 2;

            var blendedColors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var distance = (int)Math.Sqrt(Math.Pow(x - radius, 2) + Math.Pow(y - radius, 2));

                    if (distance > radius)
                    {
                        blendedColors[x + y * width] = Color.Transparent;
                        continue;
                    }

                    var opacityFactor = 1f - distance / (float)radius;

                    blendedColors[x + y * width] = new Color(opacityFactor, opacityFactor, opacityFactor, opacityFactor);
                }
            }

            return blendedColors;
        }

        public void Dispose()
        {
            if (_texture != null && !_texture.IsDisposed)
            {
                _texture.Dispose();
            }
        }
    }

    internal sealed class AuraManager
    {
        private readonly World _world;
        private readonly Aura _aura;
        private int _saveAuraUnderFeetType;

        public AuraManager(World world)
        {
            _world = world;
            _aura = new Aura(30);
        }

        public bool IsEnabled
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

                    case 1 when _world.Player != null && _world.Player.InWarMode: return true;
                    case 2 when Keyboard.Ctrl && Keyboard.Shift: return true;
                    case 3: return true;
                }
            }
        }

        public void ToggleVisibility()
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

        public void Draw(UltimaBatcher2D batcher, int x, int y, ushort hue, float depth)
        {
            _aura.Draw(batcher, x, y, hue, depth);
        }
    }
}