using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Weapons;
using System.Diagnostics;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemWeapon))]
    public class MyToolbarItemWeapon : MyToolbarItemDefinition
    {
        private int m_lastAmmoCount = -1;
        private bool m_needsWeaponSwitching = true;

		private bool m_isDeconstructor = false;
		public bool IsDeconstructor { get { return m_isDeconstructor; } }

        public MyToolbarItemWeapon() : base()
        {
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool init = base.Init(data);
            ActivateOnClick = false;

			var objectBuilder = data as MyObjectBuilder_ToolbarItemWeapon;
			if (objectBuilder != null)
			{
				m_isDeconstructor = objectBuilder.IsDeconstructor;
				if (m_isDeconstructor)
				{
					SetDisplayName(DisplayName.Append(" Deconstructor").ToString());
					var split = Icon.Split(new char[] { '_' });	// MK: TODO: Change icon properly.
					SetIcon(split[0] + "_Deconstruction.dds");
				}
			}

            return init;
        }

		public override bool Equals(object obj)
		{
			bool returnValue = base.Equals(obj);

			if(returnValue)
			{
				var otherObj = obj as MyToolbarItemWeapon;
				if (otherObj != null && otherObj.m_isDeconstructor != m_isDeconstructor)
					returnValue = false;
			}
			return returnValue;
		}

		public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
		{
			var builder = (MyObjectBuilder_ToolbarItemWeapon)base.GetObjectBuilder();
			builder.IsDeconstructor = m_isDeconstructor;

			return builder;
		}

        public override bool Activate()
        {
            if (Definition == null)
                return false;

            if (MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS &&
                (Definition.Id.TypeId == typeof(MyObjectBuilder_PhysicalGunObject) && MyFakes.OCTOBER_RELEASE_DISABLED_HANDHELD_WEAPONS.Contains(Definition.Id.SubtypeName)))
            {
                MyHud.Notifications.Add(MyNotificationSingletons.DisabledWeaponsAndTools);
                return false;
            }
            
            var controlledObject = MySession.ControlledEntity as IMyControllableEntity;
            if (controlledObject != null)
            {
                if (m_needsWeaponSwitching)
                {
                    controlledObject.SwitchToWeapon(this);
                    WantsToBeActivated = false;
                }
                else
                {
                    controlledObject.SwitchAmmoMagazine();
                }
            }

            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return true;
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            bool thisWeaponIsCurrent = false;
            bool shipHasThisWeapon = false;
            var character = MySession.LocalCharacter;
            bool characterHasThisWeapon = character != null && (character.GetInventory().ContainItems(1, Definition.Id) || !character.WeaponTakesBuilderFromInventory(Definition.Id));
            ChangeInfo changed = ChangeInfo.None;

            if (characterHasThisWeapon)
            {
                var currentWeapon = character.CurrentWeapon;
                if (currentWeapon != null)
                    thisWeaponIsCurrent = (MyDefinitionManager.Static.GetPhysicalItemForHandItem(currentWeapon.DefinitionId).Id == Definition.Id) && (currentWeapon.IsDeconstructor == m_isDeconstructor);
                if (thisWeaponIsCurrent && currentWeapon is MyAutomaticRifleGun)
                {
                    int amount = character.CurrentWeapon.GetAmmunitionAmount();
                    if (m_lastAmmoCount != amount)
                    {
                        m_lastAmmoCount = amount;
                        IconText.Clear().AppendInt32(amount);
                        changed |= ChangeInfo.IconText;
                    }
                }
            }

            var shipControler = MySession.ControlledEntity as MyShipController;
            if (shipControler != null && shipControler.GridSelectionSystem.WeaponSystem != null)
            {
            //    var shipWeaponType = shipControler.GetWeaponType(Definition.Id.TypeId);
            //    shipHasThisWeapon = shipWeaponType.HasValue && shipControler.GridSelectionSystem.WeaponSystem.HasGunsOfId(shipWeaponType.Value);
                shipHasThisWeapon = shipControler.GridSelectionSystem.WeaponSystem.HasGunsOfId(Definition.Id);
                if (shipHasThisWeapon)
                {
                    IMyGunObject<MyDeviceBase> gunObject = shipControler.GridSelectionSystem.WeaponSystem.GetGun(Definition.Id);
                    if (gunObject.GunBase is MyGunBase)
                    {
                        int ammo = 0;
                        foreach (var gun in shipControler.GridSelectionSystem.WeaponSystem.GetGunsById(Definition.Id))
                        {
                            ammo += gun.GetAmmunitionAmount();
                        }
                        if (ammo != m_lastAmmoCount)
                        {
                            m_lastAmmoCount = ammo;
                            IconText.Clear().AppendInt32(ammo);
                            changed |= ChangeInfo.IconText;
                        }
                    }
                }

                thisWeaponIsCurrent = shipControler.GridSelectionSystem.GetGunId() == Definition.Id;
            }

            changed |= SetEnabled(characterHasThisWeapon || shipHasThisWeapon);
            WantsToBeSelected = thisWeaponIsCurrent;
            m_needsWeaponSwitching = !thisWeaponIsCurrent;
            return changed;
        }
    }
}
