using System;
using ClassicUO.Game.Managers;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using SDL2;

namespace ClassicUO.Game.Scenes
{
    internal abstract class Scene : IUpdateable, IDisposable
    {
        private uint _time_cleanup = Time.Ticks + 5000;

        protected Scene(int sceneID, bool canresize, bool maximized, bool loadaudio)
        {
            CanResize = canresize;
            CanBeMaximized = maximized;
            CanLoadAudio = loadaudio;
            Camera = new Camera();
        }

        public bool IsDestroyed { get; private set; }

        public bool IsLoaded { get; private set; }

        public int RenderedObjectsCount { get; protected set; }

        public AudioManager Audio { get; private set; }

        public Camera Camera { get; }

        public virtual void Dispose()
        {
            if (IsDestroyed)
            {
                return;
            }

            IsDestroyed = true;
            Unload();
        }

        public virtual void Update(double totalTime, double frameTime)
        {
            Audio?.Update();
            Camera.Update();

            if (_time_cleanup < Time.Ticks)
            {
                ArtLoader.Instance.CleaUnusedResources(Constants.MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR);
                GumpsLoader.Instance.CleaUnusedResources(Constants.MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR);
                TexmapsLoader.Instance.CleaUnusedResources(Constants.MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR);

                AnimationsLoader.Instance.CleaUnusedResources
                    (Constants.MAX_ANIMATIONS_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR);

                World.Map?.ClearUnusedBlocks();
                LightsLoader.Instance.CleaUnusedResources(20);

                _time_cleanup = Time.Ticks + 500;
            }
        }

        public readonly bool CanResize, CanBeMaximized, CanLoadAudio;
        public readonly int ID;

        public virtual void FixedUpdate(double totalTime, double frameTime)
        {
        }


        public virtual void Load()
        {
            if (CanLoadAudio)
            {
                Audio = new AudioManager();
                Audio.Initialize();
            }

            IsLoaded = true;
        }

        public virtual void Unload()
        {
            Audio?.StopMusic();
        }

        public virtual bool Draw(UltimaBatcher2D batcher)
        {
            return true;
        }

        internal virtual bool OnMouseUp(MouseButtonType button) => false;
        internal virtual bool OnMouseDown(MouseButtonType button) => false;
        internal virtual bool OnMouseDoubleClick(MouseButtonType button) => false;
        internal virtual bool OnMouseWheel(bool up) => false;
        internal virtual bool OnMouseDragging() => false;

        internal virtual void OnTextInput(string text)
        {
        }

        internal virtual void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
        }

        internal virtual void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
        }
    }
}