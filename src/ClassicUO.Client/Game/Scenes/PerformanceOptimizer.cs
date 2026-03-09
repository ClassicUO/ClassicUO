using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Scenes
{
    public static class PerformanceOptimizer
    {
        private static Rectangle _currentViewport;
        private static Vector2 _cameraPosition;
        private static float _renderDistance = 24f;
        
        public static void UpdateViewport(Rectangle viewport, Vector2 cameraPos, float renderDist = 24f)
        {
            _currentViewport = viewport;
            _cameraPosition = cameraPos;
            _renderDistance = renderDist;
        }
        
        public static bool ShouldRenderObject(GameObject obj)
        {
            if (obj == null || !ProfileManager.CurrentProfile.EnableFrustumCulling)
                return true;
                
            var objPos = new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y);
            var distance = Vector2.Distance(_cameraPosition, objPos);
            
            if (distance > _renderDistance * 44) // 44 pixels per tile
                return false;
                
            var objRect = new Rectangle(
                (int)objPos.X - 22, 
                (int)objPos.Y - 22, 
                44, 
                44
            );
            
            return _currentViewport.Intersects(objRect);
        }
        
        internal static bool ShouldRenderStatic(Static obj)
        {
            if (obj == null || !ProfileManager.CurrentProfile.EnableFrustumCulling)
                return true;
                
            var objPos = new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y);
            var distance = Vector2.Distance(_cameraPosition, objPos);
            
            if (distance > _renderDistance * 44)
                return false;
                
            var objRect = new Rectangle(
                (int)objPos.X - 22, 
                (int)objPos.Y - 22, 
                44, 
                44
            );
            
            return _currentViewport.Intersects(objRect);
        }
        
        public static bool ShouldRenderMobile(Mobile obj)
        {
            if (obj == null || !ProfileManager.CurrentProfile.EnableFrustumCulling)
                return true;
                
            var objPos = new Vector2(obj.RealScreenPosition.X, obj.RealScreenPosition.Y);
            var distance = Vector2.Distance(_cameraPosition, objPos);
            
            if (distance > _renderDistance * 44)
                return false;
                
            var objRect = new Rectangle(
                (int)objPos.X - 22, 
                (int)objPos.Y - 22, 
                44, 
                44
            );
            
            return _currentViewport.Intersects(objRect);
        }
        
        public static void ApplyGraphicsQualitySettings()
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile == null) return;
            
            // Aplicar configurações de VSync
            if (Client.Game != null)
            {
                Client.Game.SetVSync(profile.EnableVSync);
            }
            
            switch (profile.GraphicsQuality)
            {
                case 0: // Low
                    profile.TerrainShadowsLevel = 5;
                    profile.ShadowsEnabled = false;
                    profile.ShadowsStatics = false;
                    profile.AnimatedWaterEffect = false;
                    profile.UseXBR = false;
                    profile.ReduceParticleEffects = true;
                    break;
                    
                case 1: // Medium
                    profile.TerrainShadowsLevel = 10;
                    profile.ShadowsEnabled = true;
                    profile.ShadowsStatics = false;
                    profile.AnimatedWaterEffect = false;
                    profile.UseXBR = true;
                    profile.ReduceParticleEffects = false;
                    break;
                    
                case 2: // High
                    profile.TerrainShadowsLevel = 15;
                    profile.ShadowsEnabled = true;
                    profile.ShadowsStatics = true;
                    profile.AnimatedWaterEffect = true;
                    profile.UseXBR = true;
                    profile.ReduceParticleEffects = false;
                    break;
            }
        }
        
        public static void OptimizeForPerformance()
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile == null) return;
            
            if (profile.OptimizeBackgroundRendering)
            {
                profile.HideVegetation = profile.GraphicsQuality == 0;
                profile.DrawRoofs = profile.GraphicsQuality > 0;
            }
            
            if (profile.ReduceParticleEffects)
            {
                profile.AuraUnderFeetType = 0;
                profile.PartyAura = false;
            }
        }

        public static void UpdatePvPMode()
        {
        }

        public static float GetEffectiveMaxRenderDistance()
        {
            var profile = ProfileManager.CurrentProfile;
            if (profile == null) return 24f;
            return profile.MaxRenderDistance;
        }
    }
}
