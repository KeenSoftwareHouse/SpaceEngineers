using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncSpaceBall
    {
        private MySpaceBall m_block;

        [MessageId(2286, P2PMessageEnum.Reliable)]
        protected struct ChangeParamsMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float VirtualMass;
            public float Friction;
        }

        [MessageId(2287, P2PMessageEnum.Reliable)]
        protected struct ChangeRestitutionMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float Restitution;
        }

        [MessageId(2288, P2PMessageEnum.Reliable)]
        protected struct ChangeBroadcastMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit BroadcastEnabled;
        }

        static MySyncSpaceBall()
        {
            MySyncLayer.RegisterMessage<ChangeParamsMsg>(ChangeParamsSuccess, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeRestitutionMsg>(OnChangeRestitution, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeBroadcastMsg>(OnChangeBroadcast, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Success);
        }

        public MySyncSpaceBall(MySpaceBall block)
        {
            m_block = block;
        }

        public void SendChangeParamsRequest(float virtualMass, float friction)
        {
            var msg = new ChangeParamsMsg();
            msg.EntityId = m_block.EntityId;
            msg.VirtualMass = virtualMass;
            msg.Friction = friction;

            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        public void SendChangeRestitutionRequest(float restitution)
        {
            var msg = new ChangeRestitutionMsg();
            msg.EntityId = m_block.EntityId;
            msg.Restitution = restitution;

            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        public void SendChangeBroadcastRequest(bool isEnabled)
        {
            var msg = new ChangeBroadcastMsg();
            msg.EntityId = m_block.EntityId;
            msg.BroadcastEnabled = isEnabled;

            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Success);            
        }

        static void ChangeParamsSuccess(ref ChangeParamsMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySpaceBall;
            if (block != null)
            {
                block.VirtualMass = msg.VirtualMass;
                block.Friction = msg.Friction;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
                }
            }
        }

        static void OnChangeRestitution(ref ChangeRestitutionMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySpaceBall;

            if (block != null)
            {
                block.Restitution = msg.Restitution;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
                }
            }
        }

        static void OnChangeBroadcast(ref ChangeBroadcastMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MySpaceBall;

            if (block != null)
            {
                block.UpdateRadios(msg.BroadcastEnabled & block.IsWorking);
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        }
    }
}
