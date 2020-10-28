namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders
{
    /// <summary>
    ///     Implementations of FrameDecoder are responsible for decoding
    ///     an MPEG audio frame.
    /// </summary>
    //REVIEW: the interface currently is too thin. There should be
    // methods to specify the output buffer, the synthesis filters and
    // possibly other objects used by the decoder. 
    internal interface IFrameDecoder
    {
        /// <summary>
        ///     Decodes one frame of MPEG audio.
        /// </summary>
        void DecodeFrame();
    }
}