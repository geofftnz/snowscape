﻿using System;
namespace Utils
{
    public interface IParameter
    {
        void Decrease();
        void Increase();
        string Name { get; }
        string Formatter();
        void Reset();
        object GetValue();
        T GetValue<T>();
        bool Impacts(ParameterImpact impact);
    }
}
