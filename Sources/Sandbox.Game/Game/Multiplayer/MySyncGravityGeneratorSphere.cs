using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using VRageMath;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncGravityGeneratorSphere
    {
        private MyGravityGeneratorSphere m_block;

        [MessageIdAttribute(668, P2PMessageEnum.Reliable)]
        protected struct ChangeGravityGeneratorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public float Radius;
            public float GravityAcceleration;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncGravityGeneratorSphere()
        {
            MySyncLayer.RegisterMessage<ChangeGravityGeneratorMsg>(ChangeGravityGeneratorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeGravityGeneratorMsg>(ChangeGravityGeneratorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncGravityGeneratorSphere(MyGravityGeneratorSphere block)
        {
            m_block = block;
        }

        public void SendChangeGravityGeneratorRequest(float radius, float gravityAcceleration)
        {
            var msg = new ChangeGravityGeneratorMsg();

            msg.EntityId = m_block.EntityId;
            msg.Radius = radius;
            msg.GravityAcceleration = gravityAcceleration;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeGravityGeneratorRequest(ref ChangeGravityGeneratorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyGravityGeneratorSphere)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeGravityGeneratorSuccess(ref ChangeGravityGeneratorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyGravityGeneratorSphere;
            if (block != null)
            {
                block.Radius = msg.Radius;
                block.GravityAcceleration = msg.GravityAcceleration;
            }
        }
    }
}
