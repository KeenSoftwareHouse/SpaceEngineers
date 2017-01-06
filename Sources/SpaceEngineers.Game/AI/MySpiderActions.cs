using Sandbox.Game.AI;
using Sandbox.Game.AI.Actions;
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
    [MyBehaviorDescriptor("Spider")]
    [BehaviorActionImpl(typeof(MySpiderLogic))]
    public class MySpiderActions : MyAgentActions
    {
        MySpiderTarget SpiderTarget { get { return AiTargetBase as MySpiderTarget; } }

        protected MySpiderLogic SpiderLogic
        {
            get
            {
                return Bot.AgentLogic as MySpiderLogic;
            }
        }

        public MySpiderActions(MyAnimalBot bot)
            : base(bot)
        {
        }

        protected override MyBehaviorTreeState Idle()
        {
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("Burrow", MyBehaviorTreeActionType.INIT)]
        protected void Init_Burrow()
        {
            SpiderLogic.StartBurrowing();
        }

        [MyBehaviorTreeAction("Burrow")]
        protected MyBehaviorTreeState Burrow()
        {
            if (SpiderLogic.IsBurrowing)
            {
                return MyBehaviorTreeState.RUNNING;
            }
            else
            {
                return MyBehaviorTreeState.NOT_TICKED;
            }
        }

        [MyBehaviorTreeAction("Deburrow", MyBehaviorTreeActionType.INIT)]
        protected void Init_Deburrow()
        {
            SpiderLogic.StartDeburrowing();
        }

        [MyBehaviorTreeAction("Deburrow")]
        protected MyBehaviorTreeState Deburrow()
        {
            if (SpiderLogic.IsDeburrowing)
            {
                return MyBehaviorTreeState.RUNNING;
            }
            else
            {
                return MyBehaviorTreeState.NOT_TICKED;
            }
        }

        [MyBehaviorTreeAction("Teleport", ReturnsRunning = false)]
        protected MyBehaviorTreeState Teleport()
        {
            if (Bot.Player.Character.HasAnimation("Deburrow"))
            {
                Bot.Player.Character.PlayCharacterAnimation("Deburrow", MyBlendOption.Immediate, MyFrameOption.JustFirstFrame, 0.0f, 1, sync: true);
                Bot.AgentEntity.DisableAnimationCommands();
            }

            MatrixD teleportPos;
            bool success = MySpaceBotFactory.GetSpiderSpawnPosition(out teleportPos, Bot.Player.GetPosition());
            if (!success) return MyBehaviorTreeState.FAILURE;

            Vector3D pos = teleportPos.Translation;

            //GR: Added simple check so burrowing (and Teleport) functions should happen only when on Voxel physics.
            //Not sure though if this check should be somewhere else (e.g. behaviour Tree)
            //Check position of Teleport if is clear
            var resultOnTeleportPos = Sandbox.Engine.Physics.MyPhysics.CastRay(pos + 3 * Bot.AgentEntity.WorldMatrix.Up, pos - 3 * Bot.AgentEntity.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoVoxelCollisionLayer);
            if (resultOnTeleportPos != null)
            {
                return MyBehaviorTreeState.NOT_TICKED;
            }

            //GR: Also check current position of spider if on CubeGrid do not teleport
            var resultOnSpiderPos = Sandbox.Engine.Physics.MyPhysics.CastRay(Bot.AgentEntity.WorldMatrix.Translation - 3 * Bot.AgentEntity.WorldMatrix.Up, Bot.AgentEntity.WorldMatrix.Translation + 3 * Bot.AgentEntity.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoVoxelCollisionLayer);
            if (resultOnSpiderPos != null && (resultOnSpiderPos as VRage.Game.ModAPI.IHitInfo).HitEntity != Bot.AgentEntity)
            {
                resultOnSpiderPos = Sandbox.Engine.Physics.MyPhysics.CastRay(Bot.AgentEntity.WorldMatrix.Translation - 3 * Bot.AgentEntity.WorldMatrix.Up, Bot.AgentEntity.WorldMatrix.Translation + 3 * Bot.AgentEntity.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.NoVoxelCollisionLayer);
                return MyBehaviorTreeState.NOT_TICKED;
            }

            float radius = (float)Bot.AgentEntity.PositionComp.WorldVolume.Radius;
            var planet = MyGamePruningStructure.GetClosestPlanet(pos);
            if (planet != null)
            {
                planet.CorrectSpawnLocation(ref pos, radius);
                teleportPos.Translation = pos;
            }
            else
            {
                Vector3D? freePlace = MyEntities.FindFreePlace(teleportPos.Translation, radius, stepSize: 0.2f);
                if (freePlace.HasValue)
                {
                    teleportPos.Translation = freePlace.Value;
                }
            }

            Bot.AgentEntity.SetPhysicsEnabled(false);

            Bot.AgentEntity.WorldMatrix = teleportPos;
            Bot.AgentEntity.Physics.CharacterProxy.Up = teleportPos.Up;
            Bot.AgentEntity.Physics.CharacterProxy.Forward = teleportPos.Forward;

            Bot.AgentEntity.SetPhysicsEnabled(true);

            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.INIT)]
        protected void Init_Attack()
        {
            SpiderTarget.AimAtTarget();
            //SpiderTarget.GotoTargetNoPath(0.0f);
            Vector3 dir = (Vector3)(SpiderTarget.TargetPosition - Bot.AgentEntity.PositionComp.GetPosition());
            dir.Normalize();
            //Bot.AgentEntity.Physics.ApplyImpulse(dir, Bot.AgentEntity.PositionComp.GetPosition());
            SpiderTarget.Attack();
        }

        [MyBehaviorTreeAction("Attack")]
        protected MyBehaviorTreeState Attack()
        {
            return SpiderTarget.IsAttacking ? MyBehaviorTreeState.RUNNING : MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("Attack", MyBehaviorTreeActionType.POST)]
        protected void Post_Attack()
        {
            //Bot.Navigation.Stop();
        }

        [MyBehaviorTreeAction("IsAttacking", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsAttacking()
        {
            return SpiderTarget.IsAttacking ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        // Returns a target with a better priority than the given one (better = lower priority value). Priority of value LEQ 0 or null means get any target.
        [MyBehaviorTreeAction("GetTargetWithPriority")]
        protected MyBehaviorTreeState GetTargetWithPriority([BTParam] float radius, [BTInOut] ref MyBBMemoryTarget outTarget, [BTInOut] ref MyBBMemoryInt priority)
        {
            var myPosition = Bot.Navigation.PositionAndOrientation.Translation;
            BoundingSphereD bb = new BoundingSphereD(myPosition, radius);

            if (priority == null)
            {
                priority = new MyBBMemoryInt();
            }
            int bestPriority = priority.IntValue;
            if (bestPriority <= 0)
            {
                bestPriority = int.MaxValue;
            }

            MyBehaviorTreeState retval = IsTargetValid(ref outTarget);
            if (retval == MyBehaviorTreeState.FAILURE)
            {
                bestPriority = 7;
                MyBBMemoryTarget.UnsetTarget(ref outTarget);
            }
            Vector3D? targetPosition = SpiderTarget.GetMemoryTargetPosition(outTarget);
            if (!targetPosition.HasValue || Vector3D.Distance(targetPosition.Value, Bot.AgentEntity.PositionComp.GetPosition()) > 400.0f)
            {
                bestPriority = 7;
                MyBBMemoryTarget.UnsetTarget(ref outTarget);
            }

            var myFaction = MySession.Static.Factions.GetPlayerFaction(Bot.AgentEntity.ControllerInfo.ControllingIdentityId);

            // Priorities are as follows:
            // 1st characters, 3rd turrets, 4th weapons, 5th non-armor blocks, 6th armor blocks
            var entityList = MyEntities.GetTopMostEntitiesInSphere(ref bb);
            entityList.ShuffleList(); // Prevent all spiders going for the same player
            foreach (var entity in entityList)
            {
                if (entity == Bot.AgentEntity) continue;
                if (!SpiderTarget.IsEntityReachable(entity)) continue;

                int entityPriority = 6;
                var character = entity as MyCharacter;
                var grid = entity as MyCubeGrid;

                if (character != null && character.ControllerInfo != null)
                {
                    var faction = MySession.Static.Factions.GetPlayerFaction(character.ControllerInfo.ControllingIdentityId);
                    if (myFaction != null && faction == myFaction) continue;
                    if (character.IsDead) continue;

                    //if character fly up exclude him from targets
                    var result = Sandbox.Engine.Physics.MyPhysics.CastRay(character.WorldMatrix.Translation - 3 * character.WorldMatrix.Up, character.WorldMatrix.Translation + 3 * character.WorldMatrix.Up, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.DefaultCollisionLayer);
                    if (result == null || (result as VRage.Game.ModAPI.IHitInfo).HitEntity == character)
                        continue;

                    entityPriority = 1;

                    if (entityPriority < bestPriority)
                    {
                        retval = MyBehaviorTreeState.SUCCESS;
                        bestPriority = entityPriority;
                        MyBBMemoryTarget.SetTargetEntity(ref outTarget, MyAiTargetEnum.CHARACTER, character.EntityId);
                        continue;
                    }
                }
                else if (grid != null && bestPriority > 3)
                {
                    Vector3D spiderPosInGrid = grid.WorldToGridScaledLocal(myPosition);
                    double closestDist = double.MaxValue;
                    MySlimBlock closestBlock = null;
                    foreach (var block in grid.CubeBlocks)
                    {
                        Vector3D blockLocalPos = new Vector3D(block.Min + block.Max);
                        blockLocalPos = blockLocalPos * 0.5;

                        double dist = Vector3D.RectangularDistance(ref blockLocalPos, ref spiderPosInGrid);
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
                    }
                }
            }
            entityList.Clear();

            /*var players = Sync.Players.GetOnlinePlayers();
            MyCharacter closestCharacter = null;
            double closestDistanceSq = float.MaxValue;
            foreach (var player in players)
            {
                if (player.Id.SerialId != 0)
                {
                    var bot = MyAIComponent.Static.Bots.TryGetBot<MyHumanoidBot>(player.Id.SerialId);
                    if (bot == null || bot.BotDefinition.BehaviorType == "Barbarian")
                        continue;
                }

                if (!(player.Character is MyCharacter) || !AiTargetBase.IsEntityReachable(player.Character))
                {
                    continue;
                }

                if (player.Character.IsDead)
                    continue;

                var distanceSq = Vector3D.DistanceSquared(player.Character.PositionComp.GetPosition(), myPosition);
                if (distanceSq < radius * radius && distanceSq < closestDistanceSq)
                {
                    closestCharacter = player.Character;
                    closestDistanceSq = distanceSq;
                }
            }*/

            //return closestCharacter;

            priority.IntValue = bestPriority;

            // CH: TODO: This is temporary. Remove it!
            //if (outTarget.TargetType == MyAiTargetEnum.CUBE)
            //{
            //    MyEntity outGrid;
            //    MyEntities.TryGetEntityById(outTarget.EntityId.Value, out outGrid);
            //    Debug.Assert(outGrid != null);
            //    var grid = outGrid as MyCubeGrid;
            //    MySlimBlock block = grid.GetCubeBlock(outTarget.BlockPosition);
            //    Debug.Assert(block != null);

            //    //MyTrace.Send(TraceWindow.Ai, "TARGETTING CUBE: " + grid.ToString() + " " + block.ToString());
            //}

            return retval;
        }
    }
}
