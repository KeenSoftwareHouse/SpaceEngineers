using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;
using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_BatteryBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float CurrentStoredPower = -1;

        [ProtoMember]
        public bool ProducerEnabled = true;

        [ProtoMember]
        public float MaxStoredPower;

        [ProtoMember]
        public bool SemiautoEnabled;

        [ProtoMember]
        public bool OnlyDischargeEnabled;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            CurrentStoredPower = 0;
        }
    }
}
