using ProtoBuf;
using VRage.Game;


namespace VRage.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TreeDefinition : MyObjectBuilder_EnvironmentItemDefinition
    {
        // Distance [m] from tree origin to first log with branches
        [ProtoMember]
        public float BranchesStartHeight = 0.0f;

        [ProtoMember]
        public float HitPoints = 100.0f;

        [ProtoMember]
        public string CutEffect;

        [ProtoMember]
        public string FallSound;

        [ProtoMember]
        public string BreakSound;
    }
}
