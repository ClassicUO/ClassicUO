using System;
using System.Threading.Tasks;
using System.IO;

namespace ClassicUO.Utility
{
    public static class Exstentions
    {
        public static void Raise(this EventHandler handler, object sender = null)
        {
            handler?.Invoke(sender, EventArgs.Empty);
        }

        public static void Raise<T>(this EventHandler<T> handler, T e, object sender = null)
        {
            handler?.Invoke(sender, e);
        }

        public static void RaiseAsync(this EventHandler handler, object sender = null)
        {
            if (handler != null)
                Task.Run(() => handler(sender, EventArgs.Empty)).Catch();
        }

        public static void RaiseAsync<T>(this EventHandler<T> handler, T e, object sender = null)
        {
            if (handler != null)
                Task.Run(() => handler(sender, e)).Catch();
        }

        public static Task Catch(this Task task)
        {
            return task.ContinueWith(t =>
            {
                t.Exception?.Handle((e) =>
                {
                    try
                    {
                        using (StreamWriter txt = new StreamWriter("crash.log", true))
                        {
                            txt.AutoFlush = true;
                            txt.WriteLine("Exception @ {0}", DateTime.Now.ToString("MM-dd-yy HH:mm:ss.ffff"));
                            txt.WriteLine(e.ToString());
                            txt.WriteLine("");
                            txt.WriteLine("");
                        }
                    }
                    catch
                    {
                    }
                    return true;
                });
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
