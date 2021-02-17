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

using System;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     A Type-safe representation of the the supported output channel
    ///     constants. This class is immutable and, hence, is thread safe.
    /// </summary>
    /// <author>
    ///     Mat McGowan
    /// </author>
    internal class OutputChannels
    {
        /// <summary>
        ///     Flag to indicate output should include both channels.
        /// </summary>
        public static int BOTH_CHANNELS;

        /// <summary>
        ///     Flag to indicate output should include the left channel only.
        /// </summary>
        public static int LEFT_CHANNEL = 1;

        /// <summary>
        ///     Flag to indicate output should include the right channel only.
        /// </summary>
        public static int RIGHT_CHANNEL = 2;

        /// <summary>
        ///     Flag to indicate output is mono.
        /// </summary>
        public static int DOWNMIX_CHANNELS = 3;

        public static readonly OutputChannels LEFT = new OutputChannels(LEFT_CHANNEL);
        public static readonly OutputChannels RIGHT = new OutputChannels(RIGHT_CHANNEL);
        public static readonly OutputChannels BOTH = new OutputChannels(BOTH_CHANNELS);
        public static readonly OutputChannels DOWNMIX = new OutputChannels(DOWNMIX_CHANNELS);
        private readonly int outputChannels;

        private OutputChannels(int channels)
        {
            outputChannels = channels;

            if (channels < 0 || channels > 3)
            {
                throw new ArgumentException("channels");
            }
        }

        /// <summary>
        ///     Retrieves the code representing the desired output channels.
        ///     Will be one of LEFT_CHANNEL, RIGHT_CHANNEL, BOTH_CHANNELS
        ///     or DOWNMIX_CHANNELS.
        /// </summary>
        /// <returns>
        ///     the channel code represented by this instance.
        /// </returns>
        public virtual int ChannelsOutputCode => outputChannels;

        /// <summary>
        ///     Retrieves the number of output channels represented
        ///     by this channel output type.
        /// </summary>
        /// <returns>
        ///     The number of output channels for this channel output
        ///     type. This will be 2 for BOTH_CHANNELS only, and 1
        ///     for all other types.
        /// </returns>
        public virtual int ChannelCount
        {
            get
            {
                int count = outputChannels == BOTH_CHANNELS ? 2 : 1;

                return count;
            }
        }

        /// <summary>
        ///     Creates an OutputChannels instance
        ///     corresponding to the given channel code.
        /// </summary>
        /// <param name="code">
        ///     one of the OutputChannels channel code constants.
        ///     @throws IllegalArgumentException if code is not a valid
        ///     channel code.
        /// </param>
        public static OutputChannels fromInt(int code)
        {
            switch (code)
            {
                case (int) OutputChannelsEnum.LEFT_CHANNEL: return LEFT;

                case (int) OutputChannelsEnum.RIGHT_CHANNEL: return RIGHT;

                case (int) OutputChannelsEnum.BOTH_CHANNELS: return BOTH;

                case (int) OutputChannelsEnum.DOWNMIX_CHANNELS: return DOWNMIX;

                default: throw new ArgumentException("Invalid channel code: " + code);
            }
        }

        public override bool Equals(object o)
        {
            bool equals = false;

            if (o is OutputChannels)
            {
                OutputChannels oc = (OutputChannels) o;
                equals = oc.outputChannels == outputChannels;
            }

            return equals;
        }

        public override int GetHashCode()
        {
            return outputChannels;
        }
    }
}