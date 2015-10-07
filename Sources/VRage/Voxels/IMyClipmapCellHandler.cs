using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Utils;
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
    }
}
