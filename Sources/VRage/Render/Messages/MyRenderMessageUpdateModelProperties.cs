using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateModelProperties : IMyRenderMessage
    {
        public uint ID;
        public int LOD;
        public string Model;
        public int MeshIndex;
        public string MaterialName;
        public bool? Enabled;
        public Color? DiffuseColor;
        public float? SpecularPower;
        public float? SpecularIntensity;
        public float? Emissivity;
        public Color? OutlineColor;
        public Matrix? OutlineVolume;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.UpdateModelProperties; } }
    }
}
