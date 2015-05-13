using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SessionComponent : MyObjectBuilder_Base
    {
        // try not to put anything here, this class is only for type safety
    }
}
