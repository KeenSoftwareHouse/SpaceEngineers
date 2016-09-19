using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public interface IMyClipmapCell
    {
        void UpdateMesh(MyRenderMessageUpdateClipmapCell msg);

        void UpdateWorldMatrix(ref MatrixD worldMatrix, bool sortIntoCullObjects);

        void SetDithering(float dithering);

        bool IsValid();
    }  
}
