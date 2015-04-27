using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Utils
{
    public class FrameCounter3
    {
        const int BUFLEN = 30;
        private Stopwatch sw = new Stopwatch();
        private long[] tickBuffer = new long[BUFLEN];
        private int bufferPos = 0;
        private long frameCount = 0;

        private double fpsSmoothed = 0.0;
        private double fpsLowpassAmount = 1.0;

        public double FPS
        {
            get
            {
                var ts = new TimeSpan(tickBuffer[bufferPos] - tickBuffer[(bufferPos + 1) % BUFLEN]).TotalSeconds;

                if (ts>0.0){
                    return (double)BUFLEN / ts;
                }
                return 0.0;
            }
        }

        public double FPSSmooth
        {
            get
            {
                return fpsSmoothed;
            }
        }

        public double TotalSeconds
        {
            get
            {
                return sw.Elapsed.TotalSeconds;
            }
        }

        public long Frames
        {
            get
            {
                return frameCount;
            }
        }


        public FrameCounter3()
        {
        }

        public void Start()
        {
            frameCount = 0;
            sw.Start();

            long ticks = sw.ElapsedTicks;

            for (int i = 0; i < BUFLEN; i++)
            {
                tickBuffer[i] = ticks;
            }
        }

        public void Stop()
        {
            sw.Stop();
            sw.Reset();
        }

        public void Frame()
        {
            frameCount++;
            bufferPos++;
            bufferPos %= BUFLEN;
            tickBuffer[bufferPos] = sw.ElapsedTicks;

            var f = this.FPS;
            this.fpsSmoothed = f * this.fpsLowpassAmount + (1.0 - this.fpsLowpassAmount) * this.fpsSmoothed;
        }

    }
}
