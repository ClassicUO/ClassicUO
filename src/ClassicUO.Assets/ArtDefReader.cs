// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using System.IO;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Concrete <see cref="IArtDefReader"/>. Walks every line of
    /// <c>art.def</c> and applies its "use this other index instead"
    /// substitutions to the art-file entries and land/static tiledata tables.
    /// </summary>
    internal sealed class ArtDefReader : IArtDefReader
    {
        public void Read(IUOFilePathResolver paths, ArtLoader arts, TileDataLoader tileData)
        {
            string pathdef = paths.GetUOFilePath("art.def");

            if (!File.Exists(pathdef))
            {
                return;
            }

            using (var reader = new DefReader(pathdef, 1))
            {
                while (reader.Next())
                {
                    int index = reader.ReadInt();

                    if (index < 0 || index >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT + tileData.StaticData.Length)
                    {
                        continue;
                    }

                    int[] group = reader.ReadGroup();

                    if (group == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < group.Length; i++)
                    {
                        int checkIndex = group[i];

                        if (checkIndex < 0 || checkIndex >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT + tileData.StaticData.Length)
                        {
                            continue;
                        }

                        if (index < arts.File.Entries.Length && checkIndex < arts.File.Entries.Length)
                        {
                            ref UOFileIndex currentEntry = ref arts.File.GetValidRefEntry(index);
                            ref UOFileIndex checkEntry = ref arts.File.GetValidRefEntry(checkIndex);

                            if (currentEntry.Equals(UOFileIndex.Invalid) && !checkEntry.Equals(UOFileIndex.Invalid))
                            {
                                arts.File.Entries[index] = arts.File.Entries[checkIndex];
                            }
                        }

                        if (index < ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            checkIndex < ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            checkIndex < tileData.LandData.Length &&
                            index < tileData.LandData.Length &&
                            !tileData.LandData[checkIndex].Equals(default) &&
                            tileData.LandData[index].Equals(default))
                        {
                            tileData.LandData[index] = tileData.LandData[checkIndex];

                            break;
                        }

                        if (index >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT && checkIndex >= ArtLoader.MAX_LAND_DATA_INDEX_COUNT &&
                            index < tileData.StaticData.Length && checkIndex < tileData.StaticData.Length &&
                            tileData.StaticData[index].Equals(default) && !tileData.StaticData[checkIndex].Equals(default))
                        {
                            tileData.StaticData[index] = tileData.StaticData[checkIndex];

                            break;
                        }
                    }
                }
            }
        }
    }
}
