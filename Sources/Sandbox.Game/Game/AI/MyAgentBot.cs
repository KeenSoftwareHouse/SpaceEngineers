using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.AI.Bot;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.AI;
using Sandbox.Game.AI.BehaviorTree;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRageMath;

namespace Sandbox.Game.AI
{
    [BehaviorType(typeof(MyObjectBuilder_AgentDefinition))]
    public class MyAgentBot : IMyEntityBot
    {
        protected MyPlayer m_player;
        public MyPlayer Player { get { return m_player; } }

        protected MyBotNavigation m_navigation;
        public MyBotNavigation Navigation { get { return m_navigation; } }

        public MyCharacter AgentEntity
        {
            get
            {
                if (m_player.Controller.ControlledEntity != null)
                    return m_player.Controller.ControlledEntity as MyCharacter;
                else
                    return null;
            }
        }
        public MyEntity BotEntity { get { return AgentEntity; } }

        protected MyBehaviorTree m_behaviorTree;
        public MyBehaviorTree BehaviorTree
        {
            get { return m_behaviorTree; }
            set 
            { 
                m_behaviorTree = value;
                if (m_behaviorTree != null)
                    BehaviorSubtypeName = m_behaviorTree.BehaviorTreeName;
                else
                    BehaviorSubtypeName = null;
            }
        }

        private string m_behaviorTreeName;
        public string BehaviorSubtypeName 
        {
            get { return m_behaviorTreeName; }
            set { m_behaviorTreeName = value; }
        }

        protected ActionCollection m_actionCollection;
        public ActionCollection ActionCollection { get { return m_actionCollection; } }

        protected MyBotMemory m_botMemory;
        public MyBotMemory BotMemory { get { return m_botMemory; } }

        protected MyAgentBotActionProxy m_actions;
        public MyAgentBotActionProxy AgentActions
        {
            get { return m_actions; }
        }
        public MyAbstractBotActionProxy BotActions
        {
            get { return m_actions; }
            set
            {
                Debug.Assert(value is MyAgentBotActionProxy, "Invalid action proxy type");
                m_actions = value as MyAgentBotActionProxy;
            }
        }

        protected MyAgentDefinition m_botDefinition;
        public MyBotDefinition BotDefinition { get { return m_botDefinition; } }
        public MyAgentDefinition AgentDefinition { get { return m_botDefinition; } }

        protected MyAgentLogic m_botLogic;
        public MyBotLogic BotLogic { get { return m_botLogic; } }
        public MyAgentLogic AgentLogic { get { return m_botLogic; } }
        public bool HasLogic { get { return m_botLogic != null; } }

        private bool m_respawnRequestSent;
        private long m_deathTimestamp;
        private bool m_removeAfterDeath;

        public virtual bool ShouldFollowPlayer
        { // MW:TODO remove hack
            set { Debug.Assert(false, "Shouldnt get here"); }
            get { return false; }
        }

        public virtual bool IsValidForUpdate
        {
            get
            {
                return m_player != null
                    && m_player.Controller.ControlledEntity != null
                    && m_player.Controller.ControlledEntity.Entity != null
                    && !AgentEntity.IsDead;
            }
        }

        public bool CreatedByPlayer
        {
            get;
            set;
        }

        public MyAgentBot(MyPlayer player, MyBotDefinition botDefinition)
        {
            m_player = player;
            m_navigation = new MyBotNavigation(); 
            m_actionCollection = null;
            m_botMemory = new MyBotMemory(this);
            m_botDefinition = botDefinition as MyAgentDefinition;

            m_player.Controller.ControlledEntityChanged += Controller_ControlledEntityChanged;
            m_navigation.ChangeEntity(m_player.Controller.ControlledEntity);
        }

        protected virtual void Controller_ControlledEntityChanged(IMyControllableEntity oldEntity, IMyControllableEntity newEntity)
        {
            if (oldEntity == null && newEntity is MyCharacter)
            {
                m_deathTimestamp = 0;
                m_respawnRequestSent = false;
            }

            m_navigation.ChangeEntity(newEntity);
            m_navigation.ResetAiming(true);
            if (HasLogic)
                m_botLogic.OnControlledEntityChanged(newEntity);
        }

        public virtual void Init(MyObjectBuilder_Bot botBuilder)
        {
            var ob = botBuilder as MyObjectBuilder_AgentBot;
            if (ob == null)
                return;

            if (ob.AiTarget != null)
                AgentActions.AiTarget.Init(ob.AiTarget);
            if (botBuilder.BotMemory != null)
                m_botMemory.Init(botBuilder.BotMemory);
            BehaviorSubtypeName = ob.LastBehaviorTree;
        }

