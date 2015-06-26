using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TreeObject : MyObjectBuilder_PhysicalObject
    {
        public override bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
            return false;
        }
    }
}