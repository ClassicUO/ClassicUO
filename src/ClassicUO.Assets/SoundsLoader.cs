// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class SoundsLoader : UOFileLoader
    {
        private static readonly char[] _configFileDelimiters = { ' ', ',', '\t' };
        private static readonly Dictionary<int, (string, bool)> _musicData = new Dictionary<int, (string, bool)>();

        /// <summary>
        /// Sounds of the shard's own, as loose files, looked at before the archive.
        ///
        /// There is no patch file for sounds the way there is for maps, and Sound.def cannot help:
        /// it remaps an id to another id and skips any id that already holds data, so it can add
        /// an alias and can never replace a sound. Rewriting soundLegacyMUL.uop would work and
        /// means shipping a two hundred megabyte archive to change one clang.
        ///
        /// A folder does the same job for the price of a directory listing. Anything in
        /// Sounds/&lt;id&gt;.wav wins over the archive, and any id the archive does not have becomes
        /// a sound the shard can ask for - the client already allows ids up to 0xFFFF and the
        /// archive stops at 1666, so there are sixty-three thousand free.
        /// </summary>
        private readonly Dictionary<int, string> _ours = new Dictionary<int, string>();


        public const int MAX_SOUND_DATA_INDEX_COUNT = 0xFFFF;

        private UOFile _file;

        public SoundsLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("soundLegacyMUL.uop");

            if (FileManager.IsUOPInstallation && File.Exists(path))
            {
                _file = new UOFileUop(path, "build/soundlegacymul/{0:D8}.dat");
            }
            else
            {
                path = FileManager.GetUOFilePath("sound.mul");
                string idxpath = FileManager.GetUOFilePath("soundidx.mul");

                if (File.Exists(path) && File.Exists(idxpath))
                {
                    _file = new UOFileMul(path, idxpath);
                }
                else
                {
                    throw new FileNotFoundException("no sounds found");
                }
            }

            _file.FillEntries();

            LoadOurs();

            string def = FileManager.GetUOFilePath("Sound.def");

            if (File.Exists(def))
            {
                using (DefReader reader = new DefReader(def))
                {
                    while (reader.Next())
                    {
                        int index = reader.ReadInt();

                        if (index < 0 || index >= MAX_SOUND_DATA_INDEX_COUNT || index >= _file.Entries.Length || _file.Entries[index].Length > 0)
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

                            if (checkIndex < -1 || checkIndex >= MAX_SOUND_DATA_INDEX_COUNT)
                            {
                                continue;
                            }

                            ref UOFileIndex ind = ref _file.Entries[index];

                            if (checkIndex == -1)
                            {
                                ind = default;
                            }
                            else
                            {
                                ref readonly UOFileIndex outInd = ref _file.Entries[checkIndex];

                                if (outInd.Length == 0)
                                {
                                    continue;
                                }

                                _file.Entries[index] = _file.Entries[checkIndex];
                            }
                        }
                    }
                }
            }

            path = FileManager.GetUOFilePath(FileManager.Version >= ClientVersion.CV_4011C ?  @"Music/Digital/Config.txt" : @"Music/Config.txt");

            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (TryParseConfigLine(line, out (int index, string name, bool loop) data))
                        {
                            _musicData[data.index] = (data.name, data.loop);
                        }
                    }
                }
            }
            else
            {
                _musicData.Add(0, ("oldult01", true));
                _musicData.Add(1, ("create1", false));
                _musicData.Add(2, ("dragflit", false));
                _musicData.Add(3, ("oldult02", true));
                _musicData.Add(4, ("oldult03", true));
                _musicData.Add(5, ("oldult04", true));
                _musicData.Add(6, ("oldult05", true));
                _musicData.Add(7, ("oldult06", true));
                _musicData.Add(8, ("stones2", true));
                _musicData.Add(9, ("britain1", true));
                _musicData.Add(10, ("britain2", true));
                _musicData.Add(11, ("bucsden", true));
                _musicData.Add(12, ("jhelom", false));
                _musicData.Add(13, ("lbcastle", false));
                _musicData.Add(14, ("linelle", false));
                _musicData.Add(15, ("magincia", true));
                _musicData.Add(16, ("minoc", true));
                _musicData.Add(17, ("ocllo", true));
                _musicData.Add(18, ("samlethe", false));
                _musicData.Add(19, ("serpents", true));
                _musicData.Add(20, ("skarabra", true));
                _musicData.Add(21, ("trinsic", true));
                _musicData.Add(22, ("vesper", true));
                _musicData.Add(23, ("wind", true));
                _musicData.Add(24, ("yew", true));
                _musicData.Add(25, ("cave01", false));
                _musicData.Add(26, ("dungeon9", false));
                _musicData.Add(27, ("forest_a", false));
                _musicData.Add(28, ("intown01", false));
                _musicData.Add(29, ("jungle_a", false));
                _musicData.Add(30, ("mountn_a", false));
                _musicData.Add(31, ("plains_a", false));
                _musicData.Add(32, ("sailing", false));
                _musicData.Add(33, ("swamp_a", false));
                _musicData.Add(34, ("tavern01", false));
                _musicData.Add(35, ("tavern02", false));
                _musicData.Add(36, ("tavern03", false));
                _musicData.Add(37, ("tavern04", false));
                _musicData.Add(38, ("combat1", false));
                _musicData.Add(39, ("combat2", false));
                _musicData.Add(40, ("combat3", false));
                _musicData.Add(41, ("approach", false));
                _musicData.Add(42, ("death", false));
                _musicData.Add(43, ("victory", false));
                _musicData.Add(44, ("btcastle", false));
                _musicData.Add(45, ("nujelm", true));
                _musicData.Add(46, ("dungeon2", false));
                _musicData.Add(47, ("cove", true));
                _musicData.Add(48, ("moonglow", true));
                _musicData.Add(49, ("zento", true));
                _musicData.Add(50, ("tokunodungeon", true));
                _musicData.Add(51, ("Taiko", true));
                _musicData.Add(52, ("dreadhornarea", true));
                _musicData.Add(53, ("elfcity", true));
                _musicData.Add(54, ("grizzledungeon", true));
                _musicData.Add(55, ("melisandeslair", true));
                _musicData.Add(56, ("paroxysmuslair", true));
                _musicData.Add(57, ("gwennoconversation", true));
                _musicData.Add(58, ("goodendgame", true));
                _musicData.Add(59, ("goodvsevil", true));
                _musicData.Add(60, ("greatearthserpents", true));
                _musicData.Add(61, ("humanoids_u9", true));
                _musicData.Add(62, ("minocnegative", true));
                _musicData.Add(63, ("paws", true));
                _musicData.Add(64, ("selimsbar", true));
                _musicData.Add(65, ("serpentislecombat_u7", true));
                _musicData.Add(66, ("valoriaships", true));
            }
        }

        /// <summary>
        /// Index Sounds/&lt;id&gt;.wav, if the folder is there at all. One listing, at load.
        ///
        /// A File.Exists per sound played would be a disk hit every time somebody swings a sword.
        /// </summary>
        private void LoadOurs()
        {
            _ours.Clear();

            string folder = Path.Combine(FileManager.BasePath, "Sounds");

            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (string path in Directory.EnumerateFiles(folder, "*.wav"))
            {
                if (int.TryParse(Path.GetFileNameWithoutExtension(path), out int id)
                    && id >= 0 && id < MAX_SOUND_DATA_INDEX_COUNT)
                {
                    _ours[id] = path;
                }
            }

            if (_ours.Count > 0)
            {
                Log.Trace($"{_ours.Count} sound(s) of our own in {folder}");
            }
        }

        /// <summary>
        /// The samples out of a WAV, if it is one this can play.
        ///
        /// The stream these end up in is fixed at 22,050 Hz, mono, 16-bit - Sound.cs hard-codes
        /// all three - so anything else is refused rather than played. Handing it a 44,100 stereo
        /// file plays it at half speed an octave down, which sounds like a broken file rather
        /// than like a wrong header, and nobody would know where to look.
        /// </summary>
        private static byte[] ReadWave(string path)
        {
            byte[] raw;

            try
            {
                raw = File.ReadAllBytes(path);
            }
            catch (IOException e)
            {
                Log.Warn($"could not read {path}: {e.Message}");
                return null;
            }

            if (raw.Length < 44
                || raw[0] != 'R' || raw[1] != 'I' || raw[2] != 'F' || raw[3] != 'F'
                || raw[8] != 'W' || raw[9] != 'A' || raw[10] != 'V' || raw[11] != 'E')
            {
                Log.Warn($"{Path.GetFileName(path)} is not a WAV");
                return null;
            }

            // Walk the chunks rather than assuming the layout. A WAV written by anything but the
            // simplest encoder carries LIST and fact chunks before its data.
            int at = 12;
            int channels = 0, rate = 0, bits = 0;

            while (at + 8 <= raw.Length)
            {
                string kind = Encoding.ASCII.GetString(raw, at, 4);
                int size = BitConverter.ToInt32(raw, at + 4);

                if (size < 0 || at + 8 + size > raw.Length)
                {
                    size = raw.Length - at - 8;
                }

                if (kind == "fmt " && size >= 16)
                {
                    channels = BitConverter.ToInt16(raw, at + 10);
                    rate = BitConverter.ToInt32(raw, at + 12);
                    bits = BitConverter.ToInt16(raw, at + 22);
                }
                else if (kind == "data")
                {
                    if (channels != 1 || rate != 22050 || bits != 16)
                    {
                        Log.Warn(
                            $"{Path.GetFileName(path)} is {rate}Hz {channels}ch {bits}-bit; " +
                            "it has to be 22050 mono 16-bit, so it is being ignored");

                        return null;
                    }

                    byte[] body = new byte[size];
                    Buffer.BlockCopy(raw, at + 8, body, 0, size);

                    return body;
                }

                at += 8 + size + (size & 1);         // chunks are padded to an even length
            }

            Log.Warn($"{Path.GetFileName(path)} has no data chunk");

            return null;
        }

        public unsafe bool TryGetSound(int sound, out byte[] data, out string name)
        {
            data = null;
            name = null;

            if (sound < 0)
            {
                return false;
            }

            // Ours first, so a replacement replaces and a new id works at all.
            if (_ours.TryGetValue(sound, out string mine))
            {
                data = ReadWave(mine);

                if (data != null)
                {
                    name = Path.GetFileNameWithoutExtension(mine);

                    return true;
                }
            }

            ref var entry = ref _file.GetValidRefEntry(sound);
            if (entry.Length <= 0)
                return false;

            _file.Seek(entry.Offset, SeekOrigin.Begin);

            Span<byte> buf = stackalloc byte[40];
            _file.Read(buf);

            name = Encoding.UTF8.GetString(buf);
            data = new byte[entry.Length - 40];
            _file.Read(data);

            return true;
        }

        /// <summary>
        ///     Attempts to parse a line from UO's music Config.txt.
        /// </summary>
        /// <param name="line">A line from the file.</param>
        /// <param name="?">If successful, contains a tuple with these fields: int songIndex, string songName, bool doesLoop</param>
        /// <returns>true if line could be parsed, false otherwise.</returns>
        private bool TryParseConfigLine(string line, out (int index, string name, bool loop) data)
        {
            data = default;

            string[] splits = line.Split(_configFileDelimiters);

            if (splits.Length < 2 || splits.Length > 3)
            {
                return false;
            }

            data.index = int.Parse(splits[0]);
            // check if name exists as file, ignoring case since UO isn't consistent with file case (necessary for *nix)
            // also, not every filename in Config.txt has a file extension, so let's strip it out just in case.
            data.name = GetTrueFileName(Path.GetFileNameWithoutExtension(splits[1]));
            data.loop = splits.Length == 3 && splits[2] == "loop";

            return true;
        }

        /// <summary>
        ///     Returns true filename from name, ignoring case since UO isn't consistent with file case (necessary for *nix)
        /// </summary>
        /// <param name="name">The filename from the music Config.txt</param>
        /// <returns>a string with the true case sensitive filename</returns>
        private string GetTrueFileName(string name)
        {
            // don't worry about subdirectories, we'll recursively search them all
            string dir = FileManager.BasePath + $"/Music";

            // Enumerate all files in the directory, using the file name as a pattern
            // This will list all case variants of the filename even on file systems that
            // are case sensitive.
            Regex  pattern = new Regex($"^{name}.mp3", RegexOptions.IgnoreCase);
            //string[] fileList = Directory.GetFiles(dir, "*.mp3", SearchOption.AllDirectories).Where(path => pattern.IsMatch(Path.GetFileName(path))).ToArray();
            string[] fileList = Directory.GetFiles(dir, "*.mp3", SearchOption.AllDirectories);
            fileList = Array.FindAll(fileList, path => pattern.IsMatch(Path.GetFileName(path)));

            if (fileList != null && fileList.Length != 0)
            {
                if (fileList.Length > 1)
                {
                    // More than one file with the same name but different case spelling found
                    Log.Warn($"Ambiguous File reference for {name}. More than one file found with different spellings.");
                }

                Log.Debug($"Loading music:\t\t{fileList[0]}");

                return Path.GetFileName(fileList[0]);
            }

            // If we've made it this far, there is no file with that name, regardless of case spelling
            // return name and GetMusic will fail gracefully (play nothing)
            Log.Warn($"No File found known as {name}");
            return name;
        }

        public bool TryGetMusicData(int index, out string name, out bool doesLoop)
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

        public override void ClearResources()
        {
            _musicData.Clear();
        }
    }
}
