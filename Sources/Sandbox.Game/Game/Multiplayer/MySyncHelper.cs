using ProtoBuf;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Utils;
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

            public MyStringHash Type;

            public long AttackerEntityId;
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
            public MyStringHash Type;

            [ProtoMember]
            public MyHitInfo? HitInfo;

            [ProtoMember]
            public long AttackerEntityId;
        }

		[MessageIdAttribute(13270, P2PMessageEnum.Reliable)]
		[ProtoContract]
		struct KillCharacterMsg
		{
			[ProtoMember]
			public long entityId;

            [ProtoMember]
            public MyDamageInformation DamageInfo;
		}

        static MySyncHelper()
        {
            MySyncLayer.RegisterMessage<DoDamageMsg>(DoDamage, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterMessage<DoDamageSlimBlockMsg>(DoDamageSlimBlock, MyMessagePermissions.FromServer);
			MySyncLayer.RegisterMessage<KillCharacterMsg>(OnKillCharacter, MyMessagePermissions.FromServer);
        }

        public static void DoDamageSynced(MyEntity destroyable, float damage, MyStringHash type, long attackerId)
        {
            Debug.Assert(Sync.IsServer || destroyable.SyncObject is MySyncEntity || (destroyable.SyncObject as MySyncEntity).ResponsibleForUpdate(Sync.Clients.LocalClient));
            if (!(destroyable is IMyDestroyableObject))
                return;

            var msg = new DoDamageMsg();
            msg.DestroyableEntityId = destroyable.EntityId;
            msg.Damage = damage;
            msg.Type = type;
            msg.AttackerEntityId = attackerId;

            (destroyable as IMyDestroyableObject).DoDamage(damage, type, false, attackerId: attackerId);
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
            (ent as IMyDestroyableObject).DoDamage(msg.Damage, msg.Type, false, null, msg.AttackerEntityId);
        }

		public static void KillCharacter(MyCharacter character, MyDamageInformation damageInfo)
		{
			Debug.Assert(Sync.IsServer, "KillCharacter called from client");
			KillCharacterMsg msg = new KillCharacterMsg()
			{
				entityId = character.EntityId,
                DamageInfo = damageInfo
			};

			character.Kill(false, damageInfo);
			Sync.Layer.SendMessageToAll<KillCharacterMsg>(ref msg);
		}

		static void OnKillCharacter(ref KillCharacterMsg msg, MyNetworkClient sender)
		{
			MyEntity entity = null;
			MyCharacter character = null;
			if (!MyEntities.TryGetEntityById(msg.entityId, out entity) || (character = entity as MyCharacter) == null)
				return;

			character.Kill(false, msg.DamageInfo);
		}

        internal static void DoDamageSynced(MySlimBlock block, float damage, MyStringHash damageType, MyHitInfo? hitInfo, long attackerId)
        {
            Debug.Assert(Sync.IsServer);
            var msg = new DoDamageSlimBlockMsg();
            msg.GridEntityId = block.CubeGrid.EntityId;
            msg.Position = block.Position;
            msg.Damage = damage;
            msg.HitInfo = hitInfo;
            msg.AttackerEntityId = attackerId;

            block.DoDamage(damage, damageType, hitInfo: hitInfo, attackerId: attackerId);
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
            block.DoDamage(msg.Damage, msg.Type, hitInfo: msg.HitInfo, attackerId:msg.AttackerEntityId);
        }

    }
}
