using VRageMath;
using VRageRender;

namespace VRage.Voxels
{
    public interface IMyClipmapCellHandler
    {
        IMyClipmapCell CreateCell(MyClipmapScaleEnum scaleGroup, MyCellCoord cellCoord, ref MatrixD worldMatrix);
        void DeleteCell(IMyClipmapCell cell);

        void AddToScene(IMyClipmapCell cell);
        void RemoveFromScene(IMyClipmapCell cell);

        void AddToMergeBatch(IMyClipmapCell cell);

        float GetTime(); //Seconds

        void UpdateMesh(IMyClipmapCell cell, MyRenderMessageUpdateClipmapCell msg);
        void UpdateMerging();

        void DebugDrawMergedCells();
    }
}
