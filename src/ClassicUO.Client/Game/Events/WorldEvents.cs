// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Events
{
    internal readonly record struct WeatherChangedArgs(byte WeatherType, byte Count, byte Temperature);

    internal readonly record struct SeasonChangedArgs(byte Season, byte MusicCue);

    internal readonly record struct LightLevelChangedArgs(uint Serial, byte Level, bool IsPersonal);

    internal readonly record struct ObjectDeletedArgs(uint Serial);
}
