using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Voxels
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Planet : MyObjectBuilder_VoxelMap
    {
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
        public float MaximumOxygen;

        [ProtoMember]
        public List<Vector3I> SavedEnviromentSectors;

    }
}
