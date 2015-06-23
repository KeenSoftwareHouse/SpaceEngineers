using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Serialization;
using PlayerId = Sandbox.Game.World.MyPlayer.PlayerId;

using Sandbox.Definitions;

namespace Sandbox.Game.Multiplayer
{
    [PreloadRequired]
    public class MyToolBarCollection
    {
        [MessageId(14568, P2PMessageEnum.Reliable)]
        struct PlayerToolbarCreatedMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
        }

        [MessageId(14567, P2PMessageEnum.Reliable)]
        struct PlayerToolbarClearSlotMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
            public int Index;
        }

        [MessageId(14569, P2PMessageEnum.Reliable)]
        struct PlayerToolbarChangeSlotMsg
        {
            public ulong ClientSteamId;
            public int PlayerSerialId;
            public int Index;
            public DefinitionIdBlit DefId;
        }

		[ProtoContract]
		[MessageId(14575, P2PMessageEnum.Reliable)]
		struct PlayerToolbarChangeSlotBuilderMsg
		{
			[ProtoMember]
			public ulong ClientSteamId;

			[ProtoMember]
			public int PlayerSerialId;

			[ProtoMember]
			public int Index;

			[ProtoMember]
			public MyObjectBuilder_ToolbarItem itemBuilder;
		}

        static MyToolBarCollection()
        {
            MySyncLayer.RegisterMessage<PlayerToolbarCreatedMsg>(OnNewToolbarRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<PlayerToolbarChangeSlotMsg>(OnChangeSlotItemRequest, MyMessagePermissions.ToServer);
			MySyncLayer.RegisterMessage<PlayerToolbarChangeSlotBuilderMsg>(OnChangeSlotBuilderItemRequest, MyMessagePermissions.ToServer);
            MySyncLayer.RegisterMessage<PlayerToolbarClearSlotMsg>(OnClearSlotRequest, MyMessagePermissions.ToServer);
        }

        public static void RequestClearSlot(PlayerId pid, int index)
        {
            var msg = new PlayerToolbarClearSlotMsg();
            msg.ClientSteamId = pid.SteamId;
            msg.PlayerSerialId = pid.SerialId;
            msg.Index = index;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        static void OnClearSlotRequest(ref PlayerToolbarClearSlotMsg msg, MyNetworkClient sender)
        {
            var playerId = new PlayerId(sender.SteamUserId, msg.PlayerSerialId);
            if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
                return;

            var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);

            toolbar.SetItemAtIndex(msg.Index, null);
        }

        public static void RequestChangeSlotItem(PlayerId pid, int index, MyDefinitionId defId)
        {
            var msg = new PlayerToolbarChangeSlotMsg();
            msg.ClientSteamId = pid.SteamId;
            msg.PlayerSerialId = pid.SerialId;
            msg.Index = index;
            msg.DefId = defId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

		static void OnChangeSlotItemRequest(ref PlayerToolbarChangeSlotMsg msg, MyNetworkClient sender)
		{
			var playerId = new PlayerId(sender.SteamUserId, msg.PlayerSerialId);
			if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
				return;

			MyDefinitionBase def;
			MyDefinitionManager.Static.TryGetDefinition(msg.DefId, out def);
			if (def == null)
				return;

			var ob = MyToolbarItemFactory.ObjectBuilderFromDefinition(def);
			var tItem = MyToolbarItemFactory.CreateToolbarItem(ob);
			var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);
			if (toolbar == null)
				return;
			toolbar.SetItemAtIndex(msg.Index, tItem);
		}

		public static void RequestChangeSlotItem(PlayerId pid, int index, MyObjectBuilder_ToolbarItem itemBuilder)
		{
			var msg = new PlayerToolbarChangeSlotBuilderMsg();
			msg.ClientSteamId = pid.SteamId;
			msg.PlayerSerialId = pid.SerialId;
			msg.Index = index;
			msg.itemBuilder = itemBuilder;

			Sync.Layer.SendMessageToServer(ref msg);
		}

		static void OnChangeSlotBuilderItemRequest(ref PlayerToolbarChangeSlotBuilderMsg msg, MyNetworkClient sender)
		{
			var playerId = new PlayerId(sender.SteamUserId, msg.PlayerSerialId);
			if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
				return;

			var tItem = MyToolbarItemFactory.CreateToolbarItem(msg.itemBuilder);
			var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);
			if (toolbar == null)
				return;
			toolbar.SetItemAtIndex(msg.Index, tItem);
		}

        public static void RequestCreateToolbar(PlayerId pid)
        {
            var msg = new PlayerToolbarCreatedMsg();
            msg.ClientSteamId = pid.SteamId;
            msg.PlayerSerialId = pid.SerialId;

            Sync.Layer.SendMessageToServer(ref msg);
        }

        static void OnNewToolbarRequest(ref PlayerToolbarCreatedMsg msg, MyNetworkClient sender)
        {
            var playerId = new MyPlayer.PlayerId(sender.SteamUserId, msg.PlayerSerialId);
            if (MySession.Static.Toolbars.ContainsToolbar(playerId))
                return;
            var toolbar = new MyToolbar(MyToolbarType.Character);
            MySession.Static.Toolbars.AddPlayerToolbar(playerId, toolbar);
        }

        private Dictionary<PlayerId, MyToolbar> m_playerToolbars = new Dictionary<PlayerId, MyToolbar>();

        public bool AddPlayerToolbar(PlayerId pid, MyToolbar toolbar)
        {

            if (pid == null || toolbar == null)
                return false;

            MyToolbar result;
            var success = m_playerToolbars.TryGetValue(pid, out result);

            if (!success)
            {
                m_playerToolbars.Add(pid, toolbar);
                return true;
            }
            else 
            {
                return false;
            }
        }

        public bool RemovePlayerToolbar(PlayerId pid)
        {
            if (pid == null)
                return false;

            var ret = m_playerToolbars.Remove(pid);
            return ret;
        }

        public MyToolbar TryGetPlayerToolbar(PlayerId pid)
        {
            if (pid == null)
                return null;

            MyToolbar result;
            m_playerToolbars.TryGetValue(pid, out result);

            return result;
        }

        public bool ContainsToolbar(PlayerId pid)
        {
            return m_playerToolbars.ContainsKey(pid);
        }

        public void LoadToolbars(MyObjectBuilder_Checkpoint checkpoint)
        {
            if (checkpoint.AllPlayersData != null)
            {
                foreach (var item in checkpoint.AllPlayersData.Dictionary)
                {
                    var playerId = new PlayerId(item.Key.ClientId, item.Key.SerialId);
                    var toolbar = new MyToolbar(MyToolbarType.Character);
                    toolbar.Init(item.Value.Toolbar, null, true);
                    AddPlayerToolbar(playerId, toolbar);
                }
            }
        }

        //public SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Toolbar> GetSerDictionary()
        //{ 
        //    var ret = new SerializableDictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Toolbar>();
        //    foreach (var item in m_playerToolbars)
        //    {
        //        var pid = new MyObjectBuilder_Checkpoint.PlayerId(item.Key.SteamId);
        //        pid.SerialId = item.Key.SerialId;

        //        var ob = item.Value.GetObjectBuilder();
        //        ret.Dictionary.Add(pid, ob);
        //    }
        //    return ret;
        //}

        public void SaveToolbars(MyObjectBuilder_Checkpoint checkpoint)
        {
            Dictionary<MyObjectBuilder_Checkpoint.PlayerId, MyObjectBuilder_Player> checkpointData = checkpoint.AllPlayersData.Dictionary;
            foreach (var toolbar in m_playerToolbars)
            {
                var pid = new MyObjectBuilder_Checkpoint.PlayerId(toolbar.Key.SteamId);
                pid.SerialId = toolbar.Key.SerialId;
                if (checkpointData.ContainsKey(pid))
                {
                    checkpointData[pid].Toolbar = toolbar.Value.GetObjectBuilder();
                }
            }
        }
    }
}
