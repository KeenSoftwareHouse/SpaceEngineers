using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandTool : MyObjectBuilder_EntityBase
    {
		[ProtoMember]
		public bool IsDeconstructor;
    }
}
