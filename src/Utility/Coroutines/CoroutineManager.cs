using System.Collections.Generic;

namespace ClassicUO.Utility.Coroutines
{
    internal class CoroutineManager
    {
        private static readonly QueuedPool<Coroutine> _pool = new QueuedPool<Coroutine>();
        private readonly List<Coroutine> _coroutines = new List<Coroutine>();
        private readonly List<Coroutine> _scheduled = new List<Coroutine>();
        private readonly List<Coroutine> _trashcan = new List<Coroutine>();

        public Coroutine StartNew(IEnumerator<IWaitCondition> enumerator, string name)
        {
            Coroutine coroutine = _pool.GetOne();
            coroutine.Setup(enumerator, name);

            _scheduled.Add(coroutine);

            return coroutine;
        }

        public void Clear()
        {
            _coroutines.ForEach(s => s.Cancel());
            _coroutines.ForEach(s => s.Dispose());

            _scheduled.Clear();
            _coroutines.Clear();
        }

        public void Update()
        {
            _coroutines.AddRange(_scheduled);
            _scheduled.Clear();

            _coroutines.ForEach(s =>
            {
                s.Update();

                if (s.Status != CoroutineStatus.Running && s.Status != CoroutineStatus.Paused)
                    _trashcan.Add(s);
            });

            for (int i = 0; i < _trashcan.Count; i++)
            {
                Coroutine c = _trashcan[i];
                _trashcan.RemoveAt(i--);
                _pool.ReturnOne(c);
            }

            _trashcan.Clear();
        }
    }
}