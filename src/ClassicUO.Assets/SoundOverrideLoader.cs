using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    internal class SoundOverrideLoader
    {
        public static SoundOverrideLoader Instance { get; private set; } = new SoundOverrideLoader();

        private const string SOUND_OVERRIDE_FOLDER = "SoundOverrides";
        private string exePath;
        private Dictionary<int, Tuple<string, byte[]>> soundCache = new Dictionary<int, Tuple<string, byte[]>>();
        private bool loaded = false;

        private SoundOverrideLoader()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            exePath = Path.GetDirectoryName(strExeFilePath);

            Task.Factory.StartNew(() =>
            {
                if (Directory.Exists(Path.Combine(exePath, SOUND_OVERRIDE_FOLDER)))
                {
                    string[] files = Directory.GetFiles(Path.Combine(exePath, SOUND_OVERRIDE_FOLDER), "*.mp3", SearchOption.TopDirectoryOnly);

                    for (int i = 0; i < files.Length; i++)
                    {
                        var fname = Path.GetFileName(files[i]);
                        if (int.TryParse(fname.Substring(0, fname.Length - 4), out int fID))
                        {
                            soundCache.Add(fID, new Tuple<string, byte[]>(fname, File.ReadAllBytes(files[i])));
                        }
                    }

                    loaded = true;
                }
            });
        }

        public bool TryGetSoundOverride(int id, out byte[] data, out string name)
        {
            if (loaded)
            {
                if (soundCache.ContainsKey(id))
                {
                    data = soundCache[id].Item2;
                    name = soundCache[id].Item1;
                    return true;
                }
            }
            name = null;
            data = null;
            return false;
        }
    }
}
