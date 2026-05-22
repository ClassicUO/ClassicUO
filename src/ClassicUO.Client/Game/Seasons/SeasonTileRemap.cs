// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;

namespace ClassicUO.Game.Seasons
{
    /// <summary>
    /// Concrete <see cref="ISeasonTileRemap"/> backed by five fixed-size
    /// ushort arrays (one per <see cref="Season"/>) of land-tile remaps.
    /// Zero is the sentinel for "no override".
    /// </summary>
    internal sealed class SeasonTileRemap : ISeasonTileRemap
    {
        private ushort[] _springLandTile;
        private ushort[] _summerLandTile;
        private ushort[] _fallLandTile;
        private ushort[] _winterLandTile;
        private ushort[] _desolationLandTile;

        public SeasonTileRemap()
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
        }

        public void Set(Season season, ushort orig, ushort replace)
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
