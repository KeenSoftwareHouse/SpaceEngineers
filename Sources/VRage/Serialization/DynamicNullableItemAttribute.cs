using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{ 
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class DynamicNullableItemAttribute : SerializeAttribute
    {
        public DynamicNullableItemAttribute(Type dynamicSerializerType, bool defaultTypeCommon = false)
        {
            Flags = defaultTypeCommon ? MyObjectFlags.DynamicDefault : MyObjectFlags.Dynamic;
            Flags |= MyObjectFlags.Nullable;
            DynamicSerializerType = dynamicSerializerType;
            Kind = MySerializeKind.Item;
        }
    }
}
