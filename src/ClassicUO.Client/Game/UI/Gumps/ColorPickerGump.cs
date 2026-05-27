// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Network;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class ColorPickerGump : Gump
    {
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 4;
        private readonly ColorPickerBox _box;
        private readonly StaticPic _dyeTybeImage;
        private readonly HSliderBar _slider;

        private readonly ushort _graphic;
        private readonly Action<ushort> _okClicked;

        public ColorPickerGump(World world, uint serial, ushort graphic, int x, int y, Action<ushort> okClicked) : base(world, serial, 0)
        {
            CanCloseWithRightClick = serial == 0;
            _graphic = graphic;
            CanMove = true;
            AcceptMouseInput = false;
            X = x;
            Y = y;
            Add(new GumpPic(0, 0, 0x0906, 0));

            Add
            (
                new Button(1, 0x5669, 0x566B, 0x566A)
                {
                    X = 212,
                    Y = 33,
                    ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                new Button(0, 0x0907, 0x0908, 0x909)
                {
                    X = 208, Y = 138, ButtonAction = ButtonAction.Activate
                }
            );

            Add
            (
                _slider = new HSliderBar
                (
                    39,
                    142,
                    145,
                    SLIDER_MIN,
                    SLIDER_MAX,
                    1,
                    HSliderBarStyle.BlueWidgetNoBar
                )
            );

            _slider.ValueChanged += (sender, e) => { _box.Graduation = _slider.Value; };
            Add(_box = new ColorPickerBox(World, 34, 34));
            _box.ColorSelectedIndex += (sender, e) => { _dyeTybeImage.Hue = _box.SelectedHue; };
            
            Add
            (
                _dyeTybeImage = new StaticPic(0x0FAB, 0)
                {
                    X = 200, Y = 78
                }
            );

            _okClicked = okClicked;
            _dyeTybeImage.Hue = _box.SelectedHue;
        }

        public ushort Graphic => _graphic;

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 0:
                    // "Okay"
                    if (LocalSerial != 0)
                    {
                        NetClient.Socket.Send_DyeDataResponse(LocalSerial, _graphic, _box.SelectedHue);
                    }

                    _okClicked?.Invoke(_box.SelectedHue);
                    Dispose();

                    break;
                case 1:
                    // color picker
                    if (World.TargetManager.IsTargeting)
                    {
                        World.TargetManager.CancelTarget();
                    }

                    World.TargetManager.SetTargeting(EntityTargeted, CursorType.Target, TargetType.Neutral);
                    break;
            }
        }

        private void EntityTargeted(GameObject obj)
        {
            if (obj != null)
            {
                _box.SelectedHue = obj.Hue;

                if (_box.SelectedHue == obj.Hue)
                {
                    _slider.Value = _box.Graduation;
                }
                else
                {
                    // If the hue is not valid (rejected by the setter), send a message to the user

                    string badHueMessage = Client.Game.UO.FileManager.Clilocs.GetString(1042295);

                    World.MessageManager.HandleMessage(
                        obj as Entity,
                        badHueMessage,
                        "System",
                        0,
                        MessageType.Regular,
                        3,
                        TextType.SYSTEM
                    );
                }
            }
        }
    }
}