// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using System;
using System.IO;

namespace ClassicUO.IO
{
    public class UOFile : MMFileReader
    {
        public UOFile(string filepath) : base(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Entries = Array.Empty<UOFileIndex>();

            Log.Trace($"Loading file:\t\t{filepath}");

            if (!File.Exists(filepath))
            {
                Log.Error($"{filepath} not exists.");
            }
        }


        public UOFileIndex[] Entries;


        public ref UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || Entries == null || index >= Entries.Length)
            {
                return ref UOFileIndex.Invalid;
            }

            ref UOFileIndex entry = ref Entries[index];

            if (entry.Offset < 0 || entry.Length <= 0 || entry.Offset == 0x0000_0000_FFFF_FFFF)
            {
                return ref UOFileIndex.Invalid;
            }

            return ref entry;
        }

        public virtual void FillEntries()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            Log.Trace($"Unloaded:\t\t{FilePath}");
        }
    }
}