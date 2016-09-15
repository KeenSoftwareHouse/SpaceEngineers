using VRage.Library.Utils;
using VRageMath;

namespace VRageRender.Messages
{
    public class MyRenderMessageSetCameraViewMatrix : MyRenderMessageBase
    {
        public MatrixD ViewMatrix;
        public Matrix ProjectionMatrix;
        public float FOV;
        // public float NearFOV; // not used anymore
        // public Matrix NearProjectionMatrix; // not used anymore
        public float SafeNear;
        public float NearPlane;
        public float FarPlane;
        public float NearObjectsNearPlane;
        public float NearObjectsFarPlane;
        public Vector3D CameraPosition;
        public int LastMomentUpdateIndex = 0;

        public MyTimeSpan UpdateTime;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeEvery; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetCameraViewMatrix; } }
    }
}
