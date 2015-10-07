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
using VRage;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MySyncShipController : MySyncControllableEntity
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

		[MessageIdAttribute(2493, P2PMessageEnum.Reliable)]
		protected struct SetHorizonIndicatorMsg : IEntityMessage
		{
			public long EntityId;
			public long GetEntityId() { return EntityId; }

			public BoolBlit IndicatorState;
		}

        static MySyncShipController()
        {
            MySyncLayer.RegisterEntityMessage<MySyncShipController, UpdatePilotRelativeEntryMsg>(UpdatePilotRelativeEntrySuccess, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, UpdateDampenersMsg>(UpdateDampenersSuccess, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, AttachAutopilotMsg>(OnAutopilotAttached, MyMessagePermissions.FromServer);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, ControlThrustersMsg>(OnSetControlThrusters, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, ControlWheelsMsg>(OnSetControlWheels, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
            MySyncLayer.RegisterEntityMessage<MySyncShipController, SetMainCockpitMsg>(OnSetMainCockpit, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer | MyMessagePermissions.ToSelf);
			MySyncLayer.RegisterEntityMessage<MySyncShipController, SetHorizonIndicatorMsg>(OnChangeHorizonIndicator, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterEntityMessage<MySyncShipController, SetHorizonIndicatorMsg>(OnChangeHorizonIndicator, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
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
            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        private void RaiseDampenersUpdated(bool dampenersEnabled)
        {
            var handler = DampenersUpdated;
            if (handler != null) handler(dampenersEnabled);
        }

        static void UpdateDampenersSuccess(MySyncShipController sync, ref UpdateDampenersMsg msg, MyNetworkClient sender)
        {
            sync.RaiseDampenersUpdated(msg.DampenersEnabled);
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public void SendAutopilotAttached(MyObjectBuilder_AutopilotBase autopilot)
        {
            Debug.Assert(Sync.IsServer, "Sending autopilot attach message on other computer than server!");
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
                (syncObject.m_shipController as MyCockpit).AttachAutopilot(MyAutopilotFactory.CreateAutopilot(message.Autopilot), updateSync: false);
            }
        }

        internal void SetControlThrusters(bool v)
        {
            var msg = new ControlThrustersMsg();
            msg.EntityId = m_shipController.EntityId;
            msg.ControlThrusters = v;
            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        private static void OnSetControlThrusters(MySyncShipController sync, ref ControlThrustersMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.ControlThrusters = msg.ControlThrusters;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        internal void SetControlWheels(bool v)
        {
            var msg = new ControlWheelsMsg();
            msg.EntityId = m_shipController.EntityId;
            msg.ControlWheels = v;
            Sync.Layer.SendMessageToServerAndSelf(ref msg);
        }

        private static void OnSetControlWheels(MySyncShipController sync, ref ControlWheelsMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.ControlWheels = msg.ControlWheels;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

        public void SendSetMainCockpit(bool isMainCockpit)
        {
            SetMainCockpitMsg msg;
            msg.EntityId = m_shipController.EntityId;
            msg.SetMainCockpit = isMainCockpit;
            MySession.Static.SyncLayer.SendMessageToServer(ref msg);
        }

        private static void OnSetMainCockpit(MySyncShipController sync, ref SetMainCockpitMsg msg, MyNetworkClient sender)
        {
            sync.m_shipController.IsMainCockpit = msg.SetMainCockpit;
            if (Sync.IsServer)
            {
                Sync.Layer.SendMessageToAllButOne(ref msg, sender.SteamUserId);
            }
        }

		public void SendHorizonIndicatorChanged(bool newState)
		{
			SetHorizonIndicatorMsg msg;
			msg.EntityId = m_shipController.EntityId;
			msg.IndicatorState = newState;

			MySession.Static.SyncLayer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		private static void OnChangeHorizonIndicator(MySyncShipController sync, ref SetHorizonIndicatorMsg msg, MyNetworkClient sender)
		{
			if (sync.m_shipController == null)
				return;

			sync.m_shipController.HorizonIndicatorEnabled = msg.IndicatorState;

			if(Sync.IsServer)
				MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);

		}

    }
}
