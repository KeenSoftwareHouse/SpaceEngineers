using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;
using VRage.ModAPI;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipController : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public bool UseSingleWeaponMode;

        [ProtoMember, DefaultValue(true)]
        public bool ControlThrusters = true;

        [ProtoMember, DefaultValue(false)]
        public bool ControlWheels = true;

        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId? SelectedGunId = null;

        [ProtoMember]
        public bool IsMainCockpit = false;

		[ProtoMember]
		public bool HorizonIndicatorEnabled = true;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if(Toolbar != null)
                Toolbar.Remap(remapHelper);
        }
    }
}
