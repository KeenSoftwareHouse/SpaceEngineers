using VRage.Voxels;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageCreateClipmap : IMyRenderMessage
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

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.CreateClipmap; } }
    }
}
