using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class MobileStatusRequestQueue
    {
        private ConcurrentQueue<uint> requestedSerials = new ConcurrentQueue<uint>();
        private static MobileStatusRequestQueue instance;
        private Task queueProccessor;
        public static MobileStatusRequestQueue Instance
        {
            get
            {
                instance ??= new MobileStatusRequestQueue();
                return instance;
            }
        }
        private MobileStatusRequestQueue(){        }

        public void RequestMobileStatus(uint serial)
        {
            requestedSerials.Enqueue(serial);
            if (requestedSerials.Count > 0 && (queueProccessor == null || queueProccessor.IsCompleted || !queueProccessor.Status.Equals(TaskStatus.Running)))
            {
                queueProccessor = Task.Factory.StartNew(() => {
                    while (requestedSerials.TryDequeue(out var serial))
                    {
                        GameActions.RequestMobileStatus(serial);
                        GameActions.Print($"Processing {serial}");
                        Task.Delay(1000).Wait();
                    }
                });
            }
        }
    }
}
