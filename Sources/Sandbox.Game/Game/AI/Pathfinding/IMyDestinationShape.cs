using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public interface IMyDestinationShape
    {
        /// <summary>
        /// Should set the relative transform (matrix, relative vector or whatever is needed) from the world position.
        /// This will then allow the shape to be updated using UpdateWorldTransform.
        /// </summary>
        void SetRelativeTransform(MatrixD invWorldTransform);

        /// <summary>
        /// Should update world transform of this shape from the transform previously saved by SetRelativeTransform
        /// </summary>
        void UpdateWorldTransform(MatrixD worldTransform);

        /// <summary>
        /// Should return the distance of the given point from the ideal position in the destination shape.
        /// If the point is outside of the destination shape, PositiveInfinity should be returned.
        /// Tolerance can be provided to allow points that lie further from the shape.
        /// </summary>
        float PointAdmissibility(Vector3D position, float tolerance);

        /// <summary>
        /// Returns the point from the shape that is closest to the query point
        /// </summary>
        Vector3D GetClosestPoint(Vector3D queryPoint);

        /// <summary>
        /// Returns the ideal point towards the pathfinding should steer when starting from the query point.
        /// The difference between GetClosestPoint and this method can be shown on an example of a sphere shape:
        /// For outside points, GetClosestPoint will return points on the sphere's surface, whereas this method will
        /// always return the center of the sphere.
        /// 
        /// Note that for more complicated shapes (e.g. donut), this method returns different point that GetCenter.
        /// </summary>
        Vector3D GetBestPoint(Vector3D queryPoint);

        /// <summary>
        /// Returns center of the shape for pathfinding heuristics
        /// </summary>
        Vector3D GetDestination();

        void DebugDraw();
    }
}
