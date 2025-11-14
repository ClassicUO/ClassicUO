using System.IO;
using System.Linq;

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.Assets;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Dust765.Dust765
{
    internal class TextureManager
    {
        public bool IsEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerEnabled;
        public bool IsArrowEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerArrows;
        public bool IsHalosEnabled => ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.TextureManagerHalos;


        private static BlendState _blend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha
        };

        private static Vector3 _hueVector = Vector3.Zero;

        //TEXTURES ARROW
        private int ARROW_WIDTH_HALF = 14;
        private int ARROW_HEIGHT_HALF = 14;

        private static Texture2D _arrowGreen;
        public static Texture2D ArrowGreen
        {
            get
            {
                if (_arrowGreen == null || _arrowGreen.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_green.png");
                    _arrowGreen = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowGreen;
            }
        }

        private static Texture2D _arrowPurple;
        public static Texture2D ArrowPurple
        {
            get
            {
                if (_arrowPurple == null || _arrowPurple.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_purple.png");
                    _arrowPurple = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowPurple;
            }
        }

        private static Texture2D _arrowRed;
        public static Texture2D ArrowRed
        {
            get
            {
                if (_arrowRed == null || _arrowRed.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_red.png");
                    _arrowRed = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowRed;
            }
        }

        private static Texture2D _arrowYellow;
        public static Texture2D ArrowYellow
        {
            get
            {
                if (_arrowYellow == null || _arrowYellow.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_yellow.png");
                    _arrowYellow = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowYellow;
            }
        }

        private static Texture2D _arrowOrange;
        public static Texture2D ArrowOrange
        {
            get
            {
                if (_arrowOrange == null || _arrowOrange.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_orange.png");
                    _arrowOrange = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowOrange;
            }
        }

        private static Texture2D _arrowBlue;
        public static Texture2D ArrowBlue
        {
            get
            {
                if (_arrowBlue == null || _arrowBlue.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.arrow_blue2.png");
                    _arrowBlue = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _arrowBlue;
            }
        }
        //TEXTURES HALO
        private int HALO_WIDTH_HALF = 25;

        private static Texture2D _haloGreen;
        private static Texture2D HaloGreen
        {
            get
            {
                if (_haloGreen == null || _haloGreen.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_green.png");
                    _haloGreen = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloGreen;
            }
        }

        private static Texture2D _haloPurple;
        private static Texture2D HaloPurple
        {
            get
            {
                if (_haloPurple == null || _haloPurple.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_purple.png");
                    _haloPurple = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloPurple;
            }
        }

        private static Texture2D _haloRed;
        private static Texture2D HaloRed
        {
            get
            {
                if (_haloRed == null || _haloRed.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_red.png");
                    _haloRed = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloRed;
            }
        }

        private static Texture2D _haloWhite;
        private static Texture2D HaloWhite
        {
            get
            {
                if (_haloWhite == null || _haloWhite.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_white.png");
                    _haloWhite = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloWhite;
            }
        }

        private static Texture2D _haloYellow;
        private static Texture2D HaloYellow
        {
            get
            {
                if (_haloYellow == null || _haloYellow.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_yellow.png");
                    _haloYellow = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloYellow;
            }
        }

        private static Texture2D _haloOrange;
        private static Texture2D HaloOrange
        {
            get
            {
                if (_haloOrange == null || _haloOrange.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_orange.png");
                    _haloOrange = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloOrange;
            }
        }

        private static Texture2D _haloBlue;
        private static Texture2D HaloBlue
        {
            get
            {
                if (_haloBlue == null || _haloBlue.IsDisposed)
                {
                    Stream stream = typeof(CUOEnviroment).Assembly.GetManifestResourceStream("ClassicUO.halo_blue2.png");
                    _haloBlue = Texture2D.FromStream(Client.Game.GraphicsDevice, stream);
                }

                return _haloBlue;
            }
        }
        //TEXTURES END
        public void Draw(UltimaBatcher2D batcher)
        {
            if (!IsEnabled)
                return;

            if (World.Player == null)
                return;

            // Apply performance optimizations
            PerformanceOptimizations.UpdatePerformanceMetrics();
            
            // Get visible mobiles only
            var allMobiles = World.Mobiles.Values.ToList();
            var visibleMobiles = allMobiles.Where(mobile => 
                PerformanceOptimizations.IsObjectVisible(mobile, Client.Game.Scene.Camera) &&
                PerformanceOptimizations.ShouldRenderAtLOD(mobile, Client.Game.Scene.Camera)
            ).ToList();

            // Group mobiles by type for batch optimization
            var groupedMobiles = PerformanceOptimizations.GroupObjectsByType(visibleMobiles.Cast<Entity>());
            
            // Record performance metrics
            PerformanceMonitor.RecordObjectCount(allMobiles.Count, visibleMobiles.Count);
            PerformanceMonitor.Update();

            foreach (var group in groupedMobiles)
            {
                foreach (Mobile mobile in group.Value.Cast<Mobile>())
            {
                //SKIP FOR PLAYER
                if (mobile == World.Player)
                    continue;

                _hueVector.Z = 1f;

                if (IsHalosEnabled && (HaloOrange != null || HaloGreen != null || HaloPurple != null || HaloRed != null || HaloBlue != null))
                {
                    //CALC WHERE MOBILE IS
                    Point pm = CombatCollection.CalcUnderChar(mobile);
                    //Move from center by half texture width
                    pm.X -= HALO_WIDTH_HALF;

                    //_hueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;
                    _hueVector.Y = _hueVector.X > 1.0f ? ShaderHueTranslator.SHADER_LIGHTS : ShaderHueTranslator.SHADER_NONE;
                    batcher.SetBlendState(_blend);

                    //HUMANS ONLY
                    if (ProfileManager.CurrentProfile.TextureManagerHumansOnly && !mobile.IsHuman)
                    {

                    }
                    else
                    {
                        //PURPLE HALO FOR: LAST ATTACK, LASTTARGET
                        if ((TargetManager.LastAttack == mobile.Serial & TargetManager.LastAttack != 0 & ProfileManager.CurrentProfile.TextureManagerPurple) || (TargetManager.LastTargetInfo.Serial == mobile.Serial & TargetManager.LastTargetInfo.Serial != 0 & ProfileManager.CurrentProfile.TextureManagerPurple))
                            batcher.Draw(HaloPurple, new Rectangle(pm.X, pm.Y - 15, HaloPurple.Width, HaloPurple.Height), _hueVector);

                        //GREEN HALO FOR: ALLYS AND PARTY
                        else if (mobile.NotorietyFlag == NotorietyFlag.Ally & ProfileManager.CurrentProfile.TextureManagerGreen || World.Party.Contains(mobile.Serial) & ProfileManager.CurrentProfile.TextureManagerGreen)
                            batcher.Draw(HaloGreen, new Rectangle(pm.X, pm.Y - 15, HaloGreen.Width, HaloGreen.Height), _hueVector);
                        //RED HALO FOR: CRIMINALS, GRAY, MURDERER
                        else if (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray || mobile.NotorietyFlag == NotorietyFlag.Murderer)
                        {
                            if (ProfileManager.CurrentProfile.TextureManagerRed)
                            {
                                batcher.Draw(HaloRed, new Rectangle(pm.X, pm.Y - 15, HaloRed.Width, HaloRed.Height), _hueVector);
                            }
                        }
                        //ORANGE HALO FOR: ENEMY
                        else if (mobile.NotorietyFlag == NotorietyFlag.Enemy & ProfileManager.CurrentProfile.TextureManagerOrange)
                            batcher.Draw(HaloOrange, new Rectangle(pm.X, pm.Y - 15, HaloOrange.Width, HaloOrange.Height), _hueVector);
                        //BLUE HALO FOR: INNOCENT
                        else if (mobile.NotorietyFlag == NotorietyFlag.Innocent & ProfileManager.CurrentProfile.TextureManagerBlue)
                            batcher.Draw(HaloBlue, new Rectangle(pm.X, pm.Y - 15, HaloBlue.Width, HaloBlue.Height), _hueVector);
                    }

                    batcher.SetBlendState(null);
                }
                //HALO TEXTURE

                //ARROW TEXTURE
                if (IsArrowEnabled && (ArrowGreen != null || ArrowRed != null || ArrowPurple != null || ArrowOrange != null || ArrowBlue != null))
                {
                    //CALC MOBILE HEIGHT FROM ANIMATION
                    Point p1 = CombatCollection.CalcOverChar(mobile);
                    //Move from center by half texture width
                    p1.X -= ARROW_WIDTH_HALF;
                    p1.Y -= ARROW_HEIGHT_HALF;

                    /* MAYBE USE THIS INCASE IT SHOWS OUTSIDE OF GAMESCREEN?
                    if (!(p1.X < 0 || p1.X > screenW - mobile.HitsTexture.Width || p1.Y < 0 || p1.Y > screenH))
                                {
                                    mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                                }
                    */

                    //_hueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;
                    _hueVector.Y = _hueVector.X > 1.0f ? ShaderHueTranslator.SHADER_LIGHTS : ShaderHueTranslator.SHADER_NONE;
                    batcher.SetBlendState(_blend);

                    //HUMANS ONLY
                    if (ProfileManager.CurrentProfile.TextureManagerHumansOnlyArrows && !mobile.IsHuman)
                    {
                        batcher.SetBlendState(null);
                        continue;
                    }

                    //PURPLE ARROW FOR: LAST ATTACK, LASTTARGET
                    if ((TargetManager.LastAttack == mobile.Serial & TargetManager.LastAttack != 0 & ProfileManager.CurrentProfile.TextureManagerPurpleArrows) || (TargetManager.LastTargetInfo.Serial == mobile.Serial & TargetManager.LastTargetInfo.Serial != 0 & ProfileManager.CurrentProfile.TextureManagerPurpleArrows))
                        batcher.Draw(ArrowPurple, new Rectangle(p1.X, p1.Y, ArrowPurple.Width, ArrowPurple.Height), _hueVector);
                    //GREEN ARROW FOR: ALLYS AND PARTY
                    else if ((mobile.NotorietyFlag == NotorietyFlag.Ally & ProfileManager.CurrentProfile.TextureManagerGreenArrows || World.Party.Contains(mobile.Serial)) && mobile != World.Player & ProfileManager.CurrentProfile.TextureManagerGreenArrows)
                        batcher.Draw(ArrowGreen, new Rectangle(p1.X, p1.Y, ArrowGreen.Width, ArrowGreen.Height), _hueVector);
                    //RED ARROW FOR: CRIMINALS, GRAY, MURDERER
                    else if (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Gray || mobile.NotorietyFlag == NotorietyFlag.Murderer)
                    {
                        if (ProfileManager.CurrentProfile.TextureManagerRedArrows)
                            batcher.Draw(ArrowRed, new Rectangle(p1.X, p1.Y, ArrowRed.Width, ArrowRed.Height), _hueVector);
                    }
                    //ORANGE ARROW FOR: ENEMY
                    else if (mobile.NotorietyFlag == NotorietyFlag.Enemy & ProfileManager.CurrentProfile.TextureManagerOrangeArrows)
                        batcher.Draw(ArrowOrange, new Rectangle(p1.X, p1.Y, ArrowOrange.Width, ArrowOrange.Height), _hueVector);
                    //BLUE ARROW FOR: INNOCENT
                    else if (mobile.NotorietyFlag == NotorietyFlag.Innocent & ProfileManager.CurrentProfile.TextureManagerBlueArrows)
                        batcher.Draw(ArrowBlue, new Rectangle(p1.X, p1.Y, ArrowBlue.Width, ArrowBlue.Height), _hueVector);

                    batcher.SetBlendState(null);
                }
                //ARROW TEXTURE
                }
            }
            
            // Draw performance stats if enabled
            PerformanceMonitor.Draw(batcher);
        }
    }
}