using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageCreateLineBasedObject : MyRenderMessageBase
    {
        public uint ID;
        public string DebugName;
        public string ColorMetalTexture;
        public string NormalGlossTexture;
        public string ExtensionTexture;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.CreateLineBasedObject; } }
    }

    public class MyRenderMessageUpdateLineBasedObject : MyRenderMessageBase
    {
        public uint ID;
        public Vector3D WorldPointA;
        public Vector3D WorldPointB;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateLineBasedObject; } }
    }
}
