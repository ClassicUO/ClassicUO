using System;

namespace ClassicUO.Input
{
    [Flags]
    public enum PickerType
    {
        PickNothing = 0,
        PickObjects = 1,
        PickStatics = 2,
        PickGroundTiles = 4,
        PickEverything = PickObjects | PickStatics | PickGroundTiles
    }
}
