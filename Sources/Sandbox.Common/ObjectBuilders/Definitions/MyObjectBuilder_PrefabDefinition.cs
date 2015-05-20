using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PrefabDefinition : MyObjectBuilder_DefinitionBase
    {
        // Obsolete
        [ProtoMember]
        public bool RespawnShip;
        public bool ShouldSerializeRespawnShip() { return false; }

        // Obsolete
        [ProtoMember]
        public MyObjectBuilder_CubeGrid CubeGrid;
        public bool ShouldSerializeCubeGrid() { return false; }

        [ProtoMember]
        [XmlArrayItem("CubeGrid")]
        public MyObjectBuilder_CubeGrid[] CubeGrids;

        [ProtoMember, ModdableContentFile(".sbc")]
        public String PrefabPath;
    }
}
