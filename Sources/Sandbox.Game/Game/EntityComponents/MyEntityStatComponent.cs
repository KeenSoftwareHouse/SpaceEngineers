using ProtoBuf;
using VRage.Game.ObjectBuilders.ComponentSystem;
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
using System.Linq;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Utils;
using VRage.Game.Entity;
using VRage;
using Sandbox.Game.EntityComponents;
using VRage.Game;

namespace Sandbox.Game.Components
{
	[PreloadRequired]
    [MyComponentType(typeof(MyEntityStatComponent))]
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
                    if (!actionMsg.StatActions.ContainsKey(actionPair.Key))
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
        private bool m_statActionsRequested = false;

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

            // Because of switching helmet on/off
            foreach (var script in m_scripts)
            {
                script.Close();
            }
            m_scripts.Clear();

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
							AddStat(MyStringHash.GetOrCompute(statDefinition.Name), stat, true);
					}
				}

				if (builder.ScriptNames != null && Sync.IsServer)
				{
                    // Should fix broken saves
                    // I assume that StatComponent should hold only once instance per script
                    builder.ScriptNames = builder.ScriptNames.Distinct().ToArray();

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

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            MyEntityStatComponentDefinition entityStatDefinition = definition as MyEntityStatComponentDefinition;
            Debug.Assert(entityStatDefinition != null);

            if (entityStatDefinition == null || !entityStatDefinition.Enabled || MySession.Static == null || (!entityStatDefinition.AvailableInSurvival && MySession.Static.SurvivalMode))
                return;

            foreach (var statId in entityStatDefinition.Stats)
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
                if (m_stats.TryGetValue(nameHash, out existingStat) && existingStat.StatDefinition.Id.SubtypeId == statDefinition.Id.SubtypeId)
                    continue;

                var builder = new MyObjectBuilder_EntityStat();
                builder.SubtypeName = statId.SubtypeName;
                builder.MaxValue = 1.0f;
                builder.Value = statDefinition.DefaultValue / statDefinition.MaxValue;
                AddStat(nameHash, builder);
            }

            if (Sync.IsServer)	// Only init scripts on server
            {
                Debug.Assert(m_scripts.Count == 0);
                foreach (var scriptName in entityStatDefinition.Scripts)
                {
                    InitScript(scriptName);
                }
                m_statActionsRequested = true;
            }
        }

		public virtual void Update()
		{
			var entity = Container.Entity;
			if (entity == null)
				return;

            if(!m_statActionsRequested)
            {
                RequestStatActions();   // Only request the stat actions from the server
                m_statActionsRequested = true;
            }

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

        public bool CanDoAction(string actionId, out MyTuple<ushort, MyStringHash> message, bool continuous = false)
        {
            message = new MyTuple<ushort, MyStringHash>(0, MyStringHash.NullOrEmpty);

            if (m_scripts == null || m_scripts.Count == 0)
                return true;

			bool cannotPerformAction = true;
			foreach (var script in m_scripts)
			{
                MyTuple<ushort, MyStringHash> msg;
                cannotPerformAction &= !script.CanDoAction(actionId, continuous, out msg);
                if (msg.Item1 != 0)
                    message = msg;
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

        public void ApplyModifier(string modifierId)
        {
            foreach (var script in m_scripts)
            {
                script.ApplyModifier(modifierId);
            }
        }

        public float GetEfficiencyModifier(string modifierId)
        {
            float result = 1.0f;
            foreach (var script in m_scripts)
            {
                result *= script.GetEfficiencyModifier(modifierId);
            }
            return result;
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

		private MyEntityStat AddStat(MyStringHash statId, MyObjectBuilder_EntityStat objectBuilder, bool forceNewValues = false)
		{
			MyEntityStat stat = null;

            if (m_stats.TryGetValue(statId, out stat))
            {
                if(!forceNewValues)
                    objectBuilder.Value = stat.CurrentRatio;

                stat.ClearEffects();
                m_stats.Remove(statId);
            }

            stat = new MyEntityStat();
            stat.Init(objectBuilder);

            m_stats.Add(statId, stat);

            return stat;
		}

		#endregion

        public override string ComponentTypeDebugString
        {
            get { return "Stats"; }
        }
    }
}
