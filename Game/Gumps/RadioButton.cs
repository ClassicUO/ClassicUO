using ClassicUO.Input;
using System.Linq;

namespace ClassicUO.Game.Gumps
{
    public class RadioButton : Checkbox
    {
        public RadioButton(ushort inactive,  ushort active) : base(inactive, active)
        {

        }

        public int GroupIndex { get; set; }

        public override bool IsChecked
        {
            get => base.IsChecked;
            set
            {
                if (value)
                {
                    HandleClick();
                }

                base.IsChecked = value;
            }
        }


        public override void OnMouseButton(MouseEventArgs e)
        {
            HandleClick();
            base.OnMouseButton(e);
        }


        private void HandleClick()
        {
            Parent?.GetControls<RadioButton>()
                .Where(s => s.GroupIndex == GroupIndex)
                .ToList()
                .ForEach(s => s.IsChecked = false);
        }
    }
}
