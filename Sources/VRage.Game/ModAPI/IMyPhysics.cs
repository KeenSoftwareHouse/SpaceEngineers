using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRageMath;

namespace VRage.Game.ModAPI
{
    public interface IHitInfo
    {
        Vector3D Position { get; }
        IMyEntity HitEntity { get; }
    }

    public interface IMyPhysics
    {
        /// <summary>
        /// Number of physics steps done in last second
        /// </summary>
        int StepsLastSecond { get; }

        /// <summary>
        /// Simulation ratio, when physics cannot keep up, this is smaller than 1
        /// </summary>
        float SimulationRatio { get; }

        /// <summary>
        /// Finds closest or any object on the path of the ray from->to. Uses Storage for voxels for faster 
        /// search but only good for long rays (more or less more than 50m). Use it only in such cases.
        /// </summary>
        /// <param name="from">Start of the ray.</param>
        /// <param name="to">End of the ray.</param>
        /// <param name="hitInfo">Hit info</param>
        /// <param name="any">Indicates if method should return any object found (May not be closest)</param>
        /// <returns>true if hit, false if no hit</returns>
        bool CastLongRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, bool any);

        /// <summary>
        /// Cast a ray and returns all matching entities.
        /// </summary>
        /// <param name="from">Start of ray.</param>
        /// <param name="to">End of ray.</param>
        /// <param name="toList">List of hits</param>
        /// <param name="raycastFilterLayer">Collision filter.</param>
        void CastRay(Vector3D from, Vector3D to, List<IHitInfo> toList, int raycastFilterLayer = 0);

        /// <summary>
        /// Cast a ray and return first entity.
        /// </summary>
        /// <param name="from">Start of ray.</param>
        /// <param name="to">End of ray.</param>
        /// <param name="hitInfo">Hit info</param>
        /// <param name="raycastFilterLayer">Collision filter.</param>
        /// <returns>true if hit; false if no hit</returns>
        bool CastRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, int raycastFilterLayer = 0);

        /// <summary>
        /// Cast a ray and return first entity.
        /// </summary>
        /// <param name="from">Start of ray.</param>
        /// <param name="to">End of ray.</param>
        /// <param name="hitInfo">Hit info</param>
        /// <param name="raycastCollisionFilter">Collision filter.</param>
        /// <returns>true if hit; false if no hit</returns>
        bool CastRay(Vector3D from, Vector3D to, out IHitInfo hitInfo, uint raycastCollisionFilter, bool ignoreConvexShape);

        /// <summary>
        /// Ensure aabb is inside only one subspace. If no, reorder.
        /// </summary>
        /// <param name="aabb"></param>
        void EnsurePhysicsSpace(BoundingBoxD aabb);

        /// <summary>
        /// Given a string, gets the numeric value for the collision layer. Default: 0.
        /// </summary>
        /// <param name="strLayer">Name of collision layer. See MyPhysics.CollisionLayers for valid names.</param>
        /// <returns>Numeric value from MyPhysics.CollisionLayers</returns>
        /// <remarks>Default string used if not valid: DefaultCollisionLayer</remarks>
        int GetCollisionLayer(string strLayer);
    }
}
