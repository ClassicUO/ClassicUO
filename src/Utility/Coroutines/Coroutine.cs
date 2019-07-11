using System;
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Utility.Coroutines
{
    internal sealed class Coroutine : IDisposable
    {
        private IEnumerator<IWaitCondition> _enumerator;
        private string _name;

        public CoroutineStatus Status { get; private set; }

        public void Dispose()
        {
            _enumerator.Dispose();
            Status = CoroutineStatus.Disposed;
        }

        public static Coroutine Start(Scene scene, IEnumerable<IWaitCondition> method, string name = null)
        {
            return scene.Coroutines.StartNew(method.GetEnumerator(), name);
        }

        public static Coroutine Start(Scene scene, IEnumerator<IWaitCondition> enumerator, string name = null)
        {
            return scene.Coroutines.StartNew(enumerator, name);
        }

        internal void Setup(IEnumerator<IWaitCondition> values, string name)
        {
            Status = CoroutineStatus.Running;
            _name = name;
            _enumerator?.Dispose();
            _enumerator = values;

            if (!_enumerator.MoveNext())
                Status = CoroutineStatus.Complete;
        }

        internal void Update()
        {
            if (Status != CoroutineStatus.Running)
                return;

            IWaitCondition currentYield = _enumerator.Current;

            if (currentYield == null || currentYield.Update())
            {
                if (!_enumerator.MoveNext())
                    Status = CoroutineStatus.Complete;
            }
        }

        internal void Restart()
        {
            _enumerator.Reset();
            _enumerator.MoveNext();
        }

        public void Pause()
        {
            if (Status == CoroutineStatus.Running)
                Status = CoroutineStatus.Paused;
        }

        public void Resume()
        {
            if (Status == CoroutineStatus.Paused)
                Status = CoroutineStatus.Running;
        }

        public void Cancel()
        {
            _enumerator.Dispose();
            Status = CoroutineStatus.Cancelled;
        }
    }
}