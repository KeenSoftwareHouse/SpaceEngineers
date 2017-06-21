using ProtoBuf;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Planet : MyObjectBuilder_VoxelMap
    {
        [ProtoContract]
        public struct SavedSector
        {
            [ProtoMember]
            public Vector3S IdPos;

            [ProtoMember]
            public Vector3B IdDir;

            [ProtoMember]
            [XmlElement("Item")]
            [Nullable]
            public HashSet<int> RemovedItems;
        }

        [ProtoMember]
        public float Radius;

        [ProtoMember]
        public bool HasAtmosphere;

        [ProtoMember]
        public float AtmosphereRadius;

        [ProtoMember]
        public float MinimumSurfaceRadius;

        [ProtoMember]
        public float MaximumHillRadius;

        [ProtoMember]
        public Vector3 AtmosphereWavelengths;

        [ProtoMember]
        [XmlArrayItem("Sector")]
        [Nullable]
        public SavedSector[] SavedEnviromentSectors;

        [ProtoMember]
        public float GravityFalloff;

        [ProtoMember]
        public bool MarkAreaEmpty;

        [ProtoMember]
        [Nullable]
        public MyAtmosphereSettings? AtmosphereSettings;

        [ProtoMember]
        public float SurfaceGravity = 1.0f;

        [ProtoMember]
        public bool SpawnsFlora = false;

        [ProtoMember]
        public bool ShowGPS = false;

        [ProtoMember]
        public bool SpherizeWithDistance = true;

        [ProtoMember]
        [Nullable]
        public string PlanetGenerator = "";
    }
}
