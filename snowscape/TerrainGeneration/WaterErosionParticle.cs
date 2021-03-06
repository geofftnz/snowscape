﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Microsoft.Xna.Framework;
using OpenTK;

namespace TerrainGeneration
{
    public class WaterErosionParticle
    {
        public Vector2 Pos;
        public Vector3 Vel;

        public float CarryingCapacity;
        public float CarryingAmount;
        public float Speed;
        public int Age;

        public WaterErosionParticle()
        {
            this.Pos = new Vector2(0f,0f);
            this.Vel = new Vector3(0f, 0f,0f);
            Reset(0, 0);
        }

        public WaterErosionParticle(int x, int y)
            : this()
        {
            Reset(x, y);
        }

        public void Reset(int x, int y, Random r)
        {
            if (r != null)
            {
                this.Pos.X = (float)x + 0.1f + (float)r.NextDouble() * 0.8f;
                this.Pos.Y = (float)y + 0.1f + (float)r.NextDouble() * 0.8f;
            }
            else
            {
                this.Pos.X = (float)x + 0.5f;
                this.Pos.Y = (float)y + 0.5f;
            }
          //  this.Vel = new Vector2(0.0f);
            this.CarryingAmount = 0.0f;
            this.CarryingCapacity = 0.0f;
            this.Speed = 0f;
            this.Vel.X = 0f;
            this.Vel.Y = 0f;
            this.Vel.Z = 0f;
            this.Age = 0;
        }

        public void Reset(int x, int y)
        {
            this.Reset(x, y, null);
        }
    }
}
