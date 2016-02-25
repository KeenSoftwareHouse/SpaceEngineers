using Sandbox.Common.AI;
using Sandbox.Engine.Physics;
using Sandbox.Game.AI;
using Sandbox.Game.AI.Actions;
using Sandbox.Game.AI.Logic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Trace;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.AI
{
    [MyBehaviorDescriptor("Cyberhound")]
    [BehaviorActionImpl(typeof(MyCyberhoundLogic))]
    public class MyCyberhoundActions : MyAgentActions
    {
        MyCyberhoundTarget CyberhoundTarget { get { return AiTargetBase as MyCyberhoundTarget; } }
        private Vector3D? runAwayPos = null;
        private Vector3D? lastTargetedEntityPosition = null;

        protected MyCyberhoundLogic CyberhoundLogic
        {
            get
            {
                return Bot.AgentLogic as MyCyberhoundLogic;
            }
        }

        public MyCyberhoundActions(MyAnimalBot bot)
            : base(bot)
        {
        }

        protected override MyBehaviorTreeState Idle()
        {
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.INIT)]
        protected void Init_Attack()
        {
            CyberhoundTarget.AimAtTarget();
            //CyberhoundTarget.GotoTargetNoPath(0.0f);
            Vector3 dir = (Vector3)(CyberhoundTarget.TargetPosition - Bot.AgentEntity.PositionComp.GetPosition());
            dir.Normalize();
            //Bot.AgentEntity.Physics.ApplyImpulse(dir, Bot.AgentEntity.PositionComp.GetPosition());
            CyberhoundTarget.Attack(!CyberhoundLogic.SelfDestructionActivated);
        }

        [MyBehaviorTreeAction("Attack")]
        protected MyBehaviorTreeState Attack()
        {
            return CyberhoundTarget.IsAttacking ? MyBehaviorTreeState.RUNNING : MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.POST)]
        protected void Post_Attack()
        {
            //Bot.Navigation.Stop();
        }

        [MyBehaviorTreeAction("IsAttacking", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsAttacking()
        {
            return CyberhoundTarget.IsAttacking ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("Explode")]
        protected MyBehaviorTreeState Explode()
        {
            CyberhoundLogic.ActivateSelfDestruct();
            return MyBehaviorTreeState.SUCCESS;
        }

        // Returns a target with a better priority than the given one (better = lower priority value). Priority of value LEQ 0 or null means get any target.
        [MyBehaviorTreeAction("GetTargetWithPriority")]
        protected MyBehaviorTreeState GetTargetWithPriority([BTParam] float radius, [BTInOut] ref MyBBMemoryTarget outTarget, [BTInOut] ref MyBBMemoryInt priority)
        {
            if (CyberhoundLogic.SelfDestructionActivated) // self destruction activated, do not change target.
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
            Vector3D? targetPosition = CyberhoundTarget.GetMemoryTargetPosition(outTarget);
            if (!targetPosition.HasValue 
                || Vector3D.DistanceSquared(targetPosition.Value, Bot.AgentEntity.PositionComp.GetPosition()) > 400.0f * 400.0f)
            {
                bestPriority = 7;
                MyBBMemoryTarget.UnsetTarget(ref outTarget);
            }
            if (targetPosition.HasValue)
            {
                Vector3D targetPositionValue = targetPosition.Value;
                var planet = MyGravityProviderSystem.GetNearestPlanet(targetPositionValue);
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
            entityList.ShuffleList(); // Prevent all Cyberhounds going for the same player
            foreach (var entity in entityList)
            {
                if (entity == Bot.AgentEntity 
                    || entity is MyVoxelBase 
                    || !CyberhoundTarget.IsEntityReachable(entity)) 
                    continue;
                // exclude entities above ground
                Vector3D entityPos = entity.PositionComp.GetPosition();
                var planet = MyGravityProviderSystem.GetNearestPlanet(entityPos);
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
                        lastTargetedEntityPosition = character.PositionComp.GetPosition();
                        continue;
                    }
                }
                else if (grid != null && bestPriority > 3)
                {
                    Vector3D CyberhoundPosInGrid = grid.WorldToGridScaledLocal(myPosition);
                    double closestDist = double.MaxValue;
                    MySlimBlock closestBlock = null;
                    foreach (var block in grid.CubeBlocks)
                    {
                        Vector3D blockLocalPos = new Vector3D(block.Min + block.Max);
                        blockLocalPos = blockLocalPos * 0.5;

                        double dist = Vector3D.RectangularDistance(ref blockLocalPos, ref CyberhoundPosInGrid);
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
                        lastTargetedEntityPosition = bbBlock.Center;
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
            return runAwayPos.HasValue ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
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
            if (!runAwayPos.HasValue)
            {
                Vector3D currentPosition = Bot.Player.Character.PositionComp.GetPosition();
                Vector3D planetGravityVec = MyGravityProviderSystem.CalculateNaturalGravityInPoint(currentPosition);

                var planet = MyGravityProviderSystem.GetNearestPlanet(currentPosition);
                if (planet == null) return MyBehaviorTreeState.FAILURE;

                if (lastTargetedEntityPosition.HasValue)
                {
                    Vector3D lastTargetedEntityPositionProjected = lastTargetedEntityPosition.Value;
                    lastTargetedEntityPositionProjected = planet.GetClosestSurfacePointGlobal(ref lastTargetedEntityPositionProjected);

                    Vector3D direction = currentPosition - lastTargetedEntityPositionProjected;
                    Vector3D runAwayPosCandidate = currentPosition + Vector3D.Normalize(direction) * distance;
                    runAwayPos = planet.GetClosestSurfacePointGlobal(ref runAwayPosCandidate);
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
                        runAwayPos = planet.GetClosestSurfacePointGlobal(ref runAwayPosCandidate);
                    else
                        runAwayPos = runAwayPosCandidate;
                }
                AiTargetBase.SetTargetPosition(runAwayPos.Value);
                AimWithMovement();
            }
            else
            {
                if (Bot.Navigation.Stuck)
                    return MyBehaviorTreeState.FAILURE;
            }

            AiTargetBase.GotoTargetNoPath(1.0f, false);

            if (Vector3D.DistanceSquared(runAwayPos.Value, Bot.Player.Character.PositionComp.GetPosition()) < 10.0f * 10.0f)
            {
                CyberhoundLogic.Remove();
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.RUNNING;
            }
        }
    }
}
