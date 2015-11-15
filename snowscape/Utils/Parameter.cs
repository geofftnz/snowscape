﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public enum ParameterImpact
    {
        None = 0,
        PreCalcLighting = 1
    }

    /// <summary>
    /// Represents a user-settable parameter
    /// 
    /// Knows about:
    /// - its name
    /// - its value
    /// - its min/max values
    /// - its increment function
    /// - its decrement function
    /// </summary>
    public class Parameter<T> : IParameter where T : IComparable
    {


        public string Name { get; private set; }

        private T value;
        public T Value
        {
            get { return this.value; }
            set
            {
                if (value.CompareTo(this.MinValue) < 0)
                {
                    this.value = this.MinValue;
                }
                else if (value.CompareTo(this.MaxValue) > 0)
                {
                    this.value = this.MaxValue;
                }
                else
                {
                    this.value = value;
                }
            }
        }
        public T DefaultValue { get; set; }
        public T MinValue { get; set; }
        public T MaxValue { get; set; }
        public Func<T, T> IncreaseFunc { get; set; }
        public Func<T, T> DecreaseFunc { get; set; }
        public Func<T, string> FormatFunc { get; set; }

        public ParameterImpact Impact { get; set; }

        public object GetValue()
        {
            return (object)this.Value;
        }

        public R GetValue<R>()
        {
            if (this.Value is R)
            {
                return (R)GetValue();
            }
            return default(R);
        }

        public bool Impacts(ParameterImpact impact)
        {
            return this.Impact == impact;
        }


        public Parameter(string name, T defaultValue, T minValue, T maxValue, Func<T, T> increaseFunc, Func<T, T> decreaseFunc, ParameterImpact impact, Func<T, string> formatFunc)
        {
            this.Name = name;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Value = this.DefaultValue = defaultValue;
            this.IncreaseFunc = increaseFunc;
            this.DecreaseFunc = decreaseFunc;
            this.Impact = impact;
            this.FormatFunc = formatFunc;
        }

        public Parameter(string name, T defaultValue, T minValue, T maxValue, Func<T, T> increaseFunc, Func<T, T> decreaseFunc, ParameterImpact impact)
            : this(name, defaultValue, minValue, maxValue, increaseFunc, decreaseFunc, impact, (v) => string.Format("{0:0.0000}", v))
        {
        }

        public Parameter(string name, T defaultValue, T minValue, T maxValue, Func<T, T> increaseFunc, Func<T, T> decreaseFunc)
            : this(name, defaultValue, minValue, maxValue, increaseFunc, decreaseFunc, ParameterImpact.None)
        {
        }
        public Parameter(string name, T defaultValue, T minValue, T maxValue, Func<T, T> increaseFunc, Func<T, T> decreaseFunc, Func<T, string> formatFunc)
            : this(name, defaultValue, minValue, maxValue, increaseFunc, decreaseFunc, ParameterImpact.None, formatFunc)
        {
        }

        public Parameter()
        {
        }

        public static Parameter<float> NewLinearParameter(string name, float defaultValue, float minValue, float maxValue, float change = 0.01f)
        {
            Func<float, float> increaseFunc = (v) => v + (maxValue - minValue) * change;
            Func<float, float> decreaseFunc = (v) => v - (maxValue - minValue) * change;
            return new Parameter<float>(name, defaultValue, minValue, maxValue, increaseFunc, decreaseFunc);
        }
        public static Parameter<float> NewExponentialParameter(string name, float defaultValue, float minValue, float maxValue, float changeRate = 0.01f)
        {
            Func<float, float> increaseFunc = (v) => v * (1.0f + changeRate);
            Func<float, float> decreaseFunc = (v) => v * (1.0f - changeRate);
            return new Parameter<float>(name, defaultValue, minValue, maxValue, increaseFunc, decreaseFunc);
        }

        public void Increase()
        {
            T newValue = this.DefaultValue;

            if (this.IncreaseFunc != null)
            {
                newValue = IncreaseFunc(this.Value);
            }

            this.Value = newValue;
        }

        public void Decrease()
        {
            T newValue = this.DefaultValue;

            if (this.DecreaseFunc != null)
            {
                newValue = DecreaseFunc(this.Value);
            }

            this.Value = newValue;
        }

        public void Reset()
        {
            this.Value = this.DefaultValue;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.Name, this.FormatFunc(this.Value));
        }

        public string Formatter()
        {
            throw new NotImplementedException();
        }


        public void Toggle()
        {
            if (this.Value.CompareTo(this.MinValue) == 0)
            {
                this.Increase();
            }
            else
            {
                this.Decrease();
            }
            
                
            
        }
    }
}
