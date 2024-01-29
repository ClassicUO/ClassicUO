using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class CreditsGump : Gump
    {
        private const ushort BACKGROUND_IMG = 0x0500;
        private Point _offset;
        private uint _lastUpdate;

        //TODO
        private const string CREDITS =
@"
Copyright(R) ClassicUO 2021

This project does not distribute any copyrighted game assets.
In order to run this client you'll need to legally obtain a
copy of the Ultima Online Classic Client.

Using a custom client to connect to official UO servers is
strictly forbidden. 
We do not assume any responsibility of the usage of this client.


Ultima Online(R) 2021 Electronic Arts Inc. All Rights Reserved.




                [Lead Developer]
                Karasho' - https://github.com/andreakarasho
";

        public CreditsGump(World world) : base(world, 0, 0)
        {
            Client.Game.Audio.PlayMusic(8, false, true);

            LayerOrder = UILayer.Over;
            CanCloseWithRightClick = true;

            GumpPic background = new GumpPic(0, 0, BACKGROUND_IMG, 0);
            Width = background.Width;
            Height = background.Height;

            Add(new AlphaBlendControl(1f) { Width = background.Width, Height = background.Height});

            Add(background);

            Vector2 size = Fonts.Regular.MeasureString(CREDITS);
            _offset.X = (int) (Width / 2f - size.X / 2);
            _offset.Y = Height;
        }

        public override void Update()
        {
            base.Update();

            if (_lastUpdate < Time.Ticks)
            {
                _offset.Y -= 1;
                _lastUpdate = Time.Ticks + 25;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawString(Fonts.Bold, CREDITS, x + _offset.X, y + _offset.Y, hueVector);

            return true;
        }
    }
}
