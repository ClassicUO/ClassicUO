#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Utility
{
    public static class Profiler
    {
        public const int ProfileTimeCount = 60;
        private static readonly List<ContextAndTick> m_Context;
        private static readonly List<Tuple<string[], double>> m_ThisFrameData;
        private static readonly List<ProfileData> m_AllFrameData;
        private static readonly ProfileData m_TotalTimeData;
        private static readonly Stopwatch _timer;
        private static long m_BeginFrameTicks;

        static Profiler()
        {
            m_Context = new List<ContextAndTick>();
            m_ThisFrameData = new List<Tuple<string[], double>>();
            m_AllFrameData = new List<ProfileData>();
            m_TotalTimeData = new ProfileData(null, 0d);
            _timer = Stopwatch.StartNew();
        }

        public static double LastFrameTimeMS { get; private set; }

        public static double TrackedTime => m_TotalTimeData.TimeInContext;

        public static bool Enabled = false;

        public static void BeginFrame()
        {
            if (!Enabled)
            {
                return;
            }

            if (m_ThisFrameData.Count > 0)
            {
                foreach (Tuple<string[], double> t in m_ThisFrameData)
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

        public static void EnterContext(string context_name)
        {
            if (!Enabled)
            {
                return;
            }

            m_Context.Add(new ContextAndTick(context_name, _timer.ElapsedTicks));
        }

        public static void ExitContext(string context_name)
        {
            if (!Enabled)
            {
                return;
            }

            if (m_Context[m_Context.Count - 1].Name != context_name)
            {
                Log.Error("Profiler.ExitProfiledContext: context_name does not match current context.");
            }

            string[] context = new string[m_Context.Count];

            for (int i = 0; i < m_Context.Count; i++)
            {
                context[i] = m_Context[i].Name;
            }

            double ms = (_timer.ElapsedTicks - m_Context[m_Context.Count - 1].Tick) * 1000d / Stopwatch.Frequency;

            m_ThisFrameData.Add(new Tuple<string[], double>(context, ms));
            m_Context.RemoveAt(m_Context.Count - 1);
        }

        public static bool InContext(string context_name)
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

        public static ProfileData GetContext(string context_name)
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

            public ProfileData(string[] context, double time)
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
            public string[] Context;

            public bool MatchesContext(string[] context)
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
            public readonly string Name;
            public readonly long Tick;

            public ContextAndTick(string name, long tick)
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