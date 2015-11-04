using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Diagnostics;
using VRage.Utils;

namespace Sandbox.Game.Entities
{
    public interface IMyControllableEntity : Sandbox.ModAPI.Interfaces.IMyControllableEntity
    {
        MyControllerInfo ControllerInfo { get; }

        new MyEntity Entity { get; }

        float HeadLocalXAngle { get; set; }
        float HeadLocalYAngle { get; set; }

        /// <summary>
        /// This will be called locally to start shooting with the given action
        /// </summary>
        void BeginShoot(MyShootActionEnum action);
        /// <summary>
        /// This will be called locally to start shooting with the given action
        /// </summary>
        void EndShoot(MyShootActionEnum action);

        /// <summary>
        /// This will be called back from the sync object both on local and remote clients
        /// </summary>
        void OnBeginShoot(MyShootActionEnum action);
        /// <summary>
        /// This will be called back from the sync object both on local and remote clients
        /// </summary>
        void OnEndShoot(MyShootActionEnum action);
        void UseFinished();
        void Sprint();

        void SwitchToWeapon(MyDefinitionId weaponDefinition);
		void SwitchToWeapon(MyToolbarItemWeapon weapon);
        bool CanSwitchToWeapon(MyDefinitionId? weaponDefinition);
        void SwitchAmmoMagazine();
        bool CanSwitchAmmoMagazine();

        void SwitchBroadcasting();
        bool EnabledBroadcasting { get; }

        MyToolbarType ToolbarType { get; }

        MyEntityCameraSettings GetCameraEntitySettings();

        MyStringId ControlContext { get; }
    }

    public enum MyShootActionEnum
    {
        PrimaryAction = 0,
        SecondaryAction = 1,
    }

    static class MyControllableEntityExtensions
    {
        public static void SwitchControl(this IMyControllableEntity entity, IMyControllableEntity newControlledEntity)
        {
            Debug.Assert(entity != null, "Entity is null");
            Debug.Assert(entity.ControllerInfo.Controller != null, "Entity is not controlled");

            if (entity.ControllerInfo.Controller != null)
            {
                entity.ControllerInfo.Controller.TakeControl(newControlledEntity);
            }
        }
    }
}
