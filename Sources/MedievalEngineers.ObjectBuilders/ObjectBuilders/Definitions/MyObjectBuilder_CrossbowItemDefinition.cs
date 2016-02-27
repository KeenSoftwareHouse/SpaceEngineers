using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Medieval.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("MedievalEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_CrossbowItemDefinition : MyObjectBuilder_WeaponItemDefinition
    {
        [ProtoMember]
        public string ModelLoaded;

    }
}
