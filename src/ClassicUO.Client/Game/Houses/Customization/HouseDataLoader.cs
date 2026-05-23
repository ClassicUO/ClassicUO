// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using System.IO;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Houses.Customization
{
    /// <summary>
    /// Concrete <see cref="IHouseDataLoader"/>: reads the eight customization
    /// definition text files distributed with the client and dispatches each
    /// parsed entry into the matching category list. Locked-feature filtering
    /// is honoured via the supplied <see cref="World.ClientLockedFeatures"/>.
    /// Also serves as the lookup oracle over those tables for callers that
    /// need to map a graphic back to a category / index.
    /// </summary>
    internal sealed class HouseDataLoader : IHouseDataLoader
    {
        private readonly World _world;

        public HouseDataLoader(World world)
        {
            _world = world;
        }

        public void Load(
            List<CustomHouseWallCategory> walls,
            List<CustomHouseFloor> floors,
            List<CustomHouseDoor> doors,
            List<CustomHouseMiscCategory> miscs,
            List<CustomHouseStair> stairs,
            List<CustomHouseTeleport> teleports,
            List<CustomHouseRoofCategory> roofs,
            List<CustomHousePlaceInfo> objectsInfo)
        {
            var fileManager = Client.Game.UO.FileManager;

            ParseFileWithCategory<CustomHouseWall, CustomHouseWallCategory>(walls, fileManager.GetUOFilePath("walls.txt"));
            ParseFile(floors, fileManager.GetUOFilePath("floors.txt"));
            ParseFile(doors, fileManager.GetUOFilePath("doors.txt"));
            ParseFileWithCategory<CustomHouseMisc, CustomHouseMiscCategory>(miscs, fileManager.GetUOFilePath("misc.txt"));
            ParseFile(stairs, fileManager.GetUOFilePath("stairs.txt"));
            ParseFile(teleports, fileManager.GetUOFilePath("teleprts.txt"));
            ParseFileWithCategory<CustomHouseRoof, CustomHouseRoofCategory>(roofs, fileManager.GetUOFilePath("roof.txt"));
            ParseFile(objectsInfo, fileManager.GetUOFilePath("suppinfo.txt"));
        }

        public (int, int) SeekGraphic<T>(List<T> list, ushort graphic) where T : CustomHouseObject
        {
            for (int i = 0; i < list.Count; i++)
            {
                int contains = list[i].Contains(graphic);

                if (contains != -1)
                {
                    return (i, contains);
                }
            }

            return (-1, -1);
        }

        public (int, int) SeekGraphicWithCategory<T, U>(List<U> list, ushort graphic)
            where T : CustomHouseObject
            where U : CustomHouseObjectCategory<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                U c = list[i];

                for (int j = 0; j < c.Items.Count; j++)
                {
                    int contains = c.Items[j].Contains(graphic);

                    if (contains != -1)
                    {
                        return (i, j);
                    }
                }
            }

            return (-1, -1);
        }

        private void ParseFile<T>(List<T> list, string path) where T : CustomHouseObject, new()
        {
            FileInfo file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask == 0 || ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) != 0)
                        {
                            list.Add(item);
                        }
                    }
                }
            }
        }

        private void ParseFileWithCategory<T, U>(List<U> list, string path)
            where T : CustomHouseObject, new()
            where U : CustomHouseObjectCategory<T>, new()
        {
            FileInfo file = new FileInfo(path);

            if (!file.Exists)
            {
                return;
            }

            using (StreamReader reader = File.OpenText(file.FullName))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    T item = new T();

                    if (item.Parse(line))
                    {
                        if (item.FeatureMask != 0 && ((int)_world.ClientLockedFeatures.Flags & item.FeatureMask) == 0)
                        {
                            continue;
                        }

                        bool found = false;

                        foreach (U c in list)
                        {
                            if (c.Index == item.Category)
                            {
                                c.Items.Add(item);
                                found = true;

                                break;
                            }
                        }

                        if (!found)
                        {
                            U c = new U
                            {
                                Index = item.Category
                            };

                            c.Items.Add(item);
                            list.Add(c);
                        }
                    }
                }
            }
        }
    }
}
