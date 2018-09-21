namespace ClassicUO.Game.Gumps
{
    public enum UILayer
    {
        Default,
        Over,
        Under
    }

    public class GumpControlInfo
    {
        public GumpControlInfo(GumpControl control) => Control = control;

        public UILayer Layer { get; set; }
        public bool IsModal { get; set; }
        public bool ModalClickOutsideAreaClosesThisControl { get; set; }
        public GumpControl Control { get; }
    }
}