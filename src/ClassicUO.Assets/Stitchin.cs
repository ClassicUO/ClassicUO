using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.Assets
{
    public struct StitchinEntry
    {
        public uint Covers;
        public uint CoveredBy;
        public Dictionary<ushort, ushort> Replacements;
        public List<ushort> Removals;
    }

    public sealed class Stitchin
    {
        private static readonly char[] _split = new char[] { ' ', '\t' };

        private readonly Dictionary<ushort, StitchinEntry> _entries = new Dictionary<ushort, StitchinEntry>();

        public bool IsLoaded => _entries.Count > 0;

        public bool TryGetEntry(ushort animID, out StitchinEntry entry)
        {
            return _entries.TryGetValue(animID, out entry);
        }

        public void Load(UOFileManager fileManager)
        {
            var filePath = fileManager.GetUOFilePath("stitchin.def");

            if (!File.Exists(filePath))
            {
                return;
            }

            using (var reader = new StreamReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var lines = new List<string>();
                var started = false;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        continue;
                    }

                    if (!started && line.StartsWith("# ") && !IsEndDef(line))
                    {
                        started = true;
                    }

                    if (started)
                    {
                        lines.Add(line);
                    }

                    if (started && IsEndDef(line))
                    {
                        ParseBlock(lines);
                        lines.Clear();
                        started = false;
                    }
                }
            }

            Log.Trace($"Stitchin: loaded {_entries.Count} entries");
        }

        private static bool IsEndDef(string line)
        {
            return line.StartsWith("# enddef") || line.StartsWith("#enddef");
        }

        private void ParseBlock(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                return;
            }

            // First line should be "# <animID>"
            var headerArgs = lines[0].Split(_split, StringSplitOptions.RemoveEmptyEntries);

            if (headerArgs.Length < 2 || headerArgs[0] != "#")
            {
                return;
            }

            if (!ushort.TryParse(headerArgs[1], out ushort animID))
            {
                return;
            }

            var entry = new StitchinEntry();

            for (int i = 1; i < lines.Count; i++)
            {
                var args = lines[i].Split(_split, StringSplitOptions.RemoveEmptyEntries);

                if (args.Length == 0)
                {
                    continue;
                }

                switch (args[0])
                {
                    case "coveredBy":
                        entry.CoveredBy |= ParseLayerMask(args);
                        break;

                    case "covers":
                        entry.Covers |= ParseLayerMask(args);
                        break;

                    case "replace" when args.Length >= 4:
                        if (ushort.TryParse(args[1], out ushort from) && ushort.TryParse(args[3], out ushort to))
                        {
                            entry.Replacements ??= new Dictionary<ushort, ushort>();
                            entry.Replacements[from] = to;
                        }
                        break;

                    case "remove":
                        for (int j = 1; j < args.Length; j++)
                        {
                            // Stop at inline comments
                            if (args[j].StartsWith("//"))
                            {
                                break;
                            }

                            if (ushort.TryParse(args[j], out ushort removeId))
                            {
                                entry.Removals ??= new List<ushort>();
                                entry.Removals.Add(removeId);
                            }
                        }
                        break;
                }
            }

            _entries[animID] = entry;
        }

        private static uint ParseLayerMask(string[] args)
        {
            uint result = 0;

            for (int i = 1; i < args.Length; i++)
            {
                // Stop at inline comments
                if (args[i].StartsWith("//"))
                {
                    break;
                }

                result |= GetLayerBit(args[i]);
            }

            return result;
        }

        // Body part names map to Layer enum byte values:
        // Shoes=0x03, Pants=0x04, Shirt=0x05, Helmet=0x06, Gloves=0x07,
        // Necklace=0x0A, Waist=0x0C, Torso=0x0D, Bracelet=0x0E, Face=0x0F,
        // Tunic=0x11, Earrings=0x12, Arms=0x13, Robe=0x16, Skirt=0x17, Legs=0x18
        private static uint GetLayerBit(string name)
        {
            switch (name)
            {
                case "HEAD": return 1u << 0x06;
                case "FACE": return 1u << 0x0F;
                case "EARS": return 1u << 0x12;
                case "NECK": return 1u << 0x0A;
                case "TORSO": return 1u << 0x0D;
                case "UPPER_ARMS_TOP": return 1u << 0x05;
                case "UPPER_ARMS_BOTTOM": return 1u << 0x11;
                case "LOWER_ARMS_TOP": return 1u << 0x13;
                case "LOWER_ARMS_BOTTOM": return 1u << 0x0E;
                case "HANDS": return 1u << 0x07;
                case "PELVIS": return 1u << 0x0C;
                case "UPPER_LEGS_TOP": return 1u << 0x04;
                case "UPPER_LEGS_BOTTOM": return 1u << 0x17;
                case "LOWER_LEGS_TOP": return 1u << 0x18;
                case "LOWER_LEGS_BOTTOM": return 1u << 0x16;
                case "FEET": return 1u << 0x03;
                default: return 0;
            }
        }
    }
}
