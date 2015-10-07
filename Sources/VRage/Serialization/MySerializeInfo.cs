using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Serialization
{
    public class MySerializeInfo
    {
        public static readonly MySerializeInfo Default = new MySerializeInfo();

        public readonly MyObjectFlags Flags;
        public readonly MyPrimitiveFlags PrimitiveFlags;
        public readonly ushort FixedLength;
        public readonly DynamicSerializerDelegate DynamicSerializer;

        /// <summary>
        /// Serialization settings for dictionar key.
        /// </summary>
        public readonly MySerializeInfo KeyInfo;

        /// <summary>
        /// Serialization settings for dictionary value or collection / array elements
        /// </summary>
        public readonly MySerializeInfo ItemInfo;

        public bool IsNullable { get { return (Flags & MyObjectFlags.DefaultZero) != 0 || IsNullOrEmpty; } }
        public bool IsDynamic { get { return (Flags & MyObjectFlags.Dynamic) != 0 || IsDynamicDefault; } }
        public bool IsNullOrEmpty { get { return (Flags & MyObjectFlags.DefaultValueOrEmpty) != 0; } }
        public bool IsDynamicDefault { get { return (Flags & MyObjectFlags.DynamicDefault) != 0; } }

        public bool IsSigned { get { return (PrimitiveFlags & MyPrimitiveFlags.Signed) != 0; } }
        public bool IsNormalized { get { return (PrimitiveFlags & MyPrimitiveFlags.Normalized) != 0; } }
        public bool IsVariant { get { return !IsSigned && (PrimitiveFlags & MyPrimitiveFlags.Variant) != 0; } }
        public bool IsVariantSigned { get { return (PrimitiveFlags & MyPrimitiveFlags.VariantSigned) != 0; } }
        public bool IsFixed8 { get { return (PrimitiveFlags & MyPrimitiveFlags.FixedPoint8) != 0; } }
        public bool IsFixed16 { get { return (PrimitiveFlags & MyPrimitiveFlags.FixedPoint16) != 0; } }

        public Encoding Encoding
        {
            get { return (PrimitiveFlags & MyPrimitiveFlags.Ascii) != 0 ? Encoding.ASCII : Encoding.UTF8; }
        }

        private MySerializeInfo()
        {
        }

        public MySerializeInfo(MyObjectFlags flags, MyPrimitiveFlags primitiveFlags, ushort fixedLength, DynamicSerializerDelegate dynamicSerializer, MySerializeInfo keyInfo, MySerializeInfo itemInfo)
        {
            Flags = flags;
            PrimitiveFlags = primitiveFlags;
            FixedLength = fixedLength;
            KeyInfo = keyInfo;
            ItemInfo = itemInfo;
            DynamicSerializer = dynamicSerializer;
        }

        public MySerializeInfo(SerializeAttribute attribute, MySerializeInfo keyInfo, MySerializeInfo itemInfo)
        {
            if (attribute != null)
            {
                Flags = attribute.Flags;
                PrimitiveFlags = attribute.PrimitiveFlags;
                FixedLength = attribute.FixedLength;
                if (IsDynamic)
                {
                    Debug.Assert(attribute.DynamicSerializerType != null, "DynamicSerializerType must be set when serializing dynamically!");
                    DynamicSerializer = ((IDynamicResolver)Activator.CreateInstance(attribute.DynamicSerializerType)).Serialize;
                }
            }
            KeyInfo = keyInfo;
            ItemInfo = itemInfo;
        }

        public static MySerializeInfo Create(ICustomAttributeProvider reflectionInfo)
        {
            SerializeAttribute def = new SerializeAttribute();
            SerializeAttribute key = null;
            SerializeAttribute item = null;

            foreach (SerializeAttribute attr in reflectionInfo.GetCustomAttributes(typeof(SerializeAttribute), false))
            {
                if (attr.Kind == MySerializeKind.Default)
                    def = Merge(def, attr);
                else if (attr.Kind == MySerializeKind.Key)
                    key = Merge(key, attr);
                else if (attr.Kind == MySerializeKind.Item)
                    item = Merge(item, attr);
            }
            return new MySerializeInfo(def, ToInfo(key), ToInfo(item));
        }
        
        public static MySerializeInfo CreateForParameter(ParameterInfo[] parameters, int index)
        {
            if (index >= parameters.Length)
                return MySerializeInfo.Default;
            return Create(parameters[index]);
        }

        static SerializeAttribute Merge(SerializeAttribute first, SerializeAttribute second)
        {
            if (first == null) return second;
            else if (second == null) return first;
            Debug.Assert(first.FixedLength == second.FixedLength || first.FixedLength == 0 || second.FixedLength == 0, "Two instances of SerializeAttribute sets different non-zero fixed length!");
            Debug.Assert(first.DynamicSerializerType == second.DynamicSerializerType || first.DynamicSerializerType == null || second.DynamicSerializerType == null, "Two instances of SerializeAttribute sets different non-null DynamicSerializerType");

            SerializeAttribute result = new SerializeAttribute();
            result.Flags = first.Flags | second.Flags;
            result.PrimitiveFlags = first.PrimitiveFlags | second.PrimitiveFlags;
            result.FixedLength = first.FixedLength != 0 ? first.FixedLength : second.FixedLength;
            result.DynamicSerializerType = first.DynamicSerializerType ?? second.DynamicSerializerType;
            return result;
        }

        static MySerializeInfo ToInfo(SerializeAttribute attr)
        {
            return attr != null ? new MySerializeInfo(attr, null, null) : null;
        }
    }
}
