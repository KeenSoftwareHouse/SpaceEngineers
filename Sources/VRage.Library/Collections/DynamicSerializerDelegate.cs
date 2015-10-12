using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;

namespace VRage.Library.Collections
{
    /// <summary>
    /// Dynamic object serializer, when writing, serializes object type.
    /// When reading deserializes object type and creates new instance.
    /// </summary>
    /// <param name="stream">Stream to read from or write to.</param>
    /// <param name="baseType">Hierarchy base type of dynamically serialized object, can be used to select serialization method (e.g. object builders have something special).</param>
    /// <param name="obj">Object whose type to write or read and instantiate.</param>
    public delegate void DynamicSerializerDelegate(BitStream stream, Type baseType, ref Type obj);
}
