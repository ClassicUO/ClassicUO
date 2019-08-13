﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Coroutines
{
    public interface IWaitCondition
    {
        bool Update();
    }

    class WaitCondition : IWaitCondition
    {
        private Func<bool> _condition;

        public WaitCondition(Func<bool> condition)
        {
            _condition = condition;
        }

        public bool Update() => _condition();
    }

    class WaitCondition<T> : IWaitCondition
    {
        private Func<T, T> _update;
        private Func<T, bool> _condition;
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
