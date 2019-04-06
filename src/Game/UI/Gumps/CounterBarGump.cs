using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class CounterBarGump : Gump
    {
        private AlphaBlendControl _background;

        private int _rows, _columns, _rectSize;
        private bool _isVertical;

        public CounterBarGump() : base(0, 0)
        {
        }

        public CounterBarGump(int x, int y, int rectSize = 30, int rows = 1, int columns = 1, bool vertical = false) : base(0, 0)
        {
            X = x;
            Y = y;

            if (rectSize < 30)
                rectSize = 30;
            else if (rectSize > 80)
                rectSize = 80;

            if (rows < 1)
                rows = 1;

            if (columns < 1)
                columns = 1;

            //if (vertical)
            //{
            //    int temp = rows;
            //    rows = columns;
            //    columns = temp;
            //}
     
            _rows = rows;
            _columns = columns;
            _rectSize = rectSize;
            _isVertical = vertical;

            BuildGump();
        }

        private void BuildGump()
        {
            CanMove = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;
            CanCloseWithRightClick = false;
            WantUpdateSize = false;
            CanBeSaved = true;

            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            Add(_background = new AlphaBlendControl(0.3f) { Width = Width, Height = Height });

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Add(new CounterItem(col * _rectSize + 2, row * _rectSize + 2, _rectSize - 4, _rectSize - 4));
                }
            }
        }

        public void SetDirection(bool isvertical)
        {
            if (_isVertical == isvertical)
                return;

            _isVertical = isvertical;

            
            int temp = _rows;
            _rows = _columns;
            _columns = temp;
            


            Width = _rectSize * _columns + 1;
            Height = _rectSize * _rows + 1;

            _background.Width = Width;
            _background.Height = Height;

            var items = GetControls<CounterItem>();

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    int index = _isVertical ? col * _rows + row : row * _columns + col;
                    CounterItem c = items[index];

                    c.X = col * _rectSize + 2;
                    c.Y = row * _rectSize + 2;
                }
            }
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write(_isVertical);
            writer.Write(_rows);
            writer.Write(_columns);
            writer.Write(_rectSize);

            CounterItem[] controls = GetControls<CounterItem>();

            writer.Write(controls.Length);

            for (int i = 0; i < controls.Length; i++)
            {
                var c = controls[i];

                writer.Write(c.Graphic);
            }
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            _isVertical = reader.ReadBoolean();
            _rows = reader.ReadInt32();
            _columns = reader.ReadInt32();
            _rectSize = reader.ReadInt32();

            int count = reader.ReadInt32();

            BuildGump();

            CounterItem[] items = GetControls<CounterItem>();

            for (int i = 0; i < count; i++)
            {
                items[i].SetGraphic(reader.ReadUInt16());
            }
        }



        class CounterItem : Control
        {
            private TextureControl _controlPic;
            private Graphic _graphic;
            private uint _time;
            private int _amount;


            public CounterItem(int x, int y, int w, int h)
            {
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;             

                X = x;
                Y = y;
                Width = w;
                Height = h;
            }

            public ushort Graphic => _graphic;
            

            public void SetGraphic(ushort graphic)
            {
                if (graphic == 0)
                    return;

                _graphic = graphic;

                _controlPic?.Dispose();
                _controlPic = new TextureControl()
                {
                    ScaleTexture = true,
                    Texture = FileManager.Art.GetTexture(_graphic),
                    //Hue = gs.HeldItem.Hue,
                    //IsPartial = gs.HeldItem.IsPartialHue,
                    Width = Width,
                    Height = Height,
                    AcceptMouseInput = false
                };
                Add(_controlPic);
            }

            protected override void OnMouseUp(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {
                    GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                    if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                        return;

                    Item item = World.Items.Get(gs.HeldItem.Container);

                    if (item == null)
                        return;

                    SetGraphic(gs.HeldItem.Graphic);

                    gs.DropHeldItemToContainer(item, gs.HeldItem.Position.X, gs.HeldItem.Position.Y);                                 
                }
                else if (button == MouseButton.Right && Input.Keyboard.Alt && _graphic != 0)
                {
                    _controlPic?.Dispose();
                    _amount = 0;
                    _graphic = 0;
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
            {
                if (button == MouseButton.Left)
                {        
                    Item backpack = World.Player.Equipment[(int) Layer.Backpack];
                    Item item = backpack.FindItem(_graphic);
                    if (item != null)
                        GameActions.DoubleClick(item);
                }

                return true;
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);


                if (_time < Engine.Ticks)
                {
                    _time = (uint) Engine.Ticks + 100;

                    _amount = 0;
                    GetAmount(World.Player.Equipment[(int)Layer.Backpack], _graphic, ref _amount);
                }
            }

            private static void GetAmount(Item parent, Graphic graphic, ref int amount)
            {
                foreach (Item item in parent.Items)
                {
                    GetAmount(item, graphic, ref amount);

                    if (item.Graphic == graphic)
                        amount += item.Amount;
                }
            }

            public override bool Draw(Batcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                string text = _amount.ToString();

                if (_amount >= 1000)
                {
                    text = $"{text[0]}K+";
                }

                batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x, y, Width, Height, Vector3.Zero);

                if (_graphic != 0)
                {
                    batcher.DrawString(Fonts.Bold, text, x + 2, y + Height - 15, ShaderHuesTraslator.GetHueVector(59, ShadersEffectType.Hued));
                }


                return true;
            }
        }
    }
}
