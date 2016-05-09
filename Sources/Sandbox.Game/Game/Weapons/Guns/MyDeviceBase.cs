#region Using

using System.Text;
using VRageMath;
using System;
using Sandbox.Game.Entities;
using Sandbox.Game.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;


using System.Collections.Generic;
using Sandbox.Game.Weapons.Guns;
using VRage.Game;
using VRage.Serialization;

#endregion

namespace Sandbox.Game.Weapons
{
    public abstract class MyDeviceBase 
    {
        public static string GetGunNotificationName(MyDefinitionId gunId)
        {
            var definition = MyDefinitionManager.Static.GetDefinition(gunId);
            return definition.DisplayNameText;
        }

        /// <summary>
        /// Reference to the inventory item that this device originated from.
        /// Can be used to update the inventory item (when ammo changes etc...)
        /// </summary>
        public uint? InventoryItemId { get; set; }

        public void Init(MyObjectBuilder_DeviceBase objectBuilder)
        {
            InventoryItemId = objectBuilder.InventoryItemId;
        }

        #region Methods
        public abstract Vector3D GetMuzzleLocalPosition();
        public abstract Vector3D GetMuzzleWorldPosition();

        public abstract bool CanSwitchAmmoMagazine();
        public abstract bool SwitchToNextAmmoMagazine();
        public abstract bool SwitchAmmoMagazineToNextAvailable();
        #endregion
    }
}
