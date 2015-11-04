using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.ModAPI;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipConnector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue(false)]
        public bool ThrowOut = false;

        [ProtoMember, DefaultValue(false)]
        public bool CollectAll = false;

        /// <summary>
        /// When ConnectedEntityId is not null, this tells whether the connection is only approach (yellow) or locked connection (green)
        /// </summary>
        [ProtoMember, DefaultValue(false)]
        public bool Connected = false;

        [ProtoMember, DefaultValue(0)]
        public long ConnectedEntityId = 0;

        public bool ShouldSerializeConnectedEntityId() { return ConnectedEntityId != 0; }

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (ConnectedEntityId != 0) ConnectedEntityId = remapHelper.RemapEntityId(ConnectedEntityId);
        }

        public MyObjectBuilder_ShipConnector()
        {

            DeformationRatio = 0.5f;
        }

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
        }
    }
}
