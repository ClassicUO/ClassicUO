#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Threading.Tasks;
using ClassicUO.Data;
using ClassicUO.Game;
using ClassicUO.IO.Audio;

namespace ClassicUO.IO.Resources
{
    internal class SoundsLoader : UOFileLoader
    {
        private static readonly char[] _mConfigFileDelimiters = {' ', ',', '\t'};
        private static readonly Dictionary<int, Tuple<string, bool>> _mMusicData = new Dictionary<int, Tuple<string, bool>>();
        private readonly Dictionary<int, Sound> _sounds = new Dictionary<int, Sound>(), _musics = new Dictionary<int, Sound>(), _midi_music = new Dictionary<int, Sound>()
        {
            {0,  new UOMidMusic(0, "oldult01", true) },
            {1,  new UOMidMusic(1, "oldult02", true) },
            {2,  new UOMidMusic(2, "oldult05", true) },
            {3,  new UOMidMusic(3, "britain1", true) },
            {4,  new UOMidMusic(4, "jhelom", false) },
            {5,  new UOMidMusic(5, "magincia", true) },
            {6,  new UOMidMusic(6, "samlethe", false) },
            {7,  new UOMidMusic(7, "trinsic", true) },
            {8,  new UOMidMusic(8, "yew", true) },
            {9,  new UOMidMusic(9, "forest_a", false) },
            {10, new UOMidMusic(10, "mountn_a", false) },
            {11, new UOMidMusic(11, "swamp_a", false) },
            {12, new UOMidMusic(12, "tavern03", false) },
            {13, new UOMidMusic(13, "combat2", false) },
            {14, new UOMidMusic(14, "death", false) },
            {15, new UOMidMusic(15, "nujelm", true) },
            {16, new UOMidMusic(16, "moonglow", true) },
            {17, new UOMidMusic(17, "oldult04", false) },
            {18, new UOMidMusic(18, "approach", false) },
            {19, new UOMidMusic(19, "create1", false) },
            {20, new UOMidMusic(20, "oldult03", true) },
            {21, new UOMidMusic(21, "oldult06", true) },
            {22, new UOMidMusic(22, "britain2", true) },
            {23, new UOMidMusic(23, "lbcastle", false) },
            {24, new UOMidMusic(24, "minoc", true) },
            {25, new UOMidMusic(25, "serpents", true) },
            {26, new UOMidMusic(26, "vesper", true) },
            {27, new UOMidMusic(27, "cave01", false) },
            {28, new UOMidMusic(28, "intown01", false) },
            {29, new UOMidMusic(29, "plains_a", false) },
            {30, new UOMidMusic(30, "tavern01", false) },
            {31, new UOMidMusic(31, "tavern04", false) },
            {32, new UOMidMusic(32, "combat3", false) },
            {33, new UOMidMusic(33, "victory", false) },
            {34, new UOMidMusic(34, "dungeon2", false) },
            {35, new UOMidMusic(35, "oldult02", false) },
            {36, new UOMidMusic(36, "dragflit", false) },
            {37, new UOMidMusic(37, "combat3", false) },
            {38, new UOMidMusic(38, "dragflit", false) },
            {39, new UOMidMusic(39, "oldult04", true) },
            {40, new UOMidMusic(40, "stones2", true) },
            {41, new UOMidMusic(41, "bucsden", true) },
            {42, new UOMidMusic(42, "linelle", false) },
            {43, new UOMidMusic(43, "ocllo", true) },
            {44, new UOMidMusic(44, "skarabra", true) },
            {45, new UOMidMusic(45, "wind", true) },
            {46, new UOMidMusic(46, "dungeon9", false) },
            {47, new UOMidMusic(47, "jungle_a", false) },
            {48, new UOMidMusic(48, "sailing", false) },
            {49, new UOMidMusic(49, "tavern02", false) },
            {50, new UOMidMusic(50, "combat1", false) },
            {51, new UOMidMusic(51, "approach", false) },
            {52, new UOMidMusic(52, "btcastle", false) },
            {53, new UOMidMusic(53, "cove", true) },
            {54, new UOMidMusic(54, "serpents", true) },
            {55, new UOMidMusic(55, "create1", false) },
            {56, new UOMidMusic(56, "jungle_a", false) },
        };
        private UOFile _file;

        private SoundsLoader()
        {

        }

        private static SoundsLoader _instance;
        public static SoundsLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SoundsLoader();
                }

