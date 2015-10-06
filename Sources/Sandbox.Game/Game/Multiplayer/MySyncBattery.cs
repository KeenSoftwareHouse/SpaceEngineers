using System;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    internal class MySyncBattery : MyBattery.Friend
    {
        private MyBattery m_battery;

        [MessageIdAttribute(2483, P2PMessageEnum.Reliable)]
        protected struct CapacitySyncMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public float RemainingCapacity;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncBattery()
        {
            MySyncLayer.RegisterMessage<CapacitySyncMsg>(CapacitySyncSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        public MySyncBattery(MyBattery battery)
        {
            m_battery = battery;
        }

        public void SendCapacitySync(MyCharacter owner, float remainingCapacity)
        {
            var msg = new CapacitySyncMsg();

            msg.EntityId = owner.EntityId;
            msg.RemainingCapacity = remainingCapacity;

            Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void CapacitySyncSuccess(ref CapacitySyncMsg msg, MyNetworkClient sender)
        {
            MyCharacter owner;
            MyEntities.TryGetEntityById(msg.EntityId, out owner);
            if (owner != null)
            {
                MyBattery.Friend.OnSyncCapacitySuccess(owner.SuitBattery, msg.RemainingCapacity);
            }
        }
    }
}
