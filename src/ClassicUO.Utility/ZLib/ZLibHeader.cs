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

using System;

namespace ZLibNative
{
    public enum FLevel
    {
        Faster = 0,
        Fast = 1,
        Default = 2,
        Optimal = 3
    }

    public sealed class ZLibHeader
    {
        private byte _CompressionInfo;   //CMF 4-7
        private byte _CompressionMethod; //CMF 0-3
        private byte _FCheck;            //Flag 0-4 (Check bits for CMF and FLG)

        public bool IsSupportedZLibStream { get; set; }

        public byte CompressionMethod
        {
            get => _CompressionMethod;
            set
            {
                if (value > 15)
                {
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                }

                _CompressionMethod = value;
            }
        }

        public byte CompressionInfo
        {
            get => _CompressionInfo;
            set
            {
                if (value > 15)
                {
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 15");
                }

                _CompressionInfo = value;
            }
        }

        public byte FCheck
        {
            get => _FCheck;
            set
            {
                if (value > 31)
                {
                    throw new ArgumentOutOfRangeException("Argument cannot be greater than 31");
                }

                _FCheck = value;
            }
        }

        public bool FDict { get; set; }
        public FLevel FLevel { get; set; }

        private void RefreshFCheck()
        {
            byte byteFLG = (byte) (Convert.ToByte(FLevel) << 1);
            byteFLG |= Convert.ToByte(FDict);

            FCheck = Convert.ToByte(31 - Convert.ToByte((GetCMF() * 256 + byteFLG) % 31));
        }

        private byte GetCMF()
        {
            byte byteCMF = (byte) (CompressionInfo << 4);
            byteCMF |= CompressionMethod;

            return byteCMF;
        }

        private byte GetFLG()
        {
            byte byteFLG = (byte) (Convert.ToByte(FLevel) << 6);
            byteFLG |= (byte) (Convert.ToByte(FDict) << 5);
            byteFLG |= FCheck;

            return byteFLG;
        }

        public byte[] EncodeZlibHeader()
        {
            byte[] result = new byte[2];

            RefreshFCheck();

            result[0] = GetCMF();
            result[1] = GetFLG();

            return result;
        }

        public static ZLibHeader DecodeHeader(int pCMF, int pFlag)
        {
            ZLibHeader result = new ZLibHeader();

            //Ensure that parameters are bytes
            pCMF &= 0x0FF;
            pFlag &= 0x0FF;

            //Decode bytes
            result.CompressionInfo = Convert.ToByte((pCMF & 0xF0) >> 4);
            result.CompressionMethod = Convert.ToByte(pCMF & 0x0F);

            result.FCheck = Convert.ToByte(pFlag & 0x1F);
            result.FDict = Convert.ToBoolean(Convert.ToByte((pFlag & 0x20) >> 5));
            result.FLevel = (FLevel) Convert.ToByte((pFlag & 0xC0) >> 6);

            result.IsSupportedZLibStream = result.CompressionMethod == 8 && result.CompressionInfo == 7 && (pCMF * 256 + pFlag) % 31 == 0 && result.FDict == false;

            return result;
        }
    }
}