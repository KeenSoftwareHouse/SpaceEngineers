using ProtoBuf;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders
{    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipGrinder : MyObjectBuilder_ShipToolBase
    {
    }
}
