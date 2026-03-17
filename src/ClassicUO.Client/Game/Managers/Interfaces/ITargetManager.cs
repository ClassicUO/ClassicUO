namespace ClassicUO.Game.Managers
{
    internal interface ITargetManager
    {
        uint LastAttack { get; set; }
        uint SelectedTarget { get; set; }
        uint NewTargetSystemSerial { get; set; }
        LastTargetInfo LastTargetInfo { get; }
        MultiTargetInfo MultiTargetInfo { get; }
        CursorTarget TargetingState { get; }
        bool IsTargeting { get; }
        TargetType TargetingType { get; }

        void Reset();
        void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType);
        void CancelTarget();
        void SetTargetingMulti(uint deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue);
        void Target(uint serial);
    }
}
