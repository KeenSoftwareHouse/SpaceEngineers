using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ShipController : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public bool UseSingleWeaponMode;

        [ProtoMember, DefaultValue(true)]
        public bool ControlThrusters = true;

        [ProtoMember, DefaultValue(false)]
        public bool ControlWheels = true;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public SerializableDefinitionId? SelectedGunId = null;

        [ProtoMember]
        public bool IsMainCockpit = false;

		[ProtoMember]
		public bool HorizonIndicatorEnabled = true;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Toolbar BuildToolbar;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if(Toolbar != null)
                Toolbar.Remap(remapHelper);
        }
    }
}
