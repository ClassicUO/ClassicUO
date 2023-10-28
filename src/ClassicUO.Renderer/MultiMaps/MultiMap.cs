using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Renderer.MultiMaps
{
    public sealed class MultiMap
    {
        private readonly GraphicsDevice _device;

        public MultiMap(GraphicsDevice device)
        {
            _device = device;
        }

        public SpriteInfo GetMap(int? facet, int width, int height, int startX, int startY, int endX, int endY)
        {
            var multiMapInfo = facet.HasValue && MultiMapLoader.Instance.HasFacet(facet.Value) ?
                MultiMapLoader.Instance.LoadFacet(facet.Value, width, height, startX, startY, endX, endY) :
                MultiMapLoader.Instance.LoadMap(width, height, startX, startY, endX, endY);

            if (multiMapInfo.Pixels.IsEmpty)
                return default;

            var texture = new Texture2D(_device, multiMapInfo.Width, multiMapInfo.Height, false, SurfaceFormat.Color);
            unsafe
            {
                fixed (uint* ptr = multiMapInfo.Pixels)
                {
                    texture.SetDataPointerEXT(0, null, (IntPtr)ptr, sizeof(uint) * multiMapInfo.Width * multiMapInfo.Height);
                }
            }

            return new SpriteInfo()
            {
                Texture = texture,
                UV = new Microsoft.Xna.Framework.Rectangle(0, 0, multiMapInfo.Width, multiMapInfo.Height),
                Center = Microsoft.Xna.Framework.Point.Zero
            };
        }
    }
}
