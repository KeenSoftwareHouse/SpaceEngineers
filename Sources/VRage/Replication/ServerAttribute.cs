using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Network
{
    /// <summary>
    /// Server method. Decorated method is be called by client on server or by server locally.
    /// Server performs validation. Pass null as validation method to perform no validation (not recommended)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerAttribute : Attribute
    {
        public readonly string Validation;

        public ServerAttribute()
        {
        }

        // CHECK-ME: Provisory flag (non-breaking), to be discussed. Defaults to false
        public bool ExceptLocal
        {
            get;
            set;
        }

        public ServerAttribute(string validationMethod)
        {
            Validation = validationMethod;
        }
    }
}
