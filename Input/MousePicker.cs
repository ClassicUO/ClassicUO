using Microsoft.Xna.Framework;

namespace ClassicUO.Input
{
    public class MousePicker<T> where T : class
    {
        private MouseOverItem<T> _overObject;
        private MouseOverItem<T> _overTile;

        public MousePicker() => PickOnly = PickerType.PickNothing;

        public PickerType PickOnly { get; set; }
        public Point Position { get; set; }

        public T MouseOverObject => _overObject.Equals(default) ? null : _overObject.Object;

        public Point MouseOverObjectPoint => _overObject.Equals(default) ? Point.Zero : _overObject.InTexturePoint;


        public void UpdateOverObjects(MouseOverList<T> list, Point position)
        {
            _overObject = list.GetItem(position);
        }
    }
}