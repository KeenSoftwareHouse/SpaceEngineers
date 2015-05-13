using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeenSoftwareHouse.Library.Extensions
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class EnabledAttribute : Attribute
    {
        public bool Enabled { get; set; }

        public EnabledAttribute(bool enabled = true)
        {
            Enabled = enabled;
        }
    }
}
