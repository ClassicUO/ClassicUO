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

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Data;
using ClassicUO.Game;

namespace ClassicUO.IO.Resources
{
    internal class MultiLoader : UOFileLoader
    {
        private static MultiLoader _instance;

        private MultiLoader()
        {
        }

        public static MultiLoader Instance => _instance ?? (_instance = new MultiLoader());

        public int Count { get; private set; }
        public UOFile File { get; private set; }

        public bool IsUOP { get; private set; }
        public int Offset { get; private set; }


        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string uopPath = UOFileManager.GetUOFilePath("MultiCollection.uop");

                    if (Client.IsUOPInstallation && System.IO.File.Exists(uopPath))
                    {
                        Count = Constants.MAX_MULTI_DATA_INDEX_COUNT;
                        File = new UOFileUop(uopPath, "build/multicollection/{0:D6}.bin");
                        Entries = new UOFileIndex[Count];
                        IsUOP = true;
                    }
                    else
                    {
                        string path = UOFileManager.GetUOFilePath("multi.mul");
                        string pathidx = UOFileManager.GetUOFilePath("multi.idx");

                        if (System.IO.File.Exists(path) && System.IO.File.Exists(pathidx))
                        {
                            File = new UOFileMul(path, pathidx, Constants.MAX_MULTI_DATA_INDEX_COUNT, 14);

                            Count = Offset = Client.Version >= ClientVersion.CV_7090 ? sizeof(MultiBlockNew) + 2 : sizeof(MultiBlock);
                        }
                    }

                    File.FillEntries(ref Entries);
                }
            );
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal ref struct MultiBlock
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal ref struct MultiBlockNew
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public ushort Flags;
        public uint Unknown;
    }
}