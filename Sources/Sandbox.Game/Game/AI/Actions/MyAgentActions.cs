using Sandbox.Engine.Utils;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using System;
using VRage.Utils;
using VRageMath;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.AI;

namespace Sandbox.Game.AI.Actions
{
    public abstract class MyAgentActions : MyBotActionsBase
    {
        protected MyAgentBot Bot { get; private set; }
        public MyAiTargetBase AiTargetBase { get { return Bot.AgentLogic.AiTarget; } }

        private string m_animationName = null;

        private MyRandomLocationSphere m_locationSphere;

        protected MyAgentActions(MyAgentBot bot)
        {
            Bot = bot;
            m_locationSphere = new MyRandomLocationSphere(Vector3D.Zero, 30, Vector3D.UnitX);
        }

        [MyBehaviorTreeAction("AimWithMovement", ReturnsRunning = false)]
        protected MyBehaviorTreeState AimWithMovement()
        {
            Bot.Navigation.AimWithMovement();
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("GotoTarget", MyBehaviorTreeActionType.INIT)]
        protected virtual void Init_GotoTarget()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTarget();
            }
        }

        [MyBehaviorTreeAction("GotoTarget")]
        protected MyBehaviorTreeState GotoTarget()
        {
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;
            if (Bot.Navigation.Navigating)
            {
                if (Bot.Navigation.Stuck)
                {
                    AiTargetBase.GotoFailed();
                    return MyBehaviorTreeState.FAILURE;
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

        [MyBehaviorTreeAction("GotoTarget", MyBehaviorTreeActionType.POST)]
        protected void Post_GotoTarget()
        {
            Bot.Navigation.StopImmediate(true);
        }

        [MyBehaviorTreeAction("GotoTargetNoPathfinding", MyBehaviorTreeActionType.INIT)]
        protected virtual void Init_GotoTargetNoPathfinding()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTargetNoPath(1.0f, true);
            }
        }

        [MyBehaviorTreeAction("GotoTargetNoPathfinding")]
        protected MyBehaviorTreeState GotoTargetNoPathfinding([BTParam] float radius, [BTParam] bool resetStuckDetection)
        {
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;
            if (Bot.Navigation.Navigating)
            {
                if (Bot.Navigation.Stuck)
                {
                    AiTargetBase.GotoFailed();
                    return MyBehaviorTreeState.FAILURE;
                }
                else
                {
                    AiTargetBase.GotoTargetNoPath(radius, resetStuckDetection);
                    return MyBehaviorTreeState.RUNNING;
                }
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("AimAtTarget", MyBehaviorTreeActionType.INIT)]
        protected void Init_AimAtTarget()
        {
            Init_AimAtTargetCustom();
        }

        [MyBehaviorTreeAction("AimAtTarget")]
        protected MyBehaviorTreeState AimAtTarget()
        {
            return AimAtTargetCustom(2.0f);
        }

        [MyBehaviorTreeAction("AimAtTarget", MyBehaviorTreeActionType.POST)]
        protected void Post_AimAtTarget()
        {
            Post_AimAtTargetCustom();
        }

        [MyBehaviorTreeAction("AimAtTargetCustom", MyBehaviorTreeActionType.INIT)]
        protected void Init_AimAtTargetCustom()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.AimAtTarget();
            }
        }

        [MyBehaviorTreeAction("AimAtTargetCustom")]
        protected MyBehaviorTreeState AimAtTargetCustom([BTParam] float tolerance)
        {
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;

            if (Bot.Navigation.HasRotation(MathHelper.ToRadians(tolerance)))
            {
                return MyBehaviorTreeState.RUNNING;
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("AimAtTargetCustom", MyBehaviorTreeActionType.POST)]
        protected void Post_AimAtTargetCustom()
        {
            Bot.Navigation.StopAiming();
        }

        [MyBehaviorTreeAction("PlayAnimation", ReturnsRunning = false)]
        protected MyBehaviorTreeState PlayAnimation([BTParam] string animationName, [BTParam] bool immediate)
        {
            if (Bot.Player.Character.HasAnimation(animationName))
            {
                m_animationName = animationName;
                Bot.Player.Character.PlayCharacterAnimation(animationName, immediate ? MyBlendOption.Immediate : MyBlendOption.WaitForPreviousEnd, MyFrameOption.PlayOnce, 0.0f);
                return MyBehaviorTreeState.SUCCESS;
            }
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsAtTargetPosition", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsAtTargetPosition([BTParam] float radius)
        {
            if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

            if (AiTargetBase.PositionIsNearTarget(Bot.Player.Character.PositionComp.GetPosition(), radius))
            {
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

        [MyBehaviorTreeAction("IsAtTargetPositionCylinder", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsAtTargetPositionCylinder([BTParam] float radius, [BTParam] float height)
		{
			if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

			Vector3D position = Bot.Player.Character.PositionComp.GetPosition();
			Vector3D gotoPosition;
			float gotoRadius;
            AiTargetBase.GetTargetPosition(position, out gotoPosition, out gotoRadius);
			var xzPosition = new Vector2((float)position.X, (float)position.Z);
			var xzGotoPosition = new Vector2((float)gotoPosition.X, (float)gotoPosition.Z);

			return (Vector2.Distance(xzPosition, xzGotoPosition) <= radius && xzPosition.Y < xzGotoPosition.Y && xzPosition.Y+height > xzGotoPosition.Y ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE);
		}

        [MyBehaviorTreeAction("IsNotAtTargetPosition", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsNotAtTargetPosition([BTParam] float radius)
        {
            if (!AiTargetBase.HasTarget())
            {
                return MyBehaviorTreeState.FAILURE;
            }

            if (AiTargetBase.PositionIsNearTarget(Bot.Player.Character.PositionComp.GetPosition(), radius))
            {
                return MyBehaviorTreeState.FAILURE;
            }
            else
            {
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("IsLookingAtTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsLookingAtTarget()
        {
            if (Bot.Navigation.HasRotation(MathHelper.ToRadians(2)))
                return MyBehaviorTreeState.FAILURE;
            else
                return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("SetTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState SetTarget([BTIn] ref MyBBMemoryTarget inTarget)
        {
            if (inTarget != null)
            {
                if (AiTargetBase.SetTargetFromMemory(inTarget))
                    return MyBehaviorTreeState.SUCCESS;
                else
                    return MyBehaviorTreeState.FAILURE;
            }
            else
            {
                AiTargetBase.UnsetTarget();
                return MyBehaviorTreeState.SUCCESS;
            }
        }

        [MyBehaviorTreeAction("ClearTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState ClearTarget([BTInOut] ref MyBBMemoryTarget inTarget)
		{
			if (inTarget != null)
			{
				inTarget.TargetType = MyAiTargetEnum.NO_TARGET;
				inTarget.Position = null;
				inTarget.EntityId = null;
				inTarget.TreeId = null;
			}

			return MyBehaviorTreeState.SUCCESS;
		}

        [MyBehaviorTreeAction("IsTargetValid", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsTargetValid([BTIn] ref MyBBMemoryTarget inTarget)
        {
            if (inTarget == null)
                return MyBehaviorTreeState.FAILURE;
            else
                return AiTargetBase.IsMemoryTargetValid(inTarget) ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("HasPlaceArea", ReturnsRunning = false)]
        protected MyBehaviorTreeState HasTargetArea([BTIn] ref MyBBMemoryTarget inTarget)
		{
			if (inTarget != null && inTarget.EntityId.HasValue)
			{
				MyEntity entity = null;
				if (MyEntities.TryGetEntityById(inTarget.EntityId.Value, out entity))
				{
					MyPlaceArea area = null;
					if (entity.Components.TryGet<MyPlaceArea>(out area))
					{
						return MyBehaviorTreeState.SUCCESS;
					}
				}
			}
			return MyBehaviorTreeState.FAILURE;
		}

        [MyBehaviorTreeAction("HasTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState HasTarget()
        {
            if (AiTargetBase.TargetType != MyAiTargetEnum.NO_TARGET)
            {
                return MyBehaviorTreeState.SUCCESS;
            }
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("HasNoTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState HasNoTarget()
        {
            return (HasTarget() == MyBehaviorTreeState.SUCCESS ? MyBehaviorTreeState.FAILURE : MyBehaviorTreeState.SUCCESS);
        }

        [MyBehaviorTreeAction("Stand", ReturnsRunning = false)]
        protected MyBehaviorTreeState Stand()
        {
            Bot.AgentEntity.Stand();
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("SwitchToWalk", ReturnsRunning = false)]
        protected MyBehaviorTreeState SwitchToWalk()
        {
            if (!Bot.AgentEntity.WantsWalk)
            {
                Bot.AgentEntity.SwitchWalk();
            }
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("SwitchToRun", ReturnsRunning = false)]
        protected MyBehaviorTreeState SwitchToRun()
        {
            if (Bot.AgentEntity.WantsWalk)
            {
                Bot.AgentEntity.SwitchWalk();
            }
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("GotoRandomLocation", MyBehaviorTreeActionType.INIT)]
        protected void Init_GotoRandomLocation()
        {
            // generate position and set navigation
            var position = Bot.AgentEntity.PositionComp.GetPosition();
            var up = MyPerGameSettings.NavmeshPresumesDownwardGravity ? Vector3D.UnitY : (Vector3D)MyGravityProviderSystem.CalculateTotalGravityInPoint(position);
            up.Normalize();
            var randomDir = MyUtils.GetRandomPerpendicularVector(ref up);
            var correctedPosition = position - randomDir * 15;
            AiTargetBase.SetTargetPosition(position + randomDir * 30);
            Bot.Navigation.AimAt(null,AiTargetBase.TargetPosition);
            m_locationSphere.Init(ref correctedPosition, 30, randomDir);
            Bot.Navigation.Goto(m_locationSphere);
        }

        static Vector3D GetRandomPerpendicularVector(ref Vector3D axis)
        {
            Debug.Assert(Vector3D.IsUnit(ref axis));
            Vector3D tangent = Vector3D.CalculatePerpendicularVector(axis);
            Vector3D bitangent; Vector3D.Cross(ref axis, ref tangent, out bitangent);
            double angle = MyUtils.GetRandomDouble(0, 2 * MathHelper.Pi);
            return Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent;
        }


        [MyBehaviorTreeAction("GotoRandomLocation")]
        protected MyBehaviorTreeState GotoRandomLocation()
        {
            return this.GotoTarget();
        }

        [MyBehaviorTreeAction("GotoRandomLocation", MyBehaviorTreeActionType.POST)]
        protected void Post_GotoRandomLocation()
        {
            this.Post_GotoTarget();
        }

        [MyBehaviorTreeAction("GotoAndAimTarget", MyBehaviorTreeActionType.INIT)]
        protected void Init_GotoAndAimTarget()
        {
            if (AiTargetBase.HasTarget())
            {
                AiTargetBase.GotoTarget();
                AiTargetBase.AimAtTarget();
            }
        }

        [MyBehaviorTreeAction("GotoAndAimTarget")]
        protected MyBehaviorTreeState GotoAndAimTarget()
        {
            const float rotationAngle = 2;
            if (!AiTargetBase.HasTarget())
                return MyBehaviorTreeState.FAILURE;
            if (Bot.Navigation.Navigating)
            {
                if (Bot.Navigation.Stuck)
                {
                    AiTargetBase.GotoFailed();
                    return MyBehaviorTreeState.FAILURE;
                }
                else
                {
                    // correction of target position - init of this node was called with different target or with not set target
                    Vector3D targetPos = AiTargetBase.GetTargetPosition(Bot.Navigation.PositionAndOrientation.Translation);
                    Vector3D navigationTarget = Bot.Navigation.TargetPoint;
                    if ((navigationTarget - targetPos).Length() > 0.1f )
                    {
                        CheckReplanningOfPath(targetPos, navigationTarget);
                    }
 
                    return MyBehaviorTreeState.RUNNING;
                }
            }
            else if (Bot.Navigation.HasRotation(MathHelper.ToRadians(rotationAngle)))
            {
                return MyBehaviorTreeState.RUNNING;
            }
            else if (AiTargetBase.PositionIsNearTarget(Bot.Navigation.PositionAndOrientation.Translation, 2.0f))
            {
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                AiTargetBase.GotoFailed();
                return MyBehaviorTreeState.FAILURE;
            }
        }

        void CheckReplanningOfPath(Vector3D targetPos, Vector3D navigationTarget)
        {
            // replans a path if target of navigation is too far from target position
            // current navigation target differs from actual target position
            Vector3D directionToTarget = targetPos - Bot.Navigation.PositionAndOrientation.Translation;
            Vector3D directionToNavigationTarget = navigationTarget - Bot.Navigation.PositionAndOrientation.Translation;
            double toTargetDist = directionToTarget.Length();
            double toNavigationTargetDist = directionToNavigationTarget.Length();
            if (toTargetDist != 0 && toNavigationTargetDist != 0)
            {
                double cosFi = directionToTarget.Dot(directionToNavigationTarget) / (toTargetDist * toNavigationTargetDist);
                double angle = Math.Acos(cosFi);
                double actualToPlanned = toTargetDist / toNavigationTargetDist;
                if (angle > Math.PI / 5 // angle to target is too different
                    || actualToPlanned < 0.8 // this could be ommited probably
                    || (actualToPlanned>1 && toNavigationTargetDist<2) ) // bot is approaching navigation target already
                {
                    // change of navigation target
                    AiTargetBase.GotoTarget();
                    AiTargetBase.AimAtTarget();
                }
            }
        }

        [MyBehaviorTreeAction("GotoAndAimTarget", MyBehaviorTreeActionType.POST)]
        protected void Post_GotoAndAimTarget()
        {
            Bot.Navigation.StopImmediate(true);
            Bot.Navigation.StopAiming();
        }

        [MyBehaviorTreeAction("StopAiming", ReturnsRunning = false)]
        protected MyBehaviorTreeState StopAiming()
        {
            Bot.Navigation.StopAiming();
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("GotoFailed", ReturnsRunning = false)]
        protected MyBehaviorTreeState GotoFailed()
        {
            if (AiTargetBase.HasGotoFailed)
            {
                return MyBehaviorTreeState.SUCCESS;
            }
            else
            {
                return MyBehaviorTreeState.FAILURE;
            }
        }

        [MyBehaviorTreeAction("ResetGotoFailed", ReturnsRunning = false)]
        protected MyBehaviorTreeState ResetGotoFailed()
        {
            AiTargetBase.HasGotoFailed = false;
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("IsMoving", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsMoving()
        {
            return Bot.Navigation.Navigating ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("FindClosestPlaceAreaInRadius", ReturnsRunning = false)]
        protected MyBehaviorTreeState FindClosestPlaceAreaInRadius([BTParam] float radius, [BTParam] string typeName, [BTOut] ref MyBBMemoryTarget outTarget)
        {
            if (MyItemsCollector.FindClosestPlaceAreaInSphere(new BoundingSphereD(Bot.AgentEntity.PositionComp.GetPosition(), radius), typeName, ref outTarget))
                return MyBehaviorTreeState.SUCCESS;
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsTargetBlock", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsTargetBlock([BTIn] ref MyBBMemoryTarget inTarget)
        {
            if (inTarget.TargetType == MyAiTargetEnum.COMPOUND_BLOCK || inTarget.TargetType == MyAiTargetEnum.CUBE)
                return MyBehaviorTreeState.SUCCESS;
            else
                return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsTargetNonBlock", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsTargetNonBlock([BTIn] ref MyBBMemoryTarget inTarget)
        {
            if (inTarget.TargetType == MyAiTargetEnum.COMPOUND_BLOCK || inTarget.TargetType == MyAiTargetEnum.CUBE)
                return MyBehaviorTreeState.FAILURE;
            else
                return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("FindClosestBlock", ReturnsRunning = false)]
        protected MyBehaviorTreeState FindClosestBlock([BTOut] ref MyBBMemoryTarget outBlock)
        {
            if (!AiTargetBase.IsTargetGridOrBlock(AiTargetBase.TargetType))
            {
                outBlock = null;
                return MyBehaviorTreeState.FAILURE;
            }

            var targetGrid = AiTargetBase.TargetGrid;

            Vector3 myPositionInGrid = Vector3D.Transform(Bot.BotEntity.PositionComp.GetPosition(), targetGrid.PositionComp.WorldMatrixNormalizedInv);

            float closest = float.MaxValue;
            MySlimBlock closestBlock = null;

            foreach (var block in targetGrid.GetBlocks())
            {
                Vector3 blockPos = block.Position;
                blockPos *= targetGrid.GridSize;

                float distSq = Vector3.DistanceSquared(blockPos, myPositionInGrid);
                if (distSq < closest)
                {
                    closestBlock = block;
                    closest = distSq;
                }
            }

            if (closestBlock == null)
            {
                return MyBehaviorTreeState.FAILURE;
            }

            MyBBMemoryTarget.SetTargetCube(ref outBlock, closestBlock.Position, closestBlock.CubeGrid.EntityId);
            return MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("SetAndAimTarget", ReturnsRunning = false)]
        protected MyBehaviorTreeState SetAndAimTarget([BTIn] ref MyBBMemoryTarget inTarget)
        {
            return SetTarget(true, ref inTarget);
        }

        protected MyBehaviorTreeState SetTarget(bool aim, ref MyBBMemoryTarget inTarget)
        {
            if (inTarget != null)
            {
                if (AiTargetBase.SetTargetFromMemory(inTarget))
                {
                    if (aim)
                    {
                        AiTargetBase.AimAtTarget();
                    }
                    return MyBehaviorTreeState.SUCCESS;
                }
                else
                    return MyBehaviorTreeState.FAILURE;

            }
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("FindCharacterInRadius", ReturnsRunning = false)]
        protected MyBehaviorTreeState FindCharacterInRadius([BTParam] int radius, [BTOut] ref MyBBMemoryTarget outCharacter)
        {
            var character = FindCharacterInRadius(radius);
            if (character != null)
            {
                MyBBMemoryTarget.SetTargetEntity(ref outCharacter, MyAiTargetEnum.CHARACTER, character.EntityId);
                return MyBehaviorTreeState.SUCCESS;
            }
            return MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("IsCharacterInRadius", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsCharacterInRadius([BTParam] int radius)
        {
            var character = FindCharacterInRadius(radius);
            return character == null || character.IsDead ? MyBehaviorTreeState.FAILURE : MyBehaviorTreeState.SUCCESS;
        }

        [MyBehaviorTreeAction("IsNoCharacterInRadius", ReturnsRunning = false)]
        protected MyBehaviorTreeState IsNoCharacterInRadius([BTParam] int radius)
        {
            var character = FindCharacterInRadius(radius);
            return character == null || character.IsDead ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        protected MyCharacter FindCharacterInRadius(int radius, bool ignoreReachability = false)
        {
            var myPosition = Bot.Navigation.PositionAndOrientation.Translation;
            var players = Sync.Players.GetOnlinePlayers();
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

                if (!(player.Character is MyCharacter) || (!ignoreReachability && !AiTargetBase.IsEntityReachable(player.Character)))
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
            }

            return closestCharacter;
        }

        [MyBehaviorTreeAction("HasCharacter", ReturnsRunning = false)]
        protected MyBehaviorTreeState HasCharacter()
        {
            return Bot.AgentEntity != null ? MyBehaviorTreeState.SUCCESS : MyBehaviorTreeState.FAILURE;
        }

        [MyBehaviorTreeAction("CallMoveAndRotate")]
        protected MyBehaviorTreeState CallMoveAndRotate()
        {
            if (Bot.AgentEntity == null) return MyBehaviorTreeState.FAILURE;
            Bot.AgentEntity.MoveAndRotate(Vector3.Zero, Vector2.One, 0);
            return MyBehaviorTreeState.RUNNING;
        }

        [MyBehaviorTreeAction("ClearUnreachableEntities")]
        protected MyBehaviorTreeState ClearUnreachableEntities()
        {
            AiTargetBase.ClearUnreachableEntities();
            return MyBehaviorTreeState.SUCCESS;
        }
    }
}
