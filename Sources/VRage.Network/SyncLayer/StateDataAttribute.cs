using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace VRage.Network
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class StateDataAttribute : Attribute
    {
        public int Order { get; private set; }

        public StateDataAttribute([CallerLineNumber]int order = 0)
        {
            Order = order;
        }
    }
}
