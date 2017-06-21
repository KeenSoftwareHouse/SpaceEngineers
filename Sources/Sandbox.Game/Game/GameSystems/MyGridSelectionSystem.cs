using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;

using Sandbox.Common.ObjectBuilders;
using System.Diagnostics;
using VRageMath;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Definitions;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;

namespace Sandbox.Game.GameSystems
{
    public class MyGridSelectionSystem
    {
        HashSet<IMyGunObject<MyDeviceBase>> m_currentGuns = new HashSet<IMyGunObject<MyDeviceBase>>();
        MyDefinitionId? m_gunId;
        bool m_useSingleGun;
        MyShipController m_shipController;
        MyGridWeaponSystem m_weaponSystem;

        public MyGridWeaponSystem WeaponSystem
        {
            get { return m_weaponSystem; }
            set
            {
                if (m_weaponSystem != value)
                {
                    if (m_weaponSystem != null)
                    {
                        m_weaponSystem.WeaponRegistered   -= WeaponSystem_WeaponRegistered;
                        m_weaponSystem.WeaponUnregistered -= WeaponSystem_WeaponUnregistered;
                    }

                    m_weaponSystem = value;

                    if (m_weaponSystem != null)
                    {
                        m_weaponSystem.WeaponRegistered   += WeaponSystem_WeaponRegistered;
                        m_weaponSystem.WeaponUnregistered += WeaponSystem_WeaponUnregistered;
                    }
                }
            }
        }

        public MyGridSelectionSystem(MyShipController shipController)
        {
            m_shipController = shipController;
        }

        void WeaponSystem_WeaponRegistered(MyGridWeaponSystem sender, MyGridWeaponSystem.EventArgs args)
        {
            if (m_shipController.Pilot != null && args.Weapon.DefinitionId == m_gunId)
            {
                if (m_useSingleGun)
                {
                    if (m_currentGuns.Count < 1)
                    {
                        args.Weapon.OnControlAcquired(m_shipController.Pilot);
                        m_currentGuns.Add(args.Weapon);
                    }
                }
                else
                {
                    args.Weapon.OnControlAcquired(m_shipController.Pilot);
                    m_currentGuns.Add(args.Weapon);
                }
            }
        }

        void WeaponSystem_WeaponUnregistered(MyGridWeaponSystem sender, MyGridWeaponSystem.EventArgs args)
        {
            if (m_shipController.Pilot != null && args.Weapon.DefinitionId == m_gunId)
            {
                if (m_currentGuns.Contains(args.Weapon))
                {
                    args.Weapon.OnControlReleased();
                    m_currentGuns.Remove(args.Weapon);
                }
            }
        }

        internal bool CanShoot(MyShootActionEnum action, out MyGunStatusEnum status, out IMyGunObject<MyDeviceBase> FailedGun)
        {
            FailedGun = null;
            if (m_currentGuns == null)
            {
                status = MyGunStatusEnum.NotSelected;
                return false;
            }

            bool result = false;
            status = MyGunStatusEnum.OK;

            // Report only one weapon status; Return true if any of the weapons can shoot
            foreach (var weapon in m_currentGuns)
            {
                MyGunStatusEnum weaponStatus;
                result |= weapon.CanShoot(action, m_shipController.ControllerInfo.Controller != null ? m_shipController.ControllerInfo.Controller.Player.Identity.IdentityId : m_shipController.OwnerId, out weaponStatus);
                // mw:TODO maybe autoswitch when gun has no ammo?
                //if (weaponStatus == MyGunStatusEnum.OutOfAmmo)
                //{
                //    if (weapon.GunBase.SwitchAmmoMagazineToFirstAvailable())
                //    {
                //        weaponStatus = MyGunStatusEnum.OK;
                //        result = true;
                //    }
                //}
                if (weaponStatus != MyGunStatusEnum.OK)
                {
                    FailedGun = weapon;
                    status = weaponStatus;
                }
            }

            return result;
        }

        internal void Shoot(MyShootActionEnum action)
        {
            MyGunStatusEnum status;

            foreach (var weapon in m_currentGuns)
            {
                if (!weapon.EnabledInWorldRules)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.WeaponDisabledInWorldSettings);
                    continue;
                }

                if (weapon.CanShoot(action, m_shipController.ControllerInfo.Controller != null ? m_shipController.ControllerInfo.ControllingIdentityId : m_shipController.OwnerId, out status))
                    weapon.Shoot(action, ((MyEntity)weapon).WorldMatrix.Forward, null);
            }
        }

        internal void EndShoot(MyShootActionEnum action)
        {
            foreach (var weapon in m_currentGuns)
            {
                if (!weapon.EnabledInWorldRules)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.WeaponDisabledInWorldSettings);
                    continue;
                }

                weapon.EndShoot(action);
            }
        }

        public bool CanSwitchAmmoMagazine()
        {
            bool switchMagazine = true;
            if (m_currentGuns != null)
            {
                foreach (IMyGunObject<MyDeviceBase> weapon in m_currentGuns)
                {
                    if (weapon.GunBase == null)
                    {
                        return false;
                    }
                    else
                    {
                        switchMagazine &= weapon.GunBase.CanSwitchAmmoMagazine();
                    }
                }
            }

            return switchMagazine;
        }

        internal void SwitchAmmoMagazine()
        {
            foreach (var weapon in m_currentGuns)
            {
                if (!weapon.EnabledInWorldRules)
                {
                    MyHud.Notifications.Add(MyNotificationSingletons.WeaponDisabledInWorldSettings);
                    continue;
                }

                weapon.GunBase.SwitchToNextAmmoMagazine();
            }
        }

        internal void SwitchTo(MyDefinitionId? gunId, bool useSingle = false)
        {
            //if (m_gunType == gunType)
            //    return;

            m_gunId = gunId;
            m_useSingleGun = useSingle;

            if (m_currentGuns != null)
                foreach (var gun in m_currentGuns)
                    gun.OnControlReleased();

            if (!gunId.HasValue)
            {
                m_currentGuns.Clear();
                return;
            }

            m_currentGuns.Clear();

            if (useSingle)
            {
                var gun = WeaponSystem.GetGunWithAmmo(gunId.Value, m_shipController.OwnerId);
                if (gun != null) m_currentGuns.Add(gun);
            }
            else
            {
                var guns = WeaponSystem.GetGunsById(gunId.Value);
                if (guns != null)
                {
                    foreach (var gun in guns)
                    {
                        if (gun != null) m_currentGuns.Add(gun);
                    }
                }
            }

            foreach (var gun in m_currentGuns)
                gun.OnControlAcquired(m_shipController.Pilot);
        }

        public MyDefinitionId? GetGunId()
        {
            return m_gunId;
        }


        int m_curentDrawHudIndex = 0;

        internal void DrawHud(IMyCameraController camera, long playerId)
        {
            if (m_currentGuns != null)
            {
                foreach (var gun in m_currentGuns)
                {
                    gun.DrawHud(camera, playerId);
                }
            }
        }

        internal void OnControlAcquired()
        {
            //Pilot can be null in the case of remote controlled blocks
            //Debug.Assert(m_shipController.Pilot != null);

            if (m_currentGuns == null)
                return;

            SwitchTo(m_gunId, m_useSingleGun);

            foreach (var gun in m_currentGuns)
                gun.OnControlAcquired(m_shipController.Pilot);
        }

        internal void OnControlReleased()
        {
            if (m_currentGuns == null)
                return;

            foreach (var gun in m_currentGuns)
                gun.OnControlReleased();
        }
    }
}
