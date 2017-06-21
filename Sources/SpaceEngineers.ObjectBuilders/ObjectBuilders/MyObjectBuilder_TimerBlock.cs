using ProtoBuf;
using System.ComponentModel;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_TimerBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember, DefaultValue(false)]
        public bool JustTriggered = false;

        [ProtoMember]
        public int Delay = 10 * 1000;

        [ProtoMember]
        public int CurrentTime;

        [ProtoMember]
        public bool IsCountingDown;

        [ProtoMember]
        public bool Silent = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
