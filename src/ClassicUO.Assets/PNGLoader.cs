using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public class PNGLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private string exePath;

        private uint[] gump_availableIDs;
        private Dictionary<uint, Texture2D> gump_textureCache = new Dictionary<uint, Texture2D>();

        private uint[] art_availableIDs;
        private Dictionary<uint, Texture2D> art_textureCache = new Dictionary<uint, Texture2D>();

        public GraphicsDevice GraphicsDevice { set; get; }

        public static PNGLoader _instance;
        public static PNGLoader Instance => _instance ?? (_instance = new PNGLoader());

        public Texture2D GetImageTexture(string fullImagePath)
        {
            Texture2D texture = null;

            if (GraphicsDevice != null && File.Exists(fullImagePath))
            {
                FileStream titleStream = File.OpenRead(fullImagePath);
                texture = Texture2D.FromStream(GraphicsDevice, titleStream);
                titleStream.Close();
                Color[] buffer = new Color[texture.Width * texture.Height];
                texture.GetData(buffer);
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
                texture.SetData(buffer);
            }

            return texture;
        }

        public GumpInfo LoadGumpTexture(uint graphic)
        {
            if (gump_availableIDs == null)
                return new GumpInfo();
            int index = Array.IndexOf(gump_availableIDs, graphic);
            if (index == -1) return new GumpInfo();

            Texture2D texture;

            gump_textureCache.TryGetValue(graphic, out texture);

            if (exePath != null && texture == null && GraphicsDevice != null)
            {
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER, ((int)graphic).ToString() + ".png");

                if (File.Exists(fullImagePath))
                {
                    FileStream titleStream = File.OpenRead(fullImagePath);
                    texture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    titleStream.Close();
                    FixPNGAlpha(ref texture);

                    gump_textureCache.Add(graphic, texture);
                }
            }

            if (texture == null)
            {
                return new GumpInfo();
            }

            return new GumpInfo()
            {
                Pixels = GetPixels(texture),
                Width = texture.Width,
                Height = texture.Height
            };
        }

        public ArtInfo LoadArtTexture(uint graphic)
        {
            Texture2D texture;

            if (art_availableIDs == null)
                return new ArtInfo();

            int index = Array.IndexOf(art_availableIDs, graphic);
            if (index == -1) return new ArtInfo();

            art_textureCache.TryGetValue(graphic, out texture);

            if (exePath != null && texture == null && GraphicsDevice != null)
            {
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER, (graphic -= 0x4000).ToString() + ".png");

                if (File.Exists(fullImagePath))
                {
                    FileStream titleStream = File.OpenRead(fullImagePath);
                    texture = Texture2D.FromStream(GraphicsDevice, titleStream);
                    titleStream.Close();
                    Color[] buffer = new Color[texture.Width * texture.Height];
                    texture.GetData(buffer);
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
                    texture.SetData(buffer);

                    art_textureCache.Add(graphic, texture);
                }
            }

            return new ArtInfo()
            {
                Pixels = GetPixels(texture),
                Width = texture.Width,
                Height = texture.Height,
            };
        }

        private uint[] GetPixels(Texture2D texture)
        {
            if (texture == null)
            {
                return new uint[0];
            }
            Span<uint> pixels = texture.Width * texture.Height <= 1024 ? stackalloc uint[1024] : stackalloc uint[texture.Width * texture.Height];

            Color[] pixelColors = new Color[texture.Width * texture.Height];
            texture.GetData<Color>(pixelColors);

            for (int i = 0; i < pixelColors.Length; i++)
            {
                pixels[i] = pixelColors[i].PackedValue;
            }

            return pixels.ToArray();
        }

        public Task Load()
        {
            return Task.Run(
                () =>
                {
                    string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    exePath = Path.GetDirectoryName(strExeFilePath);

                    string gumpPath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER);
                    if (Directory.Exists(gumpPath))
                    {
                        string[] files = Directory.GetFiles(gumpPath, "*.png", SearchOption.TopDirectoryOnly);
                        gump_availableIDs = new uint[files.Length];

                        for (int i = 0; i < files.Length; i++)
                        {
                            string fname = Path.GetFileName(files[i]);
                            uint.TryParse(fname.Substring(0, fname.Length - 4), out gump_availableIDs[i]);
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(gumpPath);
                    }

                    string artPath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER);
                    if (Directory.Exists(artPath))
                    {
                        string[] files = Directory.GetFiles(artPath, "*.png", SearchOption.TopDirectoryOnly);
                        art_availableIDs = new uint[files.Length];

                        for (int i = 0; i < files.Length; i++)
                        {
                            string fname = Path.GetFileName(files[i]);
                            if (uint.TryParse(fname.Substring(0, fname.Length - 4), out uint gfx))
                            {
                                art_availableIDs[i] = gfx + 0x4000;
                            }
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(artPath);
                    }
                });
        }

        public Task LoadResourceAssets()
        {
            return Task.Run(
                () =>
                {
                    var assembly = GetType().Assembly;

                    //Load the custom gump art included with TUO
                    for (uint i = 40303; i <= 40312; i++)
                    {
                        //Check if the art already exists
                        var gumpInfo = LoadGumpTexture(i);

                        if (gumpInfo.Pixels == null || gumpInfo.Pixels.IsEmpty)
                        {
                            gumpInfo = GumpsLoader.Instance.GetGump(i);
                            if (gumpInfo.Pixels != null && !gumpInfo.Pixels.IsEmpty)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        var resourceName = assembly.GetName().Name + $".gumpartassets.{i}.png";
                        Console.WriteLine(resourceName);
                        try
                        {
                            Stream stream = assembly.GetManifestResourceStream(resourceName);
                            if (stream != null)
                            {
                                Texture2D texture = Texture2D.FromStream(GraphicsDevice, stream);
                                FixPNGAlpha(ref texture);
                                gump_textureCache.Add(i, texture);


                                //Increase available gump id's
                                if (gump_availableIDs != null)
                                {
                                    uint[] availableIDs = new uint[gump_availableIDs.Length + 1];
                                    gump_availableIDs.CopyTo(availableIDs, 0);
                                    availableIDs[availableIDs.Length - 1] = i;
                                    gump_availableIDs = availableIDs;
                                }
                                else
                                {
                                    gump_availableIDs = [i];
                                }

                                stream.Dispose();
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                    }
                });
        }

        private static void FixPNGAlpha(ref Texture2D texture)
        {
            Color[] buffer = new Color[texture.Width * texture.Height];
            texture.GetData(buffer);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
            texture.SetData(buffer);
        }
    }
}
