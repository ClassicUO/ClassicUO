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

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     Base Class for audio output.
    /// </summary>
    internal abstract class ABuffer
    {
        public const int OBUFFERSIZE = 2 * 1152; // max. 2 * 1152 samples per frame
        public const int MAXCHANNELS = 2;        // max. number of channels

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public abstract void Append(int channel, short valueRenamed);

        /// <summary>
        ///     Accepts 32 new PCM samples.
        /// </summary>
        public virtual void AppendSamples(int channel, float[] f)
        {
            for (int i = 0; i < 32; i++)
            {
                Append(channel, Clip(f[i]));
            }
        }

        /// <summary>
        ///     Clip Sample to 16 Bits
        /// </summary>
        private static short Clip(float sample)
        {
            return sample > 32767.0f ? (short) 32767 : sample < -32768.0f ? (short) -32768 : (short) sample;
        }

        /// <summary>
        ///     Write the samples to the file or directly to the audio hardware.
        /// </summary>
        public abstract void WriteBuffer(int val);

        public abstract void Close();

        /// <summary>
        ///     Clears all data in the buffer (for seeking).
        /// </summary>
        public abstract void ClearBuffer();

        /// <summary>
        ///     Notify the buffer that the user has stopped the stream.
        /// </summary>
        public abstract void SetStopFlag();
    }
}