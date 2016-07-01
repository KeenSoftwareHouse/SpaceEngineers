using VRage.Game.ModAPI;

namespace Sandbox.ModAPI
{
    public enum BuildCheckResult
    {
        OK,
        NotConnected,
        IntersectedWithGrid,
        IntersectedWithSomethingElse,
        AlreadyBuilt,
        NotFound,
    }

    public interface IMyProjector : IMyFunctionalBlock, Ingame.IMyProjector
    {
        /// <summary>
        /// The grid currently being projected. Will return null if there is no active projection.
        /// </summary>
        IMyCubeGrid ProjectedGrid { get; }

        /// <summary>
        /// Checks if it's possible to build this block.
        /// </summary>
        /// <param name="projectedBlock"></param>
        /// <param name="checkHavokIntersections"></param>
        /// <returns></returns>
        BuildCheckResult CanBuild( IMySlimBlock projectedBlock, bool checkHavokIntersections );

        /// <summary>
        /// Adds the first component to construction stockpile and creates the block.
        /// This doesn't remove materials from inventory on its own.
        /// </summary>
        /// <param name="cubeBlock"></param>
        /// <param name="owner"></param>
        /// <param name="builder"></param>
        /// <param name="requestInstant"></param>
        void Build( IMySlimBlock cubeBlock, long owner, long builder, bool requestInstant );
    }
}