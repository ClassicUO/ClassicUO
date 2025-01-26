using System;
using System.Text.RegularExpressions;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Data;

// Attribution
// Original Class: "Sextant" by RunUO & ServUO contributors
// Source: https://github.com/ServUO/ServUO/blob/master/Scripts/Items/Tools/Sextant.cs
internal static partial class Sextant
{
    public static readonly Point InvalidPoint = new(-1, -1);
    
    [GeneratedRegex(@"(?<LatDegrees>\d{1,3})[°o\s]*(?<LatMinutes>\d{2})'(?<LatDirection>[NS])[\s,]*(?<LongDegrees>\d{1,3})[°o\s]*(?<LongMinutes>\d{2})'(?<LongDirection>[EW])")]
    private static partial Regex SextantCoordsRegex();

    /// <summary>
    /// Converts lat/long sextant coords into X,Y point coords
    /// </summary>
    /// <param name="map"></param>
    /// <param name="coords"></param>
    /// <returns>Point representing the map X/Y on success, or an invalid point on failure</returns>
    public static bool Parse(Map.Map map, string coords, out Point point)
    {
        Match match = SextantCoordsRegex().Match(coords.Trim());
        
        point = InvalidPoint;

        try
        {
            if (!match.Success)
                return false;

            // 100o25'S
            int latDegrees = int.Parse(match.Groups["LatDegrees"].Value);
            int latMinutes = int.Parse(match.Groups["LatMinutes"].Value);
            string latDirection = match.Groups["LatDirection"].Value;

            // 40o04'E
            int longDegrees = int.Parse(match.Groups["LongDegrees"].Value);
            int longMinutes = int.Parse(match.Groups["LongMinutes"].Value);
            string longDirection = match.Groups["LongDirection"].Value;

            point = ReverseLookup(map, longDegrees, latDegrees, longMinutes, latMinutes, longDirection == "E", latDirection == "S");

            return true;
        }
        catch(Exception e)
        {
            Log.Trace($"Failed to parse sextant coords \"{coords}\": {e.Message}");
        }
        
        return false;
    }

    private static bool ComputeMapDetails(Map.Map map, int x, int y, out int xCenter, out int yCenter, out int xWidth, out int yHeight)
    {
        xWidth = 5120;
        yHeight = 4096;

        var mapWidth = Client.Game.UO.FileManager.Maps.MapsDefaultSize[map.Index, 0];
        var mapHeight = Client.Game.UO.FileManager.Maps.MapsDefaultSize[map.Index, 1];

        var isTrammel = map.Index == 0 && mapWidth == 7168 && mapHeight == 4096;
        var isFelucca = map.Index == 1 && mapWidth == 7168 && mapHeight == 4096;
        
        if (isTrammel || isFelucca)
        {
            switch (x)
            {
                case >= 0 when y >= 0 && x < 5120 && y < 4096:
                    xCenter = 1323;
                    yCenter = 1624;

                    break;

                case >= 5120 when y >= 2304 && x < 6144 && y < 4096:
                    xCenter = 5936;
                    yCenter = 3112;

                    break;

                default:
                    xCenter = 0;
                    yCenter = 0;

                    return false;
            }
        }
        else switch (x)
        {
            case >= 0 when y >= 0 && x < mapWidth && y < mapHeight:
                xCenter = 1323;
                yCenter = 1624;

                break;

            default:
                xCenter = 0;
                yCenter = 0;

                return false;
        }

        return true;
    }

    public static Point ReverseLookup(Map.Map map, int xLong, int yLat, int xMins, int yMins, bool xEast, bool ySouth)
    {
        if (map == null)
            return InvalidPoint;

        if (!ComputeMapDetails(map, 0, 0, out int xCenter, out int yCenter, out int xWidth, out int yHeight))
            return InvalidPoint;

        double absLong = xLong + ((double)xMins / 60);
        double absLat = yLat + ((double)yMins / 60);

        if (!xEast)
            absLong = 360.0 - absLong;

        if (!ySouth)
            absLat = 360.0 - absLat;

        int x = xCenter + (int)((absLong * xWidth) / 360);
        int y = yCenter + (int)((absLat * yHeight) / 360);

        if (x < 0)
            x += xWidth;
        else if (x >= xWidth)
            x -= xWidth;

        if (y < 0)
            y += yHeight;
        else if (y >= yHeight)
            y -= yHeight;

        return new Point(x, y);
    }

    public static bool Format(Point p, Map.Map map, ref int xLong, ref int yLat, ref int xMins, ref int yMins, ref bool xEast, ref bool ySouth)
    {
        if (map == null)
            return false;

        if (!ComputeMapDetails(map, p.X, p.Y, out int xCenter, out int yCenter, out int xWidth, out int yHeight))
            return false;

        double absLong = (double)((p.X - xCenter) * 360) / xWidth;
        double absLat = (double)((p.Y - yCenter) * 360) / yHeight;

        if (absLong > 180.0)
            absLong = -180.0 + (absLong % 180.0);

        if (absLat > 180.0)
            absLat = -180.0 + (absLat % 180.0);

        bool east = (absLong >= 0), south = (absLat >= 0);

        if (absLong < 0.0)
            absLong = -absLong;

        if (absLat < 0.0)
            absLat = -absLat;

        xLong = (int)absLong;
        yLat = (int)absLat;

        xMins = (int)((absLong % 1.0) * 60);
        yMins = (int)((absLat % 1.0) * 60);

        xEast = east;
        ySouth = south;

        return true;
    }

    public static bool FormatString(Point p, Map.Map map, out string text)
    {
        int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
        bool xEast = false, ySouth = false;

        if (!Format(p, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
        {
            text = string.Empty;
            return false;
        }

        text = $"{yLat}o {yMins}'{(ySouth ? "S" : "N")}, {xLong}o {xMins}'{(xEast ? "E" : "W")}";
        return true;
    }
}