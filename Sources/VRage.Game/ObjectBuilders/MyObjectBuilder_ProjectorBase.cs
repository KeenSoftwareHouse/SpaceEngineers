using ProtoBuf;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageMath;

namespace VRage.Game
{
    // Used in space as projectors but also using this in medieval for projectors as fundation block for blueprint building.
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_ProjectorBase : MyObjectBuilder_FunctionalBlock
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
