// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>0x03 ACK Talk sent.</summary>
    internal readonly record struct AckTalkSentArgs();

    /// <summary>0xEF Seed sent — seeded handshake with client version.</summary>
    internal readonly record struct SeedSentArgs(uint Seed, byte Major, byte Minor, byte Build, byte Extra);

    /// <summary>Pre-EF seed sent — just the 4-byte legacy seed.</summary>
    internal readonly record struct SeedOldSentArgs(uint Seed);

    /// <summary>0x80 First login (account/password) sent to login server.</summary>
    internal readonly record struct FirstLoginSentArgs(string Account, string Password);

    /// <summary>0x91 Second login (post-server-pick) sent to game server.</summary>
    internal readonly record struct SecondLoginSentArgs(string Account, string Password, uint Seed);

    /// <summary>0xA0 Select server from server list.</summary>
    internal readonly record struct SelectServerSentArgs(byte Index);

    /// <summary>0x5D Select character (post-login) sent.</summary>
    internal readonly record struct SelectCharacterSentArgs(uint Index, string Name, uint LocalIp);

    /// <summary>0x00 / 0xF8 Create character request.</summary>
    internal readonly record struct CreateCharacterSentArgs(
        PlayerMobile Character,
        int CityIndex,
        uint ClientIp,
        int ServerIndex,
        uint Slot,
        byte Profession
    );

    /// <summary>0x83 Delete character request.</summary>
    internal readonly record struct DeleteCharacterSentArgs(byte Index, uint LocalIp);

    /// <summary>0xD1 Logout notification sent.</summary>
    internal readonly record struct LogoutNotificationSentArgs();

    /// <summary>0xBD Client version string sent to server.</summary>
    internal readonly record struct ClientVersionSentArgs(string Version);

    /// <summary>0xBF/0x0B Language code sent to server.</summary>
    internal readonly record struct LanguageSentArgs(string Lang);

    /// <summary>0xBF/0x0F Client-type/flags identification sent to server.</summary>
    internal readonly record struct ClientTypeSentArgs();
}
