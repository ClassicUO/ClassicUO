namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII
{
    internal class ScaleFactorTable
    {
        public int[] l;
        public int[] s;

        public ScaleFactorTable(LayerIIIDecoder enclosingInstance)
        {
            InitBlock(enclosingInstance);
            l = new int[5];
            s = new int[3];
        }

        public ScaleFactorTable(LayerIIIDecoder enclosingInstance, int[] thel, int[] thes)
        {
            InitBlock(enclosingInstance);
            l = thel;
            s = thes;
        }

        public LayerIIIDecoder Enclosing_Instance { get; private set; }

        private void InitBlock(LayerIIIDecoder enclosingInstance)
        {
            Enclosing_Instance = enclosingInstance;
        }
    }
}