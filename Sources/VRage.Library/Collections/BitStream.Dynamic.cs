using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Library.Collections
{
    public partial class BitStream
    {
        public Type ReadDynamicType(Type baseType, DynamicSerializerDelegate typeResolver)
        {
            Type type = null;
            typeResolver(this, baseType, ref type);
            return type;
        }

        public void WriteDynamicType(Type baseType, Type obj, DynamicSerializerDelegate typeResolver)
        {
            typeResolver(this, baseType, ref obj);
        }
    }
}
