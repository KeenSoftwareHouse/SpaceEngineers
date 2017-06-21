using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Serialization
{
    [Flags]
    public enum MyObjectFlags
    {
        /// <summary>
        /// No serialization flags
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Do not serialize member when it has default value, null for objects, zeros for structs.
        /// In binary streams, bit is written to indicate whether object had a value or not.
        /// </summary>
        DefaultZero = 0x1,

        /// <summary>
        /// Alias to default value.
        /// </summary>
        Nullable = 0x1,

        /// <summary>
        /// Member can store subclasses of specified type, actual member type will be serialized as well.
        /// Valid only on class members (not for value types).
        /// </summary>
        Dynamic = 0x2,

        /// <summary>
        /// Applies only to collections.
        /// When serializing empty collection (zero element count) it will behave like DefaultValue.
        /// </summary>
        DefaultValueOrEmpty = 0x4,

        /// <summary>
        /// Same as dynamic, but stores a bit indicating whether serialized type is different from member type or not.
        /// When it's same, type is not serialized. Usefull when some instances have default type.
        /// </summary>
        DynamicDefault = 0x8,
    }
}
