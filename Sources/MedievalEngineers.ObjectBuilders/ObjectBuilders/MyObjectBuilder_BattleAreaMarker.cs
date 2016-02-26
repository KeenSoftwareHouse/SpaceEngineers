using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_BattleAreaMarker : MyObjectBuilder_AreaMarker
    {
		[ProtoMember]
		public uint BattleSlot = 0;

		public override bool IsSynced { get { return true; } }
    }
}
