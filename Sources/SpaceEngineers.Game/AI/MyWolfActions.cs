using Sandbox.Game.AI;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.AI;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    [MyBehaviorDescriptor("Wolf")]
    [BehaviorActionImpl(typeof(MyWolfLogic))]
    public class MyWolfActions : MyAgentActions
    {
        MyWolfTarget WolfTarget { get { return AiTargetBase as MyWolfTarget; } }
        private Vector3D? m_runAwayPos = null;
        private Vector3D? m_lastTargetedEntityPosition = null;
		private Vector3D? m_debugTarget;

        protected MyWolfLogic WolfLogic
        {
            get
            {
                return Bot.AgentLogic as MyWolfLogic;
            }
        }

        public MyWolfActions(MyAnimalBot bot)
            : base(bot)
        {
        }

        protected override MyBehaviorTreeState Idle()
        {
            return MyBehaviorTreeState.RUNNING;
        }

		[MyBehaviorTreeAction("GoToPlayerDefinedTarget", ReturnsRunning = true)]
        protected MyBehaviorTreeState GoToPlayerDefinedTarget()
        {
            if (m_debugTarget != MyAIComponent.Static.DebugTarget)
            {
                m_debugTarget = MyAIComponent.Static.DebugTarget;

                if (MyAIComponent.Static.DebugTarget.HasValue)
                    ;//CyberhoundTarget.SetTargetPosition(MyAIComponent.Static.DebugTarget.Value);
                else
                    return MyBehaviorTreeState.FAILURE;
            }

            var botPosition = Bot.Player.Character.PositionComp.GetPosition();

            // Distance to target
            if (m_debugTarget != null)
            {
                if (Vector3D.Distance(botPosition, m_debugTarget.Value) <= 1f)
                    return MyBehaviorTreeState.SUCCESS;

                //var nextPoint = MyAIComponent.Static.PathEngineGetNextPathPoint(botPosition, MyAIComponent.Static.DebugTarget.Value);
                //var nextPoint = MyAIComponent.Static.PathfindingGetNextPathPoint(botPosition, m_debugTarget.Value);
                Vector3D point = m_debugTarget.Value;
                MyDestinationSphere destSphere = new MyDestinationSphere(ref point, 1);
                var path = MyAIComponent.Static.Pathfinding.FindPathGlobal(botPosition, destSphere, null);
                
                Vector3D nextPoint;
                float targetRadius;
                VRage.ModAPI.IMyEntity entity;
                if (path.GetNextTarget(botPosition, out nextPoint, out targetRadius, out entity))
                {
                    if (WolfTarget.TargetPosition != nextPoint)
                        //WolfTarget.SetTargetPosition(m_debugTarget.Value/*nextPoint*/);
                        WolfTarget.SetTargetPosition(nextPoint);
                    WolfTarget.AimAtTarget();
                    WolfTarget.GotoTargetNoPath(0.0f, false);
                }
                else
                    return MyBehaviorTreeState.FAILURE;
            }
            return MyBehaviorTreeState.RUNNING;
        }
		
        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.INIT)]
        protected void Init_Attack()
        {
            WolfTarget.AimAtTarget();
            //WolfTarget.GotoTargetNoPath(0.0f);
            Vector3 dir = WolfTarget.TargetPosition - Bot.AgentEntity.PositionComp.GetPosition();
            dir.Normalize();
            //Bot.AgentEntity.Physics.ApplyImpulse(dir, Bot.AgentEntity.PositionComp.GetPosition());
            WolfTarget.Attack(!WolfLogic.SelfDestructionActivated);
        }

        [MyBehaviorTreeAction("Attack")]
        protected MyBehaviorTreeState Attack()
        {
            return WolfTarget.IsAttacking ? MyBehaviorTreeState.RUNNING : MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.POST)]
        protected void Post_Attack()
        {
            //Bot.Navigation.Stop();
        }

        [MyBehaviorTreeAction("IsAttacking", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsAttacking()
        {
            return WolfTarget.IsAttacking ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("Explode")]
        protected MyBehaviorTreeState Explode()
        {
            WolfLogic.ActivateSelfDestruct();
            return MyBehaviorTreeState.SUCCESS;
        }

        // Returns a target with a better priority than the given one (better = lower priority value). Priority of value LEQ 0 or null means get any target.
        [MyBehaviorTreeAction("GetTargetWithPriority")]
        protected MyBehaviorTreeState GetTargetWithPriority([BTParam] float radius, [BTInOut] ref MyBBMemoryTarget outTarget, [BTInOut] ref MyBBMemoryInt priority)
        {
            if (WolfLogic.SelfDestructionActivated) // self destruction activated, do not change target.
                return MyBehaviorTreeState.SUCCESS;

            var myPosition = Bot.Navigation.PositionAndOrientation.Translation;
            BoundingSphereD bb = new BoundingSphereD(myPosition, radius);

            if (priority == null)
            {
                priority = new MyBBMemoryInt();
            }
            int bestPriority = priority.IntValue;
            if (bestPriority <= 0 || Bot.Navigation.Stuck)
            {
                bestPriority = int.MaxValue;
            }

            MyBehaviorTreeState retval = IsTargetValid(ref outTarget);
            if (retval == MyBehaviorTreeState.FAILURE)
            {
                bestPriority = 7;
                MyBBMemoryTarget.UnsetTarget(ref outTarget);
            }
            Vector3D? targetPosition = WolfTarget.GetMemoryTargetPosition(outTarget);
            if (!targetPosition.HasValue 
                || Vector3D.DistanceSquared(targetPosition.Value, Bot.AgentEntity.PositionComp.GetPosition()) > 400.0f * 400.0f)
            {
                bestPriority = 7;
                MyBBMemoryTarget.UnsetTarget(ref outTarget);
            }
            if (targetPosition.HasValue)
            {
                Vector3D targetPositionValue = targetPosition.Value;
                var planet = MyGamePruningStructure.GetClosestPlanet(targetPositionValue);
                if (planet != null)
                {
                    Vector3D targetPositionProjected = planet.GetClosestSurfacePointGlobal(ref targetPositionValue);
                    if (Vector3D.DistanceSquared(targetPositionProjected, targetPositionValue) > 1.5f * 1.5f &&
                        Vector3D.DistanceSquared(targetPositionProjected, Bot.AgentEntity.PositionComp.GetPosition()) < 5.0f * 5.0f)
                    {
                        bestPriority = 7;
                        MyBBMemoryTarget.UnsetTarget(ref outTarget);
                    }
                }
            }

            var myFaction = MySession.Static.Factions.GetPlayerFaction(Bot.AgentEntity.ControllerInfo.ControllingIdentityId);


            // Priorities are as follows:
            // 1st characters, 3rd turrets, 4th weapons, 5th non-armor blocks, 6th armor blocks
            var entityList = MyEntities.GetTopMostEntitiesInSphere(ref bb);
            entityList.ShuffleList(); // Prevent all Wolfs going for the same player
            foreach (var entity in entityList)
            {
                if (entity == Bot.AgentEntity 
                    || entity is MyVoxelBase 
                    || !WolfTarget.IsEntityReachable(entity)) 
                    continue;
                // exclude entities above ground
                Vector3D entityPos = entity.PositionComp.GetPosition();
                var planet = MyGamePruningStructure.GetClosestPlanet(entityPos);
                if (planet != null)
                {
                    Vector3D entityPosProjected = planet.GetClosestSurfacePointGlobal(ref entityPos);
                    if (Vector3D.DistanceSquared(entityPosProjected, entityPos) > 1.0f)
                        continue;
                }

                int entityPriority = 6;
                var character = entity as MyCharacter;
                var grid = entity as MyCubeGrid;

                if (character != null)
                {
                    var faction = MySession.Static.Factions.GetPlayerFaction(character.ControllerInfo.ControllingIdentityId);
                    if (myFaction != null && faction == myFaction) continue;
                    if (character.IsDead) continue;

                    entityPriority = 1;

                    if (entityPriority < bestPriority)
                    {
                        retval = MyBehaviorTreeState.SUCCESS;
                        bestPriority = entityPriority;
                        MyBBMemoryTarget.SetTargetEntity(ref outTarget, MyAiTargetEnum.CHARACTER, character.EntityId);
                        m_lastTargetedEntityPosition = character.PositionComp.GetPosition();
                        continue;
                    }
                }
                else if (grid != null && bestPriority > 3)
                {
                    Vector3D WolfPosInGrid = grid.WorldToGridScaledLocal(myPosition);
                    double closestDist = double.MaxValue;
                    MySlimBlock closestBlock = null;
                    foreach (var block in grid.CubeBlocks)
                    {
                        Vector3D blockLocalPos = new Vector3D(block.Min + block.Max);
                        blockLocalPos = blockLocalPos * 0.5;

                        double dist = Vector3D.RectangularDistance(ref blockLocalPos, ref WolfPosInGrid);
                        if (dist < closestDist)
                        {
                            closestBlock = block;
                            closestDist = dist;
                        }
                    }

                    if (closestBlock != null)
                    {
                        retval = MyBehaviorTreeState.SUCCESS;
                        bestPriority = 3;
                        MyBBMemoryTarget.SetTargetCube(ref outTarget, (closestBlock.Min + closestBlock.Max) / 2, grid.EntityId);
                        BoundingBoxD bbBlock;
                        closestBlock.GetWorldBoundingBox(out bbBlock);
                        m_lastTargetedEntityPosition = bbBlock.Center;
                    }
                }
            }
            entityList.Clear();
            priority.IntValue = bestPriority;

            // CH: TODO: This is temporary. Remove it!
            if (outTarget.TargetType == MyAiTargetEnum.CUBE)
            {
                MyEntity outGrid;
                MyEntities.TryGetEntityById(outTarget.EntityId.Value, out outGrid);
                Debug.Assert(outGrid != null);
                var grid = outGrid as MyCubeGrid;
                MySlimBlock block = grid.GetCubeBlock(outTarget.BlockPosition);
                Debug.Assert(block != null);

                //MyTrace.Send(TraceWindow.Ai, "TARGETTING CUBE: " + grid.ToString() + " " + block.ToString());
            }

            if (outTarget.TargetType == MyAiTargetEnum.NO_TARGET)
            {
                retval = MyBehaviorTreeState.FAILURE;
            }
            return retval;
        }

        [MyBehaviorTreeAction("IsRunningAway", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsRunningAway()
        {
            return m_runAwayPos.HasValue ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("RunAway", MyBehaviorTreeActionType.INIT)]
        protected MyBehaviorTreeState RunAway_Init()
        {
            //runAwayPos = null;
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("RunAway")]
        protected MyBehaviorTreeState RunAway([BTParam] float distance)
        {
            if (!m_runAwayPos.HasValue)
            {
                Vector3D currentPosition = Bot.Player.Character.PositionComp.GetPosition();
                Vector3D planetGravityVec = MyGravityProviderSystem.CalculateNaturalGravityInPoint(currentPosition);

                var planet = MyGamePruningStructure.GetClosestPlanet(currentPosition);
                if (planet == null) return MyBehaviorTreeState.FAILURE;

                if (m_lastTargetedEntityPosition.HasValue)
                {
                    Vector3D lastTargetedEntityPositionProjected = m_lastTargetedEntityPosition.Value;
                    lastTargetedEntityPositionProjected = planet.GetClosestSurfacePointGlobal(ref lastTargetedEntityPositionProjected);

                    Vector3D direction = currentPosition - lastTargetedEntityPositionProjected;
                    Vector3D runAwayPosCandidate = currentPosition + Vector3D.Normalize(direction) * distance;
                    m_runAwayPos = planet.GetClosestSurfacePointGlobal(ref runAwayPosCandidate);
                }
                else
                {
                    planetGravityVec.Normalize();
                    Vector3D planetTangent = Vector3D.CalculatePerpendicularVector(planetGravityVec);
                    Vector3D planetBitangent = Vector3D.Cross(planetGravityVec, planetTangent);
                    planetTangent.Normalize();
                    planetBitangent.Normalize();
                    Vector3D runAwayPosCandidate = MyUtils.GetRandomDiscPosition(ref currentPosition, distance, distance, ref planetTangent, ref planetBitangent);
                    if (planet != null)
                        m_runAwayPos = planet.GetClosestSurfacePointGlobal(ref runAwayPosCandidate);
                    else
                        m_runAwayPos = runAwayPosCandidate;
                }
                AiTargetBase.SetTargetPosition(m_runAwayPos.Value);
                AimWithMovement();
            }
            else
            {
                if (Bot.Navigation.Stuck)
                    return MyBehaviorTreeState.FAILURE;
            }

            AiTargetBase.GotoTargetNoPath(1.0f, false);

            if (Vector3D.DistanceSquared(m_runAwayPos.Value, Bot.Player.Character.PositionComp.GetPosition()) < 10.0f * 10.0f)
            {
                WolfLogic.Remove();
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.RUNNING;
            }
        }
    }
}
