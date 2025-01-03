#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.Utility.Logging;
using System;
using System.IO;

namespace ClassicUO.IO
{
    public class UOFile : MMFileReader
    {
        public UOFile(string filepath) : base(File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            FilePath = filepath;
            Entries = Array.Empty<UOFileIndex>();

            Log.Trace($"Loading file:\t\t{filepath}");

            if (!File.Exists(filepath))
            {
                Log.Error($"{filepath} not exists.");
            }
        }


        public string FilePath { get; }
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