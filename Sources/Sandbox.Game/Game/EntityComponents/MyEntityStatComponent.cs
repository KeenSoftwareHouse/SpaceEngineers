using ProtoBuf;
using Sandbox.Common.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Collections;
using VRage.Components;
using VRage.Game.ObjectBuilders;
using VRage.Utils;

namespace Sandbox.Game.Components
{
	[PreloadRequired]
	[MyComponentBuilder(typeof(MyObjectBuilder_EntityStatComponent))]
	public class MyEntityStatComponent : MyEntityComponentBase
	{
		#region Sync
		#region Sync messages

		[ProtoContract]
		struct StatInfo
		{
			[ProtoMember]
			public MyStringHash StatId;

			[ProtoMember]
			public float Amount;

			[ProtoMember]
			public float RegenLeft;
		}

		[ProtoContract]
		[MessageId(2154, P2PMessageEnum.Reliable)]
		struct EntityStatChangedMsg
		{
			[ProtoMember]
			public long EntityId;

			[ProtoMember]
			public List<StatInfo> ChangedStats;
		}

		[ProtoContract]
		[MessageId(2155, P2PMessageEnum.Reliable)]
		struct StatActionRequestMsg
		{
			[ProtoMember]
			public long EntityId;
		}

		[ProtoContract]
		[MessageId(2156, P2PMessageEnum.Reliable)]
		struct StatActionMsg
		{
			[ProtoMember]
			public long EntityId;

			[ProtoMember]
			public Dictionary<string, MyStatLogic.MyStatAction> StatActions;
		}

		#endregion


		#region Sync callbacks

