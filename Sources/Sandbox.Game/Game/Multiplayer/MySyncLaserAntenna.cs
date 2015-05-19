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
using Sandbox.Common.ObjectBuilders;
using ProtoBuf;

namespace Sandbox.Game.Multiplayer
{
   [PreloadRequired]
    class MySyncLaserAntenna
    {
        private MyLaserAntenna m_Parent;

        [MessageIdAttribute(6400, P2PMessageEnum.Reliable)]
        protected struct ChangeModeMsg : IEntityMessage
        {
            public long EntityId;
            public byte ModeByte;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }
        [MessageIdAttribute(6401, P2PMessageEnum.Reliable)]
        protected struct ConnectToMsg : IEntityMessage
        {
            public long EntityId;
            //public byte ModeByte;
            public long TargetEntityId;
            /*private long tgtEntityId;
            public long? TargetEntityId{
                set{tgtEntityId=value??0;}
                get{return (tgtEntityId==0?(long?)null:tgtEntityId);}
            }*/

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        [ProtoContract]
        [MessageIdAttribute(6402, P2PMessageEnum.Reliable)]
        protected struct CoordinatesPasted : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            [ProtoMember]
            public string Coordinates;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }
        protected static MyLaserAntenna.StateEnum GetMode(byte ModeByte)
        {
            return (MyLaserAntenna.StateEnum)(ModeByte);
        }
        public static byte SetmodeByte(MyLaserAntenna.StateEnum Mode)
        {
            return (byte)Mode;
        }

        [MessageIdAttribute(6403, P2PMessageEnum.Reliable)]
        protected struct ChangePermMsg : IEntityMessage
        {
            public long EntityId;
            public byte IsPerm;

            public long GetEntityId() { return EntityId; }
            public override string ToString() { return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText()); }
        }

        public MySyncLaserAntenna(MyLaserAntenna id)
        {
            m_Parent = id;
        }

        static MySyncLaserAntenna()
        {
            MySyncLayer.RegisterMessage<ChangeModeMsg>(ChangeModeRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangeModeMsg>(ChangeModeSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            //MySyncLayer.RegisterMessage<ChangeLaserAntennaMode>(ChangeModeFailure, MyMessagePermissions.FromServer, MyTransportMessageEnum.Failure);
            MySyncLayer.RegisterMessage<CoordinatesPasted>(PasteCoordinatesSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ConnectToMsg>(ConnectToRecRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ConnectToMsg>(ConnectToRecSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);

            MySyncLayer.RegisterMessage<ChangePermMsg>(ChangePermRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
            MySyncLayer.RegisterMessage<ChangePermMsg>(ChangePermSuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
        }

        #region paste coords
        public void PasteCoordinates(string coords)
        {
            if (!Sync.MultiplayerActive)
                m_Parent.DoPasteCoords(coords);
            else
            {
                var msg = new CoordinatesPasted();
                msg.EntityId = m_Parent.EntityId;
                msg.Coordinates = coords;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void PasteCoordinatesSuccess(ref  CoordinatesPasted msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyLaserAntenna la=(entity as MyLaserAntenna);
            if (la==null)
                return;
            la.DoPasteCoords(msg.Coordinates);
            if (Sync.IsServer)
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
        #endregion
        #region change permanent
        public void ChangePerm(bool isPerm)
        {
            if (!Sync.MultiplayerActive)
                m_Parent.DoSetIsPerm(isPerm);
            else
            {
                var msg = new ChangePermMsg();
                msg.EntityId = m_Parent.EntityId;
                msg.IsPerm = (byte)(isPerm?1:0);
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        static void ChangePermRequest(ref  ChangePermMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyLaserAntenna la = (entity as MyLaserAntenna);
            if (la == null)
                return;
            if (la.DoSetIsPerm(msg.IsPerm != 0))
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }
        static void ChangePermSuccess(ref  ChangePermMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            MyLaserAntenna la = (entity as MyLaserAntenna);
            if (la == null)
                return;
            la.DoSetIsPerm(msg.IsPerm != 0);
        }
        #endregion
        #region change mode
        public void ChangeMode(MyLaserAntenna.StateEnum Mode)
        {
            ChangeMode(Mode, true);
        }
        public void ShiftMode(MyLaserAntenna.StateEnum Mode)//same as ChengeMode but MP client side will not be propagated to server
        {
            ChangeMode(Mode, false);
        }
        protected void ChangeMode(MyLaserAntenna.StateEnum Mode, bool UploadFromClient)
        {
            if (!Sync.MultiplayerActive)
                m_Parent.ChangeMode(Mode);
            else
                if (UploadFromClient || Sync.IsServer)
                {
                    var msg = new ChangeModeMsg();
                    msg.EntityId = m_Parent.EntityId;
                    msg.ModeByte = SetmodeByte(Mode);
                    Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);//TODO!! ChangeModeRequest
                }
        }

        static void ChangeModeRequest(ref ChangeModeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity != null)
            {
                (entity as MyLaserAntenna).ChangeMode(GetMode(msg.ModeByte));
                Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ChangeModeSuccess(ref ChangeModeMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity != null)
            {
                (entity as MyLaserAntenna).DoChangeMode(GetMode(msg.ModeByte));
            }
        }
        /*static void ChangeModeFailure(ref ChangeLaserAntennaMode msg, MyNetworkClient sender)
        {
            //TODO
        }*/
#endregion change mode
        #region change dest
        public void ConnectToRec(long TgtReceiver)
        {
            if (!Sync.MultiplayerActive)
                m_Parent.ConnectTo(TgtReceiver);
            else
            {
                var msg = new ConnectToMsg();
                msg.EntityId = m_Parent.EntityId;
                //msg.ModeByte = SetmodeByte(Mode);
                msg.TargetEntityId = TgtReceiver;
                Sync.Layer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
            }
        }
        static void ConnectToRecRequest(ref ConnectToMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity != null)
            {
                if ((entity as MyLaserAntenna).ConnectTo(msg.TargetEntityId))
                    Sync.Layer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
            }
        }
        static void ConnectToRecSuccess(ref ConnectToMsg msg, MyNetworkClient sender)
        {
            MyEntity entity;
            MyEntities.TryGetEntityById(msg.EntityId, out entity);
            if (entity != null)
            {
                (entity as MyLaserAntenna).DoConnectTo(msg.TargetEntityId);
            }
        }

        #endregion change dest

    }
}
