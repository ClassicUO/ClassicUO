using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using StbRectPackSharp;

namespace ClassicUO.Renderer
{
    class TextureAtlas : IDisposable
    {
        private readonly int _width, _height;
        private readonly SurfaceFormat _format;
        private readonly GraphicsDevice _device;
        private readonly List<Texture2D> _textureList;
        private Packer _packer;
        //private readonly Dictionary<uint, (int, Rectangle)> _spritesBounds = new Dictionary<uint, (int, Rectangle)>();

        private readonly Rectangle[] _spriteBounds;
        private readonly int[] _spriteTextureIndices;

        public TextureAtlas(GraphicsDevice device, int width, int height, SurfaceFormat format, int maxSpriteCount)
        {
            _device = device;
            _width = width;
            _height= height;
            _format = format;

            _textureList = new List<Texture2D>();
            _spriteBounds = new Rectangle[maxSpriteCount];
            _spriteTextureIndices = new int[maxSpriteCount];
        }


        public int TexturesCount => _textureList.Count;


        public unsafe void AddSprite<T>(uint hash, Span<T> pixels, int width, int height) where T : unmanaged
        {
            //if (_spritesBounds.ContainsKey(hash))
            //{
            //    return;
            //}

            if (IsHashExists(hash))
            {
                return;
            }

            var index = _textureList.Count - 1;

            if (index < 0)
            {
                index = 0;
                CreateNewTexture2D();
            }


            PackerRectangle pr;
            while (!_packer.PackRect(width, height, hash, out pr))
            {
                CreateNewTexture2D();
                index = _textureList.Count - 1;
            }

            Texture2D texture = _textureList[index];

            fixed (T* src = pixels)
            {
                texture.SetDataPointerEXT
                (
                    0,
                    pr.Rectangle,
                    (IntPtr)src,
                    sizeof(T) * pixels.Length
                );
            }

            //_spritesBounds[hash] = (index, pr.Rectangle);

            _spriteBounds[hash] = pr.Rectangle;
            _spriteTextureIndices[hash] = index;
        }

        private void CreateNewTexture2D()
        {
            Texture2D texture = new Texture2D(_device, _width, _height, false, _format);
            _textureList.Add(texture);

            _packer?.Dispose();
            _packer = new Packer(_width, _height);
        }

        public Texture2D GetTexture(uint hash, out Rectangle bounds)
        {
            //if (_spritesBounds.TryGetValue(hash, out var v))
            //{
            //    bounds = v.Item2;
            //    return _textureList[v.Item1];
            //}

            //bounds = Rectangle.Empty;
            //return null;

            bounds = _spriteBounds[(int)hash];
            return _textureList[_spriteTextureIndices[(int) hash]];
        }

        public bool IsHashExists(uint hash) => _spriteTextureIndices[(int)hash] > 0;

        public void SaveImages(string name)
        {
            for (int i = 0, count = TexturesCount; i < count; ++i)
            {
                var texture = _textureList[i];

                using (var stream = System.IO.File.Create($"atlas/{name}_atlas_{i}.png"))
                {
                    texture.SaveAsPng(stream, texture.Width, texture.Height);
                }
            }
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
            _packer.Dispose();
            _textureList.Clear();
            //_spritesBounds.Clear();
        }
    }
}
