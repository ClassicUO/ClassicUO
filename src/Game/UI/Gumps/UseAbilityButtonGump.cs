using System.IO;

using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseAbilityButtonGump : AnchorableGump
    {
        private GumpPic _button;
        private AbilityDefinition _definition;
        private bool _isPrimary;

        public UseAbilityButtonGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanBeSaved = true;
            AnchorGroupName = "spell";
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
        }

        public UseAbilityButtonGump(AbilityDefinition def, bool primary) : this()
        {
            _isPrimary = primary;
            Engine.UI.GetControl<UseAbilityButtonGump>((uint) def.Index)?.Dispose();
            _definition = def;
            BuildGump();
        }

        private void BuildGump()
        {
            LocalSerial = (uint) _definition.Index;

            _button = new GumpPic(0, 0, _definition.Icon, 0)
            {
                AcceptMouseInput = true
            };
            _button.MouseDoubleClick += ButtonOnMouseDoubleClick;
            Add(_button);

            WantUpdateSize = true;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            return true;
        }

        private void ButtonOnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (_isPrimary)
                    GameActions.UsePrimaryAbility();
                else
                    GameActions.UseSecondaryAbility();
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (IsDisposed)
                return;

            int index = ((byte) World.Player.Abilities[_isPrimary ? 0 : 1] & 0x7F) - 1;

            AbilityDefinition def = AbilityData.Abilities[index];

            if (_definition.Index != def.Index)
            {
                _definition = def;
                _button.Graphic = def.Icon;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            byte index = (byte) World.Player.Abilities[_isPrimary ? 0 : 1];

            if ((index & 0x80) != 0)
                _button.Hue = 38;
            else if (_button.Hue != 0)
                _button.Hue = 0;


            return base.Draw(batcher, x, y);
        }

        public override void Save(BinaryWriter writer)
        {
            base.Save(writer);

            writer.Write(_definition.Index);
            writer.Write(_definition.Name.Length);
            writer.WriteUTF8String(_definition.Name);
            writer.Write((int) _definition.Icon);
            writer.Write(_isPrimary);
        }

        public override void Restore(BinaryReader reader)
        {
            base.Restore(reader);

            int index = reader.ReadInt32();
            string name = reader.ReadUTF8String(reader.ReadInt32());
            int graphic = reader.ReadInt32();

            _definition = new AbilityDefinition(index, name, (ushort) graphic);
            _isPrimary = reader.ReadBoolean();

            BuildGump();
        }

        public override void Dispose()
        {
            _button.MouseDoubleClick -= ButtonOnMouseDoubleClick;
            base.Dispose();
        }
    }
}