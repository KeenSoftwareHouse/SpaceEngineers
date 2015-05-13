using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using System.ComponentModel;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ShipController : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember(1)]
        public bool UseSingleWeaponMode;

        [ProtoMember(2), DefaultValue(true)]
        public bool ControlThrusters = true;

        [ProtoMember(3), DefaultValue(false)]
        public bool ControlWheels = false;

        [ProtoMember(4)]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember(5), DefaultValue(null)]
        public SerializableDefinitionId? SelectedGunId = null;

        [ProtoMember(6)]
        public bool IsMainCockpit = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if(Toolbar != null)
                Toolbar.Remap(remapHelper);
        }
    }
}
