using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LargeGatlingTurret : MyObjectBuilder_ConveyorTurretBase
    {
    }
}
