using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    public class MySyncDamage
    {
        public static void DoDamageSynced(MyEntity entity, float damage, MyStringHash type, long attackerId)
        {
            Debug.Assert(Sync.IsServer);
            IMyDestroyableObject destroyable = entity as IMyDestroyableObject;
            if (destroyable == null)
                return;

            destroyable.DoDamage(damage, type, false, attackerId: attackerId);
            MyMultiplayer.RaiseStaticEvent(s => MySyncDamage.OnDoDamage, entity.EntityId, damage, type, attackerId);
        }

        [Event, Reliable, Broadcast]
        static void OnDoDamage(long destroyableId, float damage, MyStringHash type, long attackerId)
        {
            MyEntity ent;
            if (!MyEntities.TryGetEntityById(destroyableId, out ent))
                return;

            IMyDestroyableObject destroyable = ent as IMyDestroyableObject;
            if (destroyable == null)
            {
                Debug.Fail("Damage can be done to destroyable only");
                return;
            }

            destroyable.DoDamage(damage, type, false, null, attackerId);
        }
    }
}
