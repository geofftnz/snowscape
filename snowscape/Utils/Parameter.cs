using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
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

        public object GetValue()
        {
            return (object)this.Value;
        }


        public Parameter(string name, T defaultValue, T minValue, T maxValue, Func<T, T> increaseFunc, Func<T, T> decreaseFunc)
        {
            this.Name = name;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.Value = this.DefaultValue = defaultValue;
            this.IncreaseFunc = increaseFunc;
            this.DecreaseFunc = decreaseFunc;
        }

        public Parameter()
        {
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
            return string.Format("{0}: {1:0.0000}", this.Name, this.Value);
        }




        public T GetValue<T>()
        {
            throw new NotImplementedException();
        }
    }
}
