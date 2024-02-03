#region license

// Copyright (c) 2024, andreakarasho
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

namespace ClassicUO.IO
{
    public class UOFileMul : UOFile
    {
        private readonly int _count, _patch;
        private readonly UOFileIdxMul _idxFile;

        public UOFileMul(string file, string idxfile, int count, int patch = -1) : this(file)
        {
            _idxFile = new UOFileIdxMul(idxfile);
            _count = count;
            _patch = patch;
        }

        public UOFileMul(string file) : base(file)
        {
            Load();
        }

        public UOFile IdxFile => _idxFile;


        public override void FillEntries(ref UOFileIndex[] entries)
        {
            UOFile file = _idxFile ?? (UOFile) this;

            int count = (int) file.Length / 12;
            entries = new UOFileIndex[count];

            for (int i = 0; i < count; i++)
            {
                ref UOFileIndex e = ref entries[i];
                e.Address = StartAddress;   // .mul mmf address
                e.FileSize = (uint) Length; // .mul mmf length
                e.Offset = file.ReadUInt(); // .idx offset
                e.Length = file.ReadInt();  // .idx length
                e.DecompressedLength = 0;   // UNUSED HERE --> .UOP

                int size = file.ReadInt();

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

        private class UOFileIdxMul : UOFile
        {
            public UOFileIdxMul(string idxpath) : base(idxpath, true)
            {
            }

            public override void FillEntries(ref UOFileIndex[] entries)
            {
            }
        }
    }
}