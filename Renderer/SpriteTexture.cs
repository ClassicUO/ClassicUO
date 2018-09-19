using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ClassicUO.Renderer
{
    public class SpriteTexture : Texture2D
    {
        //private static Dictionary<uint, byte[]> _hitMapCollection = new Dictionary<uint, byte[]>();

        //private static PixelPicking _pixelPicker = new PixelPicking();

        public SpriteTexture(int width, int height, bool is32bit = true) : base(Service.Get<SpriteBatch3D>().GraphicsDevice, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {

        }

        public long Ticks { get; set; }


        //public virtual bool Contains(int x, int y, bool checkpixel = true)
        //{

        //    return _pixelPicker.Get(ID, x, y);

        //    //if (_hitMapCollection.TryGetValue((uint)GetHashCode(), out var hitmap))
        //    //{
        //    //    if (x >= 0 && y >= 0 && x < Width && y < Height)
        //    //    {
        //    //        if (!checkpixel)
        //    //            return true;
        //    //        int pos = (y * Width) + x;

        //    //        if (pos < hitmap.Length)
        //    //            return hitmap[pos] != 0;
        //    //    }
        //    //}
        //    //return false;
        //}

        //public void SetDataForHitBox(int id, uint[] data)
        //{
        //    ID = id;
        //    SetData(data);

        //    _pixelPicker.Set(id, Width, Height, data);

        //    //if (_hitMapCollection.ContainsKey((uint)GetHashCode()))
        //    //    return;

        //    //int pos = 0;
        //    //byte[] hitmap = new byte[Width * Height];

        //    //for (int y = 0; y < Height; y++)
        //    //{
        //    //    for (int x = 0; x < Width; x++)
        //    //    {
        //    //        hitmap[pos] = (byte)(data[pos] != 0 ? 1 : 0);
        //    //        pos++;
        //    //    }
        //    //}

        //    //_hitMapCollection.Add((uint)GetHashCode(), hitmap);
        //}

        //public void SetDataForHitBox(int id, ushort[] data)
        //{
        //    ID = id;
        //    SetData(data);

        //    _pixelPicker.Set(id, Width, Height, data);

        //    //if (_hitMapCollection.ContainsKey((uint)GetHashCode()))
        //    //    return;

        //    //int pos = 0;
        //    //byte[] hitmap = new byte[Width * Height];

        //    //for (int y = 0; y < Height; y++)
        //    //{
        //    //    for (int x = 0; x < Width; x++)
        //    //    {
        //    //        hitmap[pos] = (byte)(data[pos] != 0 ? 1 : 0);
        //    //        pos++;
        //    //    }
        //    //}

        //    //_hitMapCollection.Add((uint)GetHashCode(), hitmap);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    uint hash = (uint)GetHashCode();
        //    if (_hitMapCollection.ContainsKey(hash))
        //        _hitMapCollection.Remove(hash);

        //    base.Dispose(disposing);
        //}

    }

}
