using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    public enum MyObjectSeedType
    {
        Empty,
        Asteroid,
        AsteroidCluster,
        EncounterAlone,
        EncounterSingle,
        Planet,
        Moon,
    }

    [ProtoContract]
    public class MyObjectSeedParams
    {
        [ProtoMember]
        public int Index = 0;
        [ProtoMember]
        public int Seed = 0;
        [ProtoMember]
        public MyObjectSeedType Type = MyObjectSeedType.Empty;
        [ProtoMember]
        public bool Generated = false;
        [ProtoMember]
        public int m_proxyId = -1;
    }

    [ProtoContract]
    public struct EmptyArea
    {
        [ProtoMember]
        public VRageMath.Vector3D Position;
        [ProtoMember]
        public float Radius;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_WorldGenerator : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public HashSet<EmptyArea> MarkedAreas = new HashSet<EmptyArea>();
        
        [ProtoMember]
        public HashSet<MyObjectSeedParams> ExistingObjectsSeeds = new HashSet<MyObjectSeedParams>();
    }
}