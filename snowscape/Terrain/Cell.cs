using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Terrain
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Cell
    {
        /// <summary>
        /// Hard rock - eroded by wind and water
        /// </summary>
        public float Hard;
        /// <summary>
        /// Loose material/soil/dust - moved by wind and water
        /// </summary>
        public float Loose;
        /// <summary>
        /// Suspended material - indicates erosion
        /// </summary>
        public float Erosion;
        /// <summary>
        /// Non-height component indicating how much flowing water is over this tile.
        /// </summary>
        public float MovingWater;

        /// <summary>
        /// Amount of suspended material carried over this tile.
        /// </summary>
        public float Carrying;

        public float Height
        {
            get
            {
                return Hard + Loose;
            }
        }

        public float WHeight
        {
            get
            {
                return Hard + Loose + MovingWater;
            }
        }
    }
}
