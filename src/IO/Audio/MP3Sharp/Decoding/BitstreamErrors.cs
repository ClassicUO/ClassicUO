#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     This struct describes all error codes that can be thrown
    ///     in BistreamExceptions.
    /// </summary>
    internal readonly struct BitstreamErrors
    {
        public static readonly int BITSTREAM_ERROR = 0x100;
        public static readonly int DECODER_ERROR = 0x200;

        public static readonly int UNKNOWN_ERROR = BITSTREAM_ERROR + 0;
        public static readonly int UNKNOWN_SAMPLE_RATE = BITSTREAM_ERROR + 1;
        public static readonly int STREAM_ERROR = BITSTREAM_ERROR + 2;
        public static readonly int UNEXPECTED_EOF = BITSTREAM_ERROR + 3;
        public static readonly int STREAM_EOF = BITSTREAM_ERROR + 4;
        public static readonly int BITSTREAM_LAST = 0x1ff;

    }
}