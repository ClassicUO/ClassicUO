using ClassicUO.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public static class PNGLoader
    {
        private const string IMAGES_FOLDER = "ExternalImages", GUMP_EXTERNAL_FOLDER = "gumps", ART_EXTERNAL_FOLDER = "art";

        private static string exePath;

        private static uint[] gump_availableIDs;
        private static Dictionary<uint, Texture2D> gump_textureCache = new Dictionary<uint, Texture2D>();

        private static uint[] art_availableIDs;
        private static Dictionary<uint, Texture2D> art_textureCache = new Dictionary<uint, Texture2D>();

        public static GraphicsDevice GraphicsDevice { set; get; }

        public static Texture2D GetImageTexture(string fullImagePath)
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

        public static Texture2D LoadGumpTexture(uint graphic)
        {
            Texture2D texture;

            if (gump_availableIDs == null)
                return null;

            int index = Array.IndexOf(gump_availableIDs, graphic);
            if (index == -1) return null;

            gump_textureCache.TryGetValue(graphic, out texture);

            if (exePath != null && texture == null && GraphicsDevice != null)
            {
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, GUMP_EXTERNAL_FOLDER, ((int)graphic).ToString() + ".png");

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

                    gump_textureCache.Add(graphic, texture);
                }
            }

            return texture;
        }

        public static Texture2D LoadArtTexture(uint graphic)
        {
            Texture2D texture;

            if (art_availableIDs == null)
                return null;

            int index = Array.IndexOf(art_availableIDs, graphic);
            if (index == -1) return null;

            art_textureCache.TryGetValue(graphic, out texture);

            if (exePath != null && texture == null && GraphicsDevice != null)
            {
                string fullImagePath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER, ((int)graphic).ToString() + ".png");

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

            return texture;
        }

        public static Task Load()
        {
            return Task.Run
            (() =>
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

                string artPath = Path.Combine(exePath, IMAGES_FOLDER, ART_EXTERNAL_FOLDER);
                if (Directory.Exists(artPath))
                {
                    string[] files = Directory.GetFiles(artPath, "*.png", SearchOption.TopDirectoryOnly);
                    art_availableIDs = new uint[files.Length];

                    for (int i = 0; i < files.Length; i++)
                    {
                        string fname = Path.GetFileName(files[i]);
                        uint.TryParse(fname.Substring(0, fname.Length - 4), out art_availableIDs[i]);
                    }
                }
            });
        }
    }
}
