
namespace VRageRender
{
    public struct MyBillboardViewProjection
    {
        public VRageMath.MatrixD View;
        public VRageMath.Matrix ViewAtZero;
        public VRageMath.Matrix Projection;
        public MyViewport Viewport;
        public VRageMath.Vector3D CameraPosition;
        public bool DepthRead;
    }
}
