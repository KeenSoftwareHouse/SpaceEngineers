using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NoSerializeAttribute : Attribute
    {
    }
}
