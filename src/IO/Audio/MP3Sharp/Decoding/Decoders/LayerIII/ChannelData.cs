namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII
{
    internal class ChannelData
    {
        public GranuleInfo[] Granules;
        public int[] ScaleFactorBits;

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public ChannelData()
        {
            ScaleFactorBits = new int[4];
            Granules = new GranuleInfo[2];
            Granules[0] = new GranuleInfo();
            Granules[1] = new GranuleInfo();
        }
    }
}