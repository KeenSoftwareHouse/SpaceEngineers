using System.Collections.Generic;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Sector : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public Vector3I Position;

        [ProtoMember(2)]
        [XmlArrayItem("MyObjectBuilder_EntityBase", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_EntityBase>))]
        public List<MyObjectBuilder_EntityBase> SectorObjects { get; set; }

//        [ProtoMember(3)]
//        public MyMwcPositionAndOrientation SpectatorPosition = new MyMwcPositionAndOrientation(Matrix.Identity);
        [ProtoMember(3)]
        public MyObjectBuilder_GlobalEvents SectorEvents;

        [ProtoMember(4)]
        public int AppVersion = 0;

        [ProtoMember(5)]
        public MyObjectBuilder_Encounters Encounters;

        // If not null, this overrides the environment definition settings.
        [ProtoMember(6)]
        public MyObjectBuilder_EnvironmentSettings Environment = null;
        public bool ShouldSerializeEnvironment() { return Environment != null; }

        [ProtoMember(7)]
        public ulong VoxelHandVolumeChanged;
    }
}
