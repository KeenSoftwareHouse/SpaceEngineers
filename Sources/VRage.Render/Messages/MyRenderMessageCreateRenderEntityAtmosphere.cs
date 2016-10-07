using System;
using VRageMath;
using VRageRender.Import;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateRenderEntityAtmosphere : MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public string Model;
        public MatrixD WorldMatrix;
        public MyMeshDrawTechnique Technique;
        public RenderFlags Flags;
        public CullingOptions CullingOptions;
        public float MaxViewDistance;
        public float AtmosphereRadius;
        public float PlanetRadius;
        public Vector3 AtmosphereWavelengths;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateRenderEntityAtmosphere; } }

        // Debug output
        public override string ToString()
        {
            return DebugName ?? String.Empty + ", " + Model ?? String.Empty;
        }
    }
}