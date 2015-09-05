using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AreaMarker : MyObjectBuilder_EntityBase
    {
		public virtual bool IsSynced { get { return false; } }
    }
}
