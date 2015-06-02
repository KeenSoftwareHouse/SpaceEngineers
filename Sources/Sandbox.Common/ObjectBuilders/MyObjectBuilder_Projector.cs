using ProtoBuf;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Projector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_CubeGrid ProjectedGrid;
        [ProtoMember]
        public Vector3I ProjectionOffset;
        [ProtoMember]
        public Vector3I ProjectionRotation;
        [ProtoMember]
        public bool KeepProjection = false;
        [ProtoMember]
        public bool ShowOnlyBuildable = false;

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
