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
    class MySyncOreDetector
    {
        private MyOreDetector m_oreDetector;

        [MessageIdAttribute(1565, P2PMessageEnum.Reliable)]
        protected struct ChangeOreDetectorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit BroadcastUsingAntennas;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncOreDetector()
        {
            MySyncLayer.RegisterMessage<ChangeOreDetectorMsg>(ChangeOreDetector, MyMessagePermissions.ToServer|MyMessagePermissions.FromServer|MyMessagePermissions.ToSelf);
        }

        public MySyncOreDetector(MyOreDetector detector)
        {
            m_oreDetector = detector;
        }


        public void SendChangeOreDetector(bool broadcastUsingAntennas)
        {
            var msg = new ChangeOreDetectorMsg();

            msg.EntityId = m_oreDetector.EntityId;
            msg.BroadcastUsingAntennas = broadcastUsingAntennas;

            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeOreDetector(ref ChangeOreDetectorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.EntityId, out entity))
            {
                (entity as MyOreDetector).BroadcastUsingAntennas = msg.BroadcastUsingAntennas;
                if (Sync.IsServer)
                {
                    Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
                }
            }
        
        }

    }
}
