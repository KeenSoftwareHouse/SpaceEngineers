using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyPhysics
    {
        /// <summary>
        /// Total grid mass, in kg, including inventory. Does not include subgrids
        /// </summary>
        double Mass { get; }

        /// <summary>
        /// Center of mass, in local coordinates. Does not take into account subgrids
        /// </summary>
        Vector3D CenterOfMassLocal { get; }

        /// <summary>
        /// Center of mass, in world coordinates. Does not take into account subgrids
        /// </summary>
        Vector3D CenterOfMassWorld { get; }

        /// <summary>
        /// Moment of inertia, in kg * m^2, relative to the object. Does not take into account subgrids
        /// </summary>
        Vector3D MomentOfInertiaLocal { get; }

        /// <summary>
        /// Moment of inertia, in kg * m^2, relative to the world. Does not take into account subgrids
        /// </summary>
        Vector3D MomentOfInertiaWorld { get; }

        /// <summary>
        /// Linear velocity, relative to the object, in m/s
        /// </summary>
        Vector3D LinearVelocityLocal { get; }

        /// <summary>
        /// Linear velocity, relative to the world, in m/s
        /// </summary>
        Vector3D LinearVelocityWorld { get; }

        /// <summary>
        /// Angular velocity, relative to the object, in rad/s
        /// </summary>
        Vector3D AngularVelocityLocal { get; }

        /// <summary>
        /// Angular velocity, relative to the world, in rad/s
        /// </summary>
        Vector3D AngularVelocityWorld { get; }

        /// <summary>
        /// Linear acceleration, relative to the object, in m/s^2
        /// </summary>
        Vector3D LinearAccelerationLocal { get; }

        /// <summary>
        /// Linear acceleration, relative to the world, in m/s^2
        /// </summary>
        Vector3D LinearAccelerationWorld { get; }

        /// <summary>
        /// Angular acceleration, relative to the object, in rad/s^2
        /// </summary>
        Vector3D AngularAccelerationLocal { get; }

        /// <summary>
        /// Angular acceleration, relative to the world, in rad/s^2
        /// </summary>
        Vector3D AngularAccelerationWorld { get; }
    }
}
