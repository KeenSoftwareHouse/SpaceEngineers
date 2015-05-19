using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using VRage;
using VRageMath;
using Sandbox.Engine.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.AI.BehaviorTree;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Common.AI;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.AI.Pathfinding;

namespace Sandbox.Game.AI
{
    public class MySandboxBot : IMyEntityBot
    {
        internal MyPlayer m_player;

        MyBotNavigation m_navigation;
        public MyBotNavigation Navigation { get { return m_navigation; } }

        private Vector3? m_positionToGo = null;

        bool m_respawnRequestSent;

        public string BehaviorSubtypeName { get { return BotDefinition.BotBehaviorTree.SubtypeName; } }
        public MyBehaviorTree BehaviorTree { get; set; }

        private ActionCollection m_actionCollection;
        public ActionCollection ActionCollection { get { return m_actionCollection; } }

        private MyBotMemory m_botMemory;
        public MyBotMemory BotMemory { get { return m_botMemory; } }

        private MyAgentDefinition m_definition;
        public MyBotDefinition BotDefinition { get { return m_definition as MyBotDefinition; } }
        public MyAgentDefinition AgentDefinition { get { return m_definition; } }

        private MyAbstractBotActionProxy m_actions;

        public bool IsValidForUpdate
        {
            get { return false; }
        }

        public bool CreatedByPlayer
        {
            get;
            set;
        }

        // TODO: Remove this terrible hack :-)
        public bool ShouldFollowPlayer { get { return false; } set { return; } }

        public MyEntity BotEntity
        {
            get { return m_player.Character; }
        }

        public MySandboxBot(MyPlayer botPlayer, MyBotDefinition botDefinition)
        {
            m_definition = botDefinition as MyAgentDefinition;

            m_player = botPlayer;
            m_navigation = new MyBotNavigation();
            m_respawnRequestSent = false;
            m_actionCollection = null;
            m_botMemory = new MyBotMemory(this);

            m_player.Controller.ControlledEntityChanged += Controller_ControlledEntityChanged;

            m_navigation.ChangeEntity(m_player.Controller.ControlledEntity);
        }

        void Controller_ControlledEntityChanged(Entities.IMyControllableEntity oldEntity, Entities.IMyControllableEntity newEntity)
        {
            m_navigation.ChangeEntity(newEntity);
        }

        public void Init(MyObjectBuilder_Bot botBuilder)
        {
            var ob = (MyObjectBuilder_BarbarianBot)botBuilder; // MW:TODO replace with sandbox builder
        }

        public void InitActions(ActionCollection actionCollection)
        {
            m_actionCollection = actionCollection;
        }

        public void Spawn(Vector3D? spawnPosition, bool spawnedByPlayer)
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

        public MyObjectBuilder_Bot GetBotData()
        {
            MyObjectBuilder_BarbarianBot botData = new MyObjectBuilder_BarbarianBot();
            botData.BotDefId = m_definition.Id;
            return botData;
        }

        public void Cleanup()
        {
            m_player.Controller.ControlledEntityChanged -= Controller_ControlledEntityChanged;
            m_player = null;
        }

        public void Update()
        {
            if (m_player.Controller.ControlledEntity != null)
            {
                var controlledEntity = m_player.Controller.ControlledEntity.Entity;
                if (controlledEntity != null)
                {
                    m_navigation.Update();
                    m_navigation.DebugDraw();
                    if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                        VRageRender.MyRenderProxy.DebugDrawSphere(m_navigation.PositionAndOrientation.Translation, 0.15f, Color.White.ToVector3(), 1.0f, false);
                    if (BehaviorTree != null)
                        BehaviorTree.Tick(this);
                }
            }
        }

        public void DebugDraw() { }

        [MyBehaviorTreeActionAttribute("Test")]
        internal MyBehaviorTreeState Action_Test()
        {
            if (MySession.ControlledEntity != null)
            {
                var character = m_player.Controller.ControlledEntity as MyCharacter;
                if (character != null)
                {
                    m_navigation.Goto(new Vector3D(100, 100, 100));
                }
            }
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeActionAttribute("AnyPlayerInArea40")]
        internal MyBehaviorTreeState Condition_AnyPlayerInArea40()
        {
            if (MySession.LocalCharacter != null)
            {
                var position = MySession.LocalCharacter.PositionComp.GetPosition();
                if ((position - m_player.Character.PositionComp.GetPosition()).LengthSquared() <= 40 * 40)
                {
                    m_positionToGo = position;
                    return MyBehaviorTreeState.SUCCESS;
                }
            }

            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("GoToPosition")]
        internal MyBehaviorTreeState Action_GoToPosition()
        {
            if ((m_player.Character.PositionComp.GetPosition() - m_positionToGo.Value).LengthSquared() >= 10 * 10)
            {
                m_navigation.Goto(m_positionToGo.Value);
                return MyBehaviorTreeState.RUNNING;
            }
            else
            {
                m_navigation.StopImmediate();
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("Idle")]
        internal MyBehaviorTreeState Action_Idle()
        {
            m_navigation.StopImmediate();
            m_player.Character.PlayCharacterAnimation("Idle", true, MyPlayAnimationMode.WaitForPreviousEnd, 0.5f);
            return MyBehaviorTreeState.RUNNING;
        }

        public void Reset()
        {
            BotMemory.ResetMemory(BehaviorTree, true);
            //m_target.UnsetTarget();
            m_navigation.StopImmediate(true);
        }

        MyAbstractBotActionProxy IMyBot.BotActions
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }


        public void InitLogic(MyBotLogic logic)
        {
            throw new NotImplementedException();
        }


        public MyBotLogic BotLogic
        {
            get { throw new NotImplementedException(); }
        }
    }
}
