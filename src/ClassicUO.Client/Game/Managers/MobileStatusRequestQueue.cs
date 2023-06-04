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
            if (queueProccessor == null || queueProccessor.IsCompleted)
            {
                queueProccessor = Task.Factory.StartNew(() => {
                    while (requestedSerials.TryDequeue(out var serial))
                    {
                        GameActions.RequestMobileStatus(serial);
                        Task.Delay(750).Wait();
                    }
                });
            }
        }
    }
}
