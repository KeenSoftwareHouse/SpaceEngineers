using ProtoBuf;
using System.ComponentModel;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_AirVent : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsDepressurizing;

        [ProtoMember, DefaultValue(null)]
        [Nullable, DynamicObjectBuilder]
        public MyObjectBuilder_ToolbarItem OnEmptyAction = null;

        [ProtoMember, DefaultValue(null)]
        [Nullable, DynamicObjectBuilder]
        public MyObjectBuilder_ToolbarItem OnFullAction = null;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);

            if (OnEmptyAction != null)
            {
                OnEmptyAction.Remap(remapHelper);
            }

            if (OnFullAction != null)
            {
                OnFullAction.Remap(remapHelper);
            }
        }
    }
}
