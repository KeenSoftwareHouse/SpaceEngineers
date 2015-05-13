using System.ComponentModel;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BatteryBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public float CurrentStoredPower = 0;

        [ProtoMember(2)]
        public bool ProducerEnabled = true;

        [ProtoMember(3)]
        public float MaxStoredPower;

        [ProtoMember(4)]
        public bool SemiautoEnabled = false;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            CurrentStoredPower = 0;
        }
    }
}
