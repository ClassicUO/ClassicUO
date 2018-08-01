using System.Diagnostics;
using System.IO;

namespace ClassicUO.AssetsLoader
{
    public static class FileManager
    {
        private static string _uofolderpath;

        public static string UoFolderPath
        {
            get => _uofolderpath;
            set
            {
                _uofolderpath = value;
                FileInfo client = new FileInfo(Path.Combine(value, "client.exe"));
                if (!client.Exists)
                    throw new FileNotFoundException();

                FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(client.FullName);

                ClientVersion = (ClientVersions) ((versInfo.ProductMajorPart << 24) | (versInfo.ProductMinorPart << 16) | (versInfo.ProductBuildPart << 8) | versInfo.ProductPrivatePart);
            }
        }

        public static ClientVersions ClientVersion { get; private set; }
        public static bool IsUOPInstallation => ClientVersion >= ClientVersions.CV_70240;
        public static ushort GraphicMask => IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;


        public static void LoadFiles()
        {
            Map.Load();
            Art.Load();
            Cliloc.Load();
            Animations.Load();
            Gumps.Load();
            Fonts.Load();
            Hues.Load();
            TileData.Load();
            Multi.Load();
            Skills.Load();
            TextmapTextures.Load();
            SpecialKeywords.Load();
            AnimData.Load();
        }
    }
}