using ProtoBuf;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Projector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_CubeGrid ProjectedGrid;
        [ProtoMember]
        public Vector3I ProjectionOffset;
        [ProtoMember]
        public Vector3I ProjectionRotation;
        [ProtoMember]
        public bool KeepProjection = false;
        [ProtoMember]
        public bool ShowOnlyBuildable = false;
        [ProtoMember]
        public bool InstantBuildingEnabled = false;
        [ProtoMember]
        public int MaxNumberOfProjections = 5;
        [ProtoMember]
        public int MaxNumberOfBlocks = 200;
        [ProtoMember]
        public int ProjectionsRemaining = 0;
        [ProtoMember]
        public bool GetOwnershipFromProjector = false;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (ProjectedGrid != null)
            {
                ProjectedGrid.Remap(remapHelper);
            }
        }
    }
}
