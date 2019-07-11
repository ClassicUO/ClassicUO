namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII
{
    internal class GranuleInfo
    {
        public int BigValues;
        public int BlockType;
        public int Count1TableSelect;
        public int GlobalGain;
        public int MixedBlockFlag;
        public int Part23Length;
        public int Preflag;
        public int Region0Count;
        public int Region1Count;
        public int ScaleFacCompress;
        public int ScaleFacScale;
        public int[] SubblockGain;
        public int[] TableSelect;
        public int WindowSwitchingFlag;

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public GranuleInfo()
        {
            TableSelect = new int[3];
            SubblockGain = new int[3];
        }
    }
}