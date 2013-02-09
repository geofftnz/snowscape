using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NLog;

namespace Utils
{
    public class PerfMonitor
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public class Timer
        {
            public Stopwatch sw;
            public double totalTimeMS;
            public int count;

            public double averageTimeMS
            {
                get
                {
                    if (count > 0)
                    {
                        return totalTimeMS / (double)count;
                    }
                    return -1.0;
                }
            }

            public Timer()
            {
                this.sw = new Stopwatch();
            }

            public void Start()
            {
                this.sw.Start();
            }
            public void Stop()
            {
                sw.Stop();
                count++;
                totalTimeMS += sw.Elapsed.TotalMilliseconds;
                sw.Reset();
            }

        }

        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        public Dictionary<string, Timer> Timers { get { return timers; } }

        public PerfMonitor()
        {

        }

        private Timer AddIfNotExists(string name)
        {
            if (!this.Timers.ContainsKey(name))
            {
                var t = new Timer { totalTimeMS = 0.0, count = 0, sw = new Stopwatch() };
                this.Timers.Add(name, t);
                return t;
            }
            return this.Timers[name];
        }

        public void DumpToLog(string name)
        {
            if (this.Timers.ContainsKey(name)){

                var t = this.Timers[name];

                if (t.count > 0)
                {
                    log.Trace("Timer {0}: {1} ms avg, {2} calls", name, t.averageTimeMS, t.count);
                }
            }
        }
        public void DumpToLog()
        {
            foreach (var k in this.Timers.Keys)
            {
                DumpToLog(k);
            }
        }

        public void Start(string name)
        {
            this.AddIfNotExists(name).Start();
        }

        public void Stop(string name)
        {
            if (this.Timers.ContainsKey(name))
            {
                this.Timers[name].Stop();
            }
        }

        public double AverageMS(string name)
        {
            if (this.Timers.ContainsKey(name))
            {
                return this.Timers[name].averageTimeMS;
            }
            return -1.0;
        }

        public IEnumerable<Timer> AllTimers()
        {
            foreach (var timer in this.Timers.Values)
            {
                yield return timer;
            }
        }

        public IEnumerable<Tuple<string, double>> AllAverageTimes()
        {
            foreach (var k in this.Timers.Keys)
            {
                if (this.Timers[k].count > 0)
                {
                    yield return new Tuple<string, double>(k, this.Timers[k].averageTimeMS);
                }
            }
        }

    }
}
