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

namespace ClassicUO.IO
{
    public class UOFileMul : UOFile
    {
        private readonly UOFile _idxFile;

        public UOFileMul(string file, string idxfile) : this(file)
        {
            _idxFile = new UOFile(idxfile);
        }

        public UOFileMul(string file) : base(file)
        {

        }

        public UOFile IdxFile => _idxFile;


        public override void FillEntries()
        {
            UOFile f = _idxFile ?? this;
            int count = (int)f.Length / 12;
            Entries = new UOFileIndex[count];

            for (int i = 0; i < Entries.Length; i++)
            {
                ref var e = ref Entries[i];
                e.File = this;   // .mul mmf address
                e.Offset = f.ReadUInt32(); // .idx offset
                e.Length = f.ReadInt32();  // .idx length
                e.DecompressedLength = 0;   // UNUSED HERE --> .UOP

                int size = f.ReadInt32();

                if (size > 0)
                {
                    e.Width = (short) (size >> 16);
                    e.Height = (short) (size & 0xFFFF);
                }
            }
        }

        public override void Dispose()
        {
            _idxFile?.Dispose();
            base.Dispose();
        }
    }
}