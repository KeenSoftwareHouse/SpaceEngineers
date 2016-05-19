using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyProjector : IMyFunctionalBlock
    {
        [Obsolete( "Use ProjectionOffset vector instead." )]
        int ProjectionOffsetX { get; }

        [Obsolete( "Use ProjectionOffset vector instead." )]
        int ProjectionOffsetY { get; }

        [Obsolete( "Use ProjectionOffset vector instead." )]
        int ProjectionOffsetZ { get; }

        [Obsolete( "Use ProjectionRotation vector instead." )]
        int ProjectionRotX { get; }

        [Obsolete( "Use ProjectionRotation vector instead." )]
        int ProjectionRotY { get; }

        [Obsolete( "Use ProjectionRotation vector instead." )]
        int ProjectionRotZ { get; }

        /// <summary>
        /// Checks if there is an active projection
        /// </summary>
        bool IsProjecting { get; }

        /// <summary>
        /// Total number of blocks in the projection
        /// </summary>
        int TotalBlocks { get; }

        /// <summary>
        /// Number of blocks left to be welded
        /// </summary>
        int RemainingBlocks { get; }

        /// <summary>
        /// A comprehensive list of blocks left to be welded
        /// </summary>
        Dictionary<MyDefinitionBase, int> RemainingBlocksPerType { get; }

        /// <summary>
        /// Number of armor blocks left to be welded
        /// </summary>
        int RemainingArmorBlocks { get; }

        /// <summary>
        /// Count of blocks which can be welded now
        /// </summary>
        int BuildableBlocksCount { get; }

        Vector3I ProjectionOffset { get; set; }

        /// <summary>
        /// These values are not in degrees. 1 = 90 degrees, 2 = 180 degrees
        /// </summary>
        Vector3I ProjectionRotation { get; set; }

        /// <summary>
        /// Call this after setting ProjectionOffset and ProjectionRotation to update the projection
        /// </summary>
        void UpdateOffsetAndRotation();

        bool LoadRandomBlueprint( string searchPattern );
        bool LoadBlueprint( string name );
    }
}
