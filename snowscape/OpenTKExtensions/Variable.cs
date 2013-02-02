using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenTKExtensions
{
    public class Variable:Tuple<int,string>
    {
        public int Index 
        {
            get { return this.Item1; }
        }
        public string Name
        {
            get { return this.Item2; }
        }

        public Variable(int index,string name):base(index,name)
        {
        }
    }
}
