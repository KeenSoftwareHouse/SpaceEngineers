using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncConveyorSorter
    {
        private MyConveyorSorter m_Parent;

        [MessageIdAttribute(6300, P2PMessageEnum.Reliable)]
        protected struct DrainAllMsg : IEntityMessage
        {
            public long EntityId;
            public BoolBlit DrainAll;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        [MessageIdAttribute(6301, P2PMessageEnum.Reliable)]
        protected struct BlWlMsg : IEntityMessage
        {
            public long EntityId;
            public BoolBlit IsWhitelist;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        [ProtoContract]
        [MessageIdAttribute(6302, P2PMessageEnum.Reliable)]
        protected struct ListChangeIdMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            [ProtoMember]
            public bool Add;
            [ProtoMember]
            public SerializableDefinitionId Id;


            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        [MessageIdAttribute(6303, P2PMessageEnum.Reliable)]
        protected struct ListChangeTypeMsg : IEntityMessage
        {
            public long EntityId;
            public BoolBlit Add;
            public byte Type;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        public MySyncConveyorSorter(MyConveyorSorter id)
        {
            m_Parent = id;
        }

        static MySyncConveyorSorter()
        {
            MySyncLayer.RegisterMessage<DrainAllMsg>(DrainAllRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<DrainAllMsg>(DrainAllSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<BlWlMsg>(BlWlRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<BlWlMsg>(BlWlSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ListChangeIdMsg>(ListChangeIdRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ListChangeIdMsg>(ListChangeIdSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterMessage<ListChangeTypeMsg>(ListChangeTypeRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ListChangeTypeMsg>(ListChangeTypeSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        #region DrainAll

        public void ChangeDrainAll(bool cAll)
        {
            DrainAllMsg msg = new DrainAllMsg();
            msg.EntityId = m_Parent.EntityId;
            msg.DrainAll = cAll;

            if (!Sync.IsServer)
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            else
                if (m_Parent.DoChangeDrainAll(cAll))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void DrainAllRequest(ref DrainAllMsg msg, MyNetworkClient sender)
        {
            if (DoChangeDrainAll(msg.EntityId, msg.DrainAll))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void DrainAllSuccess(ref DrainAllMsg msg, MyNetworkClient sender)
        {
            DoChangeDrainAll(msg.EntityId, msg.DrainAll);
        }

        static bool DoChangeDrainAll(long EntityId, bool cAll)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(EntityId, out entity);
            if (entity != null)
            {
                return ((MyConveyorSorter)entity).DoChangeDrainAll(cAll);
            }
            return false;
        }

        #endregion

        #region Blacklist/whitelist

        public void ChangeBlWl(bool IsWhitelist)
        {
            BlWlMsg msg = new BlWlMsg();
            msg.EntityId = m_Parent.EntityId;
            msg.IsWhitelist = IsWhitelist;

            if (!Sync.IsServer)
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            else
                if (m_Parent.DoChangeBlWl(IsWhitelist))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void BlWlRequest(ref BlWlMsg msg, MyNetworkClient sender)
        {
            if (DoChangeBlWl(msg.EntityId, msg.IsWhitelist))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void BlWlSuccess(ref BlWlMsg msg, MyNetworkClient sender)
        {
            DoChangeBlWl(msg.EntityId, msg.IsWhitelist);
        }

        static bool DoChangeBlWl(long EntityId, bool IsWhitelist)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(EntityId, out entity);
            if (entity != null)
            {
                return ((MyConveyorSorter)entity).DoChangeBlWl(IsWhitelist);
            }
            return false;
        }

        #endregion

        #region change ID in list

        public void ChangeListId(SerializableDefinitionId id, bool add)
        {
            ListChangeIdMsg msg=new ListChangeIdMsg();
            msg.EntityId=m_Parent.EntityId;
            msg.Id=id;
            msg.Add=add;
            if (!Sync.IsServer)
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            else
                if (m_Parent.DoChangeListId(id,add))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ListChangeIdRequest(ref ListChangeIdMsg msg, MyNetworkClient sender)
        {
            if (DoChangeListId(msg.EntityId, msg.Id, msg.Add))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ListChangeIdSuccess(ref ListChangeIdMsg msg, MyNetworkClient sender)
        {
            DoChangeListId(msg.EntityId, msg.Id, msg.Add);
        }

        static bool DoChangeListId(long EntityId, SerializableDefinitionId id, bool add)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(EntityId, out entity);
            if (entity != null)
            {
                return ((MyConveyorSorter)entity).DoChangeListId(id,add);
            }
            return false;
        }

        public void ChangeListType(byte type, bool add)
        {
            ListChangeTypeMsg msg = new ListChangeTypeMsg();
            msg.EntityId = m_Parent.EntityId;
            msg.Type = type;
            msg.Add = add;
            if (!Sync.IsServer)
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            else
                if (m_Parent.DoChangeListType(type, add))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ListChangeTypeRequest(ref ListChangeTypeMsg msg, MyNetworkClient sender)
        {
            if (DoChangeListType(msg.EntityId, msg.Type, msg.Add))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void ListChangeTypeSuccess(ref ListChangeTypeMsg msg, MyNetworkClient sender)
        {
            DoChangeListType(msg.EntityId, msg.Type, msg.Add);
        }

        static bool DoChangeListType(long EntityId, byte type, bool add)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(EntityId, out entity);
            if (entity != null)
            {
                return ((MyConveyorSorter)entity).DoChangeListType(type, add);
            }
            return false;
        }

        #endregion
    }
}
