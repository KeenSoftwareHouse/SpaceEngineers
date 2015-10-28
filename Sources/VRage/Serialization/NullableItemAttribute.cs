using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class NullableItemAttribute : SerializeAttribute
    {
        public NullableItemAttribute()
        {
            Flags = MyObjectFlags.Nullable;
            Kind = MySerializeKind.Item;
        }
    }
}
