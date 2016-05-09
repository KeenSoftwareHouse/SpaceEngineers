using ProtoBuf;
using VRage.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.ModAPI;
using VRage.Serialization;
using VRage;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ShipConnector : MyObjectBuilder_FunctionalBlock
    {
        public const float DefaultStrength = 0.00015f;

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
        [NoSerialize]
        public bool Connected;

        [ProtoMember, DefaultValue(0)]
        public long ConnectedEntityId = 0;

        /// <summary>
        /// Master is connector with higher EntityId
        /// </summary>
        [ProtoMember]
        public MyPositionAndOrientation? MasterToSlaveTransform;

        [ProtoMember, DefaultValue(DefaultStrength)]
        public float Strength = DefaultStrength;

        [Serialize(MyObjectFlags.Nullable)]
        public MyDeltaTransform? MasterToSlaveGrid;


        public bool? IsMaster;

        public bool ShouldSerializeConnectedEntityId() { return ConnectedEntityId != 0; }
        public bool ShouldSerializeConnected() { return false; }

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
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }
        }
    }
}
