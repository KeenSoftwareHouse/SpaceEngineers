using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    public interface IMyHandheldGunObject<out T> : IMyGunObject<T> where T : MyDeviceBase
    {
        MyObjectBuilder_PhysicalGunObject PhysicalObject { get; }
        MyPhysicalItemDefinition PhysicalItemDefinition { get; }

        // Use animation directly without applying inverse kinematics.
        bool ForceAnimationInsteadOfIK { get; }

        bool IsBlocking { get; }

        int CurrentAmmunition { set; get; }
        int CurrentMagazineAmmunition { set; get; }
    }

    public interface IStoppableAttackingTool
    {
        void StopShooting(MyEntity attacker);
    }
}