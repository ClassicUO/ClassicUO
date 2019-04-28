using System;

namespace ClassicUO.Utility.Coroutines
{
    public interface IWaitCondition
    {
        bool Update();
    }

    internal class WaitCondition : IWaitCondition
    {
        private readonly Func<bool> _condition;

        public WaitCondition(Func<bool> condition)
        {
            _condition = condition;
        }

        public bool Update()
        {
            return _condition();
        }
    }

    internal class WaitCondition<T> : IWaitCondition
    {
        private readonly Func<T, bool> _condition;
        private readonly Func<T, T> _update;
        private T _parameter;

        public WaitCondition(Func<T, T> update, Func<T, bool> condition, T startingValue)
        {
            _update = update;
            _condition = condition;
            _parameter = startingValue;
        }

        public bool Update()
        {
            _parameter = _update(_parameter);

            return _condition(_parameter);
        }
    }
}