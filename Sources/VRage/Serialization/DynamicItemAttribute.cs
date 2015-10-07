using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class DynamicItemAttribute : SerializeAttribute
    {
        public DynamicItemAttribute(Type dynamicSerializerType, bool defaultTypeCommon = false)
        {
            Flags = defaultTypeCommon ? MyObjectFlags.DynamicDefault : MyObjectFlags.Dynamic;
            DynamicSerializerType = dynamicSerializerType;
            Kind = MySerializeKind.Item;
        }
    }
}
