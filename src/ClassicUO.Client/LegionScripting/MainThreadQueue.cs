using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ClassicUO.LegionScripting
{
    internal static class MainThreadQueue
    {
        private static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<InvokeJob> _invokeJobs = new ConcurrentQueue<InvokeJob>();

        private sealed class InvokeJob
        {
            internal Func<object> Func;
            internal ManualResetEventSlim Done;
            internal object Result;
            internal Exception Exception;
        }

        public static void EnqueueAction(Action action)
        {
            if (action == null) return;
            _actions.Enqueue(action);
        }

        public static T InvokeOnMainThread<T>(Func<T> func)
        {
            if (func == null) return default;
            var done = new ManualResetEventSlim(false);
            var job = new InvokeJob
            {
                Func = () => func(),
                Done = done,
                Result = null
            };
            _invokeJobs.Enqueue(job);
            done.Wait();
            if (job.Exception != null)
                throw new InvalidOperationException("MainThreadQueue invoke failed.", job.Exception);
            return (T)job.Result;
        }

        public static void Process()
        {
            while (_invokeJobs.TryDequeue(out var job))
            {
                try
                {
                    job.Result = job.Func?.Invoke();
                }
                catch (Exception ex)
                {
                    job.Exception = ex;
                }
                job.Done?.Set();
            }
            while (_actions.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch { }
            }
        }
    }
}
