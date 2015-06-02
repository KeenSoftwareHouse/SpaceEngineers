using System.ComponentModel;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BatteryBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float CurrentStoredPower = 0;

        [ProtoMember]
        public bool ProducerEnabled = true;

        [ProtoMember]
        public float MaxStoredPower;

        [ProtoMember]
        public bool SemiautoEnabled = false;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            CurrentStoredPower = 0;
        }
    }
}
