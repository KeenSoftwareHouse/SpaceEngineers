
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncConveyors
    {
        [MessageId(2476, P2PMessageEnum.Reliable)]
        struct ChangeUseConveyorsMsg : IEntityMessage
        {
            public long ProductionEntityId;
            public long GetEntityId() { return ProductionEntityId; }
            public BoolBlit Value;
        }

        static MySyncConveyors()
        {
            MySyncLayer.RegisterMessage<ChangeUseConveyorsMsg>(ChangeUseConveyorSystemSucess, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf, MyTransportMessageEnum.Success);
        }

        public static void SendChangeUseConveyorSystemRequest(long entityId, bool newVal)
        {
            var msg = new ChangeUseConveyorsMsg();
            msg.ProductionEntityId = entityId;
            msg.Value = newVal;
            Sync.Layer.SendMessageToServerAndSelf(ref msg, MyTransportMessageEnum.Success);
        }

        static void ChangeUseConveyorSystemSucess(ref ChangeUseConveyorsMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(msg.GetEntityId(), out entity))
            {
                if (entity as IMyInventoryOwner != null)
                {
                    (entity as IMyInventoryOwner).UseConveyorSystem = msg.Value;
                    if (Sync.IsServer)
                    {
                        Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
                    }
                }
            }
        }
    }
}
