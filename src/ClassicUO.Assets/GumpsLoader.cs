// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class GumpsLoader : UOFileLoader
    {
        public const int MAX_GUMP_DATA_INDEX_COUNT = 0x10000;

        /// <summary>
        /// Gump art of the shard's own, as loose files, by the number the server asks for.
        ///
        /// gump.def can only fill an empty slot - it skips anything the archive already has - so
        /// it aliases and never replaces. These do both: a file here is used whether or not the
        /// archive has that number, which is what makes correcting an existing gump possible.
        ///
        /// The archive uses 5,571 of the 65,536 numbers and its highest is 61,728, so there is
        /// plenty of room to put ours somewhere nothing will ever collide with.
        /// </summary>
        private readonly Dictionary<int, string> _ours = new Dictionary<int, string>();

        private UOFile _file;

        public GumpsLoader(UOFileManager fileManager) : base(fileManager) { }


        public bool UseUOPGumps = false;
        public UOFile File => _file;

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("gumpartLegacyMUL.uop");

            if (FileManager.IsUOPInstallation && System.IO.File.Exists(path))
            {
                _file = new UOFileUop(path, "build/gumpartlegacymul/{0:D8}.tga", true);
                UseUOPGumps = true;
            }
            else
            {
                path = FileManager.GetUOFilePath("gumpart.mul");
                string pathidx = FileManager.GetUOFilePath("gumpidx.mul");

                if (!System.IO.File.Exists(path))
                {
                    path = FileManager.GetUOFilePath("Gumpart.mul");
                }

                if (!System.IO.File.Exists(pathidx))
                {
                    pathidx = FileManager.GetUOFilePath("Gumpidx.mul");
                }

                _file = new UOFileMul(path, pathidx);

                UseUOPGumps = false;
            }

            _file.FillEntries();

            LoadOurs();

            string pathdef = FileManager.GetUOFilePath("gump.def");

            if (!System.IO.File.Exists(pathdef))
            {
                return;
            }

            using (DefReader defReader = new DefReader(pathdef, 3))
            {
                while (defReader.Next())
                {
                    int ingump = defReader.ReadInt();

                    if (
                        ingump < 0
                        || ingump >= MAX_GUMP_DATA_INDEX_COUNT
                        || ingump >= _file.Entries.Length
                        || _file.Entries[ingump].Length > 0
                    )
                    {
                        continue;
                    }

                    int[] group = defReader.ReadGroup();

                    if (group == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];

                        if (
                            checkIndex < 0
                            || checkIndex >= MAX_GUMP_DATA_INDEX_COUNT
                            || checkIndex >= _file.Entries.Length
                            || _file.Entries[checkIndex].Length <= 0
                        )
                        {
                            continue;
                        }

                        _file.Entries[ingump] = _file.Entries[checkIndex];
                        _file.Entries[ingump].Hue = (ushort)defReader.ReadInt();

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Find the shard's own gump art once, at load, rather than asking the disk every time
        /// something is drawn.
        ///
        /// A File.Exists per gump would be a disk hit for every frame of every open window.
        /// </summary>
        private void LoadOurs()
        {
            _ours.Clear();

            string folder = Path.Combine(FileManager.BasePath, "Gumps");

            if (!Directory.Exists(folder))
            {
                return;
            }

            foreach (string path in Directory.EnumerateFiles(folder, "*.gump"))
            {
                if (int.TryParse(Path.GetFileNameWithoutExtension(path), out int id)
                    && id >= 0 && id < MAX_GUMP_DATA_INDEX_COUNT)
                {
                    _ours[id] = path;
                }
            }

            if (_ours.Count > 0)
            {
                Log.Trace($"{_ours.Count} gump(s) of our own in {folder}");
            }
        }

        /// <summary>
        /// One of ours off the disk.
        ///
        /// The file is the same run-length picture the archive stores, with its size in front:
        /// two 32-bit numbers for width and height, then the rows. That is not a new format -
        /// the archive's own compressed entries carry their size inline in exactly this way and
        /// are read by exactly this code a few lines down. Writing it out is a matter of leaving
        /// the size where the reader below already looks for it.
        /// </summary>
        private GumpInfo ReadOurs(string path)
        {
            byte[] raw;

            try
            {
                raw = System.IO.File.ReadAllBytes(path);
            }
            catch (IOException e)
            {
                Log.Warn($"could not read {path}: {e.Message}");
                return default;
            }

            if (raw.Length < 8)
            {
                Log.Warn($"{Path.GetFileName(path)} is too short to be a gump");
                return default;
            }

            var reader = new StackDataReader(raw);
            var w = reader.ReadUInt32LE();
            var h = reader.ReadUInt32LE();

            // A bad size is worth naming out loud. Believed, it asks for a picture of some
            // billions of pixels and takes the client down with it.
            if (w == 0 || h == 0 || w > 4096 || h > 4096)
            {
                Log.Warn($"{Path.GetFileName(path)} claims to be {w}x{h}");
                return default;
            }

            if (reader.Remaining < h * 4)
            {
                Log.Warn($"{Path.GetFileName(path)} has no room for {h} rows");
                return default;
            }

            return Decode(ref reader, w, h, 0);
        }

        public GumpInfo GetGump(uint index)
        {
            // Ours first, so a file can correct a gump the archive already has and not only fill
            // a number it lacks.
            if (_ours.Count > 0 && _ours.TryGetValue((int)index, out string ours))
            {
                GumpInfo mine = ReadOurs(ours);

                if (mine.Width > 0)
                {
                    return mine;
                }

                // A broken file falls through to the archive rather than leaving a hole, so a bad
                // import shows the old art and a warning instead of nothing at all.
            }

            ref var entry = ref _file.GetValidRefEntry((int)index);

            if (entry.CompressionFlag != CompressionType.ZlibBwt && entry.Width <= 0 && entry.Height <= 0)
            {
                return default;
            }

            ushort color = entry.Hue;

            var file = _file;
            if (entry.File != null)
                file = entry.File;

            file.Seek(entry.Offset, SeekOrigin.Begin);

            var buf = new byte[entry.Length];
            file.Read(buf);

            var reader = new StackDataReader(buf);
            var w = (uint)entry.Width;
            var h = (uint)entry.Height;

            if (entry.CompressionFlag >= CompressionType.Zlib)
            {
                var dbuf = new byte[entry.DecompressedLength];
                var result = ClassicUO.Utility.ZLib.Decompress(reader.Buffer.Slice(reader.Position), dbuf);
                if (result != Utility.ZLib.ZLibError.Ok)
                {
                    return default;
                }

                if (entry.CompressionFlag == CompressionType.ZlibBwt)
                {
                    dbuf = ClassicUO.Utility.BwtDecompress.Decompress(dbuf);
                }

                reader = new StackDataReader(dbuf);
                w = reader.ReadUInt32LE();
                h = reader.ReadUInt32LE();

                if (entry.Width <= 0)
                    entry.Width = (int)w;
                if (entry.Height <= 0)
                    entry.Height = (int)h;
            }

            return Decode(ref reader, w, h, color);
        }

        /// <summary>
        /// The rows of a gump, from wherever they came from.
        ///
        /// A row lookup of one 32-bit word per row - an offset from the table's own beginning, in
        /// units of four bytes - and then pairs of (colour, run), the colour being 16-bit with
        /// zero meaning transparent, which is why a mostly-empty gump costs so little.
        /// </summary>
        private GumpInfo Decode(scoped ref StackDataReader reader, uint w, uint h, ushort color)
        {
            Span<uint> pixels = new uint[w * h];
            var len = reader.Remaining;
            var halfLen = len >> 2;

            var start = reader.Position;
            var rowLookup = new int[h];
            reader.Read(MemoryMarshal.AsBytes<int>(rowLookup.AsSpan()));

            for (var y = 0; y < h; ++y)
            {
                reader.Seek(start + (rowLookup[y] << 2));
                var pixelIndex = (int)(y * w);
                var gsize = (y < h - 1) ? rowLookup[y + 1] - rowLookup[y] : halfLen - rowLookup[y];
                for (var i = 0; i < gsize; ++i)
                {
                    var value = reader.ReadUInt16LE();
                    var run = reader.ReadUInt16LE();
                    var rbga = 0u;

                    if (color != 0 && value != 0)
                    {
                        value = FileManager.Hues.GetColor16(value, color);
                    }

                    if (value != 0)
                    {
                        rbga = HuesHelper.Color16To32(value) | 0xFF_00_00_00;
                    }

                    // A run that would spill past the end of the row means the file disagrees
                    // with its own header. Trusting it walks off the end of the picture, and an
                    // IndexOutOfRange thrown from inside the drawing code is a long way from the
                    // file that caused it.
                    if (pixelIndex + run > pixels.Length)
                    {
                        run = (ushort)Math.Max(0, pixels.Length - pixelIndex);

                        if (run == 0)
                        {
                            break;
                        }
                    }

                    pixels.Slice(pixelIndex, run).Fill(rbga);
                    pixelIndex += run;
                }
            }

            return new GumpInfo()
            {
                Pixels = pixels,
                Width = (int)w,
                Height = (int)h
            };
        }
    }

    public ref struct GumpInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
