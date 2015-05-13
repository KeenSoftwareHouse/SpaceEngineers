using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Projector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember(1)]
        public MyObjectBuilder_CubeGrid ProjectedGrid;
        [ProtoMember(2)]
        public Vector3I ProjectionOffset;
        [ProtoMember(3)]
        public Vector3I ProjectionRotation;
        [ProtoMember(4)]
        public bool KeepProjection = false;

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
