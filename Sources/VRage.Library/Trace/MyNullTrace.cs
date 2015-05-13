using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Trace
{
    class MyNullTrace : ITrace
    {
        public void Send(string msg, string comment = null) { }
        public void Watch(string name, object value) { }
    }
}
