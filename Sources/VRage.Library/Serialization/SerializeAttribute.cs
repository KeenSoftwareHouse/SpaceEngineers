using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SerializeAttribute : Attribute
    {
        /// <summary>
        /// Serialization flags for member itself.
        /// </summary>
        public MyObjectFlags Flags;

        /// <summary>
        /// Serialization flags for primitive types, when defined for types, passed down the hierarchy.
        /// </summary>
        public MyPrimitiveFlags PrimitiveFlags;

        /// <summary>
        /// Length for fixed length arrays, collections and strings.
        /// </summary>
        public ushort FixedLength;

        /// <summary>
        /// Type of dynamic resolver.
        /// </summary>
        public Type DynamicSerializerType;

        /// <summary>
        /// Kind of attribute, specify Item for collections like list, array or dictionary value, specify Key for dictionary key.
        /// </summary>
        public MySerializeKind Kind = MySerializeKind.Default;

        public SerializeAttribute()
        {
        }

        public SerializeAttribute(MyObjectFlags flags)
        {
            Flags = flags;
        }

        public SerializeAttribute(MyObjectFlags flags, Type dynamicResolverType)
        {
            Flags = flags;
            DynamicSerializerType = dynamicResolverType;
        }

        public SerializeAttribute(MyObjectFlags flags, ushort fixedLength)
        {
            Flags = flags;
            FixedLength = fixedLength;
        }

        public SerializeAttribute(MyPrimitiveFlags flags)
        {
            PrimitiveFlags = flags;
        }

        public SerializeAttribute(MyPrimitiveFlags flags, ushort fixedLength)
        {
            PrimitiveFlags = flags;
            FixedLength = fixedLength;
        }
    }
}
