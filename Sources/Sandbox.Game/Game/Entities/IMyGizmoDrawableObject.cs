using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public interface IMyGizmoDrawableObject
    {
        Color GetGizmoColor();
        bool CanBeDrawed();
        BoundingBox? GetBoundingBox();
        float GetRadius();
        MatrixD GetWorldMatrix();
        Vector3 GetPositionInGrid();
        bool EnableLongDrawDistance();
    }
}
