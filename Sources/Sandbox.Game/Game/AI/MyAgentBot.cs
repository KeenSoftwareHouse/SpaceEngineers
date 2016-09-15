using Sandbox.Definitions;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.AI.Navigation;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;
using VRageMath;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Game.AI
{
    [MyBotType(typeof(MyObjectBuilder_AgentBot))]
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

        public string BehaviorSubtypeName 
        {
            get { return MyAIComponent.Static.BehaviorTrees.GetBehaviorName(this); }
        }

        protected ActionCollection m_actionCollection;
        public ActionCollection ActionCollection { get { return m_actionCollection; } }

        protected MyBotMemory m_botMemory;
        public MyBotMemory BotMemory { get { return m_botMemory; } }

        private MyBotMemory m_LastBotMemory = null;
        public MyBotMemory LastBotMemory
        { 
            get{ return m_LastBotMemory; } 
            set{ m_LastBotMemory = value; } 
        }
        public void ReturnToLastMemory()
        {
            if ( m_LastBotMemory != null )
                m_botMemory = m_LastBotMemory;
        }

        protected MyAgentActions m_actions;
        public MyAgentActions AgentActions
        {
            get { return m_actions; }
        }
        public MyBotActionsBase BotActions
        {
            get { return m_actions; }
            set
            {
                Debug.Assert(value is MyAgentActions, "Invalid action proxy type");
                m_actions = value as MyAgentActions;
            }
        }

        protected MyAgentDefinition m_botDefinition;
        public MyBotDefinition BotDefinition { get { return m_botDefinition; } }
        public MyAgentDefinition AgentDefinition { get { return m_botDefinition; } }

        protected MyAgentLogic m_botLogic;
        public MyBotLogic BotLogic { get { return m_botLogic; } }
        public MyAgentLogic AgentLogic { get { return m_botLogic; } }
        public bool HasLogic { get { return m_botLogic != null; } }

        private int m_deathCountdownMs;
        private int m_lastCountdownTime;

        private bool m_respawnRequestSent;
        private bool m_removeAfterDeath;
        private bool m_botRemoved;
        private bool m_joinRequestSent;

        public class SLastRunningState
        {
            public string actionName;
            public int counter;
        }

        public class MyLastActions
        {
            private List<SLastRunningState> m_LastActions = new List<SLastRunningState>();
            int MaxActionsCount = 5;
            string GetLastAction() { return m_LastActions.Last().actionName; }
            public void AddLastAction(string lastAction)
            {
                if (m_LastActions.Count != 0)
                {
                    if (lastAction == GetLastAction())
                    {
                        m_LastActions.Last().counter++;
                        return;
                    }
                    if (m_LastActions.Count == MaxActionsCount)
                    {
                        // we should remove first
                        m_LastActions.RemoveAt(0);
                    }
                }
                m_LastActions.Add(new SLastRunningState() { actionName = lastAction, counter = 1 });
            }
            public void Clear() { m_LastActions.Clear(); }
            public string GetLastActionsString()
            {
                StringBuilder res = new StringBuilder();
                for ( int i=0; i<m_LastActions.Count; i++ )
                {
                    res.AppendFormat("{0}-{1}", m_LastActions[i].counter, m_LastActions[i].actionName);
                    if (i != m_LastActions.Count - 1)
                        res.AppendFormat(", ");
                }
                return res.ToString();
            }
        }
        public MyLastActions LastActions = new MyLastActions();

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
                    && AgentEntity != null && !AgentEntity.IsDead;
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

            m_removeAfterDeath = m_botDefinition.RemoveAfterDeath;
            m_respawnRequestSent = false;
            m_botRemoved = false;

            m_player.Controller.ControlledEntityChanged += Controller_ControlledEntityChanged;
            m_navigation.ChangeEntity(m_player.Controller.ControlledEntity);

            Sandbox.Game.Gui.MyCestmirDebugInputComponent.PlacedAction += DebugGoto;
        }

        protected virtual void Controller_ControlledEntityChanged(IMyControllableEntity oldEntity, IMyControllableEntity newEntity)
        {
            if (oldEntity == null && newEntity is MyCharacter) EraseRespawn();

            m_navigation.ChangeEntity(newEntity);
            m_navigation.AimWithMovement();

	        var newCharacter = newEntity as MyCharacter;
            if (newCharacter != null)
            {
                var character = m_player.Controller.ControlledEntity as MyCharacter;
	            var jetpack = newCharacter.JetpackComp;
				if(jetpack != null)
					jetpack.TurnOnJetpack(false);
            }

            if (HasLogic)
                m_botLogic.OnControlledEntityChanged(newEntity);
        }

        public virtual void Init(MyObjectBuilder_Bot botBuilder)
        {
            var ob = botBuilder as MyObjectBuilder_AgentBot;
            if (ob == null)
                return;

            m_deathCountdownMs = ob.RespawnCounter;

            if (AgentDefinition.FactionTag != null)
            {
                var faction = MySession.Static.Factions.TryGetOrCreateFactionByTag(AgentDefinition.FactionTag);
                if (faction != null)
                {
                    MyFactionCollection.SendJoinRequest(faction.FactionId, Player.Identity.IdentityId);
                    m_joinRequestSent = true;
                }
            }

            if (ob.AiTarget != null)
                AgentActions.AiTargetBase.Init(ob.AiTarget);
            if (botBuilder.BotMemory != null)
                m_botMemory.Init(botBuilder.BotMemory);
            MyAIComponent.Static.BehaviorTrees.SetBehaviorName(this, ob.LastBehaviorTree);
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
                    MyPlayerCollection.RespawnRequest(false, false, 0, null, m_player.Id.SerialId, spawnPosition, BotDefinition.Id);
                    ProfilerShort.End();
                }
            }
        }

        public virtual void Cleanup()
        {
            Sandbox.Game.Gui.MyCestmirDebugInputComponent.PlacedAction -= DebugGoto;

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
                if (AgentEntity != null && AgentEntity.IsDead && !m_respawnRequestSent)
                {
                    HandleDeadBot();
                }
                else
                {
                    if (AgentEntity != null && !AgentEntity.IsDead && m_respawnRequestSent) EraseRespawn();
                    UpdateInternal();
                }
            }
            else if (!m_respawnRequestSent)
            {
                HandleDeadBot();
            }
        }

        private void StartRespawn()
        {
            m_lastCountdownTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
            if (m_removeAfterDeath) m_deathCountdownMs = AgentDefinition.RemoveTimeMs;
            else m_deathCountdownMs = AgentDefinition.RemoveTimeMs;
        }

        private void EraseRespawn()
        {
            m_deathCountdownMs = 0;
            m_respawnRequestSent = false;
        }

        protected virtual void UpdateInternal()
        {
            m_navigation.Update(m_botMemory.TickCounter);
            m_botLogic.Update();

            if (m_joinRequestSent == false && m_botDefinition.FactionTag != null && m_botDefinition.FactionTag.Length > 0)
            {
                var upper = m_botDefinition.FactionTag.ToUpperInvariant();
                var desiredFaction = MySession.Static.Factions.TryGetFactionByTag(upper);
                if (desiredFaction == null) return;

                var identityId = AgentEntity.ControllerInfo.ControllingIdentityId;
                var faction = MySession.Static.Factions.TryGetPlayerFaction(identityId);
                if (faction == null && !m_joinRequestSent)
                {
                    MyFactionCollection.SendJoinRequest(desiredFaction.FactionId, identityId);
                    m_joinRequestSent = true;
                }
            }
        }

        public virtual void Reset()
        {
            BotMemory.ResetMemory(true); 
            m_navigation.StopImmediate(true);
            AgentActions.AiTargetBase.UnsetTarget();
        }

        public virtual MyObjectBuilder_Bot GetObjectBuilder()
        {
            MyObjectBuilder_AgentBot botData = MyAIComponent.BotFactory.GetBotObjectBuilder(this) as MyObjectBuilder_AgentBot;
            botData.BotDefId = BotDefinition.Id;
            botData.AiTarget = AgentActions.AiTargetBase.GetObjectBuilder();
            botData.BotMemory = m_botMemory.GetObjectBuilder();
            botData.LastBehaviorTree = BehaviorSubtypeName;
            botData.RemoveAfterDeath = m_removeAfterDeath;
            botData.RespawnCounter = m_deathCountdownMs;
            return botData;
        }

        private void HandleDeadBot()
        {
            if (m_deathCountdownMs <= 0)
            {
                Vector3D spawnPosition = Vector3D.Zero;
                if (!m_removeAfterDeath && MyAIComponent.BotFactory.GetBotSpawnPosition(BotDefinition.BehaviorType, out spawnPosition))
                {
                    MyPlayerCollection.RespawnRequest(false, false, 0, null, Player.Id.SerialId, spawnPosition, BotDefinition.Id);
                    m_respawnRequestSent = true;
                }
                else if (!m_botRemoved)
                {
                    m_botRemoved = true;
                    MyAIComponent.Static.RemoveBot(Player.Id.SerialId);
                }
            }
            else
            {
                var currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_deathCountdownMs -= currentTime - m_lastCountdownTime;
                m_lastCountdownTime = currentTime;

            }
        }

        public virtual void DebugDraw()
        {
            if (AgentEntity == null) return;

            m_navigation.DebugDraw();

            var aiTarget = m_actions.AiTargetBase as MyAiTargetBase;
            if (aiTarget != null)
            {
                if (aiTarget.HasTarget())
                {
                    VRageRender.MyRenderProxy.DebugDrawPoint(aiTarget.TargetPosition, Color.Aquamarine, false);
                    if (BotEntity != null && aiTarget.TargetEntity != null)
                    {
                        string text = null;
                        if (aiTarget.TargetType == MyAiTargetEnum.CUBE)
                        {
                            text = string.Format("Target:{0}", aiTarget.GetTargetBlock());
                        }
                        else
                        {
                            text = string.Format("Target:{0}", aiTarget.TargetEntity.ToString());
                        }

                        var markerPos = BotEntity.PositionComp.WorldAABB.Center;
                        markerPos.Y += BotEntity.PositionComp.WorldAABB.HalfExtents.Y + 0.2f;
                        VRageRender.MyRenderProxy.DebugDrawText3D(markerPos, text, Color.Red, 1f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                    }

                }
            }

            m_botLogic.DebugDraw();
        }

        public virtual void DebugGoto(Vector3D point, MyEntity entity = null)
        {
            if (m_player.Id.SerialId == 0) return;

            /*{
                var path = MyAIComponent.Static.Pathfinding.FindPathGlobal(m_navigation.PositionAndOrientation.Translation, point, entity);
                Navigation.FollowPath(path);

                var statues = MyBarbarianComponent.Static.GetAllStatues();
                double closestSq = double.MaxValue;
                MyEntity closestStatue = null;
                Vector3D currentPos = Navigation.PositionAndOrientation.Translation;
                foreach (var statue in statues)
                {
                    double dsq = Vector3D.DistanceSquared(currentPos, statue.WorldMatrix.Translation);
                    if (dsq < closestSq)
                    {
                        closestSq = dsq;
                        closestStatue = statue;
                    }
                    if (

                    if (statue.CubeGrid == targetGrid)
                    {
                        inoutTarget.SetTargetCube(statue.SlimBlock.Position, statue.CubeGrid.EntityId);
                        return MyBehaviorTreeState.SUCCESS;
                    }
                }

                if (closestStatue == null) return;

                //MyBBMemoryTarget target = new MyBBMemoryTarget();
                var target = HumanoidActions.AiTarget as MyAiTarget;
                target.SetTargetEntity(closestStatue);
                target.GotoTarget(m_navigation);
            }*/
            m_navigation.AimWithMovement();
            m_navigation.GotoNoPath(point, 0.0f, entity);
            //m_navigation.Goto(point, 0.0f, entity);
        }
    }
}
