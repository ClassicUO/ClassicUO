using System.Threading.Tasks;
using ClassicUO.Game;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class LightsLoader : UOFileLoader<UOTexture>
    {
        private static LightsLoader _instance;
        private UOFileMul _file;

        private LightsLoader(int count) : base(count)
        {
        }

        public static LightsLoader Instance =>
            _instance ?? (_instance = new LightsLoader(Constants.MAX_LIGHTS_DATA_INDEX_COUNT));

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("light.mul");
                    string pathidx = UOFileManager.GetUOFilePath("lightidx.mul");

                    FileSystemHelper.EnsureFileExists(path);
                    FileSystemHelper.EnsureFileExists(pathidx);

                    _file = new UOFileMul(path, pathidx, Constants.MAX_LIGHTS_DATA_INDEX_COUNT);
                    _file.FillEntries(ref Entries);
                }
            );
        }

        public override UOTexture GetTexture(uint id)
        {
            if (id >= Resources.Length)
            {
                return null;
            }

            ref UOTexture texture = ref Resources[id];

            if (texture == null || texture.IsDisposed)
            {
                uint[] pixels = GetLight(id, out int w, out int h);

                if (w == 0 && h == 0)
                {
                    return null;
                }

                texture = new UOTexture(w, h);
                texture.PushData(pixels);

                SaveId(id);
            }
            else
            {
                texture.Ticks = Time.Ticks;
            }

            return texture;
        }


        private uint[] GetLight(uint idx, out int width, out int height)
        {
            ref UOFileIndex entry = ref GetValidRefEntry((int) idx);

            width = entry.Width;
            height = entry.Height;

            if (width == 0 && height == 0)
            {
                return null;
            }

            uint[] pixels = new uint[width * height];

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            for (int i = 0; i < height; i++)
            {
                int pos = i * width;

                for (int j = 0; j < width; j++)
                {
                    ushort val = _file.ReadByte();
                    val = (ushort) ((val << 10) | (val << 5) | val);

                    if (val != 0)
                    {
                        pixels[pos + j] = HuesHelper.Color16To32(val) | 0xFF_00_00_00;
                        ;
                    }
                }
            }

            return pixels;
        }
    }
}