// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ClassicUO.Utility
{
    public static class Profiler
    {
        public const int ProfileTimeCount = 60;
        private static readonly List<ContextAndTick> m_Context;
        private static readonly List<Tuple<ProfilerContext[], double>> m_ThisFrameData;
        private static readonly List<ProfileData> m_AllFrameData;
        private static readonly ProfileData m_TotalTimeData;
        private static readonly Stopwatch _timer;
        private static long m_BeginFrameTicks;

        static Profiler()
        {
            m_Context = new List<ContextAndTick>();
            m_ThisFrameData = new List<Tuple<ProfilerContext[], double>>();
            m_AllFrameData = new List<ProfileData>();
            m_TotalTimeData = new ProfileData(null, 0d);
            _timer = Stopwatch.StartNew();
        }

        public static double LastFrameTimeMS { get; private set; }

        public static double TrackedTime => m_TotalTimeData.TimeInContext;

        public static bool Enabled = false;

        public enum ProfilerContext
        {
            UPDATE_WORLD,
            RENDER_FRAME,
            RENDER_FRAME_WORLD,
            RENDER_FRAME_WORLD_PREPARE,
            RENDER_FRAME_UI,
            OUT_OF_CONTEXT
        }

        public static void BeginFrame()
        {
            if (!Enabled)
            {
                return;
            }

            if (m_ThisFrameData.Count > 0)
            {
                foreach (Tuple<ProfilerContext[], double> t in m_ThisFrameData)
                {
                    bool added = false;

                    foreach (ProfileData t1 in m_AllFrameData)
                    {
                        if (t1.MatchesContext(t.Item1))
                        {
                            t1.AddNewHitLength(t.Item2);
                            added = true;

                            break;
                        }
                    }

                    if (!added)
                    {
                        m_AllFrameData.Add(new ProfileData(t.Item1, t.Item2));
                    }
                }

                m_ThisFrameData.Clear();
            }

            m_BeginFrameTicks = _timer.ElapsedTicks;
        }

        public static void EndFrame()
        {
            if (!Enabled)
            {
                return;
            }

            LastFrameTimeMS = (_timer.ElapsedTicks - m_BeginFrameTicks) * 1000d / Stopwatch.Frequency;
            m_TotalTimeData.AddNewHitLength(LastFrameTimeMS);
        }

        public static void EnterContext(ProfilerContext context_name)
        {
            if (!Enabled)
            {
                return;
            }

            m_Context.Add(new ContextAndTick(context_name, _timer.ElapsedTicks));
        }

        public static void ExitContext(ProfilerContext context_name)
        {
            if (!Enabled)
            {
                return;
            }

            if (m_Context[m_Context.Count - 1].Name != context_name)
            {
                Log.Error("Profiler.ExitProfiledContext: context_name does not match current context.");
            }

            ProfilerContext[] context = new ProfilerContext[m_Context.Count];

            for (int i = 0; i < m_Context.Count; i++)
            {
                context[i] = m_Context[i].Name;
            }

            double ms = (_timer.ElapsedTicks - m_Context[m_Context.Count - 1].Tick) * 1000d / Stopwatch.Frequency;

            m_ThisFrameData.Add(new Tuple<ProfilerContext[], double>(context, ms));
            m_Context.RemoveAt(m_Context.Count - 1);
        }

        public static bool InContext(ProfilerContext context_name)
        {
            if (!Enabled)
            {
                return false;
            }

            if (m_Context.Count == 0)
            {
                return false;
            }

            return m_Context[m_Context.Count - 1].Name == context_name;
        }

        public static ProfileData GetContext(ProfilerContext context_name)
        {
            if (!Enabled)
            {
                return ProfileData.Empty;
            }

            for (int i = 0; i < m_AllFrameData.Count; i++)
            {
                if (m_AllFrameData[i].Context[m_AllFrameData[i].Context.Length - 1] == context_name)
                {
                    return m_AllFrameData[i];
                }
            }

            return ProfileData.Empty;
        }

        public class ProfileData
        {
            public static ProfileData Empty = new ProfileData(null, 0d);
            private uint m_LastIndex;
            private readonly double[] m_LastTimes = new double[ProfileTimeCount];

            public ProfileData(ProfilerContext[] context, double time)
            {
                Context = context;
                m_LastIndex = 0;
                AddNewHitLength(time);
            }

            public double LastTime => m_LastTimes[m_LastIndex % ProfileTimeCount];

            public double TimeInContext
            {
                get
                {
                    double time = 0;

                    for (int i = 0; i < ProfileTimeCount; i++)
                    {
                        time += m_LastTimes[i];
                    }

                    return time;
                }
            }

            public double AverageTime => TimeInContext / ProfileTimeCount;
            public ProfilerContext[] Context;

            public bool MatchesContext(ProfilerContext[] context)
            {
                if (Context.Length != context.Length)
                {
                    return false;
                }

                for (int i = 0; i < Context.Length; i++)
                {
                    if (Context[i] != context[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public void AddNewHitLength(double time)
            {
                m_LastTimes[m_LastIndex % ProfileTimeCount] = time;
                m_LastIndex++;
            }

            public override string ToString()
            {
                string name = string.Empty;

                for (int i = 0; i < Context.Length; i++)
                {
                    if (name != string.Empty)
                    {
                        name += ":";
                    }

                    name += Context[i];
                }

                return $"{name} - {TimeInContext:0.0}ms";
            }
        }

        private readonly struct ContextAndTick
        {
            public readonly ProfilerContext Name;
            public readonly long Tick;

            public ContextAndTick(ProfilerContext name, long tick)
            {
                Name = name;
                Tick = tick;
            }

            public override string ToString()
            {
                return string.Format("{0} [{1}]", Name, Tick);
            }
        }
    }
}