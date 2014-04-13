using System;
namespace Utils
{
    public interface IParameter
    {
        void Decrease();
        void Increase();
        string Name { get; }
        void Reset();
        object GetValue();
        bool Impacts(ParameterImpact impact);
    }
}
