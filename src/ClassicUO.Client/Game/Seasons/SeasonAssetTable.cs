// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Concrete <see cref="ISeasonAssetTable"/> backed by ten fixed-size
    /// ushort arrays (static + land tile, one pair per <see cref="Season"/>).
    /// Zero is the sentinel for "no override".
    /// </summary>
    internal sealed class SeasonAssetTable : ISeasonAssetTable
    {
        private ushort[] _springLandTile;
        private ushort[] _summerLandTile;
        private ushort[] _fallLandTile;
        private ushort[] _winterLandTile;
        private ushort[] _desolationLandTile;

        private ushort[] _springGraphic;
        private ushort[] _summerGraphic;
        private ushort[] _fallGraphic;
        private ushort[] _winterGraphic;
        private ushort[] _desolationGraphic;

        public SeasonAssetTable()
        {
            Reset();
        }

        public void Reset()
        {
            _springLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _summerLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _fallLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _winterLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];
            _desolationLandTile = new ushort[ArtLoader.MAX_LAND_DATA_INDEX_COUNT];

            _springGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _summerGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _fallGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _winterGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
            _desolationGraphic = new ushort[ArtLoader.MAX_STATIC_DATA_INDEX_COUNT];
        }

        public void SetStatic(Season season, ushort orig, ushort replace)
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

        public void SetLand(Season season, ushort orig, ushort replace)
        {
            switch (season)
            {
                case Season.Spring: _springLandTile[orig] = replace; break;
                case Season.Summer: _summerLandTile[orig] = replace; break;
                case Season.Fall: _fallLandTile[orig] = replace; break;
                case Season.Winter: _winterLandTile[orig] = replace; break;
                case Season.Desolation: _desolationLandTile[orig] = replace; break;
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

        public ushort GetLandSeasonGraphic(Season season, ushort graphic)
        {
            switch (season)
            {
                case Season.Spring: return _springLandTile[graphic] == 0 ? graphic : _springLandTile[graphic];
                case Season.Summer: return _summerLandTile[graphic] == 0 ? graphic : _summerLandTile[graphic];
                case Season.Fall: return _fallLandTile[graphic] == 0 ? graphic : _fallLandTile[graphic];
                case Season.Winter: return _winterLandTile[graphic] == 0 ? graphic : _winterLandTile[graphic];
                case Season.Desolation: return _desolationLandTile[graphic] == 0 ? graphic : _desolationLandTile[graphic];
            }

            return graphic;
        }
    }
}
