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
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Network;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Multiplayer
{
    [StaticEventOwner]
    public class MyToolBarCollection
    {
        public static void RequestClearSlot(PlayerId pid, int index)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyToolBarCollection.OnClearSlotRequest, pid.SerialId, index);
        }

        [Event, Reliable, Server]
        static void OnClearSlotRequest(int playerSerialId, int index)
        {
            ulong senderId = GetSenderIdSafe();
            var playerId = new PlayerId(senderId, playerSerialId);
            if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
                return;

            var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);

            toolbar.SetItemAtIndex(index, null);
        }

        public static void RequestChangeSlotItem(PlayerId pid, int index, MyDefinitionId defId)
        {
            DefinitionIdBlit defIdBlit = new DefinitionIdBlit();
            defIdBlit = defId;
            MyMultiplayer.RaiseStaticEvent(s => MyToolBarCollection.OnChangeSlotItemRequest, pid.SerialId, index, defIdBlit);
        }

        [Event, Reliable, Server]
        static void OnChangeSlotItemRequest(int playerSerialId, int index, DefinitionIdBlit defId)
		{
            ulong senderId = GetSenderIdSafe();
            var playerId = new PlayerId(senderId, playerSerialId);
			if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
				return;

			MyDefinitionBase def;
			MyDefinitionManager.Static.TryGetDefinition(defId, out def);
			if (def == null)
				return;

			var ob = MyToolbarItemFactory.ObjectBuilderFromDefinition(def);
			var tItem = MyToolbarItemFactory.CreateToolbarItem(ob);
			var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);
			if (toolbar == null)
				return;
			toolbar.SetItemAtIndex(index, tItem);
		}

		public static void RequestChangeSlotItem(PlayerId pid, int index, MyObjectBuilder_ToolbarItem itemBuilder)
		{
            MyMultiplayer.RaiseStaticEvent(s => MyToolBarCollection.OnChangeSlotBuilderItemRequest, pid.SerialId, index, itemBuilder);
		}

        [Event, Reliable, Server]
        static void OnChangeSlotBuilderItemRequest(int playerSerialId, int index,
            [Serialize(MyObjectFlags.Dynamic, DynamicSerializerType = typeof(MyObjectBuilderDynamicSerializer))] MyObjectBuilder_ToolbarItem itemBuilder)
		{
            ulong senderId = GetSenderIdSafe();
            var playerId = new PlayerId(senderId, playerSerialId);
			if (!MySession.Static.Toolbars.ContainsToolbar(playerId))
				return;

            var tItem = MyToolbarItemFactory.CreateToolbarItem(itemBuilder);
			var toolbar = MySession.Static.Toolbars.TryGetPlayerToolbar(playerId);
			if (toolbar == null)
				return;
            toolbar.SetItemAtIndex(index, tItem);
		}

        public static void RequestCreateToolbar(PlayerId pid)
        {
            MyMultiplayer.RaiseStaticEvent(s => MyToolBarCollection.OnNewToolbarRequest, pid.SerialId);
        }

        [Event, Reliable, Server]
        static void OnNewToolbarRequest(int playerSerialId)
        {
            ulong senderId = GetSenderIdSafe();
            var playerId = new MyPlayer.PlayerId(senderId, playerSerialId);
            MySession.Static.Toolbars.CreateDefaultToolbar(playerId);
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

        private void CreateDefaultToolbar(PlayerId playerId)
        {
            if (ContainsToolbar(playerId))
                return;
            var toolbar = new MyToolbar(MyToolbarType.Character);
            toolbar.Init(MySession.Static.Scenario.DefaultToolbar, null, true);
            AddPlayerToolbar(playerId, toolbar);
        }

        static ulong GetSenderIdSafe()
        {
            if (MyEventContext.Current.IsLocallyInvoked)
                return Sync.MyId;
            else
                return MyEventContext.Current.Sender.Value;
        }
    }
}
