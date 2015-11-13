using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_RadioAntenna : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float BroadcastRadius;
        [ProtoMember]
        public bool ShowShipName;
        [ProtoMember]
        public bool EnableBroadcasting = true;
        
        // Nearby Antenna Patch
        [ProtoMember,DefaultValue(false)]
        public bool DataTransferEnabled = false;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string[] PendingDataPacks = null;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public long[] PendingSenderIds = null;
    }
}
