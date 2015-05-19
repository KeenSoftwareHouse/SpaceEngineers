using System;
using System.Text;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SteamSDK;
using Sandbox.Game.Entities.Cube;
using System.Diagnostics;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    static class MySyncBlockHelpers
    {
        [MessageId(15268, P2PMessageEnum.Reliable)]
        struct EnableMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit Enable;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(15272, P2PMessageEnum.Reliable)]
        struct ShowOnHUDMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit Show;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(15269, P2PMessageEnum.Reliable)]
        [ProtoContract]
        struct ChangeNameMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            [ProtoMember]
            public string Name;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        [MessageId(15273, P2PMessageEnum.Reliable)]
        struct ShowInTerminalMsg : IEntityMessage
        {
            public long EntityId;

            public long GetEntityId() { return EntityId; }

            public BoolBlit Show;

            public override string ToString()
            {
                return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
            }
        }

        static MySyncBlockHelpers()
        {
            MySyncLayer.RegisterMessage<EnableMsg>(EnableRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<EnableMsg>(EnableSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ShowOnHUDMsg>(ShowOnHUDRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ShowOnHUDMsg>(ShowOnHUDSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangeNameMsg>(ChangeNameRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeNameMsg>(ChangeNameSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ShowInTerminalMsg>(ShowInTerminalRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ShowInTerminalMsg>(ShowInTerminalSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

        }

        static bool GetBlock<T>(long entityId, out T block)
            where T : MyEntity
        {
            MyEntity entity;
            if (MyEntities.TryGetEntityById(entityId, out entity))
            {
                block = entity as T;
                return block != null;
            }
            block = default(T);
            return false;
        }

        public static void SendShowOnHUDRequest(MyTerminalBlock block, bool show)
        {
            var msg = new ShowOnHUDMsg();
            msg.EntityId = block.EntityId;
            msg.Show = show;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ShowOnHUDRequest(ref ShowOnHUDMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ShowOnHUDSuccess(ref ShowOnHUDMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                block.ShowOnHUD = msg.Show;
            }
        }

        public static void SendEnableRequest(MyFunctionalBlock block, bool enable)
        {
            var msg = new EnableMsg();
            msg.EntityId = block.EntityId;
            msg.Enable = enable;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void EnableRequest(ref EnableMsg msg, MyNetworkClient sender)
        {
            MyFunctionalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void EnableSuccess(ref EnableMsg msg, MyNetworkClient sender)
        {
            MyFunctionalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                block.Enabled = msg.Enable;
            }
        }

        public static void SendChangeNameRequest(MyTerminalBlock block, StringBuilder name)
        {
            if (name.CompareTo(block.CustomName) != 0)
            {
                block.UpdateCustomName(name);

                var msg = new ChangeNameMsg();
                msg.EntityId = block.EntityId;
                msg.Name = name.ToString(); // Allocation will be either here or in deserialization...or messages would have to be pooled

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        public static void SendChangeNameRequest(MyTerminalBlock block, string name)
        {
            if (name.CompareTo(block.CustomName.ToString()) != 0)
            {
                block.UpdateCustomName(name);

                var msg = new ChangeNameMsg();
                msg.EntityId = block.EntityId;
                msg.Name = name; // Allocation will be either here or in deserialization...or messages would have to be pooled

                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }

        static void ChangeNameRequest(ref ChangeNameMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                block.UpdateCustomName(msg.Name);
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId, MyTransportMessageEnum.Success);
            }
        }

        static void ChangeNameSuccess(ref ChangeNameMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                block.UpdateCustomName(msg.Name);
            }
        }

        public static void SendShowInTerminalRequest(MyTerminalBlock block, bool show)
        {
            var msg = new ShowInTerminalMsg();
            msg.EntityId = block.EntityId;
            msg.Show = show;

            Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
        }

        static void ShowInTerminalRequest(ref ShowInTerminalMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                Sync.Layer.SendMessageToAllAndSelf(ref msg, MyTransportMessageEnum.Success);
            }
        }

        static void ShowInTerminalSuccess(ref ShowInTerminalMsg msg, MyNetworkClient sender)
        {
            MyTerminalBlock block;
            if (GetBlock(msg.EntityId, out block))
            {
                block.ShowInTerminal = msg.Show;
            }
        }
    }
}
