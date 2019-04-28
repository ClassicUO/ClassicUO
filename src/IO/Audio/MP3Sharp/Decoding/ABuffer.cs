namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     Base Class for audio output.
    /// </summary>
    internal abstract class ABuffer
    {
        public const int OBUFFERSIZE = 2 * 1152; // max. 2 * 1152 samples per frame
        public const int MAXCHANNELS = 2; // max. number of channels

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public abstract void Append(int channel, short valueRenamed);

        /// <summary>
        ///     Accepts 32 new PCM samples.
        /// </summary>
        public virtual void AppendSamples(int channel, float[] f)
        {
            for (int i = 0; i < 32; i++) Append(channel, Clip(f[i]));
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