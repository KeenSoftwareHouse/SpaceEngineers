using ProtoBuf;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI.Interfaces;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncHelper
    {
        [MessageIdAttribute(13268, P2PMessageEnum.Reliable)]
        struct DoDamageMsg
        {
            public long DestroyableEntityId;

            public float Damage;

            public MyDamageType Type;
        }

        
        [MessageIdAttribute(13269, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct DoDamageSlimBlockMsg
        {
            [ProtoMember]
            public long GridEntityId;

            [ProtoMember]
            public Vector3I Position;

            [ProtoMember]
            public float Damage;

            [ProtoMember]
            public MyDamageType Type;

            [ProtoMember]
            public MyHitInfo? HitInfo;
        }

        static MySyncHelper()
        {
            MySyncLayer.RegisterMessage<DoDamageMsg>(DoDamage, MyMessagePermissions.Any);
            MySyncLayer.RegisterMessage<DoDamageSlimBlockMsg>(DoDamageSlimBlock, MyMessagePermissions.Any);
        }

        public static void DoDamageSynced(MyEntity destroyable, float damage, MyDamageType type)
        {
            Debug.Assert(Sync.IsServer || destroyable.SyncObject is MySyncEntity || (destroyable.SyncObject as MySyncEntity).ResponsibleForUpdate(Sync.Clients.LocalClient));
            if (!(destroyable is IMyDestroyableObject))
                return;

            var msg = new DoDamageMsg();
            msg.DestroyableEntityId = destroyable.EntityId;
            msg.Damage = damage;
            msg.Type = type;

            (destroyable as IMyDestroyableObject).DoDamage(damage, type, false);
            Sync.Layer.SendMessageToAll<DoDamageMsg>(ref msg);
        }

        static void DoDamage(ref DoDamageMsg msg, MyNetworkClient sender)
        {
            MyEntity ent;
            if(!MyEntities.TryGetEntityById(msg.DestroyableEntityId, out ent))
                return;
            if (!(ent is IMyDestroyableObject))
            {
                Debug.Fail("Damage can be done to destroyable only");
                return;
            }
            (ent as IMyDestroyableObject).DoDamage(msg.Damage, msg.Type, false);
        }

        internal static void DoDamageSynced(MySlimBlock block, float damage, MyDamageType damageType, MyHitInfo? hitInfo)
        {
            Debug.Assert(Sync.IsServer);
            var msg = new DoDamageSlimBlockMsg();
            msg.GridEntityId = block.CubeGrid.EntityId;
            msg.Position = block.Position;
            msg.Damage = damage;
            msg.HitInfo = hitInfo;

            block.DoDamage(damage, damageType, hitInfo: hitInfo);
            Sync.Layer.SendMessageToAll<DoDamageSlimBlockMsg>(ref msg);
        }

        static void DoDamageSlimBlock(ref DoDamageSlimBlockMsg msg, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);
            MyCubeGrid grid;
            if (!MyEntities.TryGetEntityById<MyCubeGrid>(msg.GridEntityId, out grid))
                return;
            var block = grid.GetCubeBlock(msg.Position);
            if (block == null)
                return;
            block.DoDamage(msg.Damage, msg.Type, hitInfo: msg.HitInfo);
        }

    }
}
