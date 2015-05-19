using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;


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
        public bool ControlWheels = false;

        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember, DefaultValue(null)]
        public SerializableDefinitionId? SelectedGunId = null;

        [ProtoMember]
        public bool IsMainCockpit = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if(Toolbar != null)
                Toolbar.Remap(remapHelper);
        }
    }
}
