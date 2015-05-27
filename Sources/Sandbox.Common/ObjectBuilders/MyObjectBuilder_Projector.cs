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
        [ProtoMember]
        public MyObjectBuilder_CubeGrid ProjectedGrid;
        [ProtoMember]
        public Vector3I ProjectionOffset;
        [ProtoMember]
        public Vector3I ProjectionRotation;
        [ProtoMember]
        public bool KeepProjection = false;
        [ProtoMember]
        public float BuildableTransparency = 0.0f;
        [ProtoMember]
        public float PendingTransparency = 0.0f;


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
