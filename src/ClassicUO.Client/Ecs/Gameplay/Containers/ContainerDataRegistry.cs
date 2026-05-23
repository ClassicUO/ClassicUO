// Port of Game.Managers.ContainerManager into a plain ECS resource.
// Legacy ContainerManager takes a `World` reference in its ctor that it never
// actually uses for parsing; this version drops the dependency and exposes the
// same lookup surface (`Get(graphic)`) for new systems.

using System.Collections.Generic;
using System.IO;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;

namespace ClassicUO.Ecs;

internal sealed class ContainerDataRegistry
{
    private readonly Dictionary<ushort, ContainerData> _data = new();

    public int DefaultX { get; } = 40;
    public int DefaultY { get; } = 40;

    public ContainerDataRegistry()
    {
        Build(false);
    }

    public ContainerData Get(ushort graphic)
    {
        if (!_data.TryGetValue(graphic, out var v))
            _data[graphic] = v = new ContainerData(graphic, 0, 0, 44, 65, 186, 159);
        return v;
    }

    private void Build(bool forceDefault)
    {
        var path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client", "containers.txt");
        if (forceDefault || !File.Exists(path))
        {
            MakeDefault();
            return;
        }

        try
        {
            using var reader = new StreamReader(File.OpenRead(path));
            string line;
            int lineNo = 1;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    lineNo++;
                    continue;
                }

                var parts = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 7
                    && ushort.TryParse(parts[0], out var graphic)
                    && ushort.TryParse(parts[1], out var openSound)
                    && ushort.TryParse(parts[2], out var closeSound)
                    && int.TryParse(parts[3], out var x)
                    && int.TryParse(parts[4], out var y)
                    && int.TryParse(parts[5], out var w)
                    && int.TryParse(parts[6], out var h))
                {
                    ushort iconized = 0;
                    int mx = 0, my = 0;
                    if (parts.Length >= 8) ushort.TryParse(parts[7], out iconized);
                    if (parts.Length >= 9) int.TryParse(parts[8], out mx);
                    if (parts.Length >= 10) int.TryParse(parts[9], out my);
                    _data[graphic] = new ContainerData(graphic, openSound, closeSound, x, y, w, h, iconized, mx, my);
                }
                lineNo++;
            }
        }
        catch
        {
            MakeDefault();
        }

        if (_data.Count == 0)
            MakeDefault();
    }

    private void MakeDefault()
    {
        _data.Clear();
        _data[0x0007] = new ContainerData(0x0007, 0x0000, 0x0000, 30, 30, 270, 170);
        _data[0x0009] = new ContainerData(0x0009, 0x0000, 0x0000, 20, 85, 124, 196);
        _data[0x003C] = new ContainerData(0x003C, 0x0048, 0x0058, 44, 65, 186, 159, 0x0050, 105, 162);
        _data[0x003D] = new ContainerData(0x003D, 0x0048, 0x0058, 29, 34, 137, 128);
        _data[0x003E] = new ContainerData(0x003E, 0x002F, 0x002E, 33, 36, 142, 148);
        _data[0x003F] = new ContainerData(0x003F, 0x004F, 0x0058, 19, 47, 182, 123);
        _data[0x0040] = new ContainerData(0x0040, 0x002D, 0x002C, 16, 38, 152, 125);
        _data[0x0041] = new ContainerData(0x0041, 0x004F, 0x0058, 40, 30, 139, 123);
        _data[0x0042] = new ContainerData(0x0042, 0x002D, 0x002C, 18, 105, 162, 178);
        _data[0x0043] = new ContainerData(0x0043, 0x002D, 0x002C, 16, 51, 184, 124);
        _data[0x0044] = new ContainerData(0x0044, 0x002D, 0x002C, 20, 10, 170, 100);
        _data[0x0047] = new ContainerData(0x0047, 0x0000, 0x0000, 16, 10, 148, 138);
        _data[0x0048] = new ContainerData(0x0048, 0x002F, 0x002E, 16, 10, 154, 94);
        _data[0x0049] = new ContainerData(0x0049, 0x002D, 0x002C, 18, 105, 162, 178);
        _data[0x004A] = new ContainerData(0x004A, 0x002D, 0x002C, 18, 105, 162, 178);
        _data[0x004B] = new ContainerData(0x004B, 0x002D, 0x002C, 16, 51, 184, 124);
        _data[0x004C] = new ContainerData(0x004C, 0x002D, 0x002C, 46, 74, 196, 184);
        _data[0x004D] = new ContainerData(0x004D, 0x002F, 0x002E, 76, 12, 140, 68);
        _data[0x004E] = new ContainerData(0x004E, 0x002D, 0x002C, 24, 18, 100, 152);
        _data[0x004F] = new ContainerData(0x004F, 0x002D, 0x002C, 24, 18, 100, 152);
        _data[0x0051] = new ContainerData(0x0051, 0x002F, 0x002E, 16, 10, 154, 94);
        _data[0x0052] = new ContainerData(0x0052, 0x0000, 0x0000, 0, 0, 110, 62);
        _data[0x0102] = new ContainerData(0x0102, 0x004F, 0x0058, 35, 10, 190, 95);
        _data[0x0103] = new ContainerData(0x0103, 0x0048, 0x0058, 41, 21, 173, 104);
        _data[0x0104] = new ContainerData(0x0104, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x0105] = new ContainerData(0x0105, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x0106] = new ContainerData(0x0106, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x0107] = new ContainerData(0x0107, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x0108] = new ContainerData(0x0108, 0x004F, 0x0058, 10, 10, 160, 105);
        _data[0x0109] = new ContainerData(0x0109, 0x002D, 0x002C, 10, 10, 160, 105);
        _data[0x010A] = new ContainerData(0x010A, 0x002D, 0x002C, 10, 10, 160, 105);
        _data[0x010B] = new ContainerData(0x010B, 0x002D, 0x002C, 10, 10, 160, 105);
        _data[0x010C] = new ContainerData(0x010C, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x010D] = new ContainerData(0x010D, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x010E] = new ContainerData(0x010E, 0x002F, 0x002E, 10, 10, 160, 105);
        _data[0x0116] = new ContainerData(0x0116, 0x0000, 0x0000, 40, 25, 140, 110);
        _data[0x011A] = new ContainerData(0x011A, 0x0000, 0x0000, 10, 65, 125, 160);
        _data[0x011B] = new ContainerData(0x011B, 0x0000, 0x0000, 45, 10, 175, 95);
        _data[0x011C] = new ContainerData(0x011C, 0x0000, 0x0000, 37, 10, 175, 105);
        _data[0x011D] = new ContainerData(0x011D, 0x0000, 0x0000, 43, 10, 165, 110);
        _data[0x011E] = new ContainerData(0x011E, 0x0000, 0x0000, 30, 22, 263, 106);
        _data[0x011F] = new ContainerData(0x011F, 0x0000, 0x0000, 45, 10, 175, 95);
        _data[0x0120] = new ContainerData(0x0120, 0x0000, 0x0000, 56, 30, 160, 107);
        _data[0x0121] = new ContainerData(0x0121, 0x0000, 0x0000, 77, 32, 162, 107);
        _data[0x0123] = new ContainerData(0x0123, 0x0000, 0x0000, 36, 19, 111, 157);
        _data[0x0484] = new ContainerData(0x0484, 0x0000, 0x0000, 0, 45, 175, 125);
        _data[0x058E] = new ContainerData(0x058E, 0x0000, 0x0000, 50, 150, 348, 250);
        _data[0x06D3] = new ContainerData(0x06D3, 0x0000, 0x0000, 10, 65, 125, 160);
        _data[0x06D4] = new ContainerData(0x06D4, 0x0000, 0x0000, 10, 65, 125, 160);
        _data[0x06D5] = new ContainerData(0x06D5, 0x0000, 0x0000, 10, 65, 125, 160);
        _data[0x06D6] = new ContainerData(0x06D6, 0x0000, 0x0000, 10, 65, 125, 160);
        _data[0x06E5] = new ContainerData(0x06E5, 0x0000, 0x0000, 66, 74, 306, 520);
        _data[0x06E6] = new ContainerData(0x06E6, 0x0000, 0x0000, 66, 74, 306, 520);
        _data[0x06E7] = new ContainerData(0x06E7, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x06E8] = new ContainerData(0x06E8, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x06E9] = new ContainerData(0x06E9, 0x0000, 0x0000, 60, 80, 318, 324);
        _data[0x06EA] = new ContainerData(0x06EA, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x091A] = new ContainerData(0x091A, 0x0000, 0x0000, 0, 0, 282, 230);
        _data[0x092E] = new ContainerData(0x092E, 0x0000, 0x0000, 0, 0, 282, 210);
        _data[0x266A] = new ContainerData(0x266A, 0x0000, 0x0000, 16, 51, 184, 124);
        _data[0x266B] = new ContainerData(0x266B, 0x0000, 0x0000, 16, 51, 184, 124);
        _data[0x2A63] = new ContainerData(0x2A63, 0x0187, 0x01C9, 60, 33, 460, 348);
        _data[0x4D0C] = new ContainerData(0x4D0C, 0x0000, 0x0000, 25, 65, 220, 155);
        _data[0x775E] = new ContainerData(0x775E, 0x0048, 0x0058, 44, 65, 186, 159, 0x775F, 105, 178);
        _data[0x7760] = new ContainerData(0x7760, 0x0048, 0x0058, 44, 65, 186, 159, 0x7761, 105, 178);
        _data[0x7762] = new ContainerData(0x7762, 0x0048, 0x0058, 44, 65, 186, 159, 0x7763, 105, 178);
        _data[0x777A] = new ContainerData(0x777A, 0x0000, 0x0000, 32, 40, 184, 116);
        _data[0x9CD9] = new ContainerData(0x9CD9, 0x0000, 0x0000, 10, 10, 160, 105);
        _data[0x9CDB] = new ContainerData(0x9CDB, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x9CDD] = new ContainerData(0x9CDD, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x9CDF] = new ContainerData(0x9CDF, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x9CE3] = new ContainerData(0x9CE3, 0x0000, 0x0000, 50, 60, 548, 308);
        _data[0x9CE4] = new ContainerData(0x9CE4, 0x0000, 0x0000, 44, 65, 186, 159);
        _data[0x9CE5] = new ContainerData(0x9CE5, 0x0000, 0x0000, 44, 65, 186, 159);
        _data[0x9CE7] = new ContainerData(0x9CE7, 0x0000, 0x0000, 44, 65, 186, 159);
    }
}
