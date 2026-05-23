// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using System.Text;
using ClassicUO.Resources;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Skills
{
    /// <summary>
    /// Concrete <see cref="ISkillsGroupMulLoader"/>. Parses the legacy UO
    /// <c>skillgrp.mul</c> file (8-bit and unicode header variants) and
    /// appends the decoded groups to the supplied store. The first group is
    /// always renamed to <see cref="ResGeneral.Miscellaneous"/> to match the
    /// original client's catch-all group used as the deletion target.
    /// </summary>
    internal sealed class SkillsGroupMulLoader : ISkillsGroupMulLoader
    {
        public bool LoadMULFile(string path, ISkillsGroupStore store)
        {
            FileInfo info = new FileInfo(path);

            if (!info.Exists)
            {
                return false;
            }

            try
            {
                byte skillidx = 0;
                bool unicode = false;

                using (BinaryReader bin = new BinaryReader(File.OpenRead(info.FullName)))
                {
                    int start = 4;
                    int strlen = 17;
                    int count = bin.ReadInt32();

                    if (count == -1)
                    {
                        unicode = true;
                        count = bin.ReadInt32();
                        start *= 2;
                        strlen *= 2;
                    }


                    StringBuilder sb = new StringBuilder(17);

                    SkillsGroup g = new SkillsGroup();
                    g.Name = ResGeneral.Miscellaneous;

                    SkillsGroup[] groups = new SkillsGroup[count];
                    groups[0] = g;

                    for (int i = 0; i < count - 1; ++i)
                    {
                        short strbuild;
                        bin.BaseStream.Seek(start + i * strlen, SeekOrigin.Begin);

                        if (unicode)
                        {
                            while ((strbuild = bin.ReadInt16()) != 0)
                            {
                                sb.Append((char) strbuild);
                            }
                        }
                        else
                        {
                            while ((strbuild = bin.ReadByte()) != 0)
                            {
                                sb.Append((char) strbuild);
                            }
                        }

                        groups[i + 1] = new SkillsGroup
                        {
                            Name = sb.ToString()
                        };

                        sb.Clear();
                    }

                    bin.BaseStream.Seek(start + (count - 1) * strlen, SeekOrigin.Begin);

                    while (bin.BaseStream.Length != bin.BaseStream.Position)
                    {
                        int grp = bin.ReadInt32();

                        if (grp < groups.Length && skillidx < Client.Game.UO.FileManager.Skills.SkillsCount)
                        {
                            groups[grp].Add(skillidx++);
                        }
                    }

                    for (int i = 0; i < groups.Length; i++)
                    {
                        store.Add(groups[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while reading skillgrp.mul, using CUO defaults! exception given is: {e}");

                return false;
            }

            return store.Groups.Count != 0;
        }
    }
}