        public virtual void InitActions(ActionCollection actionCollection)
        {
            m_actionCollection = actionCollection;
        }

        public virtual void InitLogic(MyBotLogic botLogic)
        {
            m_botLogic = botLogic as MyAgentLogic;
            if (HasLogic)
            {
                m_botLogic.Init();
                if (AgentEntity != null)
                    AgentLogic.OnCharacterControlAcquired(AgentEntity);
            }
        }

        public virtual void Spawn(Vector3D? spawnPosition, bool spawnedByPlayer)
        {
            CreatedByPlayer = spawnedByPlayer;
            var character = m_player.Controller.ControlledEntity as MyCharacter;
            if (character != null && character.IsDead || m_player.Identity.IsDead)
            {
                if (!m_respawnRequestSent)
                {
                    m_respawnRequestSent = true;
                    ProfilerShort.Begin("Bot.RespawnRequest");
                    MyPlayerCollection.RespawnRequest(false, false, 0, null, m_player.Id.SerialId, spawnPosition);
                    ProfilerShort.End();
                }
            }
        }

        public virtual void Cleanup()
        {
            m_navigation.Cleanup();
            if (HasLogic)
                m_botLogic.Cleanup();
            m_player.Controller.ControlledEntityChanged -= Controller_ControlledEntityChanged;
            m_player = null;
        }

        public void Update()
        {
            if (m_player.Controller.ControlledEntity != null)
            {
                if (AgentEntity.IsDead && !m_respawnRequestSent)
                {
                    HandleDeadBot();
                }
                else
                {
                    UpdateInternal();
                }
            }
            else if (m_deathTimestamp != 0 && !m_respawnRequestSent)
            {
                HandleDeadBot();
            }
        }

        protected virtual void UpdateInternal()
        {
            m_navigation.Update();
            AgentActions.AiTarget.Update();
            m_botLogic.Update();
        }

        public virtual void Reset()
        {
            if (BehaviorTree != null)
                BotMemory.ResetMemory(BehaviorTree, true); 
            m_navigation.StopImmediate(true);
            AgentActions.AiTarget.UnsetTarget();
        }

        public virtual MyObjectBuilder_Bot GetBotData()
        {
            MyObjectBuilder_AgentBot botData = new MyObjectBuilder_AgentBot(); // MW:TODO replace with proper object builders
            botData.BotDefId = BotDefinition.Id;
            botData.AiTarget = AgentActions.AiTarget.GetObjectBuilder();
            botData.BotMemory = m_botMemory.GetObjectBuilder();
            botData.LastBehaviorTree = BehaviorSubtypeName;
            return botData;
        }

        public virtual void DebugDraw()
        {
            if (AgentEntity == null) return;

            m_navigation.DebugDraw();

            var aiTarget = m_actions.AiTarget as MyAiTargetBase;
            if (aiTarget != null)
            {
                if (aiTarget.HasTarget())
                    VRageRender.MyRenderProxy.DebugDrawPoint(aiTarget.TargetPosition, Color.Aquamarine, false);
            }

            m_botLogic.DebugDraw();
        }

        private void HandleDeadBot()
        {
            const long TIME_TO_RESPAWN = 10; // s
            const long TIME_TO_REMOVE = 30; // s
            if (m_deathTimestamp == 0)
            {
                m_removeAfterDeath = !(MyAIComponent.BotFactory.CanCreateBotOfType(BotDefinition.BehaviorType, false));
                if (m_removeAfterDeath)
                    m_deathTimestamp = (long)(Stopwatch.GetTimestamp() + (TIME_TO_REMOVE * Stopwatch.Frequency));
                else
                    m_deathTimestamp = (long)(Stopwatch.GetTimestamp() + (TIME_TO_RESPAWN * Stopwatch.Frequency));
            }
            else if (Stopwatch.GetTimestamp() > m_deathTimestamp)
            {
                if (m_removeAfterDeath)
                {
                    MyAIComponent.Static.RemoveBot(Player.Id.SerialId);
                }
                else
                {
                    Vector3D spawnPosition = Vector3D.Zero;
                    if (MyAIComponent.BotFactory.GetBotSpawnPosition(BotDefinition.BehaviorType, out spawnPosition))
                    {
                        MyPlayerCollection.RespawnRequest(false, false, 0, null, Player.Id.SerialId, spawnPosition);
                        m_respawnRequestSent = true;
                    }
                    else
                    {
                        MyAIComponent.Static.RemoveBot(Player.Id.SerialId);
                    }
                }
                m_deathTimestamp = 0;
            }
        }
    }
}
