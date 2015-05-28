using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_VirtualMass : MyObjectBuilder_FunctionalBlock
    {
        /// <summary>
        ///     Block's virtual mass. Default value is handled separately in MyVirtualMass.Init() method
        /// </summary>
        [ProtoMember]
        public float VirtualMass = -1;
    }
}
