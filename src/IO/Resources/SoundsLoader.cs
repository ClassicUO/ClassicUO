#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ClassicUO.Game;
using ClassicUO.IO.Audio;

namespace ClassicUO.IO.Resources
{
    internal class SoundsLoader : ResourceLoader
    {
        private static readonly char[] _mConfigFileDelimiters = {' ', ',', '\t'};
        private static readonly Dictionary<int, Tuple<string, bool>> _mMusicData = new Dictionary<int, Tuple<string, bool>>();
        private readonly Dictionary<int, Sound> _sounds = new Dictionary<int, Sound>(), _musics = new Dictionary<int, Sound>();
        private UOFile _file;


        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "soundLegacyMUL.uop");

            if (File.Exists(path))
                _file = new UOFileUop(path, ".dat", Constants.MAX_SOUND_DATA_INDEX_COUNT);
            else
            {
                path = Path.Combine(FileManager.UoFolderPath, "sound.mul");
                string idxpath = Path.Combine(FileManager.UoFolderPath, "soundidx.mul");

                if (File.Exists(path) && File.Exists(idxpath))
                    _file = new UOFileMul(path, idxpath, Constants.MAX_SOUND_DATA_INDEX_COUNT);
                else
                    throw new FileNotFoundException("no sounds found");
            }

            string def = Path.Combine(FileManager.UoFolderPath, "Sound.def");

            if (File.Exists(def))
            {
                using (DefReader reader = new DefReader(def))
                {
                    while (reader.Next())
                    {
                        int index = reader.ReadInt();

                        if (index < 0 || index >= Constants.MAX_SOUND_DATA_INDEX_COUNT || index >= _file.Length || _file.Entries[index].Length != 0)
                            continue;

                        int[] group = reader.ReadGroup();

                        if (group == null)
                            continue;

                        for (int i = 0; i < group.Length; i++)
                        {
                            int checkIndex = group[i];

                            if (checkIndex < -1 || checkIndex >= Constants.MAX_SOUND_DATA_INDEX_COUNT)
                                continue;

                            ref UOFileIndex3D ind = ref _file.Entries[index];

                            if (checkIndex == -1)
                                ind = default;
                            else
                            {
                                ref readonly UOFileIndex3D outInd = ref _file.Entries[checkIndex];

                                if (outInd.Length == 0)
                                    continue;

                                _file.Entries[index] = _file.Entries[checkIndex];
                            }
                        }
                    }
                }
            }

            path = Path.Combine(FileManager.UoFolderPath, @"Music/Digital/Config.txt");

            if (File.Exists(path))
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (TryParseConfigLine(line, out Tuple<int, string, bool> songData)) _mMusicData.Add(songData.Item1, new Tuple<string, bool>(songData.Item2, songData.Item3));
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

            long offset = _file.Position;

            if (offset < 0 || length <= 0) return false;

            _file.Seek(offset);

            byte[] stringBuffer = _file.ReadArray<byte>(40);
            data = _file.ReadArray<byte>(length - 40);

            name = Encoding.UTF8.GetString(stringBuffer);
            int end = name.IndexOf('\0');
            name = name.Substring(0, end);

            return true;
        }

        /// <summary>
        ///     Attempts to parse a line from UO's music Config.txt.
        /// </summary>
        /// <param name="line">A line from the file.</param>
        /// <param name="?">If successful, contains a tuple with these fields: int songIndex, string songName, bool doesLoop</param>
        /// <returns>true if line could be parsed, false otherwise.</returns>
        private static bool TryParseConfigLine(string line, out Tuple<int, string, bool> songData)
        {
            songData = null;

            string[] splits = line.Split(_mConfigFileDelimiters);

            if (splits.Length < 2 || splits.Length > 3) return false;

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

            if (_mMusicData.ContainsKey(index))
            {
                name = _mMusicData[index].Item1;
                doesLoop = _mMusicData[index].Item2;

                return true;
            }

            return false;
        }

        public Sound GetSound(int index)
        {
            if (!_sounds.TryGetValue(index, out Sound sound) && TryGetSound(index, out byte[] data, out string name))
            {
                sound = new UOSound(name, index, data);
                _sounds.Add(index, sound);
            }

            return sound;
        }

        public Sound GetMusic(int index)
        {
            if (!_musics.TryGetValue(index, out Sound music) && TryGetMusicData(index, out string name, out bool loop))
            {
                music = new UOMusic(index, name, loop);
                _musics.Add(index, music);
            }

            return music;
        }

        protected override void CleanResources()
        {
        }
    }
}