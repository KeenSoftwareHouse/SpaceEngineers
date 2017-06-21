using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace SpaceEngineers.Game.ModAPI.Ingame
{
    public interface IMyGravityGeneratorBase : IMyFunctionalBlock
    {
        /// <summary>
        /// Gets or sets the gravity acceleration in Gs.
        /// </summary>
        float Gravity { get; set; }

        /// <summary>
        /// Gets or sets the gravity acceleration in m/s.
        /// </summary>
        float GravityAcceleration { get; set; }

        /// <summary>
        /// Tests if the specified point is within the gravity of this entity.
        /// </summary>
        /// <param name="worldPoint">Point to test</param>
        /// <returns><b>true</b> if in range; <b>false</b> if not</returns>
        bool IsPositionInRange(Vector3D worldPoint);

        /// <summary>
        /// Gets the gravity vector at the specified point.
        /// </summary>
        /// <param name="worldPoint">World position to retrieve gravity for.</param>
        /// <returns>Direction of gravity starting at specified point.</returns>
        Vector3 GetWorldGravity(Vector3D worldPoint);
    }
}
