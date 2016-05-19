using Sandbox.Game.AI;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using System;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.AI;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Medieval.AI
{
    [MyBehaviorDescriptor("Animal")]
    [BehaviorActionImpl(typeof(MyAnimalBotLogic))]
    public class MyAnimalBotActions : MyAgentActions
    {
        private MyAnimalBot m_bot;

        private long m_eatTimeInS = 10;
        private long m_eatCounter;
        private long m_soundCounter;
        private bool m_usingPathfinding = false;

        private static readonly double COS15 = Math.Cos(MathHelper.ToRadians(15));

        private MyAnimalBotLogic AnimalLogic { get { return m_bot.BotLogic as MyAnimalBotLogic; } }

        public MyAnimalBotActions(MyAnimalBot bot)
            :
            base(bot)
        { 
            m_bot = bot;
        }

        /// <summary>
        /// Changes the state to an idle state sensing danger
        /// </summary>
        [MyBehaviorTreeAction("IdleDanger", ReturnsRunning = false)]
        protected MyBehaviorTreeState IdleDanger()
        {
            // TODO: animate deer to idle danger
            m_bot.AgentEntity.SoundComp.StartSecondarySound("BotDeerBark", sync: true);
            return MyBehaviorTreeState.SUCCESS;
        }

        protected override void Init_Idle()
        {
            m_bot.Navigation.StopImmediate(true);
            m_eatCounter = (long)(Stopwatch.GetTimestamp() + (m_eatTimeInS * Stopwatch.Frequency));
            if (MyUtils.GetRandomInt(2) == 0)
            {
                var randomSoundValue = ((MyUtils.GetRandomLong() % (m_eatTimeInS - 2)) + 1);
                m_soundCounter = (long)(Stopwatch.GetTimestamp() + (randomSoundValue * Stopwatch.Frequency));
            }
            else
            {
                m_soundCounter = 0;
            }

            m_bot.AgentEntity.PlayCharacterAnimation("Idle", MyBlendOption.Immediate, MyFrameOption.Loop, 0.5f);
        }

        protected override MyBehaviorTreeState Idle()
        {
            var timestamp = Stopwatch.GetTimestamp();
            if (m_soundCounter != 0 && m_soundCounter < timestamp)
            {
                if (MyRandom.Instance.NextFloat() > 0.7f)
                    m_bot.AgentEntity.SoundComp.StartSecondarySound("BotDeerRoar", sync: true);
                else
                    m_bot.AgentEntity.SoundComp.StartSecondarySound("BotDeerBark", sync: true);
                m_soundCounter = 0;
            }
            if (m_eatCounter > timestamp)
                return MyBehaviorTreeState.RUNNING;
            else
                return MyBehaviorTreeState.SUCCESS;
        }

        protected override void Init_GotoTarget()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTargetNoPath(0.0f);
                m_bot.Navigation.AimWithMovement();
            }
        }

        [MyBehaviorTreeAction("FindWanderLocation", ReturnsRunning = false)]
        protected MyBehaviorTreeState FindWanderLocation([BTOut] ref MyBBMemoryTarget outTarget)
        {
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsHumanInArea", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsHumanInArea([BTParam] int standingRadius, [BTParam] int crouchingRadius, [BTOut] ref MyBBMemoryTarget outTarget)
        {
            MyCharacter foundCharacter = null;
            if (TryFindValidHumanInArea(standingRadius, crouchingRadius, out foundCharacter))
            {
                MyBBMemoryTarget.SetTargetEntity(ref outTarget, MyAiTargetEnum.CHARACTER, foundCharacter.EntityId);
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

        [MyBehaviorTreeAction("AmIBeingFollowed", ReturnsRunning = false)]
        protected MyBehaviorTreeState AmIBeingFollowed([BTIn] ref MyBBMemoryTarget inTarget)
        {
            if(inTarget != null)
                return MyBehaviorTreeState.SUCCESS;
            else
                return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsHumanNotInArea", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsHumanNotInArea([BTParam] int standingRadius, [BTParam] int crouchingRadius, [BTOut] ref MyBBMemoryTarget outTarget)
        {
            return InvertState(IsHumanInArea(standingRadius, crouchingRadius, ref outTarget));
        }

        private MyBehaviorTreeState InvertState(MyBehaviorTreeState state)
        {
            if (state == MyBehaviorTreeState.SUCCESS)
                return MyBehaviorTreeState.FAILURE;
            else if (state == MyBehaviorTreeState.FAILURE)
                return MyBehaviorTreeState.SUCCESS;

            return state;
        }

        private bool TryFindValidHumanInArea(int standingRadius, int crouchingRadius, out MyCharacter foundCharacter)
        {
            var position = m_bot.AgentEntity.PositionComp.GetPosition();
            var forward = m_bot.AgentEntity.PositionComp.WorldMatrix.Forward;
            var players = Sync.Players.GetOnlinePlayers();      
            foreach (var player in players)
            {
                if (player.Id.SerialId != 0 && MyAIComponent.Static.Bots.GetBotType(player.Id.SerialId) != BotType.HUMANOID)
                    continue;

                if (player.Character != null && !player.Character.MarkedForClose && !player.Character.IsDead)
                {
                    var characterPosition = player.Character.PositionComp.GetPosition();
                    var vectorToTarget = characterPosition - position;
                    vectorToTarget.Y = 0;
                    var distance = vectorToTarget.Normalize();
                    bool matchCriterion = false;

                    if (distance < standingRadius)
                    {
                        if (Vector3D.Dot(vectorToTarget, forward) > COS15)
                        {
                            matchCriterion = true;
                        }
                        else if (player.Character.IsCrouching && !player.Character.IsSprinting)
                        {
                            matchCriterion = distance < crouchingRadius;
                        }
                        else
                        {
                            matchCriterion = true;
                        }
                    }
                        
                    if (matchCriterion)
                    {
                        foundCharacter = player.Character;
                        return true;
                    }
                }
            }

            foundCharacter = null;
            return false;
        }

        [MyBehaviorTreeAction("FindRandomSafeLocation", ReturnsRunning = false)]
        protected MyBehaviorTreeState FindRandomSafeLocation([BTIn] ref MyBBMemoryTarget inTargetEnemy, [BTOut] ref MyBBMemoryTarget outTargetLocation)
        {
            if (inTargetEnemy == null || !inTargetEnemy.EntityId.HasValue)
                return MyBehaviorTreeState.FAILURE;
            MyEntity targetEntity = null;
            if (MyEntities.TryGetEntityById(inTargetEnemy.EntityId.Value, out targetEntity))
            {
                Vector3D botPosition = m_bot.AgentEntity.PositionComp.GetPosition();
                Vector3D directionToBot = botPosition - targetEntity.PositionComp.GetPosition();
                directionToBot.Normalize();

                Vector3D safeLocation = default(Vector3D);
                if (!AiTargetBase.GetRandomDirectedPosition(botPosition, directionToBot, out safeLocation))
                {
                    safeLocation = botPosition + directionToBot * 30;
                }

                MyBBMemoryTarget.SetTargetPosition(ref outTargetLocation, safeLocation);
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

        [MyBehaviorTreeAction("RunAway", MyBehaviorTreeActionType.INIT)]
        protected void Init_RunAway()
        {
            AnimalLogic.EnableCharacterAvoidance(true);
            m_bot.Navigation.AimWithMovement();
            AiTargetBase.GotoTargetNoPath(0.0f);
        }

        [MyBehaviorTreeAction("RunAway")]
        protected MyBehaviorTreeState RunAway()
        {
            if (m_bot.Navigation.Navigating)
            {
                if (m_bot.Navigation.Stuck)
                {
                    if (m_usingPathfinding)
                    {
                        return MyBehaviorTreeState.FAILURE;
                    }
                    else
                    {
                        m_usingPathfinding = true;
                        AnimalLogic.EnableCharacterAvoidance(false);
                        AiTargetBase.GotoTarget();
                        return MyBehaviorTreeState.RUNNING;
                    }
                }
                else
                {
                    return MyBehaviorTreeState.RUNNING;
                }
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("RunAway", MyBehaviorTreeActionType.POST)]
        protected void Post_RunAway()
        {
            m_usingPathfinding = false;
            m_bot.Navigation.StopImmediate(true);
        }


        [MyBehaviorTreeAction("PlaySound", ReturnsRunning = false)]
        protected MyBehaviorTreeState PlaySound([BTParam] string soundtrack)
        {
            m_bot.AgentEntity.SoundComp.StartSecondarySound(soundtrack, sync: true);
            return MyBehaviorTreeState.SUCCESS;
        }
    }
}
