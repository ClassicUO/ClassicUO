using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using texture_index = System.Int32;

namespace ClassicUO.Renderer
{
    // can it be useful?
    // https://github.com/raizam/Monogame.TextureAtlas/blob/master/Monogame.TextureAtlas.PipelineExtension/Packing/ArevaloRectanglePacker.cs

    class TextureAtlas : IDisposable
    {
        private readonly int _width, _height;
        private readonly SurfaceFormat _format;
        private readonly GraphicsDevice _device;
        private readonly List<Texture2D> _textureList = new List<Texture2D>();
        private readonly Dictionary<uint, (texture_index, Rectangle)> _spritesBounds = new Dictionary<uint, (texture_index, Rectangle)>();

        private Rectangle _maxBoundsSize;

        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format)
        {
            _device = device;
            _width = width;
            _height= height;
            _format = format;

            ResetCurrentTextureBounds();
        }


        public int TexturesCount => _textureList.Count;


        public unsafe void AddSprite<T>(uint hash, Span<T> pixels, Rectangle spriteBounds) where T : unmanaged
        {
            texture_index index = _textureList.Count - 1;

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D();
            }

            if (TryRegisterSprite(hash, spriteBounds))
            {
                Texture2D texture = _textureList[index];

                Rectangle targetBounds = new Rectangle(0, 0, spriteBounds.Width, spriteBounds.Height);

                //if (_maxBoundsSize.X + targetBounds.Width > _maxBoundsSize.Width)
                //{
                //    targetBounds.X = 0;
                //    targetBounds.Y += _height - _maxBoundsSize.Y;
                //}

                //if (_maxBoundsSize.Y + targetBounds.Height > _maxBoundsSize.Height)
                //{
                //    targetBounds.Y = 0;

                //}

                if (_maxBoundsSize.Contains(targetBounds))
                {

                }
                else // no space for this texture, create a new one
                {
                    targetBounds.X = 0;
                    targetBounds.Y = 0;
                    texture = CreateNewTexture2D();
                }

                PackSprite(texture, pixels, targetBounds);
            }
        }

        private unsafe bool PackSprite<T>(Texture2D texture, Span<T> pixels, Rectangle bounds) where T : unmanaged
        {
            fixed (T* src = pixels)
            {
                texture.SetDataPointerEXT
                (
                    0,
                    bounds,
                    (IntPtr)src,
                    sizeof(T) * pixels.Length
                );
            }

            return true;
        }

        private bool TryRegisterSprite(uint hash, Rectangle bounds)
        {
            if (!_spritesBounds.ContainsKey(hash))
            {
                _spritesBounds[hash] = (_textureList.Count - 1, bounds);

                return true;
            }

            return false;
        }

        private void ResetCurrentTextureBounds()
        {
            _maxBoundsSize.X = 0;
            _maxBoundsSize.Y = 0;
            _maxBoundsSize.Width = _width;
            _maxBoundsSize.Height = _height;
        }

        private Texture2D CreateNewTexture2D()
        {
            ResetCurrentTextureBounds();

            Texture2D texture = new Texture2D(_device, _width, _height, false, _format);

            _textureList.Add(texture);

            return texture;
        }

        public Texture2D GetTexture(uint hash, out Rectangle bounds)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            foreach (Texture2D texture in _textureList)
            {
                if (!texture.IsDisposed)
                {
                    texture.Dispose();
                }
            }

            _textureList.Clear();
            _spritesBounds.Clear();
        }
    }
}
