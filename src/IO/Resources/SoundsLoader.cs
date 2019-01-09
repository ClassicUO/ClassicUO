using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game;
using ClassicUO.IO.Audio;

using Microsoft.Xna.Framework.Media;

namespace ClassicUO.IO.Resources
{
    class SoundsLoader : ResourceLoader
    {
        private UOFile _file;
        private readonly Dictionary<int, ASound> _sounds = new Dictionary<int, ASound>(), _musics = new Dictionary<int, ASound>();
        //private readonly Dictionary<int, int> _translations = new Dictionary<int, int>();

        public override void Load()
        {         
            string path = Path.Combine(FileManager.UoFolderPath, "soundLegacyMUL.uop");

            if (File.Exists(path))
            {
                _file = new UOFileUop(path, ".dat", Constants.MAX_SOUND_DATA_INDEX_COUNT);
            }
            else
            {
                path = Path.Combine(FileManager.UoFolderPath, "sound.mul");
                string idxpath = Path.Combine(FileManager.UoFolderPath, "soundidx.mul");

                if (File.Exists(path) && File.Exists(idxpath))
                {
                    _file = new UOFileMul(path, idxpath, Constants.MAX_SOUND_DATA_INDEX_COUNT);
                }
                else
                {
                    throw new FileNotFoundException("no sounds found");
                }
            }

            string def = Path.Combine(FileManager.UoFolderPath, "Sound.def");

            if (!File.Exists(def))
                return;
            return;
            using (DefReader reader = new DefReader(def))
            {
                while (reader.Next())
                {
                    int index = reader.ReadInt();

                    if (index < 0 || index >= Constants.MAX_SOUND_DATA_INDEX_COUNT || index >= _file.Length || _file.Entries[index].Length == 0)
                        continue;

                    int[] group = reader.ReadGroup();

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];

                        if (checkIndex < -1 || checkIndex >= Constants.MAX_SOUND_DATA_INDEX_COUNT)
                            continue;

                        ref UOFileIndex3D ind = ref _file.Entries[index];

                        if (checkIndex == -1)
                        {
                            ind = default;
                        }
                        else
                        {
                            ref UOFileIndex3D outInd = ref _file.Entries[checkIndex];

                            if (outInd.Length == 0)
                                continue;

                            _file.Entries[index] = _file.Entries[checkIndex];
                        }
                    }
                }
            }

        }

        private bool TryGetSound(int sound, out byte[] data, out string name)
        {
            data = null;
            name = null;

            if (sound < 0)
                return false;

            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(sound);

            long streamStart = _file.Position;
            long offset = _file.Position;

            if (offset < 0 || length <= 0)
            {
                return false;
                //if (!_translations.TryGetValue(sound, out sound))
                //    return false;

                //(length, extra, patcher) = _file.SeekByEntryIndex(sound);
                //streamStart = _file.Position;
                //offset = _file.Position;

                //if (offset < 0 || length <= 0)
                //    return false;
            }

            _file.Seek(offset);

            byte[] stringBuffer = _file.ReadArray<byte>(40);
            data = _file.ReadArray<byte>(length - 40);

            name = Encoding.UTF8.GetString(stringBuffer);
            int end = name.IndexOf('\0');
            name = name.Substring(0, end);

            return true;
        }

       

        public ASound GetSound(int index)
        {
            if (!_sounds.TryGetValue(index, out ASound sound))
            {
                if (TryGetSound(index, out byte[] data, out string name))
                {
                    sound = new UOSound(name, data);
                    _sounds.Add(index, sound);
                }
            }

            return sound;
        }

        public ASound GetMusic(int index)
        {
            if (!_musics.TryGetValue(index, out ASound music))
            {
                if (MusicData.TryGetMusicData(index, out string name, out bool loop))
                {
                    music = new UOMusic(index, name, loop);
                    _musics.Add(index, music);
                }
            }

            return music;
        }

        protected override void CleanResources()
        {
        }
    }


    class MusicData
    {
        private const string m_ConfigFilePath = @"Music/Digital/Config.txt";
        private static char[] m_configFileDelimiters = { ' ', ',', '\t' };

        private static readonly Dictionary<int, Tuple<string, bool>> m_MusicData = new Dictionary<int, Tuple<string, bool>>();

        static MusicData()
        {
            string path = Path.Combine(FileManager.UoFolderPath, m_ConfigFilePath);

            // open UO's music Config.txt
            if (!File.Exists(path))
                return;
            // attempt to read out all the values from the file.
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Tuple<int, string, bool> songData;
                    if (TryParseConfigLine(line, out songData))
                    {
                        m_MusicData.Add(songData.Item1, new Tuple<string, bool>(songData.Item2, songData.Item3));
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to parse a line from UO's music Config.txt.
        /// </summary>
        /// <param name="line">A line from the file.</param>
        /// <param name="?">If successful, contains a tuple with these fields: int songIndex, string songName, bool doesLoop</param>
        /// <returns>true if line could be parsed, false otherwise.</returns>
        private static bool TryParseConfigLine(string line, out Tuple<int, string, bool> songData)
        {
            songData = null;

            string[] splits = line.Split(m_configFileDelimiters);
            if (splits.Length < 2 || splits.Length > 3)
            {
                return false;
            }

            int index = int.Parse(splits[0]);
            string name = splits[1].Trim();
            bool doesLoop = splits.Length == 3 && splits[2] == "loop";

            songData = new Tuple<int, string, bool>(index, name, doesLoop);
            return true;
        }

        public static bool TryGetMusicData(int index, out string name, out bool doesLoop)
        {
            name = null;
            doesLoop = false;

            if (m_MusicData.ContainsKey(index))
            {
                name = m_MusicData[index].Item1;
                doesLoop = m_MusicData[index].Item2;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
