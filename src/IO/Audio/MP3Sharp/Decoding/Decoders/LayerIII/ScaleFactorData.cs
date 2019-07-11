namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII
{
    internal class ScaleFactorData
    {
        public int[] l; /* [cb] */
        public int[][] s; /* [window][cb] */

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public ScaleFactorData()
        {
            l = new int[23];
            s = new int[3][];
            for (int i = 0; i < 3; i++) s[i] = new int[13];
        }
    }
}