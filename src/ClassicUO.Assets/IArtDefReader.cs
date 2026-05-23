// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Assets
{
    /// <summary>
    /// Reads <c>art.def</c> (when present) and fills in missing land/static
    /// art and tile-data entries by copying from a referenced index in the
    /// same file. One responsibility: applying the art-def aliasing table.
    /// </summary>
    internal interface IArtDefReader
    {
        /// <summary>
        /// Reads <c>art.def</c> via the supplied path resolver and patches
        /// the supplied art + tiledata tables in place. No-op if the file
        /// is missing.
        /// </summary>
        void Read(IUOFilePathResolver paths, ArtLoader arts, TileDataLoader tileData);
    }
}
