
using ClassicUO.Game.Gumps.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ClassicUO.Game.Gumps.Controls.InGame;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using Microsoft.Xna.Framework.Graphics;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class MiniMapGump : Gump
    {

        private static Scenes.GameScene _scene;
        const float ReticleBlinkMS = 250f;
        private bool _useLargeMap;
        float _timeMS;
        private double _frameMS;
        private SpriteTexture _gumpTexture;
        private Texture2D _playerIndicator;
        private static UIManager _uiManager;
        private bool _miniMap_LargeFormat;
        private static MiniMapGump _self;


        public MiniMapGump(Scenes.GameScene scene) : base(0, 0)
        {
            
            CanMove = true;
            AcceptMouseInput = true;
            _useLargeMap = _miniMap_LargeFormat;
            X = 600; Y = 50;
            ControlInfo.Layer = UILayer.Over;
            _scene = scene;


        }

        public static bool MiniMap_LargeFormat
        {
            get;
            set;
        }

        public static void Toggle(Scenes.GameScene scene)
        {
            
            var ui = Service.Get<UIManager>();
            if (!ui.getControlls().OfType<MiniMapGump>().Any())
            {
                Service.Get<UIManager>().Add(_self = new MiniMapGump(scene));
            }
            else
            {
                _self.Dispose();
                
            }


        }


        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position)
        {
            var player = World.Player;
            float x = (float)Math.Round((player.Position.X % 256) + player.Position.X+0f) / 256f;
            float y = (float)Math.Round((player.Position.Y % 256) + player.Position.Y+0f) / 256f;
            float minimapU = (_gumpTexture.Width / 256f) / 2f;
            float minimapV = (_gumpTexture.Height / 256f) / 2f;
            Vector3 playerPosition = new Vector3(x - y, x + y, 0f);
            SpriteVertex[] v =
            {
                new SpriteVertex(new Vector3(position.X, position.Y, 0), playerPosition + new Vector3(-minimapU, -minimapV, 0), new Vector3(0, 0, 0)),
                new SpriteVertex(new Vector3(position.X + Width, position.Y, 0), playerPosition + new Vector3(minimapU, -minimapV, 0), new Vector3(1, 0, 0)),
                new SpriteVertex(new Vector3(position.X, position.Y + Height, 0), playerPosition + new Vector3(-minimapU, minimapV, 0), new Vector3(0, 1, 0)),
                new SpriteVertex(new Vector3(position.X + Width, position.Y + Height, 0), playerPosition + new Vector3(minimapU, minimapV, 0), new Vector3(1, 1, 0))
            };

            
            //DRAW SPRITE
            spriteBatch.DrawSprite(_gumpTexture, v);

            MapGump mapGump = new MapGump();
            spriteBatch.DrawSprite(mapGump.Load2(), v);


            _timeMS += (float)_frameMS;
            if (_timeMS >= ReticleBlinkMS)
            {
                if (_playerIndicator == null)
                {
                    _playerIndicator = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                    _playerIndicator.SetData(new uint[1] { 0xFFFFFFFF });
                }
                //DRAW DOT OF PLAYER
                spriteBatch.Draw2D(_playerIndicator, new Vector3(position.X + Width / 2, position.Y + Height / 2 - 8, 0), Vector3.Zero);
            }
            if (_timeMS >= ReticleBlinkMS * 2)
            {
                _timeMS -= ReticleBlinkMS * 2;
            }

            return true;
        }

        

        public override void Update(double totalMS, double frameMS)
        {
            _frameMS = frameMS;
            
            if (_gumpTexture == null || _useLargeMap != _miniMap_LargeFormat)
            {
                _useLargeMap = _miniMap_LargeFormat;
                if (_gumpTexture != null)
                {
                    _gumpTexture = null;
                }
                _gumpTexture = IO.Resources.Gumps.GetGumpTexture((_useLargeMap ? (ushort)5011 : (ushort)5010));
                Size = new Point(_gumpTexture.Width, _gumpTexture.Height);
                
            }
            _gumpTexture.Ticks = (long)totalMS;
            
        }


        protected override void OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                MiniMapGump.MiniMap_LargeFormat = !MiniMapGump.MiniMap_LargeFormat ? true : false;
                _miniMap_LargeFormat = !MiniMap_LargeFormat;
            }
        }


    }
}
