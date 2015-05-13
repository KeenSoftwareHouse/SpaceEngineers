using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Trace
{
    public interface ITrace
    {
        void Watch(string name, object value);
        void Send(string msg, string comment = null);
    }
}
