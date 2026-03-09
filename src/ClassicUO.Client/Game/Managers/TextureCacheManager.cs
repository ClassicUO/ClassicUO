using ClassicUO.Configuration;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Managers
{
    public static class TextureCacheManager
    {
        private static readonly Dictionary<string, CachedTexture> _textureCache = new Dictionary<string, CachedTexture>();
        private static readonly object _cacheLock = new object();
        private static int _maxCacheSize = 1000;
        private static long _lastCleanupTime = 0;
        private static readonly long _cleanupInterval = 30000; // 30 seconds
        
        private class CachedTexture
        {
            public Texture2D Texture { get; set; }
            public DateTime LastAccessed { get; set; }
            public int AccessCount { get; set; }
            public long Size { get; set; }
        }
        
        public static void SetMaxCacheSize(int size)
        {
            _maxCacheSize = size;
        }
        
        public static Texture2D GetOrCreateTexture(string key, Func<Texture2D> textureFactory)
        {
            if (!ProfileManager.CurrentProfile?.EnableTextureCaching ?? true)
            {
                return textureFactory();
            }
            
            lock (_cacheLock)
            {
                CleanupIfNeeded();
                
                if (_textureCache.TryGetValue(key, out var cached))
                {
                    cached.LastAccessed = DateTime.Now;
                    cached.AccessCount++;
                    return cached.Texture;
                }
                
                var texture = textureFactory();
                if (texture != null)
                {
                    var size = EstimateTextureSize(texture);
                    _textureCache[key] = new CachedTexture
                    {
                        Texture = texture,
                        LastAccessed = DateTime.Now,
                        AccessCount = 1,
                        Size = size
                    };
                }
                
                return texture;
            }
        }
        
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                foreach (var cached in _textureCache.Values)
                {
                    cached.Texture?.Dispose();
                }
                _textureCache.Clear();
            }
        }
        
        public static void RemoveTexture(string key)
        {
            lock (_cacheLock)
            {
                if (_textureCache.TryGetValue(key, out var cached))
                {
                    cached.Texture?.Dispose();
                    _textureCache.Remove(key);
                }
            }
        }
        
        public static int GetCacheSize()
        {
            lock (_cacheLock)
            {
                return _textureCache.Count;
            }
        }
        
        public static long GetCacheMemoryUsage()
        {
            lock (_cacheLock)
            {
                return _textureCache.Values.Sum(c => c.Size);
            }
        }
        
        private static void CleanupIfNeeded()
        {
            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - _lastCleanupTime < _cleanupInterval)
                return;
                
            _lastCleanupTime = now;
            
            if (_textureCache.Count <= _maxCacheSize)
                return;
                
            // Remove least recently used textures
            var toRemove = _textureCache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .ThenBy(kvp => kvp.Value.AccessCount)
                .Take(_textureCache.Count - _maxCacheSize)
                .ToList();
                
            foreach (var kvp in toRemove)
            {
                kvp.Value.Texture?.Dispose();
                _textureCache.Remove(kvp.Key);
            }
        }
        
        private static long EstimateTextureSize(Texture2D texture)
        {
            if (texture == null) return 0;
            
            // Rough estimation: width * height * 4 bytes per pixel
            return (long)texture.Width * texture.Height * 4;
        }
    }
}
