using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    /// <summary>
    /// An interface for describing a path for an agent
    /// It can be used by the bot navigation to steer the bot along a path, regardless of the underlying pathfinding implementation.
    /// This is what you should return from a pathfinding system to abstract away the implementation details of that system.
    /// </summary>
    public interface IMyPath
    {
        /// <summary>
        /// Current destination (goal) for the path 
        /// </summary>
        IMyDestinationShape Destination { get; }

        /// <summary>
        /// Entity towards which the path is going
        /// </summary>
        IMyEntity EndEntity { get; }

        /// <summary>
        /// Says, whether the path is in a valid and consistent state.
        /// As an example, a path can be invalid when no path could be found or when the underlying data structues changed while using the path
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Will be true when the previously set path is successfully completed
        /// </summary>
        bool PathCompleted { get; }

        /// <summary>
        /// Will be called on the path if it should be invalidated for any reason.
        /// Also, it the path is no longer needed, it should be invalidated to prevent holding references etc.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Returns the next target along the path, provided that the agent is currently on the given position
        /// </summary>
        /// <param name="position">Current position of the agent</param>
        /// <param name="target">Next target for the agent to go to</param>
        /// <param name="targetRadius">Destination radius allowing some tolerance for steering</param>
        /// <param name="relativeEntity">If the target is relative to an entity, the entity will be returned in this argument (currently unused).</param>
        /// <returns>True if the next target was successfully found</returns>
        bool GetNextTarget(Vector3D position, out Vector3D target, out float targetRadius, out IMyEntity relativeEntity);

        /// <summary>
        /// Reinitializes the path, starting from the given position. The target stays the same.
        /// The purpose of this is to try to move towards the goal again if PathCompleted returns true, but the target moved while going towards it.
        /// </summary>
        /// <param name="position">The current position from which we want the pathfinding to be performed</param>
        void Reinit(Vector3D position);

        /// <summary>
        /// Gets called every frame for the purposes of debug draw
        /// </summary>
        void DebugDraw();
    }
}
