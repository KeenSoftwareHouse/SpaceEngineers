using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.AI;
using ProtoBuf;
using SteamSDK;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    class MySyncShipController : MySyncControllableEntity
    {
        [MessageIdAttribute(2480, P2PMessageEnum.Reliable)]
        protected struct UpdatePilotRelativeEntryMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public MyPositionAndOrientation RelativeEntry;
        }

        [MessageIdAttribute(2481, P2PMessageEnum.Reliable)]
        protected struct UpdateDampenersMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit DampenersEnabled;
        }

        [ProtoContract]
        [MessageIdAttribute(2487, P2PMessageEnum.Reliable)]
        protected struct AttachAutopilotMsg : IEntityMessage
        {
            [ProtoMember]
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            [ProtoMember]
            public MyObjectBuilder_AutopilotBase Autopilot;
        }


        [MessageIdAttribute(2488, P2PMessageEnum.Reliable)]
        protected struct ControlThrustersMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit ControlThrusters;
        }

        [MessageIdAttribute(2489, P2PMessageEnum.Reliable)]
        protected struct ControlWheelsMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit ControlWheels;
        }

        [MessageIdAttribute(2492, P2PMessageEnum.Reliable)]
        protected struct SetMainCockpitMsg : IEntityMessage
        {
            public long EntityId;
            public long GetEntityId() { return EntityId; }

            public BoolBlit SetMainCockpit;
        }

        static MySyncShipController()
        {
            MySyncLayer.RegisterEntityMessage<MySyncShipController, UpdatePilotRelativeEntryMsg>(UpdatePilotRelativeEntrySuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, UpdateDampenersMsg>(UpdateDampenersSuccess, MyMessagePermissions.Any, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, AttachAutopilotMsg>(OnAutopilotAttached, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, ControlThrustersMsg>(OnSetControlThrusters, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, ControlWheelsMsg>(OnSetControlWheels, MyMessagePermissions.Any);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, SetMainCockpitMsg>(OnSetMainCockpit, MyMessagePermissions.Any);
        }

        private MyShipController m_shipController;

        public event Action<MyPositionAndOrientation> PilotRelativeEntryUpdated;
        public event Action<bool> DampenersUpdated;

        public MySyncShipController(MyShipController shipController) :
            base(shipController)
        {
            this.m_shipController = shipController;
        }

        static void UpdatePilotRelativeEntrySuccess(MySyncShipController sync, ref UpdatePilotRelativeEntryMsg msg, MyNetworkClient sender)
        {
            sync.RaisePilotRelativeEntryUpdated(msg.RelativeEntry);
        }

        private void RaisePilotRelativeEntryUpdated(MyPositionAndOrientation relativeEntry)
        {
            var handler = PilotRelativeEntryUpdated;
            if (handler != null) handler(relativeEntry);
        }

        public void SendPilotRelativeEntryUpdate(ref MyPositionAndOrientation relativeEntry)
        {
            UpdatePilotRelativeEntryMsg msg;
            msg.EntityId = m_shipController.EntityId;
            msg.RelativeEntry = relativeEntry;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }


        public void SendDampenersUpdate(bool dampenersEnabled)
        {
            UpdateDampenersMsg msg;
            msg.EntityId = m_shipController.EntityId;
            msg.DampenersEnabled = dampenersEnabled;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
        }

        private void RaiseDampenersUpdated(bool dampenersEnabled)
        {
            var handler = DampenersUpdated;
            if (handler != null) handler(dampenersEnabled);
        }

        static void UpdateDampenersSuccess(MySyncShipController sync, ref UpdateDampenersMsg msg, MyNetworkClient sender)
        {
            sync.RaiseDampenersUpdated(msg.DampenersEnabled);
        }

        public void SendAutopilotAttached(MyObjectBuilder_AutopilotBase autopilot)
        {
            AttachAutopilotMsg msg;
            msg.EntityId = m_shipController.EntityId;
            msg.Autopilot = autopilot;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        private static void OnAutopilotAttached(MySyncShipController syncObject, ref AttachAutopilotMsg message, MyNetworkClient sender)
        {
            var cockpit = syncObject.m_shipController as MyCockpit;
            Debug.Assert(cockpit != null, "Trying to assing autopilot to something else than cockpit!");
            if (cockpit != null)
            {
                (syncObject.m_shipController as MyCockpit).AttachAutopilot(MyAutopilotFactory.CreateAutopilot(message.Autopilot));
            }
        }

        internal void SetControlThrusters(bool v)
        {
            var msg = new ControlThrustersMsg();
            msg.EntityId = m_shipController.EntityId;
            msg.ControlThrusters = v;
            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnSetControlThrusters(MySyncShipController sync, ref ControlThrustersMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.ControlThrusters = msg.ControlThrusters;
        }

        internal void SetControlWheels(bool v)
        {
            var msg = new ControlWheelsMsg();
            msg.EntityId = m_shipController.EntityId;
            msg.ControlWheels = v;
            Sync.Layer.SendMessageToAllAndSelf(ref msg);
        }

        private static void OnSetControlWheels(MySyncShipController sync, ref ControlWheelsMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.ControlWheels = msg.ControlWheels;
        }

        public void SendSetMainCockpit(bool isMainCockpit)
        {
            SetMainCockpitMsg msg;
            msg.EntityId = m_shipController.EntityId;
            msg.SetMainCockpit = isMainCockpit;
            MySession.Static.SyncLayer.SendMessageToAll(ref msg);
        }

        private static void OnSetMainCockpit(MySyncShipController sync, ref SetMainCockpitMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.IsMainCockpit = msg.SetMainCockpit;
        }
    }
}
