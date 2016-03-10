using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.Utils;
using VRageMath;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Multiplayer;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities.EnvironmentItems
{
    /// <summary>
    /// Class for managing all static bushes as one entity.
    /// </summary>
    [MyEntityType(typeof(MyObjectBuilder_Bushes), mainBuilder: false)]
    [MyEntityType(typeof(MyObjectBuilder_DestroyableItems), mainBuilder: true)]
    public class MyDestroyableItems : MyEnvironmentItems
    {
        public MyDestroyableItems() { }

        public override void DoDamage(float damage, int instanceId, Vector3D position, Vector3 normal, MyStringHash type)
        {
            if (!Sync.IsServer) return;

            RemoveItem(instanceId, sync: true);
        }

        protected override MyEntity DestroyItem(int itemInstanceId)
        {
            RemoveItem(itemInstanceId, sync: true);
            return null;
        }
    }
}
