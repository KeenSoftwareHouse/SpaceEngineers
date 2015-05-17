using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncRadar
    {
        private MyRadar m_radar;

        [MessageId(12857, P2PMessageEnum.Reliable)]
        protected struct ChangeRadarMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId()
            {
                return EntityId;
            }

            public BoolBlit BroadcastUsingAntennas;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncRadar()
        {
            MySyncLayer.RegisterMessage<ChangeRadarMsg>(ChangeRadar, MyMessagePermissions.Any);
        }

        public MySyncRadar(MyRadar detector)
        {
            m_radar = detector;
        }

        public void SendChangeOreDetector(bool broadcastUsingAntennas)
        {
            var msg = new ChangeRadarMsg();

            msg.EntityId = m_radar.EntityId;
            msg.BroadcastUsingAntennas = broadcastUsingAntennas;

            Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeRadar(ref ChangeRadarMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
                (entity as MyRadar).BroadcastUsingAntennas = msg.BroadcastUsingAntennas;
        
        }

    }
}
