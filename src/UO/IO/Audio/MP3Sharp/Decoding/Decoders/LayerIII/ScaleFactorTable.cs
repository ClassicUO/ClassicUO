namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII
{
    class ScaleFactorTable
    {
        private LayerIIIDecoder enclosingInstance;
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

        public LayerIIIDecoder Enclosing_Instance
        {
            get { return enclosingInstance; }
        }

        private void InitBlock(LayerIIIDecoder enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
    }
}
