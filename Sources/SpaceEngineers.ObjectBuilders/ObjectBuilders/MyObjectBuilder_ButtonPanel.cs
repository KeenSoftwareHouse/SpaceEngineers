using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Serialization;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using System.Xml.Serialization;
using VRage.Game;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_ButtonPanel : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Toolbar Toolbar;

        [ProtoMember]
        public bool AnyoneCanUse;

        [ProtoMember]
        [NoSerialize]
        public SerializableDictionary<int, String> CustomButtonNames;

        [Serialize]
        [NullableItem, Nullable]
        [XmlIgnore]
#if !XB1 // XB1_NOPROTOBUF
        [ProtoIgnore]
#endif // !XB1
        public Dictionary<int, String> CustomButtonNames_BinarySerialization
        {
            get { return CustomButtonNames == null ? null : CustomButtonNames.Dictionary; }
            set
            {
                if (CustomButtonNames == null)
                    CustomButtonNames = new SerializableDictionary<int, string>();
                CustomButtonNames.Dictionary = value;
            }
        }

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            Toolbar.Remap(remapHelper);
        }
    }
}