		static MyEntityStatComponent()
		{
			MySyncLayer.RegisterMessage<EntityStatChangedMsg>(OnStatChangedRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<EntityStatChangedMsg>(OnStatChangedMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
			MySyncLayer.RegisterMessage<StatActionRequestMsg>(OnStatActionRequest, MyMessagePermissions.ToServer, MyTransportMessageEnum.Request);
			MySyncLayer.RegisterMessage<StatActionMsg>(OnStatActionMessage, MyMessagePermissions.FromServer, MyTransportMessageEnum.Success);
		}

		public void RequestStatChange(MyEntityStat stat)
		{
			EntityStatChangedMsg msg = new EntityStatChangedMsg();
			msg.EntityId = Entity.EntityId;
			msg.ChangedStats = new List<StatInfo>();

			msg.ChangedStats.Add(new StatInfo() { StatId = stat.StatId, Amount = stat.Value });	// Regen left not used

			MySession.Static.SyncLayer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		private static void OnStatChangedRequest(ref EntityStatChangedMsg msg, MyNetworkClient sender)
		{
			Debug.Assert(Sync.IsServer);

			MyEntity entity;
			if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
				return;

			MyEntityStatComponent statComp = null;
			if (!entity.Components.TryGet<MyEntityStatComponent>(out statComp))
				return;

			foreach (var statChange in msg.ChangedStats)
			{
				MyEntityStat localStat;
				if (!statComp.TryGetStat(statChange.StatId, out localStat))
					continue;
				localStat.Value = statChange.Amount;
			}
		}

		private void SendStatsChanged(List<MyEntityStat> stats)
		{
			Debug.Assert(Sync.IsServer);

			EntityStatChangedMsg msg = new EntityStatChangedMsg();
			msg.EntityId = Entity.EntityId;
			msg.ChangedStats = new List<StatInfo>();

			foreach (var stat in stats)
			{
				stat.CalculateRegenLeftForLongestEffect();
				msg.ChangedStats.Add(new StatInfo() { StatId = stat.StatId, Amount = stat.Value, RegenLeft = stat.StatRegenLeft, });
			}

			MySession.Static.SyncLayer.SendMessageToAll(ref msg, MyTransportMessageEnum.Success);
		}

		private static void OnStatChangedMessage(ref EntityStatChangedMsg msg, MyNetworkClient sender)
		{
			MyEntity entity;
			if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
				return;

			MyEntityStatComponent statComp = null;
			if (!entity.Components.TryGet<MyEntityStatComponent>(out statComp))
				return;

			foreach (var statChange in msg.ChangedStats)
			{
				MyEntityStat localStat;
				if (!statComp.TryGetStat(statChange.StatId, out localStat))
					continue;
				localStat.Value = statChange.Amount;
				localStat.StatRegenLeft = statChange.RegenLeft;
			}
		}

		private void RequestStatActions()
		{
			StatActionRequestMsg msg = new StatActionRequestMsg()
			{
					EntityId = Entity.EntityId,
			};

			MySession.Static.SyncLayer.SendMessageToServer(ref msg, MyTransportMessageEnum.Request);
		}

		private static void OnStatActionRequest(ref StatActionRequestMsg msg, MyNetworkClient sender)
		{
			Debug.Assert(Sync.IsServer);
			MyEntity entity = null;
			if(!MyEntities.TryGetEntityById(msg.EntityId, out entity))
				return;

			MyEntityStatComponent statComponent = null;
			if (!entity.Components.TryGet<MyEntityStatComponent>(out statComponent))
				return;

			StatActionMsg actionMsg = new StatActionMsg()
			{
				EntityId = msg.EntityId,
				StatActions = new Dictionary<string, MyStatLogic.MyStatAction>(),
			};

			foreach(var script in statComponent.m_scripts)
			{
				foreach(var actionPair in script.StatActions)
				{
					actionMsg.StatActions.Add(actionPair.Key, actionPair.Value);
				}
			}

			MySession.Static.SyncLayer.SendMessage(ref actionMsg, sender.SteamUserId, MyTransportMessageEnum.Success);
		}

		private static void OnStatActionMessage(ref StatActionMsg msg, MyNetworkClient sender)
		{
			if (msg.StatActions == null)
				return;

			MyEntity entity = null;
			if (!MyEntities.TryGetEntityById(msg.EntityId, out entity))
				return;

			MyEntityStatComponent statComponent = null;
			if (!entity.Components.TryGet<MyEntityStatComponent>(out statComponent))
				return;

			MyStatLogic script = new MyStatLogic();
			script.Init(entity as IMyCharacter, statComponent.m_stats, "LocalStatActionScript");
			foreach(var actionPair in msg.StatActions)
			{
				script.AddAction(actionPair.Key, actionPair.Value);
			}

			statComponent.m_scripts.Add(script);
		}

		#endregion
		#endregion

		private Dictionary<MyStringHash, MyEntityStat> m_stats;
		public DictionaryValuesReader<MyStringHash, MyEntityStat> Stats { get { return new DictionaryValuesReader<MyStringHash, MyEntityStat>(m_stats); } }

		protected List<MyStatLogic> m_scripts;
		private static List<MyEntityStat> m_statSyncList = new List<MyEntityStat>();

		private int m_updateCounter = 0;

		public MyEntityStatComponent()
		{
			m_stats = new Dictionary<MyStringHash, MyEntityStat>(MyStringHash.Comparer);
			m_scripts = new List<MyStatLogic>();
		}

		#region Serialization

		public override MyObjectBuilder_ComponentBase Serialize()
		{
			var baseBuilder = base.Serialize();
			var builder = baseBuilder as MyObjectBuilder_CharacterStatComponent;

			if (builder == null)
				return baseBuilder;

			builder.Stats = null;
			builder.ScriptNames = null;

			if (m_stats != null && m_stats.Count > 0)
			{
				builder.Stats = new MyObjectBuilder_EntityStat[m_stats.Count];
				int index = 0;
				foreach (var stat in m_stats)
				{
					builder.Stats[index++] = stat.Value.GetObjectBuilder();
				}
			}

			if (m_scripts != null && m_scripts.Count > 0)
			{
				builder.ScriptNames = new string[m_scripts.Count];
				int index = 0;
				foreach (var script in m_scripts)
				{
					builder.ScriptNames[index++] = script.Name;
				}
			}

			return builder;
		}

		public override void Deserialize(MyObjectBuilder_ComponentBase objectBuilder)
		{
			var builder = objectBuilder as MyObjectBuilder_CharacterStatComponent;
			if (builder != null)
			{
				if (builder.Stats != null)
				{
					foreach (var stat in builder.Stats)
					{
						MyEntityStatDefinition statDefinition = null;
						if(MyDefinitionManager.Static.TryGetDefinition<MyEntityStatDefinition>(new MyDefinitionId(stat.TypeId, stat.SubtypeId), out statDefinition)
							&& statDefinition.Enabled
							&& ((statDefinition.EnabledInCreative && MySession.Static.CreativeMode)
							|| (statDefinition.AvailableInSurvival && MySession.Static.SurvivalMode)))
							AddStat(stat.SubtypeId, stat);
					}
				}

				if (builder.ScriptNames != null && Sync.IsServer)
				{
					foreach (var scriptName in builder.ScriptNames)
					{
						InitScript(scriptName);
					}
				}
			}

			base.Deserialize(objectBuilder);
		}

		public override bool IsSerialized()
		{
			return true;
		}

		#endregion

		public void InitStats(MyStatsDefinition definition)
		{
			if (definition == null || !definition.Enabled || (!definition.AvailableInSurvival && MySession.Static.SurvivalMode))
				return;

			foreach (var statId in definition.Stats)
			{
				MyEntityStatDefinition statDefinition = null;
				if (!MyDefinitionManager.Static.TryGetDefinition(statId, out statDefinition))
					continue;

				if (!statDefinition.Enabled
					|| (!statDefinition.EnabledInCreative && MySession.Static.CreativeMode)
					|| (!statDefinition.AvailableInSurvival && MySession.Static.SurvivalMode))
					continue;

				var nameHash = MyStringHash.GetOrCompute(statDefinition.Name);
				MyEntityStat existingStat = null;
				if (m_stats.TryGetValue(statId.SubtypeId, out existingStat) && existingStat.StatId == nameHash)
					continue;

				var builder = new MyObjectBuilder_EntityStat();
				builder.SubtypeName = statId.SubtypeName;
				builder.MaxValue = 1.0f;
				builder.Value = statDefinition.DefaultValue / statDefinition.MaxValue;
				AddStat(nameHash, builder);
			}

			if (Sync.IsServer)	// Only init scripts on server
			{
                // MW: remove all scripts because of the broken saves (Medieval character has multiple scripts (peasant's and player's))
                foreach (var script in m_scripts)
                {
                    script.Close();
                }
                m_scripts.Clear();

				foreach (var scriptName in definition.Scripts)
				{
					InitScript(scriptName);
				}
			}
			else
			{
				RequestStatActions();	// Only request the stat actions from the server
			}
		}

		public virtual void Update()
		{
			var entity = Container.Entity;
			if (entity == null)
				return;

			foreach (var script in m_scripts)
			{
				script.Update();
			}

			if (m_updateCounter++ % 10 == 0)
			{
				foreach (var script in m_scripts)
				{
					script.Update10();
				}
			}

			foreach (var stat in m_stats.Values)
			{
				stat.Update();
				if (Sync.IsServer && stat.ShouldSync)
					m_statSyncList.Add(stat);
			}

			if (m_statSyncList.Count > 0)
			{
				SendStatsChanged(m_statSyncList);
				m_statSyncList.Clear();
			}
		}

		public bool TryGetStat(MyStringHash statId, out MyEntityStat outStat)
		{
			return m_stats.TryGetValue(statId, out outStat);
		}

		public override void OnAddedToContainer()
		{
			base.OnAddedToContainer();

			foreach (var script in m_scripts)
			{
				script.Character = Entity as IMyCharacter;
			}
		}

		public override void OnBeforeRemovedFromContainer()
		{
			foreach (var script in m_scripts)
			{
				script.Close();
			}

			base.OnBeforeRemovedFromContainer();
		}

		public bool CanDoAction(string actionId)
		{
			if (m_scripts == null || m_scripts.Count == 0)
				return true;

			bool cannotPerformAction = true;
			foreach (var script in m_scripts)
			{
				cannotPerformAction &= !script.CanDoAction(actionId);
			}

			return !cannotPerformAction;
		}

		public bool DoAction(string actionId)
		{
			bool actionPerformed = false;
			foreach (var script in m_scripts)
			{
				if (script.DoAction(actionId))
					actionPerformed = true;
			}
			return actionPerformed;
		}

		#region Private helper methods

		private void InitScript(string scriptName)
		{
			Type scriptType;
			if (MyScriptManager.Static.StatScripts.TryGetValue(scriptName, out scriptType))
			{
				var script = (MyStatLogic)Activator.CreateInstance(scriptType);
				Debug.Assert(script != null, "Stat script could not be initialized!");
				if (script == null)
					return;

				script.Init(Entity as IMyCharacter, m_stats, scriptName);
				m_scripts.Add(script);
			}
		}

		private MyEntityStat AddStat(MyStringHash statId, MyObjectBuilder_EntityStat objectBuilder)
		{
			MyEntityStat stat = null;

			if (!m_stats.TryGetValue(statId, out stat))
			{
				stat = new MyEntityStat();
				stat.Init(objectBuilder);

				m_stats.Add(statId, stat);
			}
			else
				stat.ClearEffects();

			return stat;
		}

		#endregion

        public override string ComponentTypeDebugString
        {
            get { return "Stats"; }
        }
    }
}
