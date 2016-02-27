using System;
using System.Diagnostics;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using VRage.Game.Entity;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    internal static class MySyncHandTool
    {
        [MessageId(7263, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct StopShootingMsg
        {
            [ProtoMember]
            public long EntityId;
            [ProtoMember]
            public float AttackDelay;
        }

        static MySyncHandTool()
        {
            MySyncLayer.RegisterMessage<StopShootingMsg>(OnStopShootingChanged, MyMessagePermissions.FromServer, MyTransportMessageEnum.Request);
        }

        internal static void StopShootingRequest(long entityId, float attackDelay)
        {
            Debug.Assert(Sync.IsServer);

            var msg = new StopShootingMsg();
            msg.EntityId = entityId;
            msg.AttackDelay = attackDelay;

            Sync.Layer.SendMessageToAll(ref msg);
        }

        private static void OnStopShootingChanged(ref StopShootingMsg message, MyNetworkClient sender)
        {
            Debug.Assert(!Sync.IsServer);

            MyEntity entity = null;
            MyEntities.TryGetEntityById(message.EntityId, out entity);
            MyHandToolBase handTool = entity as MyHandToolBase;
            if (handTool != null)
                handTool.StopShooting(message.AttackDelay);
        }
    }
}
