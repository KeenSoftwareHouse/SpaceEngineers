using System;
using VRageMath;

namespace Sandbox.ModAPI.Ingame
{
    public interface IMyJumpDrive : IMyFunctionalBlock
    {
        /// <summary>
        /// Is the jumpdrive in a state that would allow a jump.
        /// </summary>
        bool CanJump { get; }

        /// <summary>
        /// Is the jumpdrive full of energy.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Is the jumpdrive currently performing a jump.
        /// </summary>
        bool IsJumping { get; }

        /// <summary>
        /// Gets the jump distance ratio.
        /// </summary>
        float JumpDistanceRatio { get; }

        /// <summary>
        /// Attempts to set the <see cref="JumpDistanceRatio"/>. Values passed in are clamped to a range between 0 and 100.
        /// </summary>
        /// <param name="ratio">The new value to set.</param>
        void RequestJumpDistanceRatio(float ratio);

        /// <summary>
        /// The amount of power that needs to be stored to perform a jump in Megawatts.
        /// </summary>
        float PowerNeededForJump { get; }

        /// <summary>
        /// The amount of power stored in the jumpdrive in Megawatts.
        /// </summary>
        float StoredPower { get; }

        /// <summary>
        /// The number of seconds remaining in the recharge time.
        /// </summary>
        float RechargeTimeRemaining { get; }

        /// <summary>
        /// Should the jump drive draw power to recharge itself.
        /// </summary>
        bool Recharging { get; }

        /// <summary>
        /// Tries to change the value of <see cref="Recharging"/>.
        /// </summary>
        /// <param name="enabled">The new value to set.</param>
        void RequestRecharging(bool enabled);

        /// <summary>
        /// Returns the maximum distance in meters that the jumpdrive will jump with the current <see cref="JumpDistanceRatio"/>.
        /// </summary>
        double ComputeMaxDistance();

        /// <summary>
        /// Returns if the drive is ready to jump and could be jumped by the specified user.
        /// </summary>
        /// <param name="userId">The userId to check against.</param>
        bool CanJumpAndHasAccess(long userId);

        /// <summary>
        /// Deselects the jump waypoint if one was set.
        /// </summary>
        void RemoveSelected();

        /// <summary>
        /// Manually sets the target jump point.
        /// </summary>
        /// <param name="cords">The coordinates to jump to.</param>
        /// <param name="name">The name of the GPS point to display in the GUI.</param>
        /// <exception cref="ArgumentNullException">The parameter <see cref="name"/> was null.</exception>
        void SetTarget(Vector3D cords, string name);

        /// <summary>
        /// Performs a jump, can only be done my NPC factions.
        /// </summary>
        /// <returns>If the jump request was successful.</returns>
        bool PerformJump();
    }
}