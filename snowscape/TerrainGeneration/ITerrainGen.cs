﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTKExtensions.Framework;
using Utils;

namespace TerrainGeneration
{
    public interface ITerrainGen : IGameComponent
    {
        bool NeedThread { get; }
        int Width { get; }
        int Height { get; }
        float GetHeightAt(float x, float y);
        void Init();
        void Unload();
        void ResetTerrain();
        void ModifyTerrain();
        void Load(string filename);
        void Save(string filename);
        float[] GetRawData();
        IEnumerable<IParameter> GetParameters();

        /// <summary>
        /// Returns the lower bound for height in the terrain. 
        /// </summary>
        /// <returns></returns>
        float GetMinHeight();

        /// <summary>
        /// Returns the upper bound for height in the terrain.
        /// </summary>
        /// <returns></returns>
        float GetMaxHeight();
    }
}
