using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class DynamicAttribute : SerializeAttribute
    {
        public DynamicAttribute(Type dynamicSerializerType, bool defaultTypeCommon = false)
        {
            Flags = defaultTypeCommon ? MyObjectFlags.DynamicDefault : MyObjectFlags.Dynamic;
            DynamicSerializerType = dynamicSerializerType;
        }
    }
}