                return _instance;
            }
        }


        public override Task Load()
        {
            return Task.Run(() =>
            {
                string path = UOFileManager.GetUOFilePath("soundLegacyMUL.uop");

                if (Client.IsUOPInstallation && File.Exists(path))
                {
                    _file = new UOFileUop(path, "build/soundlegacymul/{0:D8}.dat");
                    Entries = new UOFileIndex[Constants.MAX_SOUND_DATA_INDEX_COUNT];
                }
                else
                {
                    path = UOFileManager.GetUOFilePath("sound.mul");
                    string idxpath = UOFileManager.GetUOFilePath("soundidx.mul");

                    if (File.Exists(path) && File.Exists(idxpath))
                    {
                        _file = new UOFileMul(path, idxpath, Constants.MAX_SOUND_DATA_INDEX_COUNT);
                    }
                    else
                        throw new FileNotFoundException("no sounds found");
                }

                _file.FillEntries(ref Entries);

                string def = UOFileManager.GetUOFilePath("Sound.def");

                if (File.Exists(def))
                {
                    using (DefReader reader = new DefReader(def))
                    {
                        while (reader.Next())
                        {
                            int index = reader.ReadInt();

                            if (index < 0 || index >= Constants.MAX_SOUND_DATA_INDEX_COUNT || index >= _file.Length || Entries[index].Length != 0)
                                continue;

                            int[] group = reader.ReadGroup();

                            if (group == null)
                                continue;

                            for (int i = 0; i < group.Length; i++)
                            {
                                int checkIndex = group[i];

                                if (checkIndex < -1 || checkIndex >= Constants.MAX_SOUND_DATA_INDEX_COUNT)
                                    continue;

                                ref UOFileIndex ind = ref Entries[index];

                                if (checkIndex == -1)
                                    ind = default;
                                else
                                {
                                    ref readonly UOFileIndex outInd = ref Entries[checkIndex];

                                    if (outInd.Length == 0)
                                        continue;

                                    Entries[index] = Entries[checkIndex];
                                }
                            }
                        }
                    }
                }

                path = UOFileManager.GetUOFilePath(@"Music/Digital/Config.txt");

                if (File.Exists(path))
                {
                    using (StreamReader reader = new StreamReader(path))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (TryParseConfigLine(line, out Tuple<int, string, bool> songData))
                                _mMusicData[songData.Item1] = new Tuple<string, bool>(songData.Item2, songData.Item3);
                        }
                    }
                }
                else if (Client.Version <= ClientVersion.CV_5090)
                {
                    _mMusicData.Add(0, new Tuple<string, bool>("oldult01", true));
                    _mMusicData.Add(1, new Tuple<string, bool>("create1", false));
                    _mMusicData.Add(2, new Tuple<string, bool>("dragflit", false));
                    _mMusicData.Add(3, new Tuple<string, bool>("oldult02", true));
                    _mMusicData.Add(4, new Tuple<string, bool>("oldult03", true));
                    _mMusicData.Add(5, new Tuple<string, bool>("oldult04", true));
                    _mMusicData.Add(6, new Tuple<string, bool>("oldult05", true));
                    _mMusicData.Add(7, new Tuple<string, bool>("oldult06", true));
                    _mMusicData.Add(8, new Tuple<string, bool>("stones2", true));
                    _mMusicData.Add(9, new Tuple<string, bool>("britain1", true));
                    _mMusicData.Add(10, new Tuple<string, bool>("britain2", true));
                    _mMusicData.Add(11, new Tuple<string, bool>("bucsden", true));
                    _mMusicData.Add(12, new Tuple<string, bool>("jhelom", false));
                    _mMusicData.Add(13, new Tuple<string, bool>("lbcastle", false));
                    _mMusicData.Add(14, new Tuple<string, bool>("linelle", false));
                    _mMusicData.Add(15, new Tuple<string, bool>("magincia", true));
                    _mMusicData.Add(16, new Tuple<string, bool>("minoc", true));
                    _mMusicData.Add(17, new Tuple<string, bool>("ocllo", true));
                    _mMusicData.Add(18, new Tuple<string, bool>("samlethe", false));
                    _mMusicData.Add(19, new Tuple<string, bool>("serpents", true));
                    _mMusicData.Add(20, new Tuple<string, bool>("skarabra", true));
                    _mMusicData.Add(21, new Tuple<string, bool>("trinsic", true));
                    _mMusicData.Add(22, new Tuple<string, bool>("vesper", true));
                    _mMusicData.Add(23, new Tuple<string, bool>("wind", true));
                    _mMusicData.Add(24, new Tuple<string, bool>("yew", true));
                    _mMusicData.Add(25, new Tuple<string, bool>("cave01", false));
                    _mMusicData.Add(26, new Tuple<string, bool>("dungeon9", false));
                    _mMusicData.Add(27, new Tuple<string, bool>("forest_a", false));
                    _mMusicData.Add(28, new Tuple<string, bool>("intown01", false));
                    _mMusicData.Add(29, new Tuple<string, bool>("jungle_a", false));
                    _mMusicData.Add(30, new Tuple<string, bool>("mountn_a", false));
                    _mMusicData.Add(31, new Tuple<string, bool>("plains_a", false));
                    _mMusicData.Add(32, new Tuple<string, bool>("sailing", false));
                    _mMusicData.Add(33, new Tuple<string, bool>("swamp_a", false));
                    _mMusicData.Add(34, new Tuple<string, bool>("tavern01", false));
                    _mMusicData.Add(35, new Tuple<string, bool>("tavern02", false));
                    _mMusicData.Add(36, new Tuple<string, bool>("tavern03", false));
                    _mMusicData.Add(37, new Tuple<string, bool>("tavern04", false));
                    _mMusicData.Add(38, new Tuple<string, bool>("combat1", false));
                    _mMusicData.Add(39, new Tuple<string, bool>("combat2", false));
                    _mMusicData.Add(40, new Tuple<string, bool>("combat3", false));
                    _mMusicData.Add(41, new Tuple<string, bool>("approach", false));
                    _mMusicData.Add(42, new Tuple<string, bool>("death", false));
                    _mMusicData.Add(43, new Tuple<string, bool>("victory", false));
                    _mMusicData.Add(44, new Tuple<string, bool>("btcastle", false));
                    _mMusicData.Add(45, new Tuple<string, bool>("nujelm", true));
                    _mMusicData.Add(46, new Tuple<string, bool>("dungeon2", false));
                    _mMusicData.Add(47, new Tuple<string, bool>("cove", true));
                    _mMusicData.Add(48, new Tuple<string, bool>("moonglow", true));
                    _mMusicData.Add(49, new Tuple<string, bool>("zento", true));
                    _mMusicData.Add(50, new Tuple<string, bool>("tokunodungeon", true));
                    _mMusicData.Add(51, new Tuple<string, bool>("Taiko", true));
                    _mMusicData.Add(52, new Tuple<string, bool>("dreadhornarea", true));
                    _mMusicData.Add(53, new Tuple<string, bool>("elfcity", true));
                    _mMusicData.Add(54, new Tuple<string, bool>("grizzledungeon", true));
                    _mMusicData.Add(55, new Tuple<string, bool>("melisandeslair", true));
                    _mMusicData.Add(56, new Tuple<string, bool>("paroxysmuslair", true));
                    _mMusicData.Add(57, new Tuple<string, bool>("gwennoconversation", true));
                    _mMusicData.Add(58, new Tuple<string, bool>("goodendgame", true));
                    _mMusicData.Add(59, new Tuple<string, bool>("goodvsevil", true));
                    _mMusicData.Add(60, new Tuple<string, bool>("greatearthserpents", true));
                    _mMusicData.Add(61, new Tuple<string, bool>("humanoids_u9", true));
                    _mMusicData.Add(62, new Tuple<string, bool>("minocnegative", true));
                    _mMusicData.Add(63, new Tuple<string, bool>("paws", true));
                    _mMusicData.Add(64, new Tuple<string, bool>("selimsbar", true));
                    _mMusicData.Add(65, new Tuple<string, bool>("serpentislecombat_u7", true));
                    _mMusicData.Add(66, new Tuple<string, bool>("valoriaships", true));
                }

            });
        }

        private bool TryGetSound(int sound, out byte[] data, out string name)
        {
            data = null;
            name = null;

            if (sound < 0)
                return false;

            ref readonly var entry = ref GetValidRefEntry(sound);

            _file.Seek(entry.Offset);

            long offset = _file.Position;

            if (offset < 0 || entry.Length <= 0) return false;

            _file.Seek(offset);

            byte[] stringBuffer = _file.ReadArray<byte>(40);
            data = _file.ReadArray<byte>(entry.Length - 40);

            name = Encoding.UTF8.GetString(stringBuffer);
            int end = name.IndexOf('\0');
            if (end >= 0)
                name = name.Substring(0, end);

            return true;
        }

        /// <summary>
        ///     Attempts to parse a line from UO's music Config.txt.
        /// </summary>
        /// <param name="line">A line from the file.</param>
        /// <param name="?">If successful, contains a tuple with these fields: int songIndex, string songName, bool doesLoop</param>
        /// <returns>true if line could be parsed, false otherwise.</returns>
        private bool TryParseConfigLine(string line, out Tuple<int, string, bool> songData)
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

        private bool TryGetMusicData(int index, out string name, out bool doesLoop)
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
            Sound music;

            if (Client.Version >= ClientVersion.CV_306E)
            {
                if (!_musics.TryGetValue(index, out music) && TryGetMusicData(index, out string name, out bool loop))
                {
                    music = new UOMusic(index, name, loop);
                    _musics.Add(index, music);
                }
            }
            else if (_midi_music.TryGetValue(index, out music))
            {
                // nothing
            }

            return music;
        }

        public override void CleanResources()
        {
        }
    }
}