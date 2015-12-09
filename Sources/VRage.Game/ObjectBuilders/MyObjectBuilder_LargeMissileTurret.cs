using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_LargeMissileTurret : MyObjectBuilder_ConveyorTurretBase
    {
        public MyObjectBuilder_LargeMissileTurret()
        {
            TargetCharacters = false;
        }
    }
}
