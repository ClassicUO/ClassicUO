namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     This struct describes all error codes that can be thrown
    ///     in BistreamExceptions.
    /// </summary>
    internal readonly struct BitstreamErrors
    {
        public static readonly int UNKNOWN_ERROR = BITSTREAM_ERROR + 0;
        public static readonly int UNKNOWN_SAMPLE_RATE = BITSTREAM_ERROR + 1;
        public static readonly int STREAM_ERROR = BITSTREAM_ERROR + 2;
        public static readonly int UNEXPECTED_EOF = BITSTREAM_ERROR + 3;
        public static readonly int STREAM_EOF = BITSTREAM_ERROR + 4;
        public static readonly int BITSTREAM_LAST = 0x1ff;

        public static readonly int BITSTREAM_ERROR = 0x100;
        public static readonly int DECODER_ERROR = 0x200;
    }
}