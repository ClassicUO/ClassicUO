// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Concrete <see cref="ISeasonAssetSwap"/> backed by five fixed-size
    /// ushort arrays (one per <see cref="Season"/>) of static-art remaps.
    /// Zero is the sentinel for "no override".
    /// </summary>
    internal sealed class SeasonAssetSwap : ISeasonAssetSwap
    {
        private ushort[] _springGraphic;
        private ushort[] _summerGraphic;
        private ushort[] _fallGraphic;
        private ushort[] _winterGraphic;
        private ushort[] _desolationGraphic;

        public SeasonAssetSwap()
        {
            Reset();
        }

        public void Reset()
        {
            _springGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _summerGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _fallGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _winterGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _desolationGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
        }

        public void Set(Season season, ushort orig, ushort replace)
        {
            switch (season)
            {
                case Season.Spring: _springGraphic[orig] = replace; break;
                case Season.Summer: _summerGraphic[orig] = replace; break;
                case Season.Fall: _fallGraphic[orig] = replace; break;
                case Season.Winter: _winterGraphic[orig] = replace; break;
                case Season.Desolation: _desolationGraphic[orig] = replace; break;
            }
        }

        public ushort GetSeasonGraphic(Season season, ushort graphic)
        {
            switch (season)
            {
                case Season.Spring: return _springGraphic[graphic] == 0 ? graphic : _springGraphic[graphic];
                case Season.Summer: return _summerGraphic[graphic] == 0 ? graphic : _summerGraphic[graphic];
                case Season.Fall: return _fallGraphic[graphic] == 0 ? graphic : _fallGraphic[graphic];
                case Season.Winter: return _winterGraphic[graphic] == 0 ? graphic : _winterGraphic[graphic];
                case Season.Desolation: return _desolationGraphic[graphic] == 0 ? graphic : _desolationGraphic[graphic];
            }

            return graphic;
        }
    }
}
