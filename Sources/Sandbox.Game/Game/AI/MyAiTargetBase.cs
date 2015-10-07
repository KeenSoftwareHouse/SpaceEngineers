using Sandbox.Common.AI;
using Sandbox.Common.ObjectBuilders.AI;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI
{
    public class MyAiTargetBase
    {
        private const int UNREACHABLE_ENTITY_TIMEOUT = 60000;
        private const int UNREACHABLE_BLOCK_TIMEOUT = 60000;
        private const int UNREACHABLE_CHARACTER_TIMEOUT = 20000;

        protected MyAiTargetEnum m_currentTarget;
        protected IMyEntityBot m_user;
        protected MyAgentBot m_bot;

        protected MyEntity m_targetEntity = null;
        protected Vector3I m_targetCube = Vector3I.Zero;
        protected Vector3D m_targetPosition = Vector3D.Zero;
        protected Vector3I m_targetInVoxelCoord = Vector3I.Zero;
        protected ushort? m_compoundId = null;

        protected Dictionary<MyEntity, int> m_unreachableEntities = new Dictionary<MyEntity, int>();

        protected static List<MyEntity> m_tmpEntities = new List<MyEntity>();
        protected static List<MyPhysics.HitInfo> m_tmpHits = new List<MyPhysics.HitInfo>();

        public MyAiTargetEnum TargetType
        {
            get { return m_currentTarget; }
        }

        public bool HasTarget()
        {
            return m_currentTarget != MyAiTargetEnum.NO_TARGET;
        }

        public MyCubeGrid TargetGrid
        {
            get
            {
                Debug.Assert(IsTargetGridOrBlock(m_currentTarget) && m_targetEntity is MyCubeGrid);
                return m_targetEntity as MyCubeGrid;
            }
        }

        public MyEntity TargetEntity
        {
            get { return m_targetEntity; }
        }

        public Vector3D TargetPosition
        {
            get
            {
                return m_targetPosition;
            }
        }

        public Vector3D TargetCubeWorldPosition
        {
            get
            {
                Debug.Assert(m_currentTarget == MyAiTargetEnum.CUBE);
                var cubeblock = GetCubeBlock();
                Debug.Assert(cubeblock != null);
                if (cubeblock.FatBlock != null)
                {
                    return cubeblock.FatBlock.PositionComp.WorldAABB.Center;
                }
                else
                {
                    return TargetGrid.GridIntegerToWorld(m_targetCube);
                }
            }
        }

        public bool HasGotoFailed { get; set; }

        public bool IsTargetGridOrBlock(MyAiTargetEnum type)
        {
            return type == MyAiTargetEnum.CUBE || type == MyAiTargetEnum.GRID;
        }

        public MyAiTargetBase(IMyEntityBot bot)
        {
            m_user = bot;
            m_bot = bot as MyAgentBot;
            m_currentTarget = MyAiTargetEnum.NO_TARGET;

            MyAiTargetManager.AddAiTarget(this);
        }

        public virtual void Init(MyObjectBuilder_AiTarget builder)
        {
            m_currentTarget = builder.CurrentTarget;

            m_targetEntity = null;
            if (builder.EntityId.HasValue)
            {
                if (!MyEntities.TryGetEntityById(builder.EntityId.Value, out m_targetEntity))
                {
                    m_currentTarget = MyAiTargetEnum.NO_TARGET;
                }
            }
            else
            {
                m_currentTarget = MyAiTargetEnum.NO_TARGET;
            }

            m_targetCube = builder.TargetCube;
            m_targetPosition = builder.TargetPosition;
            m_compoundId = builder.CompoundId;

            if (builder.UnreachableEntities != null)
            {
                foreach (var data in builder.UnreachableEntities)
                {
                    MyEntity entity = null;
                    if (MyEntities.TryGetEntityById(data.UnreachableEntityId, out entity))
                        m_unreachableEntities.Add(entity, MySandboxGame.TotalGamePlayTimeInMilliseconds + data.Timeout);
                    else
                        Debug.Assert(false, "Couldn't find entity with given id: " + data.UnreachableEntityId);
                }
            }
        }

        public virtual MyObjectBuilder_AiTarget GetObjectBuilder()
        {
            var builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AiTarget>();
            builder.EntityId = m_targetEntity != null ? m_targetEntity.EntityId : (long?)null;
            builder.CurrentTarget = m_currentTarget;
            builder.TargetCube = m_targetCube;
            builder.TargetPosition = m_targetPosition;
            builder.CompoundId = m_compoundId;
            builder.UnreachableEntities = new List<MyObjectBuilder_AiTarget.UnreachableEntitiesData>();
            foreach (var pair in m_unreachableEntities)
            {
                var data = new MyObjectBuilder_AiTarget.UnreachableEntitiesData();
                data.UnreachableEntityId = pair.Key.EntityId;
                data.Timeout = pair.Value - MySandboxGame.TotalGamePlayTimeInMilliseconds;
                builder.UnreachableEntities.Add(data);
            }
            return builder;
        }

        public virtual void UnsetTarget()
        {
            m_currentTarget = MyAiTargetEnum.NO_TARGET;
            m_targetEntity = null;
        }

        public virtual void DebugDraw()
        {
        }

        public virtual void Cleanup()
        {
			MyAiTargetManager.RemoveAiTarget(this);
        }

        public virtual void Update()
        {
            m_tmpEntities.Clear();

            foreach (var entity in m_unreachableEntities)
            {
                if (entity.Value - MySandboxGame.TotalGamePlayTimeInMilliseconds < 0)
                    m_tmpEntities.Add(entity.Key);
            }

            foreach (var entity in m_tmpEntities)
            {
                RemoveUnreachableEntity(entity);
            }

            m_tmpEntities.Clear();
        }

        private void AddUnreachableEntity(MyEntity entity, int timeout)
        {
            m_unreachableEntities[entity] = MySandboxGame.TotalGamePlayTimeInMilliseconds + timeout;
            entity.OnClosing -= RemoveUnreachableEntity;
            entity.OnClosing += RemoveUnreachableEntity;
        }

        public bool IsEntityReachable(MyEntity entity)
        {
            bool parentsReachable = true;
            if (entity.Parent != null)
            {
                parentsReachable &= IsEntityReachable(entity.Parent);
            }
            return parentsReachable && !m_unreachableEntities.ContainsKey(entity);
        }

        private void RemoveUnreachableEntity(MyEntity entity)
        {
            entity.OnClosing -= RemoveUnreachableEntity;
            m_unreachableEntities.Remove(entity);
        }

        public bool PositionIsNearTarget(Vector3D position, float radius)
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return false;

            Vector3D gotoPosition;
            float gotoRadius;
            GetGotoPosition(position, out gotoPosition, out gotoRadius);

            return Vector3D.Distance(position, gotoPosition) <= radius + gotoRadius;
        }

        public void ClearUnreachableEntities()
        {
            m_unreachableEntities.Clear();
        }

        public void GotoTarget()
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION || m_currentTarget == MyAiTargetEnum.VOXEL)
            {
                m_bot.Navigation.Goto(m_targetPosition, 0.0f, m_targetEntity);
            }
            else
            {
                Vector3D gotoPosition;
                float radius;
                GetGotoPosition(m_bot.Navigation.PositionAndOrientation.Translation, out gotoPosition, out radius);
                m_bot.Navigation.Goto(gotoPosition, radius, m_targetEntity);
            }
        }

        public void GotoTargetNoPath(float radius)
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION || m_currentTarget == MyAiTargetEnum.VOXEL)
            {
                m_bot.Navigation.GotoNoPath(m_targetPosition, radius);
            }
            else
            {
                Vector3D gotoPosition;
                float r;
                GetGotoPosition(m_bot.Navigation.PositionAndOrientation.Translation, out gotoPosition, out r);
                m_bot.Navigation.GotoNoPath(gotoPosition, radius + r);
            }
        }

        public void GetGotoPosition(Vector3D startingPosition, out Vector3D gotoPosition, out float radius)
        {
            gotoPosition = default(Vector3D);
            radius = 0.0f;

            Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION)
            {
                gotoPosition = m_targetPosition;
                return;
            }
            else
            {
                Vector3D target = m_targetEntity.PositionComp.GetPosition();

                radius = 0.75f;

                if (m_currentTarget == MyAiTargetEnum.CUBE)
                {
                    Vector3D localVec = GetLocalCubeProjectedPosition(ref startingPosition);

                    radius = (float)localVec.Length() * 0.3f;
                    target = TargetCubeWorldPosition;

                    //if (MyFakes.NAVMESH_PRESUMES_DOWNWARD_GRAVITY)
                    //{
                    //    target += Vector3.Down * TargetGrid.GridSize * 0.5f;
                    //}
                }
                else if (m_currentTarget == MyAiTargetEnum.CHARACTER)
                {
                    radius = 0.5f;
                }
                else if (m_currentTarget == MyAiTargetEnum.ENVIRONMENT_ITEM)
                {
                    target = m_targetPosition;
                    radius = 0.75f;
                }
                else if (m_currentTarget == MyAiTargetEnum.VOXEL)
                {
                    target = m_targetPosition;
                }
                else if (m_currentTarget == MyAiTargetEnum.ENTITY)
                {
                    if (m_targetPosition != Vector3D.Zero)
                        target = m_targetPosition;

                    radius = m_targetEntity.PositionComp.LocalAABB.HalfExtents.Length();
                }

                gotoPosition = target;
            }
        }

        public void AimAtTarget()
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION || m_currentTarget == MyAiTargetEnum.VOXEL)
            {
                m_bot.Navigation.AimAt(null, m_targetPosition);
            }
            else
            {
                m_targetPosition = GetAimAtPosition(m_bot.Navigation.AimingPositionAndOrientation.Translation);
                m_bot.Navigation.AimAt(m_targetEntity, m_targetPosition);
            }
        }

        public void GotoFailed()
        {
            HasGotoFailed = true;
            if (m_currentTarget == MyAiTargetEnum.CHARACTER)
            {
                AddUnreachableEntity(m_targetEntity, UNREACHABLE_CHARACTER_TIMEOUT);
            }
            else if (m_currentTarget == MyAiTargetEnum.CUBE)
            {
                var grid = m_targetEntity as MyCubeGrid;
                var unreachableBlock = GetCubeBlock();
                if (unreachableBlock != null && unreachableBlock.FatBlock != null)
                {
                    AddUnreachableEntity(unreachableBlock.FatBlock, UNREACHABLE_BLOCK_TIMEOUT);
                }
            }
            else if (m_targetEntity != null && m_currentTarget != MyAiTargetEnum.VOXEL)
            {
                AddUnreachableEntity(m_targetEntity, UNREACHABLE_ENTITY_TIMEOUT);
            }

            UnsetTarget();
        }

        public virtual bool SetTargetFromMemory(MyBBMemoryTarget memoryTarget)
        {
            if (memoryTarget.TargetType == MyAiTargetEnum.POSITION)
            {
                Debug.Assert(memoryTarget.Position.HasValue, "Position was not set correctly in memory.");
                if (!memoryTarget.Position.HasValue) return false;

                SetTargetPosition(memoryTarget.Position.Value);
                return true;
            }
            else if (memoryTarget.TargetType == MyAiTargetEnum.ENVIRONMENT_ITEM)
            {
                Debug.Assert(memoryTarget.TreeId.HasValue, "Tree id was not set correctly in memory.");
                if (!memoryTarget.TreeId.HasValue) return false;

                var tree = new MyEnvironmentItems.ItemInfo();
                tree.LocalId = memoryTarget.TreeId.Value;
                tree.Transform.Position = memoryTarget.Position.Value;
                SetTargetTree(ref tree, memoryTarget.EntityId.Value);

                return true;
            }
            else if (memoryTarget.TargetType != MyAiTargetEnum.NO_TARGET)
            {
                Debug.Assert(memoryTarget.EntityId.HasValue, "Entity id was not set correctly in memory.");
                if (!memoryTarget.EntityId.HasValue) return false;

                MyEntity entity = null;
                if (MyEntities.TryGetEntityById(memoryTarget.EntityId.Value, out entity))
                {
                    if (memoryTarget.TargetType == MyAiTargetEnum.CUBE
                        || memoryTarget.TargetType == MyAiTargetEnum.COMPOUND_BLOCK)
                    {
                        var cubeGrid = entity as MyCubeGrid;
                        var cubeBlock = cubeGrid.GetCubeBlock(memoryTarget.BlockPosition);
                        Debug.Assert(cubeBlock != null, "Invalid position for a block");

                        if (cubeBlock != null)
                        {
                            if (memoryTarget.TargetType == MyAiTargetEnum.COMPOUND_BLOCK)
                            {
                                var realBlock = (cubeBlock.FatBlock as MyCompoundCubeBlock).GetBlock(memoryTarget.CompoundId.Value);
                                Debug.Assert(realBlock != null, "Block does not exist in the compound block");
                                if (realBlock == null)
                                    return false;
                                cubeBlock = realBlock;
                                m_compoundId = memoryTarget.CompoundId;
                            }
                            SetTargetBlock(cubeBlock);
                        }
                        else
                            return false;
                    }
                    else if (memoryTarget.TargetType == MyAiTargetEnum.ENTITY)
                    {
                        if (memoryTarget.Position.HasValue && entity is MyFracturedPiece)
                            m_targetPosition = memoryTarget.Position.Value;
                        else
                            m_targetPosition = entity.PositionComp.GetPosition();

                        SetTargetEntity(entity);
                        m_targetEntity = entity;
                    }
                    else if (memoryTarget.TargetType == MyAiTargetEnum.VOXEL)
                    {
                        var voxelMap = entity as MyVoxelMap;
                        Debug.Assert(memoryTarget.Position.HasValue, "Tree id was not set correctly in memory.");
                        if (!memoryTarget.Position.HasValue) return false;

                        Debug.Assert(voxelMap != null, "Voxel map hasn't been set.");
                        if (voxelMap == null) return false;

                        SetTargetVoxel(memoryTarget.Position.Value, voxelMap);
                        m_targetEntity = voxelMap;
                    }
                    else
                    {
                        SetTargetEntity(entity);
                    }
                    return true;
                }
                else
                {
                    UnsetTarget();
                    return false;
                }
            }
            else if (memoryTarget.TargetType == MyAiTargetEnum.NO_TARGET)
            {
                UnsetTarget();
                return true;
            }
            else
            {
                Debug.Assert(false, "Unrecognized type of target!");
                UnsetTarget();
                return false;
            }
        }

        protected virtual void SetTargetEntity(MyEntity entity)
        {
            if (entity == null)
            {
                if (m_targetEntity != null)
                {
                    UnsetTargetEntity();
                }
                return;
            }

            if (entity is MyCubeBlock)
            {
                SetTargetBlock((entity as MyCubeBlock).SlimBlock);
            }
            else
            {
                UnsetTarget();

                m_targetEntity = entity;

                if (entity is MyCubeGrid)
                {
                    (entity as MyCubeGrid).OnBlockRemoved += BlockRemoved;
                    m_currentTarget = MyAiTargetEnum.GRID;
                }
                else if (entity is MyCharacter)
                {
                    m_currentTarget = MyAiTargetEnum.CHARACTER;
                }
                else if (entity is MyVoxelBase)
                {
                    m_currentTarget = MyAiTargetEnum.VOXEL;
                }
                else if (entity is MyEntity)
                {
                    m_currentTarget = MyAiTargetEnum.ENTITY;
                }
            }
        }

        protected virtual void UnsetTargetEntity()
        {
            Debug.Assert(m_targetEntity != null);

            if (IsTargetGridOrBlock(m_currentTarget) && m_targetEntity is MyCubeGrid)
            {
                var grid = m_targetEntity as MyCubeGrid;
                grid.OnBlockRemoved -= BlockRemoved;
            }

            m_compoundId = null;
            m_targetEntity = null;
            m_currentTarget = MyAiTargetEnum.NO_TARGET;
        }

        private void BlockRemoved(MySlimBlock block)
        {
            Debug.Assert(IsTargetGridOrBlock(m_currentTarget));
            Debug.Assert(m_targetEntity == block.CubeGrid);

            var grid = TargetGrid;
            var block2 = GetCubeBlock();
            if (block2 == null)
            {
                UnsetTarget();
            }
        }

        public void SetTargetBlock(MySlimBlock slimBlock, ushort? compoundId = null)
        {
            if (m_targetEntity != slimBlock.CubeGrid)
            {
                UnsetTarget();
                SetTargetEntity(slimBlock.CubeGrid);
            }

            m_targetCube = slimBlock.Position;


            m_currentTarget = MyAiTargetEnum.CUBE;
        }

        public void SetTargetTree(ref MyTrees.ItemInfo targetTree, long treesId)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(treesId, out entity))
                return;

            UnsetTarget();

            m_targetPosition = targetTree.Transform.Position;
            m_targetEntity = entity;
            SetTargetEntity(entity);
        }

        public void SetTargetPosition(Vector3D pos)
        {
            UnsetTarget();
            m_targetPosition = pos;
            m_currentTarget = MyAiTargetEnum.POSITION;
        }

        public void SetTargetVoxel(Vector3D pos, MyVoxelMap voxelMap)
        {
            UnsetTarget();

            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref pos, out m_targetInVoxelCoord);
            m_targetPosition = pos;
            m_currentTarget = MyAiTargetEnum.VOXEL;
        }

        protected Vector3D GetLocalCubeProjectedPosition(ref Vector3D toProject)
        {
            Debug.Assert(m_currentTarget == MyAiTargetEnum.CUBE);

            var block = GetCubeBlock();
            Vector3D localVec = Vector3D.Transform(toProject, TargetGrid.PositionComp.WorldMatrixNormalizedInv);
            localVec -= (m_targetCube + new Vector3(0.5f)) * TargetGrid.GridSize;

            float mult = 1.0f;
            if (Math.Abs(localVec.Y) > Math.Abs(localVec.Z))
            {
                if (Math.Abs(localVec.Y) > Math.Abs(localVec.X))
                    mult = 1.0f / (float)Math.Abs(localVec.Y);
                else
                    mult = 1.0f / (float)Math.Abs(localVec.X);
            }
            else
            {
                if (Math.Abs(localVec.Z) > Math.Abs(localVec.X))
                    mult = 1.0f / (float)Math.Abs(localVec.Z);
                else
                    mult = 1.0f / (float)Math.Abs(localVec.X);
            }
            localVec *= mult;
            localVec *= TargetGrid.GridSize * 0.5f;
            return localVec;
        }

        public Vector3D GetAimAtPosition(Vector3D startingPosition)
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return Vector3D.Zero;

            if (m_currentTarget == MyAiTargetEnum.POSITION)
            {
                return m_targetPosition;
            }
            else if (m_currentTarget == MyAiTargetEnum.ENVIRONMENT_ITEM)
            {
                return m_targetPosition;
            }
            else
            {
                Vector3D target = m_targetEntity.PositionComp.GetPosition();

                if (m_currentTarget == MyAiTargetEnum.CUBE)
                {
                    Vector3D localVec = GetLocalCubeProjectedPosition(ref startingPosition);
                    target = TargetCubeWorldPosition;
                }
                else if (m_currentTarget == MyAiTargetEnum.CHARACTER)
                {
                    target = (m_targetEntity as MyCharacter).PositionComp.WorldVolume.Center;
                }
                else if (m_currentTarget == MyAiTargetEnum.VOXEL)
                {
                    target = m_targetPosition;
                }
                else if (m_currentTarget == MyAiTargetEnum.ENTITY)
                {
                    if (m_targetPosition != Vector3D.Zero)
                        target = m_targetPosition;
                }

                return target;
            }
        }

        protected MySlimBlock GetCubeBlock()
        {
            if (m_compoundId.HasValue)
            {
                var block = TargetGrid.GetCubeBlock(m_targetCube);
                if (block == null)
                    return null;
                return (block.FatBlock as MyCompoundCubeBlock).GetBlock(m_compoundId.Value);
            }
            else
            {
                return TargetGrid.GetCubeBlock(m_targetCube);
            }
        }
    }
}
