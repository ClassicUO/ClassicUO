// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Assets
{
    /// <summary>
    /// Orchestrates the bulk-load phase of <see cref="UOFileManager"/>:
    /// drives every individual asset loader in the right order, then applies
    /// any verdata patches against the freshly loaded tables. One
    /// responsibility: sequencing the cold-start file pipeline.
    /// </summary>
    internal interface IUOAssetLoaderRunner
    {
        /// <summary>
        /// Runs the full asset-load + verdata-patching pipeline for the
        /// supplied <paramref name="manager"/>. Honors
        /// <paramref name="useVerdata"/> (auto-forced for ancient clients
        /// when verdata patches exist), then routes patches into Maps,
        /// Arts, Gumps, Multis, Skills, TileData and Hues.
        /// </summary>
        void Run(UOFileManager manager, bool useVerdata, string lang, string mapsLayouts);
    }
}
