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
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemWeapon))]
    public class MyToolbarItemWeapon : MyToolbarItemDefinition
    {
        protected int m_lastAmmoCount = -1;
        protected bool m_needsWeaponSwitching = true;
        protected string m_lastTextValue = String.Empty;

        public int AmmoCount
        {
            get { return m_lastAmmoCount; }
        }

        public MyToolbarItemWeapon()
            : base()
        {
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool init = base.Init(data);
            ActivateOnClick = false;
            return init;
        }

        public override bool Equals(object obj)
        {
            bool returnValue = base.Equals(obj);

            if (returnValue)
            {
                var otherObj = obj as MyToolbarItemWeapon;
                if (otherObj == null)
                    returnValue = false;
            }
            return returnValue;
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            var builder = (MyObjectBuilder_ToolbarItemWeapon)base.GetObjectBuilder();

            return builder;
        }

        public override bool Activate()
        {
            if (Definition == null)
                return false;

            var controlledObject = MySession.Static.ControlledEntity as IMyControllableEntity;
            if (controlledObject != null)
            {
                if (m_needsWeaponSwitching)
                {
                    controlledObject.SwitchToWeapon(this);
                    WantsToBeActivated = true; //by Gregory: changed to true because of 'Toolbar switching not working correctly' bug
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
            var character = MySession.Static.LocalCharacter;
            bool characterHasThisWeapon = character != null && (character.FindWeaponItemByDefinition(Definition.Id).HasValue || !character.WeaponTakesBuilderFromInventory(Definition.Id));
            ChangeInfo changed = ChangeInfo.None;

            if (characterHasThisWeapon)
            {
                var currentWeapon = character.CurrentWeapon;
                if (currentWeapon != null)
                    thisWeaponIsCurrent = (MyDefinitionManager.Static.GetPhysicalItemForHandItem(currentWeapon.DefinitionId).Id == Definition.Id);
                if (character.LeftHandItem != null)
                    thisWeaponIsCurrent |= Definition == character.LeftHandItem.PhysicalItemDefinition;
                if (thisWeaponIsCurrent && currentWeapon != null)
                {
                    var weaponItemDefinition = MyDefinitionManager.Static.GetPhysicalItemForHandItem(currentWeapon.DefinitionId) as MyWeaponItemDefinition;
                    if (weaponItemDefinition != null && weaponItemDefinition.ShowAmmoCount)
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
            }

            var shipControler = MySession.Static.ControlledEntity as MyShipController;
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

            // Detecting external change of the text..
            if (m_lastTextValue != IconText.ToString())
            {
                changed |= ChangeInfo.IconText;
            }
            m_lastTextValue = IconText.ToString();

            return changed;
        }
    }
}
