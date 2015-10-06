using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using VRageMath;
using Sandbox.Game.Entities.Cube;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncRadioBroadcaster
    {
        private MyRadioBroadcaster m_broadcaster;

        [MessageIdAttribute(2545, P2PMessageEnum.Reliable)]
        protected struct ChangeRadioAntennaMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float BroadcastRadius;

            public BoolBlit BroadcastOn;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageIdAttribute(2546, P2PMessageEnum.Reliable)]
        protected struct ChangeRadioAntennaDisplayNameMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit ShowShipName;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncRadioBroadcaster()
        {
            MySyncLayer.RegisterMessage<ChangeRadioAntennaMsg>(ChangeRadioBroadcasterRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeRadioAntennaMsg>(ChangeRadioBroadcasterSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ChangeRadioAntennaDisplayNameMsg>(ChangeRadioAntennaDisplayName, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
        }

        public MySyncRadioBroadcaster(MyRadioBroadcaster broadcaster)
        {
            m_broadcaster = broadcaster;
        }

        public void SendChangeRadioAntennaRequest(float broadcastRadius, bool on)
        {
            var msg = new ChangeRadioAntennaMsg();

            msg.EntityId = m_broadcaster.Parent.EntityId;
            msg.BroadcastRadius = broadcastRadius;
            msg.BroadcastOn = on;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeRadioBroadcasterRequest(ref ChangeRadioAntennaMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var broadcasterOwner = entity as IMyComponentOwner<MyDataBroadcaster>;
            if (broadcasterOwner != null)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        public void SendChangeRadioAntennaDisplayName(bool showShipName)
        {
            var msg = new ChangeRadioAntennaDisplayNameMsg();

            msg.EntityId = m_broadcaster.Parent.EntityId;
            msg.ShowShipName = showShipName;

            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeRadioAntennaDisplayName(ref ChangeRadioAntennaDisplayNameMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                (entity as MyRadioAntenna).ShowShipName = msg.ShowShipName;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        
        }


        static void ChangeRadioBroadcasterSuccess(ref ChangeRadioAntennaMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyDataBroadcaster broadcaster;
            var broadcasterOwner = entity as IMyComponentOwner<MyDataBroadcaster>;
            if (broadcasterOwner != null)
            {
                broadcasterOwner.GetComponent(out broadcaster);
                (broadcaster as MyRadioBroadcaster).BroadcastRadius = msg.BroadcastRadius;
                (broadcaster as MyRadioBroadcaster).Enabled = msg.BroadcastOn;
                (broadcaster as MyRadioBroadcaster).WantsToBeEnabled = msg.BroadcastOn;
            }
        }

    }
}
