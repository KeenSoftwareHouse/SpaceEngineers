using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BlockItem : MyObjectBuilder_PhysicalObject
    {
        [ProtoMember]
        public SerializableDefinitionId BlockDefId;

        public override bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
            return false;
        }

        public override bool CanStack(MyObjectBuilderType typeId, MyStringHash subtypeId, MyItemFlags flags)
        {
            return false;
        }

        public override Sandbox.Definitions.MyDefinitionId GetObjectId()
        {
            return BlockDefId;
        }
    }
}
