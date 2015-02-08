using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using System.Diagnostics;

namespace Utils
{
    public class FrameTracker
    {
        // number of frames of history to track
        public static int BUFLEN = 128;
        public static int MAXSTEPS = 32;


        public class Quad
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }
            public Vector4 Colour { get; set; }
        }

        public struct FrameStep
        {
            public string Name { get; set; }
            public Vector4 Colour { get; set; }
            public double EndTime { get; set; }
            public double StepTime { get; set; }
        }

        public class Frame
        {
            private FrameStep[] steps = new FrameStep[MAXSTEPS];
            private int numSteps = 0;
            private double startTime = 0.0;
            private double lastTime = 0.0;

            public Frame()
            {
                numSteps = 0;
                for (int i = 0; i < MAXSTEPS; i++)
                {
                    steps[i] = new FrameStep();
                    steps[i].StepTime = 0f;
                    steps[i].EndTime = 0f;
                }
            }

            public void Reset()
            {
                this.numSteps = 0;
            }

            public void Start(double currentTime)
            {
                this.startTime = currentTime;
                this.lastTime = currentTime;
                this.numSteps = 0;
            }

            public void AddStep(string name, Vector4 col, double currentTime)
            {
                if (numSteps < MAXSTEPS)
                {
                    steps[numSteps].Name = name;
                    steps[numSteps].Colour = col;
                    steps[numSteps].EndTime = currentTime - startTime;
                    steps[numSteps].StepTime = currentTime - lastTime;
                    numSteps++;
                    lastTime = currentTime;
                }
            }

            public IEnumerable<FrameStep> AllSteps()
            {
                for (int i = 0; i < numSteps; i++)
                {
                    yield return steps[i];
                }
            }

            public IEnumerable<Quad> GetQuads(float x, float width)
            {
                float y = 0.0f;
                for (int i = 0; i < numSteps; i++)
                {
                    yield return new Quad { X = x, Width = width, Y = y, Height = (float)steps[i].StepTime, Colour = steps[i].Colour };
                    y += (float)steps[i].StepTime;
                }
            }
        }

        private Stopwatch sw = new Stopwatch();
        private Frame[] frames = new Frame[BUFLEN];
        private int bufferPos = 0;
        private int totalSamples = 0;
        private double frameStartTime = 0.0;

        public double CurrentFrameTime { get { return this.sw.Elapsed.TotalSeconds - frameStartTime; } }

        public FrameTracker()
        {
            this.sw.Reset();
            this.sw.Start();

            for (int i = 0; i < BUFLEN; i++)
            {
                frames[i] = new Frame();
            }
        }

        public void StartFrame()
        {
            this.frameStartTime = this.sw.Elapsed.TotalSeconds;
            this.totalSamples++;
            this.bufferPos = (this.bufferPos + 1) % BUFLEN;
            this.frames[this.bufferPos].Start(frameStartTime);
        }

        public void Step(string name, Vector4 col)
        {
            this.frames[this.bufferPos].AddStep(name, col, this.sw.Elapsed.TotalSeconds);
        }

        public IEnumerable<Quad> GetQuads()
        {
            float x = 0.0f;
            float width = 1.0f / (float)BUFLEN;
            for (int frame = 0; frame < BUFLEN; frame++)
            {
                int f = (bufferPos + frame + 1) % BUFLEN;


                foreach (var quad in frames[f].GetQuads(x, width))
                {
                    yield return quad;
                }
                x += width;
            }
        }

        public IEnumerable<FrameStep> AllSteps()
        {
            if (this.totalSamples < BUFLEN)
            {
                for (int i = 1; i <= this.totalSamples; i++)
                {
                    foreach (var fs in this.frames[i].AllSteps())
                    {
                        yield return fs;
                    }
                }
            }
            else
            {
                for (int i = 0; i < BUFLEN; i++)
                {
                    foreach (var fs in this.frames[i].AllSteps())
                    {
                        yield return fs;
                    }
                }
            }
        }

        public class StepStats
        {
            public string Name { get; set; }
            public Vector4 Colour { get; set; }
            public double AverageTime { get; set; }
        }

        public IEnumerable<StepStats> GetStepStats()
        {
            return 
                from s in AllSteps()
                group s by s.Name into g
                select new StepStats { Name = g.Key, AverageTime = g.Select(a=>a.StepTime).Average(), Colour = g.Select(a=>a.Colour).FirstOrDefault() };
        }
    }
}
