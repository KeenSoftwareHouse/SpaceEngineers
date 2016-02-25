using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Sector : MyObjectBuilder_Base
    {
        [ProtoMember]
        public Vector3I Position;

        [ProtoMember]
        [XmlArrayItem("MyObjectBuilder_EntityBase", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        public List<MyObjectBuilder_EntityBase> SectorObjects { get; set; }

//        [ProtoMember]
//        public MyMwcPositionAndOrientation SpectatorPosition = new MyMwcPositionAndOrientation(Matrix.Identity);
        [ProtoMember]
        public MyObjectBuilder_GlobalEvents SectorEvents;

        [ProtoMember]
        public int AppVersion = 0;

        [ProtoMember]
        public MyObjectBuilder_Encounters Encounters;

        // If not null, this overrides the environment definition settings.
        [ProtoMember]
        public MyObjectBuilder_EnvironmentSettings Environment = null;
        public bool ShouldSerializeEnvironment() { return Environment != null; }

        [ProtoMember]
        public ulong VoxelHandVolumeChanged;
    }
}
