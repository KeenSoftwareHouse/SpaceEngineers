using VRage.Voxels;
using VRageMath;
using VRageRender.Voxels;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateClipmap : MyRenderMessageBase
    {
        public uint ClipmapId;
        public MatrixD WorldMatrix;
        public Vector3I SizeLod0;
        public float AtmosphereRadius;
        public float PlanetRadius;
        public bool HasAtmosphere;
        public Vector3D Position;
        public MyClipmapScaleEnum ScaleGroup;
        public Vector3? AtmosphereWaveLenghts;
        public bool SpherizeWithDistance;
		public RenderFlags AdditionalRenderFlags;
        public MyClipmap.PruningFunc PrunningFunc;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateClipmap; } }

        public override void Close()
        {
            base.Close();
            ClipmapId = uint.MaxValue;
            AtmosphereWaveLenghts = null;
            PrunningFunc = null;
        }
    }
}
