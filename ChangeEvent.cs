using System;
using System.Collections.Generic;
using System.Text;
using spatial;

namespace flightmanagementcomputer
{
    

    public class ChangeEvent
    {
        Position p;
        VVector v;
        string source;

        public ChangeEvent(string source,Position p, VVector v)
        {
            this.p = p;
            this.v = v;
            this.source = source;
        }

        public Position getPosition() => p;

        public VVector getVector() => v;
        public override string ToString()
        {
            return p.ToString() + v.ToString();
        }

    }
}
