using ClassicUO.IO;

namespace ClassicUO.Ecs;

// Server asks the client to enter targeting mode. The same 0x6C id is reused
// for the client's response (see Send_TargetObject / Send_TargetXYZ /
// Send_TargetCancel) — only the leading mode/cursor/type fields matter for the
// request; the remaining bytes of the fixed 19-byte packet are zero here.
internal struct OnTargetCursorPacket_0x6C : IPacket
{
    public byte Id => 0x6C;

    // 0 = pick an object, 1 = pick a ground/static position, 2 = multi placement.
    public byte CursorTarget { get; private set; }
    // Server session id echoed back verbatim in the response.
    public uint CursorId { get; private set; }
    // 0 = neutral, 1 = harmful, 2 = beneficial, 3 = cancel.
    public byte CursorType { get; private set; }

    public void Fill(StackDataReader reader)
    {
        CursorTarget = reader.ReadUInt8();
        CursorId = reader.ReadUInt32BE();
        CursorType = reader.ReadUInt8();
    }
}
