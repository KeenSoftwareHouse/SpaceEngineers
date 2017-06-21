using VRage.Voxels;
using VRageMath;
using VRageRender.Messages;

namespace VRageRender.Voxels
{
    public interface IMyClipmapCellHandler
    {
        IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref MatrixD worldMatrix);
        void DeleteCell(IMyClipmapCell cell);

        void AddToScene(IMyClipmapCell cell);
        void RemoveFromScene(IMyClipmapCell cell);

        float GetTime(); //Seconds

        void UpdateMesh(IMyClipmapCell cell, MyRenderMessageUpdateClipmapCell msg);
    }
}
