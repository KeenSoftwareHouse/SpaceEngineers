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
using Sandbox.Game.Entities.Blocks;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncAirtightDoorGeneric
    {
        private MyAirtightDoorGeneric m_Parent;

        [MessageIdAttribute(6350, P2PMessageEnum.Reliable)]
        protected struct OpenCloseMsg : IEntityMessage
        {
            public long EntityId;
            public BoolBlit Open;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        public MySyncAirtightDoorGeneric(MyAirtightDoorGeneric id)
        {
            m_Parent = id;
        }

        static MySyncAirtightDoorGeneric()
        {
            MySyncLayer.RegisterMessage<OpenCloseMsg>(OpenCloseRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<OpenCloseMsg>(OpenCloseSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        #region OpenClose
        public void ChangeOpenClose(bool open)
        {
            OpenCloseMsg msg = new OpenCloseMsg();
            msg.EntityId = m_Parent.EntityId;
            msg.Open = open;

            if (!Sync.IsServer)
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            else
                if (m_Parent.DoChangeOpenClose(open))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OpenCloseRequest(ref OpenCloseMsg msg, MyNetworkClient sender)
        {
            if (DoChangeOpenClose(msg.EntityId, msg.Open))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        static void OpenCloseSuccess(ref OpenCloseMsg msg, MyNetworkClient sender)
        {
            DoChangeOpenClose(msg.EntityId, msg.Open);
        }

        static bool DoChangeOpenClose(long EntityId, bool open)
        {
            MyAirtightDoorGeneric doors;
            MyEntities.TryGetEntityById(EntityId, out doors);
            if (doors != null)
            {
                return doors.DoChangeOpenClose(open);
            }
            return false;
        }
        #endregion
    }
}
