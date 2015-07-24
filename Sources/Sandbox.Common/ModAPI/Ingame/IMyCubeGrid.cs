using System;
using System.Collections.Generic;
using VRage.ModAPI;
namespace Sandbox.ModAPI.Ingame
{
    /// <summary>
    /// Grid interface
    /// </summary>
    public interface IMyCubeGrid : IMyEntity
    {
        /// <summary>
        /// Grid size in meters
        /// </summary>
        float GridSize { get; }

        /// <summary>
        /// Grid size enum
        /// </summary>
        Sandbox.Common.ObjectBuilders.MyCubeSize GridSizeEnum { get; }

        /// <summary>
        /// Station = static
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Maximum coordinates of blocks in grid
        /// </summary>
        VRageMath.Vector3I Max { get; }

        /// <summary>
        /// Minimum coordinates of blocks in grid
        /// </summary>
        VRageMath.Vector3I Min { get; }

        /// <summary>
        /// Returns true if there is any block occupying given position
        /// </summary>
        bool CubeExists(VRageMath.Vector3I pos);

        /// <summary>
        /// Get cube block at given position
        /// </summary>
        /// <param name="pos">Block position</param>
        /// <returns>Block or null if none is present at given position</returns>
        IMySlimBlock GetCubeBlock(VRageMath.Vector3I pos);

        /// <summary>
        /// Converts grid coordinates to world space
        /// </summary>
        VRageMath.Vector3D GridIntegerToWorld(VRageMath.Vector3I gridCoords);
        
        /// <summary>
        /// Converts world coordinates to grid space cell coordinates
        /// </summary>
        VRageMath.Vector3I WorldToGridInteger(VRageMath.Vector3D coords);

        /// <summary>
        /// Get the center of mass position in world coordinates
        /// </summary>
        VRageMath.Vector3D CenterOfMassWorld { get; }
        /// <summary>
        /// Get the mass in kg
        /// </summary>
        float Mass { get; }
        /// <summary>
        /// The grids linear movement velocity.
        /// </summary>
        VRageMath.Vector3 LinearVelocity { get; }
        /// <summary>
        /// The grids angular movement velocity.
        /// </summary>
        VRageMath.Vector3 AngularVelocity { get; }
        /// <summary>
        /// The grids linear movement acceleration.
        /// </summary>
        VRageMath.Vector3 LinearAcceleration { get; }
        /// <summary>
        /// The grids angular movement acceleration.
        /// </summary>
        VRageMath.Vector3 AngularAcceleration { get; }
        /// <summary>
        /// The grids Moment of Inertia tensor.
        /// </summary>
        VRageMath.Matrix InertiaTensor { get; }
        /// <summary>
        /// The grids inverted Moment of Inertia tensor.
        /// </summary>
        VRageMath.Matrix InverseInertiaTensor { get; }
    }
}
