// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>
    /// Static hub for outgoing-packet events. Mirrors <see cref="EventSink"/>
    /// but for the client-to-server direction.
    /// <para/>
    /// Every <c>NetClient.Send_*</c> extension raises one typed event here
    /// before serializing bytes. Subscribers (logging, plugins, replay,
    /// instrumentation) observe the typed parameters without parsing wire
    /// format. The caller API is unchanged — <c>Send_*</c> still writes the
    /// packet directly.
    /// </summary>
    internal static class OutgoingEventSink
    {
        // ---- Network / session ----
        public static event Action<PingSentArgs> PingSent;

        public static void RaisePingSent(in PingSentArgs e) => Invoke(PingSent, e);

        // Per-category event blocks land here as the outgoing migration phases
        // wire packets in. See OUTGOING-PACKETS-MIGRATION for the rollout plan.

        private static void Invoke<T>(Action<T> handler, in T args)
        {
            if (handler is null) return;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d)(args);
                }
                catch (Exception ex)
                {
                    Log.Error($"OutgoingEventSink handler failed for {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            PingSent = null;
        }
    }
}
