using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BattleAreaMarker : MyObjectBuilder_AreaMarker
    {
		[ProtoMember]
		public uint BattleSlot = 0;

		public override bool IsSynced { get { return true; } }
    }
}
