using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    /// <summary>
    /// A gateway to all the pathfinding tasks and queries
    /// </summary>
    public interface IMyPathfinding
    {
        /// <summary>
        /// Finds a path from the world-space point begin to the given destination shape.
        /// </summary>
        /// <param name="begin">From where to do the pathfinding</param>
        /// <param name="end">The destination shape for this pathfinding query</param>
        /// <param name="relativeEntity">If not null, the begin parameter is not in world space, but in local space relative to this entity</param>
        /// <returns>The found path if any, null otherwise</returns>
        IMyPath FindPathGlobal(Vector3D begin, IMyDestinationShape end, MyEntity relativeEntity);

        /// <summary>
        /// Checks, whether the given destination is reachable, but stops the search after some threshold distance.
        /// </summary>
        /// <param name="begin">Where to start the search from</param>
        /// <param name="end">The destination for the reachability query</param>
        /// <param name="thresholdDistance">The distance after which the query should stop</param>
        /// <returns>True if the destination is reachable, false if it could not be found after the given threshold distance</returns>
        bool ReachableUnderThreshold(Vector3D begin, IMyDestinationShape end, float thresholdDistance);

        /// <summary>
        /// For BW compatibility only. You most certainly don't need to implement this!
        /// </summary>
        IMyPathfindingLog GetPathfindingLog();

        /// <summary>
        /// Recieves periodic updates from the game loop
        /// </summary>
        void Update();

        /// <summary>
        /// Gets called when the MyAIComponent is unloaded
        /// </summary>
        void UnloadData();

        /// <summary>
        /// All the debug draw code should go here
        /// </summary>
        void DebugDraw();
    }
}
