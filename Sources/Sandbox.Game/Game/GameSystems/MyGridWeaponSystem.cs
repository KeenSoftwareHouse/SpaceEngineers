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
using Sandbox.Definitions;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Utils;

namespace Sandbox.Game.GameSystems
{
    public class MyGridWeaponSystem
    {
        public struct EventArgs
        {
            public IMyGunObject<MyDeviceBase> Weapon;
        }

        static MyGridWeaponSystem()
        {
        }

        Dictionary<MyDefinitionId, HashSet<IMyGunObject<MyDeviceBase>>> m_gunsByDefId;

        public event Action<MyGridWeaponSystem, EventArgs> WeaponRegistered;
        public event Action<MyGridWeaponSystem, EventArgs> WeaponUnregistered;

        public MyGridWeaponSystem()
        {
            m_gunsByDefId = new Dictionary<MyDefinitionId, HashSet<IMyGunObject<MyDeviceBase>>>(5, MyDefinitionId.Comparer);
        }

        public IMyGunObject<MyDeviceBase> GetGun(MyDefinitionId defId)
        {
            if (m_gunsByDefId.ContainsKey(defId))
                return m_gunsByDefId[defId].FirstOrDefault();
            return null;
        }

        public Dictionary<MyDefinitionId, HashSet<IMyGunObject<MyDeviceBase>>> GetGunSets()
        {
            return m_gunsByDefId;
        }

        public bool HasGunsOfId(MyDefinitionId defId)
        {
            if (m_gunsByDefId.ContainsKey(defId))
                return m_gunsByDefId[defId].Count > 0;
            return false;
        }

        internal void Register(IMyGunObject<MyDeviceBase> gun)
        {
            MyDebug.AssertDebug(gun != null);

            if (!m_gunsByDefId.ContainsKey(gun.DefinitionId))
            {
                m_gunsByDefId.Add(gun.DefinitionId, new HashSet<IMyGunObject<MyDeviceBase>>());
            }

            MyDebug.AssertDebug(!m_gunsByDefId[gun.DefinitionId].Contains(gun));
            m_gunsByDefId[gun.DefinitionId].Add(gun);

            if (WeaponRegistered != null)
                WeaponRegistered(this, new EventArgs() { Weapon = gun });
        }

        internal void Unregister(IMyGunObject<MyDeviceBase> gun)
        {
            MyDebug.AssertDebug(gun != null);
            if (!m_gunsByDefId.ContainsKey(gun.DefinitionId))
            {
                MyDebug.FailRelease("deinition ID " + gun.DefinitionId + " not in m_gunsByDefId");
                return;
            }
            MyDebug.AssertDebug(m_gunsByDefId[gun.DefinitionId].Contains(gun));

            m_gunsByDefId[gun.DefinitionId].Remove(gun);

            if (WeaponUnregistered != null)
                WeaponUnregistered(this, new EventArgs() { Weapon = gun });
        }

        public HashSet<IMyGunObject<MyDeviceBase>> GetGunsById(MyDefinitionId gunId)
        {
            if (m_gunsByDefId.ContainsKey(gunId))
                return m_gunsByDefId[gunId];
            return null;
        }

        internal IMyGunObject<MyDeviceBase> GetGunWithAmmo(MyDefinitionId gunId, long shooter)
        {
            if (!m_gunsByDefId.ContainsKey(gunId))
                return null;

            IMyGunObject<MyDeviceBase> result = m_gunsByDefId[gunId].FirstOrDefault();
            foreach (var gun in m_gunsByDefId[gunId])
            {
                MyGunStatusEnum status;
                if (gun.CanShoot(MyShootActionEnum.PrimaryAction, shooter, out status))
                {
                    result = gun;
                    break;
                }
            }
            return result;
        }
    }
}
