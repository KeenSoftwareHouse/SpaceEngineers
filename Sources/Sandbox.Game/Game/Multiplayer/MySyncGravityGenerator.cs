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
    class MySyncGravityGenerator
    {
        private MyGravityGenerator m_block;

        [MessageIdAttribute(666, P2PMessageEnum.Reliable)]
        protected struct ChangeGravityGeneratorMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public Vector3 FieldSize;
            public float GravityAcceleration;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncGravityGenerator()
        {
            MySyncLayer.RegisterMessage<ChangeGravityGeneratorMsg>(ChangeGravityGeneratorRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeGravityGeneratorMsg>(ChangeGravityGeneratorSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncGravityGenerator(MyGravityGenerator block)
        {
            m_block = block;
        }

        public void SendChangeGravityGeneratorRequest(ref Vector3 fieldSize, float gravityAcceleration)
        {
            var msg = new ChangeGravityGeneratorMsg();

            msg.EntityId = m_block.EntityId;
            msg.FieldSize = fieldSize;
            msg.GravityAcceleration = gravityAcceleration;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ChangeGravityGeneratorRequest(ref ChangeGravityGeneratorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity is MyGravityGenerator)
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeGravityGeneratorSuccess(ref ChangeGravityGeneratorMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyGravityGenerator;
            if (block != null)
            {
                block.FieldSize           = msg.FieldSize;
                block.GravityAcceleration = msg.GravityAcceleration;
            }
        }
    }
}
