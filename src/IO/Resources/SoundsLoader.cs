#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
        private static readonly char[] _configFileDelimiters = { ' ', ',', '\t' };
        private static readonly Dictionary<int, Tuple<string, bool>> _musicData = new Dictionary<int, Tuple<string, bool>>();

        private static SoundsLoader _instance;

        private UOFile _file;
        private readonly Sound[] _musics = new Sound[Constants.MAX_SOUND_DATA_INDEX_COUNT];
        private readonly Sound[] _sounds = new Sound[Constants.MAX_SOUND_DATA_INDEX_COUNT];

        private SoundsLoader()
        {
        }

        public static SoundsLoader Instance => _instance ?? (_instance = new SoundsLoader());

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("soundLegacyMUL.uop");

                    if (Client.IsUOPInstallation && File.Exists(path))
                    {
                        _file = new UOFileUop(path, "build/soundlegacymul/{0:D8}.dat");
                        Entries = new UOFileIndex[Math.Max(((UOFileUop) _file).TotalEntriesCount, Constants.MAX_SOUND_DATA_INDEX_COUNT)];
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
                        {
                            throw new FileNotFoundException("no sounds found");
                        }
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
                                {
                                    continue;
                                }

                                int[] group = reader.ReadGroup();

                                if (group == null)
                                {
                                    continue;
                                }

                                for (int i = 0; i < group.Length; i++)
                                {
                                    int checkIndex = group[i];

                                    if (checkIndex < -1 || checkIndex >= Constants.MAX_SOUND_DATA_INDEX_COUNT)
                                    {
                                        continue;
                                    }

                                    ref UOFileIndex ind = ref Entries[index];

                                    if (checkIndex == -1)
                                    {
                                        ind = default;
                                    }
                                    else
                                    {
                                        ref readonly UOFileIndex outInd = ref Entries[checkIndex];

                                        if (outInd.Length == 0)
                                        {
                                            continue;
                                        }

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
                                {
                                    _musicData[songData.Item1] = new Tuple<string, bool>(songData.Item2, songData.Item3);
                                }
                            }
                        }
                    }
                    else if (Client.Version <= ClientVersion.CV_5090)
                    {
                        _musicData.Add(0, new Tuple<string, bool>("oldult01", true));
                        _musicData.Add(1, new Tuple<string, bool>("create1", false));
                        _musicData.Add(2, new Tuple<string, bool>("dragflit", false));
                        _musicData.Add(3, new Tuple<string, bool>("oldult02", true));
                        _musicData.Add(4, new Tuple<string, bool>("oldult03", true));
                        _musicData.Add(5, new Tuple<string, bool>("oldult04", true));
                        _musicData.Add(6, new Tuple<string, bool>("oldult05", true));
                        _musicData.Add(7, new Tuple<string, bool>("oldult06", true));
                        _musicData.Add(8, new Tuple<string, bool>("stones2", true));
                        _musicData.Add(9, new Tuple<string, bool>("britain1", true));
                        _musicData.Add(10, new Tuple<string, bool>("britain2", true));
                        _musicData.Add(11, new Tuple<string, bool>("bucsden", true));
                        _musicData.Add(12, new Tuple<string, bool>("jhelom", false));
                        _musicData.Add(13, new Tuple<string, bool>("lbcastle", false));
                        _musicData.Add(14, new Tuple<string, bool>("linelle", false));
                        _musicData.Add(15, new Tuple<string, bool>("magincia", true));
                        _musicData.Add(16, new Tuple<string, bool>("minoc", true));
                        _musicData.Add(17, new Tuple<string, bool>("ocllo", true));
                        _musicData.Add(18, new Tuple<string, bool>("samlethe", false));
                        _musicData.Add(19, new Tuple<string, bool>("serpents", true));
                        _musicData.Add(20, new Tuple<string, bool>("skarabra", true));
                        _musicData.Add(21, new Tuple<string, bool>("trinsic", true));
                        _musicData.Add(22, new Tuple<string, bool>("vesper", true));
                        _musicData.Add(23, new Tuple<string, bool>("wind", true));
                        _musicData.Add(24, new Tuple<string, bool>("yew", true));
                        _musicData.Add(25, new Tuple<string, bool>("cave01", false));
                        _musicData.Add(26, new Tuple<string, bool>("dungeon9", false));
                        _musicData.Add(27, new Tuple<string, bool>("forest_a", false));
                        _musicData.Add(28, new Tuple<string, bool>("intown01", false));
                        _musicData.Add(29, new Tuple<string, bool>("jungle_a", false));
                        _musicData.Add(30, new Tuple<string, bool>("mountn_a", false));
                        _musicData.Add(31, new Tuple<string, bool>("plains_a", false));
                        _musicData.Add(32, new Tuple<string, bool>("sailing", false));
                        _musicData.Add(33, new Tuple<string, bool>("swamp_a", false));
                        _musicData.Add(34, new Tuple<string, bool>("tavern01", false));
                        _musicData.Add(35, new Tuple<string, bool>("tavern02", false));
                        _musicData.Add(36, new Tuple<string, bool>("tavern03", false));
                        _musicData.Add(37, new Tuple<string, bool>("tavern04", false));
                        _musicData.Add(38, new Tuple<string, bool>("combat1", false));
                        _musicData.Add(39, new Tuple<string, bool>("combat2", false));
                        _musicData.Add(40, new Tuple<string, bool>("combat3", false));
                        _musicData.Add(41, new Tuple<string, bool>("approach", false));
                        _musicData.Add(42, new Tuple<string, bool>("death", false));
                        _musicData.Add(43, new Tuple<string, bool>("victory", false));
                        _musicData.Add(44, new Tuple<string, bool>("btcastle", false));
                        _musicData.Add(45, new Tuple<string, bool>("nujelm", true));
                        _musicData.Add(46, new Tuple<string, bool>("dungeon2", false));
                        _musicData.Add(47, new Tuple<string, bool>("cove", true));
                        _musicData.Add(48, new Tuple<string, bool>("moonglow", true));
                        _musicData.Add(49, new Tuple<string, bool>("zento", true));
                        _musicData.Add(50, new Tuple<string, bool>("tokunodungeon", true));
                        _musicData.Add(51, new Tuple<string, bool>("Taiko", true));
                        _musicData.Add(52, new Tuple<string, bool>("dreadhornarea", true));
                        _musicData.Add(53, new Tuple<string, bool>("elfcity", true));
                        _musicData.Add(54, new Tuple<string, bool>("grizzledungeon", true));
                        _musicData.Add(55, new Tuple<string, bool>("melisandeslair", true));
                        _musicData.Add(56, new Tuple<string, bool>("paroxysmuslair", true));
                        _musicData.Add(57, new Tuple<string, bool>("gwennoconversation", true));
                        _musicData.Add(58, new Tuple<string, bool>("goodendgame", true));
                        _musicData.Add(59, new Tuple<string, bool>("goodvsevil", true));
                        _musicData.Add(60, new Tuple<string, bool>("greatearthserpents", true));
                        _musicData.Add(61, new Tuple<string, bool>("humanoids_u9", true));
                        _musicData.Add(62, new Tuple<string, bool>("minocnegative", true));
                        _musicData.Add(63, new Tuple<string, bool>("paws", true));
                        _musicData.Add(64, new Tuple<string, bool>("selimsbar", true));
                        _musicData.Add(65, new Tuple<string, bool>("serpentislecombat_u7", true));
                        _musicData.Add(66, new Tuple<string, bool>("valoriaships", true));
                    }
                }
            );
        }

        private bool TryGetSound(int sound, out byte[] data, out string name)
        {
            data = null;
            name = null;

            if (sound < 0)
            {
                return false;
            }

            ref UOFileIndex entry = ref GetValidRefEntry(sound);

            _file.SetData(entry.Address, entry.FileSize);
            _file.Seek(entry.Offset);

            long offset = _file.Position;

            if (offset < 0 || entry.Length <= 0)
            {
                return false;
            }

            _file.Seek(offset);

            byte[] stringBuffer = _file.ReadArray<byte>(40);
            data = _file.ReadArray<byte>(entry.Length - 40);

            name = Encoding.UTF8.GetString(stringBuffer);
            int end = name.IndexOf('\0');

            if (end >= 0)
            {
                name = name.Substring(0, end);
            }

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

            string[] splits = line.Split(_configFileDelimiters);

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

        private bool TryGetMusicData(int index, out string name, out bool doesLoop)
        {
            name = null;
            doesLoop = false;

            if (_musicData.ContainsKey(index))
            {
                name = _musicData[index].Item1;

                doesLoop = _musicData[index].Item2;

                return true;
            }

            return false;
        }


        public Sound GetSound(int index)
        {
            if (index >= 0 && index < Constants.MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref Sound sound = ref _sounds[index];

                if (sound == null && TryGetSound(index, out byte[] data, out string name))
                {
                    sound = new UOSound(name, index, data);
                }

                return sound;
            }

            return null;
        }

        public Sound GetMusic(int index)
        {
            if (index >= 0 && index < Constants.MAX_SOUND_DATA_INDEX_COUNT)
            {
                ref Sound music = ref _musics[index];

                if (music == null && TryGetMusicData(index, out string name, out bool loop))
                {
                    music = new UOMusic(index, name, loop);
                }

                return music;
            }

            return null;
        }

        public override void ClearResources()
        {
            for (int i = 0; i < Constants.SOUND_DELTA; i++)
            {
                if (_sounds[i] != null)
                {
                    _sounds[i].Dispose();

                    _sounds[i] = null;
                }

                if (_musics[i] != null)
                {
                    _musics[i].Dispose();

                    _musics[i] = null;
                }
            }

            _musicData.Clear();
        }
    }
}