namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     This interface provides constants describing the error
    ///     codes used by the Decoder to indicate errors.
    /// </summary>
    internal readonly struct DecoderErrors
    {
        public static readonly int UNKNOWN_ERROR = BitstreamErrors.DECODER_ERROR + 0;
        public static readonly int UNSUPPORTED_LAYER = BitstreamErrors.DECODER_ERROR + 1;
    }
}