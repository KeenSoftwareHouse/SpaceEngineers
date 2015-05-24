using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncVirtualMass
    {
        private MyVirtualMass m_block;

        [MessageId(2290, P2PMessageEnum.Reliable)]
        protected struct ChangeVirtualMassMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float VirtualMass;
        }
        
        static MySyncVirtualMass()
        {
            MySyncLayer.RegisterMessage<ChangeVirtualMassMsg>(ChangeParamsSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
        }

        public MySyncVirtualMass(MyVirtualMass block)
        {
            m_block = block;
        }

        public void SendChangeVirtualMassRequest(float virtualMass)
        {
            var msg = new ChangeVirtualMassMsg();
            msg.EntityId = m_block.EntityId;
            msg.VirtualMass = virtualMass;

            Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
        }
        
        static void ChangeParamsSuccess(ref ChangeVirtualMassMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            var block = entity as MyVirtualMass;
            if (block != null)
            {
                block.VirtualMass = msg.VirtualMass;
            }
        }
    }
}
