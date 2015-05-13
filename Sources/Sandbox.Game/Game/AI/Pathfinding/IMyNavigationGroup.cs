using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Algorithms;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    // CH: TODO: Some of these methods are unimplemented for higher level groups.
    // Consider splitting the interface into IMyNavigationGroup and IMyLowLevelNavigationGroup or something like thta
    public interface IMyNavigationGroup
    {
        int GetExternalNeighborCount(MyNavigationPrimitive primitive);
        MyNavigationPrimitive GetExternalNeighbor(MyNavigationPrimitive primitive, int index);
        IMyPathEdge<MyNavigationPrimitive> GetExternalEdge(MyNavigationPrimitive primitive, int index);

        void RefinePath(MyPath<MyNavigationPrimitive> path, List<Vector4D> output, ref Vector3 startPoint, ref Vector3 endPoint, int begin, int end);

        Vector3 GlobalToLocal(Vector3D globalPos);
        Vector3D LocalToGlobal(Vector3 localPos);

        MyHighLevelGroup HighLevelGroup { get; }
        MyHighLevelPrimitive GetHighLevelPrimitive(MyNavigationPrimitive myNavigationTriangle);
        IMyHighLevelComponent GetComponent(MyHighLevelPrimitive highLevelPrimitive);
        
        MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, ref double closestDistanceSq);
    }
}
