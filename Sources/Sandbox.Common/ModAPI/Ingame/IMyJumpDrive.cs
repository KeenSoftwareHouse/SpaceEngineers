using System;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyJumpDrive : IMyFunctionalBlock
    {
        bool CanJump { get; }

        bool IsFull { get; }

        /// <summary>
        /// Sets the jump distance ratio. Values passed in are clamped to a range between 0 and 100.
        /// </summary>
        float JumpDistanceRatio { get; set; }

        bool Recharging { get; set; }



        double ComputeMaxDistance();

        bool CanJumpAndHasAccess(long userId);

        void RemoveSelected();

        /// <summary>
        /// Manually sets the target jump point
        /// </summary>
        /// <param name="cords">The cordinates to jump to.</param>
        /// <param name="name">The name of the GPS point to display in the GUI.</param>
        /// <exception cref="ArgumentNullException">The parameter <see cref="name"/> was null.</exception>
        void SetTarget(Vector3D cords, string name);

    }
}