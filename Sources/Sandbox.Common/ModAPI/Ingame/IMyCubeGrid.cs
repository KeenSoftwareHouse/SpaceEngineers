using System;
using System.Collections.Generic;
using VRage.Game;
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
        MyCubeSize GridSizeEnum { get; }

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
        /// Total grid mass, in kg, including inventory. Does not include subgrids
        /// </summary>
        double Mass { get; }

        /// <summary>
        /// Center of mass, relative to the grid. Does not take into account subgrids
        /// </summary>
        VRageMath.Vector3D CenterOfMassGrid { get; }

        /// <summary>
        /// Center of mass, in world coordinates. Does not take into account subgrids
        /// </summary>
        VRageMath.Vector3D CenterOfMassWorld { get; }

        /// <summary>
        /// Moment of inertia, in kg * m^2, relative to the grid. Does not take into account subgrids
        /// </summary>
        VRageMath.Vector3D MomentOfInertiaGrid { get; }

        /// <summary>
        /// Moment of inertia, in kg * m^2, relative to the world. Does not take into account subgrids
        /// </summary>
        VRageMath.Vector3D MomentOfInertiaWorld { get; }

        /// <summary>
        /// Linear velocity, relative to the grid, in m/s
        /// </summary>
        VRageMath.Vector3D LinearVelocityGrid { get; }

        /// <summary>
        /// Linear velocity, relative to the world, in m/s
        /// </summary>
        VRageMath.Vector3D LinearVelocityWorld { get; }

        /// <summary>
        /// Angular velocity, relative to the grid, in rad/s
        /// </summary>
        VRageMath.Vector3D AngularVelocityGrid { get; }

        /// <summary>
        /// Angular velocity, relative to the world, in rad/s
        /// </summary>
        VRageMath.Vector3D AngularVelocityWorld { get; }

        /// <summary>
        /// Linear acceleration, relative to the grid, in m/s^2
        /// </summary>
        VRageMath.Vector3D LinearAccelerationGrid { get; }

        /// <summary>
        /// Linear acceleration, relative to the world, in m/s^2
        /// </summary>
        VRageMath.Vector3D LinearAccelerationWorld { get; }

        /// <summary>
        /// Angular acceleration, relative to the grid, in rad/s^2
        /// </summary>
        VRageMath.Vector3D AngularAccelerationGrid { get; }
        
        /// <summary>
        /// Angular acceleration, relative to the world, in rad/s^2
        /// </summary>
        VRageMath.Vector3D AngularAccelerationWorld { get; }
    }
}
