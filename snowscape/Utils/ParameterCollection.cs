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


                this.DisplayOffset = currentIndex - this.DisplayLength / 2;
                
            }
        }

        public int DisplayLength { get; set; }

        private int displayOffset;
        public int DisplayOffset
        {
            get { return displayOffset; }
            set
            {
                this.displayOffset = value.ClampInclusive(0, (this.Count - this.DisplayLength).ClampInclusive(0, int.MaxValue));
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
            this.DisplayLength = 10;
            this.DisplayOffset = 0;
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
