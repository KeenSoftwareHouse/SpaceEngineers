using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.ObjectBuilders
{
    public class MyObjectBuilderDynamicSerializer : IDynamicResolver
    {
        void IDynamicResolver.Serialize(BitStream stream, Type baseType, ref Type obj)
        {
            MyObjectBuilderSerializer.SerializeDynamic(stream, baseType, ref obj);
        }
    }
}
