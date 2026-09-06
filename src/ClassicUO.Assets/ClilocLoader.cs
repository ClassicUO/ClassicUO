// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ClassicUO.Assets
{
    public sealed class ClilocLoader : UOFileLoader
    {
        private const string MISSING_CLILOC_TEXT = "MegaCliloc: missing {0} [~1_val~] [~2_val~]";
        private string _cliloc;
        private readonly Dictionary<int, string> _entries = new Dictionary<int, string>();

        public ClilocLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public void Load(string lang)
        {
            if (string.IsNullOrEmpty(lang))
            {
                lang = "enu";
            }

            _cliloc = $"Cliloc.{lang}";
            Log.Trace($"searching for: '{_cliloc}'");

            if (!File.Exists(FileManager.GetUOFilePath(_cliloc)))
            {
                Log.Warn($"'{_cliloc}' not found. Rolled back to Cliloc.enu");

                _cliloc = "Cliloc.enu";
            }

            Load();
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(_cliloc))
            {
                _cliloc = "Cliloc.enu";
            }

            string path = FileManager.GetUOFilePath(_cliloc);

            if (!File.Exists(path))
            {
                Log.Error($"cliloc not found: '{path}'");
                return;
            }

            if (string.Compare(_cliloc, "cliloc.enu", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                string enupath = FileManager.GetUOFilePath("Cliloc.enu");
                ReadCliloc(enupath);
            }

            ReadCliloc(path);

            ReadOurs();
        }

        /// <summary>
        /// Strings of the shard's own, from a plain text file, read last so they win.
        ///
        /// A context menu entry, a gump caption and an item's name are all cliloc numbers, and a
        /// shard is limited to whatever the client already happens to say. There is no number for
        /// "Send to kennel" because Origin never needed one, so anything a shard adds is either
        /// approximated with a string that nearly fits or shown as a raw number.
        ///
        /// The file is read after Cliloc.enu and after the language file, and this dictionary is
        /// last-write-wins - which is how the language file already overrides the English one. So
        /// the same mechanism adds new numbers and corrects existing ones, and neither needs the
        /// compressed original touched.
        ///
        /// Plain text rather than the client's own format on purpose: a line somebody can read,
        /// diff and edit beats a BWT-compressed blob for a file that will hold a few dozen
        /// entries. Anything above 3,100,000 is free - the client's own stop at 3,011,032.
        ///
        ///     # Clilocs.txt, in the client folder
        ///     3100001    Breed
        ///     3100002    Send to kennel
        /// </summary>
        private void ReadOurs()
        {
            string path = FileManager.GetUOFilePath("Clilocs.txt");

            if (!File.Exists(path))
            {
                return;
            }

            var added = 0;

            try
            {
                foreach (string line in File.ReadLines(path))
                {
                    string trimmed = line.Trim();

                    if (trimmed.Length == 0 || trimmed[0] == '#')
                    {
                        continue;
                    }

                    // A tab or the first run of spaces separates the number from the text, so the
                    // text itself may contain anything including tabs after that point.
                    int at = trimmed.IndexOfAny(new[] { '\t', ' ' });

                    if (at <= 0)
                    {
                        continue;
                    }

                    if (!int.TryParse(trimmed.AsSpan(0, at), out int number))
                    {
                        continue;
                    }

                    _entries[number] = string.Intern(trimmed[(at + 1)..].TrimStart());
                    added++;
                }
            }
            catch (IOException e)
            {
                Log.Warn($"could not read {path}: {e.Message}");
                return;
            }

            if (added > 0)
            {
                Log.Trace($"{added} cliloc string(s) of our own from {Path.GetFileName(path)}");
            }
        }

        void ReadCliloc(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);

            int bytesRead;
            var totalRead = 0;
            var buf = new byte[fileStream.Length];
            while ((bytesRead = fileStream.Read(buf, totalRead, Math.Min(4096, buf.Length - totalRead))) > 0)
                totalRead += bytesRead;

            var output = buf[3] == 0x8E /*|| FileManager.Version >= ClientVersion.CV_7010400*/ ? BwtDecompress.Decompress(buf) : buf;

            var reader = new StackDataReader(output);
            reader.ReadInt32LE();
            reader.ReadInt16LE();

            while (reader.Remaining > 0)
            {
                var number = reader.ReadInt32LE();
                var flag = reader.ReadUInt8();
                var length = reader.ReadInt16LE();
                var text = string.Intern(reader.ReadUTF8(length));

                _entries[number] = text;
            }
        }

        public override void ClearResources()
        {
            _entries.Clear();
        }

        [return: NotNull]
        public string GetString(int clilocNum, string fallback = null)
        {
            _entries.TryGetValue(clilocNum, out string text);

            return text ?? fallback ?? string.Format(MISSING_CLILOC_TEXT, clilocNum);
        }

        [return: NotNull]
        public string GetString(int clilocNum, bool camelcase, string fallback = "")
        {
            string text = GetString(clilocNum, fallback);

            if (camelcase)
            {
                return StringHelper.CapitalizeAllWords(text);
            }

            return text;
        }

        [return: NotNull]
        public unsafe string Translate(int clilocNum, string arg = "", bool capitalize = false)
        {
            string baseCliloc = GetString(clilocNum);

            if (arg == null)
            {
                arg = "";
            }

            var roChars = arg.AsSpan();


            // get count of valid args
            int i = 0;
            int totalArgs = 0;
            int trueStart = -1;

            for (; i < roChars.Length; ++i)
            {
                if (roChars[i] != '\t')
                {
                    if (trueStart == -1)
                    {
                        trueStart = i;
                    }
                }
                else if (trueStart >= 0)
                {
                    ++totalArgs;
                }
            }

            if (trueStart == -1)
            {
                trueStart = 0;
            }

            // store index locations
            Span<(int, int)> locations = stackalloc (int, int)[++totalArgs];
            i = trueStart;
            for (int j = 0; i < roChars.Length; ++i)
            {
                if (roChars[i] == '\t')
                {
                    locations[j].Item1 = trueStart;
                    locations[j].Item2 = i;

                    trueStart = i + 1;

                    ++j;
                }
            }

            bool has_arguments = totalArgs - 1 > 0;

            locations[totalArgs - 1].Item1 = trueStart;
            locations[totalArgs - 1].Item2 = i;

            ValueStringBuilder sb = new ValueStringBuilder(baseCliloc.AsSpan());
            {
                int index, pos = 0;

                while (pos < sb.Length)
                {
                    int poss = pos;
                    pos = sb.RawChars.Slice(pos, sb.Length - pos).IndexOf('~');

                    if (pos == -1)
                    {
                        break;
                    }

                    pos += poss;

                    int pos2 = sb.RawChars.Slice(pos + 1, sb.Length - (pos + 1)).IndexOf('~');

                    if (pos2 == -1) //non valid arg
                    {
                        break;
                    }

                    pos2 += pos + 1;

                    index = sb.RawChars.Slice(pos + 1, pos2 - (pos + 1)).IndexOf('_');

                    if (index == -1)
                    {
                        //there is no underscore inside the bounds, so we use all the part to get the number of argument
                        index = pos2;
                    }
                    else
                    {
                        index += pos + 1;
                    }

                    int start = pos + 1;
                    int max = index - start;
                    int count = 0;

                    for (; count < max; count++)
                    {
                        if (!char.IsNumber(sb.RawChars[start + count]))
                        {
                            break;
                        }
                    }

                    if (!int.TryParse(sb.RawChars.Slice(start, count).ToString(), out index))
                    {
                        return $"MegaCliloc: error for {clilocNum}";
                    }

                    --index;

                    var a = index < 0 || index >= totalArgs ? string.Empty.AsSpan() : arg.AsSpan().Slice(locations[index].Item1, locations[index].Item2 - locations[index].Item1);

                    if (a.Length > 1)
                    {
                        if (a[0] == '#')
                        {
                            if (int.TryParse(a.Slice(1).ToString(), out int id1))
                            {
                                var ss = GetString(id1);

                                if (string.IsNullOrEmpty(ss))
                                {
                                    a = string.Empty.AsSpan();
                                }
                                else
                                {
                                    a = ss.AsSpan();
                                }
                            }
                        }
                        else if (has_arguments && int.TryParse(a.ToString(), out int clil))
                        {
                            if (_entries.TryGetValue(clil, out string value) && !string.IsNullOrEmpty(value))
                            {
                                a = value.AsSpan();
                            }
                        }
                    }

                    sb.Remove(pos, pos2 - pos + 1);
                    sb.Insert(pos, a);

                    if (index >= 0 && index < totalArgs)
                    {
                        pos += a.Length /*locations[index].Y - locations[index].X*/;
                    }
                }

                baseCliloc = sb.ToString();

                sb.Dispose();

                if (capitalize)
                {
                    baseCliloc = StringHelper.CapitalizeAllWords(baseCliloc);
                }

                return baseCliloc;
            }
        }
    }
}
