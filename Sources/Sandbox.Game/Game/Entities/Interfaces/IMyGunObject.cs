using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Interfaces;
using System;
using VRageMath;

namespace Sandbox.Game.Entities
{
    public enum MyGunStatusEnum
    {
        OK = 0,            // Gun is capable of shooting
        Cooldown = 1,      // Gun is cooling down after previous shooting
        OutOfPower = 2,    // Gun does not have enough power to shoot
        NotFunctional = 3, // Gun is damaged beyond functionality
        OutOfAmmo = 4,     // Gun does not have ammo
        Disabled = 5,      // Gun is disabled by player
        Failed = 6,        // Any other reason not given here
        NotSelected = 7,   // No gun was selected, so nothing could shoot
        AccessDenied = 8,   // Shooter has not access to the weapon
    }

    /// <summary>
    /// This can be hand held weapon (including welders and drills) as well as 
    /// weapons on ship (including ship drills).
    /// </summary>
    /// 
    public interface IMyGunObject<out T> where T : MyDeviceBase
    {
        float BackkickForcePerSecond { get; }
        float ShakeAmount { get; }
        MyDefinitionId DefinitionId { get; }
        bool EnabledInWorldRules { get; }
        T GunBase { get; }

		bool IsDeconstructor { get; }

        Vector3 DirectionToTarget(Vector3D target);

        /// <summary>
        /// Should return true if and only if the gun would be able to shoot using the given shoot action.
        /// This method should not do any side-effects such as play sounds or create particle FX.
        /// </summary>
        /// <param name="action">The shooting action to test</param>
        /// <param name="status">Detailed status of the gun, telling why the gun couldn't perform the given shoot action</param>
        /// <returns></returns>
        bool CanShoot(MyShootActionEnum action, long shooter, out MyGunStatusEnum status);

        /// <summary>
        /// Perform the shoot action according to the action parameter.
        /// This method should only be called when CanShoot returns true for the given action!
        /// </summary>
        /// <param name="action">The shooting action to perform</param>
        /// <param name="direction">The prefered direction of shooting</param>
        void Shoot(MyShootActionEnum action, Vector3 direction);
        void EndShoot(MyShootActionEnum action);

        /// <summary>
        /// Should return true when the weapon is shooting projectiles and other classes should react accordingly (i.e.apply backkick force etc.)
        /// </summary>
        bool IsShooting { get; }

        /// <summary>
        /// Zero means that the gun should not update shoot direction at all
        /// </summary>
        /// <returns>Minimal time interval in milliseconds between two direction updates</returns>
        int ShootDirectionUpdateTime { get; }

        /// <summary>
        /// Perform a fail reaction to begin shoot that is shown on all clients (e.g. fail sound, etc.)
        /// </summary>
        /// <param name="action">The shooting action, whose begin shoot failed</param>
        /// <param name="status">Why the begin shoot failed</param>
        void BeginFailReaction(MyShootActionEnum action, MyGunStatusEnum status);
        void BeginFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status);

        void ShootFailReactionLocal(MyShootActionEnum action, MyGunStatusEnum status);

        int GetAmmunitionAmount();

        void OnControlAcquired(MyCharacter owner);
        void OnControlReleased();

        void DrawHud(IMyCameraController camera, long playerId);
    }
}
