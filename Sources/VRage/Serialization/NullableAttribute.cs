using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class NullableAttribute : SerializeAttribute
    {
        public NullableAttribute()
        {
            Flags = MyObjectFlags.Nullable;
        }
    }
}
