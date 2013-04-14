using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Utils
{
    public class ParameterCollection : Collection<IParameter>
    {

        private int currentIndex;
        public int CurrentIndex
        {
            get 
            {
                if (currentIndex >= this.Count)
                {
                    currentIndex = this.Count - 1;
                }
                return currentIndex;
            }
            set 
            {
                currentIndex = value;
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }
                if (currentIndex >= this.Count)
                {
                    currentIndex = this.Count - 1;
                }
            }
        }

        public IParameter Current
        {
            get
            {
                return this[this.CurrentIndex];
            }
        }


        public ParameterCollection()
        {

        }

        public IParameter this[string name]
        {
            get
            {
                return this.Where(p => p.Name == name).FirstOrDefault();
            }
            set
            {
                var existing = this.Where(p => p.Name == name).FirstOrDefault();

                if (existing != null)
                {
                    this.Remove(existing);
                }

                this.Add(value);
            }
        }

        
    }
}
