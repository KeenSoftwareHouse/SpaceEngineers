using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.AI
{
    public class MyAiTargetBase
    {
        private const int UNREACHABLE_ENTITY_TIMEOUT = 20000; // character target is currently transformed into Entity Target in SetTargetEntity function so UNREACHABLE_CHARACTER_TIMEOUT wasn't applied on characters at all...
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
        protected int m_targetTreeId = 0;

        protected Dictionary<MyEntity, int> m_unreachableEntities = new Dictionary<MyEntity, int>();
        protected Dictionary<Tuple<MyEntity, int>, int> m_unreachableTrees = new Dictionary<Tuple<MyEntity, int>, int>();

        protected static List<MyEntity> m_tmpEntities = new List<MyEntity>();
        protected static List<Tuple<MyEntity, int>> m_tmpTrees = new List<Tuple<MyEntity, int>>();
        protected static List<MyPhysics.HitInfo> m_tmpHits = new List<MyPhysics.HitInfo>();

        public MyAiTargetEnum TargetType
        {
            get { return m_currentTarget; }
        }

        public bool HasTarget()
        {
            return m_currentTarget != MyAiTargetEnum.NO_TARGET;
        }

        private void Clear()
        {
            m_currentTarget = MyAiTargetEnum.NO_TARGET;
            m_targetEntity = null;
            m_targetCube = Vector3I.Zero;
            m_targetPosition = Vector3D.Zero;
            m_targetInVoxelCoord = Vector3I.Zero;
            m_compoundId = null;
            m_targetTreeId = 0;
        }

        void SetMTargetPosition(Vector3D pos)
        {
            m_targetPosition = pos;
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
                switch (m_currentTarget)
                {
                    case MyAiTargetEnum.NO_TARGET:
                        return Vector3D.Zero;
                    case MyAiTargetEnum.POSITION:
                        return m_targetPosition;
                    case MyAiTargetEnum.VOXEL:
                        return m_targetPosition;//zxc?

                    case MyAiTargetEnum.COMPOUND_BLOCK:
                    case MyAiTargetEnum.CUBE:
                        {
                            MyCubeGrid target = m_targetEntity as MyCubeGrid;
                            if (target == null) 
                                return Vector3D.Zero;
                            MySlimBlock block = target.GetCubeBlock(m_targetCube);
                            if (block == null) 
                                return Vector3D.Zero;
                            return target.GridIntegerToWorld(block.Position);
                        }

                    case MyAiTargetEnum.ENVIRONMENT_ITEM:
                        return m_targetEntity.PositionComp.GetPosition();
                    case MyAiTargetEnum.CHARACTER:
                    case MyAiTargetEnum.GRID:
                    case MyAiTargetEnum.ENTITY:
                        return m_targetEntity.PositionComp.GetPosition();
                    default:
                        Debug.Assert(false, "Unhandled target type");
                        return Vector3D.Zero;
                }
            }
        }

        public Vector3D TargetCubeWorldPosition
        {
            get
            {
                Debug.Assert(m_currentTarget == MyAiTargetEnum.CUBE);
                var cubeblock = GetCubeBlock();
                Debug.Assert(cubeblock != null);
                if (cubeblock != null && cubeblock.FatBlock != null)
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

        public virtual bool IsMemoryTargetValid(MyBBMemoryTarget targetMemory)
        {
            if (targetMemory == null) return false;
            switch (targetMemory.TargetType)
            {
                case MyAiTargetEnum.CHARACTER:
                    {
                        MyCharacter target = null;
                        if (MyEntities.TryGetEntityById(targetMemory.EntityId.Value, out target))
                            return IsEntityReachable(target);
                        else
                            return false;
                    }
                case MyAiTargetEnum.CUBE:
                case MyAiTargetEnum.COMPOUND_BLOCK:
                    {
                        MyCubeGrid target = null;
                        if (MyEntities.TryGetEntityById(targetMemory.EntityId.Value, out target))
                        {
                            MySlimBlock block = target.GetCubeBlock(targetMemory.BlockPosition);
                            if (block == null) return false;
                            if (block.FatBlock != null)
                                return IsEntityReachable(block.FatBlock);
                            else
                                return IsEntityReachable(target);
                        }
                        else
                            return false;
                    }
                case MyAiTargetEnum.VOXEL:
                case MyAiTargetEnum.ENVIRONMENT_ITEM:
                    return true;
                case MyAiTargetEnum.ENTITY:
                case MyAiTargetEnum.GRID:
                    MyEntity entity = null;
                    if (MyEntities.TryGetEntityById(targetMemory.EntityId.Value, out entity))
                        return IsEntityReachable(entity);
                    else
                        return false;
                default:
                    return false;
            }
        }

        public Vector3D? GetMemoryTargetPosition(MyBBMemoryTarget targetMemory)
        {
            if (targetMemory == null) return null;
            switch (targetMemory.TargetType)
            {
                case MyAiTargetEnum.CHARACTER:
                case MyAiTargetEnum.ENTITY:
                case MyAiTargetEnum.GRID:
                    {
                        MyCharacter target = null;
                        if (MyEntities.TryGetEntityById(targetMemory.EntityId.Value, out target))
                            return target.PositionComp.GetPosition();
                        else
                            return null;
                    }
                case MyAiTargetEnum.CUBE:
                case MyAiTargetEnum.COMPOUND_BLOCK:
                    {
                        MyCubeGrid target = null;
                        if (MyEntities.TryGetEntityById(targetMemory.EntityId.Value, out target) &&
                            target.CubeExists(targetMemory.BlockPosition))
                        {
                            return target.GridIntegerToWorld(targetMemory.BlockPosition);
                        }
                        else
                            return null;
                    }
                case MyAiTargetEnum.POSITION:
                case MyAiTargetEnum.VOXEL:
                case MyAiTargetEnum.ENVIRONMENT_ITEM:
                    // CH: This seems wrong, but GetGotoPosition does the same weird thing.
                    // I should look at this later and fix it if possible...
                    return m_targetPosition;
                case MyAiTargetEnum.NO_TARGET:
                default:
                    return null;
            }
        }

        public virtual bool IsTargetValid()
        {
            switch (m_currentTarget)
            {
                case MyAiTargetEnum.CHARACTER:
                    {
                        MyCharacter target = m_targetEntity as MyCharacter;
                        return target != null && IsEntityReachable(target);
                    }
                case MyAiTargetEnum.CUBE:
                case MyAiTargetEnum.COMPOUND_BLOCK:
                    {
                        MyCubeGrid target = m_targetEntity as MyCubeGrid;
                        if (target == null) return false;
                        MySlimBlock block = target.GetCubeBlock(m_targetCube);
                        if (block == null) return false;
                        if (block.FatBlock != null)
                            return IsEntityReachable(block.FatBlock);
                        else
                            return IsEntityReachable(target);
                    }
                case MyAiTargetEnum.VOXEL:
                case MyAiTargetEnum.ENVIRONMENT_ITEM:
                    return true;
                case MyAiTargetEnum.ENTITY:
                case MyAiTargetEnum.GRID:
                    return IsEntityReachable(m_targetEntity);
                default:
                    return false;
            }
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
            SetMTargetPosition(builder.TargetPosition);
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
            switch (m_currentTarget)
            {
                case MyAiTargetEnum.NO_TARGET:
                case MyAiTargetEnum.CHARACTER:
                case MyAiTargetEnum.ENTITY:
                case MyAiTargetEnum.CUBE:
                case MyAiTargetEnum.GRID:
				case MyAiTargetEnum.ENVIRONMENT_ITEM:
				case MyAiTargetEnum.VOXEL:
                    if (m_targetEntity!=null)
                        UnsetTargetEntity();
                    break;
            }
            Debug.Assert(m_targetEntity == null);
            Clear();
        }

        public virtual void DebugDraw()
        {
        }

        public virtual void DrawLineToTarget(Vector3D from)
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

            m_tmpTrees.Clear();
            foreach (var entity in m_unreachableTrees)
            {
                if (entity.Value - MySandboxGame.TotalGamePlayTimeInMilliseconds < 0)
                    m_tmpTrees.Add(entity.Key);
            }
            foreach (var entity in m_tmpTrees)
            {
                RemoveUnreachableTree(entity);
            }
            m_tmpTrees.Clear();
        }

        private void AddUnreachableEntity(MyEntity entity, int timeout)
        {
            m_unreachableEntities[entity] = MySandboxGame.TotalGamePlayTimeInMilliseconds + timeout;
            entity.OnClosing -= RemoveUnreachableEntity;
            entity.OnClosing += RemoveUnreachableEntity;
        }

        private void AddUnreachableTree(MyEntity entity, int treeId, int timeout)
        {
            m_unreachableTrees[new Tuple<MyEntity, int>(entity, treeId)] = MySandboxGame.TotalGamePlayTimeInMilliseconds + timeout;
            entity.OnClosing -= RemoveUnreachableTrees;
            entity.OnClosing += RemoveUnreachableTrees;
        }


        public bool IsEntityReachable(MyEntity entity)
        {
            if (entity == null) return false;
            bool parentsReachable = true;
            if (entity.Parent != null)
            {
                parentsReachable &= IsEntityReachable(entity.Parent);
            }
            return parentsReachable && !m_unreachableEntities.ContainsKey(entity);
        }
        public bool IsTreeReachable(MyEntity entity, int treeId)
        {
            if (entity == null) return false;
            bool parentsReachable = true;
            if (entity.Parent != null)
            {
                parentsReachable &= IsEntityReachable(entity.Parent);
            }
            return parentsReachable && !m_unreachableTrees.ContainsKey(new Tuple<MyEntity,int>(entity, treeId));
        }

        private void RemoveUnreachableEntity(MyEntity entity)
        {
            entity.OnClosing -= RemoveUnreachableEntity;
            m_unreachableEntities.Remove(entity);
        }
        private void RemoveUnreachableTree(Tuple<MyEntity,int> tree)
        {
            m_unreachableTrees.Remove(tree);
        }

        private void RemoveUnreachableTrees(MyEntity entity)
        {
            entity.OnClosing -= RemoveUnreachableTrees;

            m_tmpTrees.Clear();
            foreach (var tree in m_unreachableTrees.Keys)
            {
                MyEntity ent = tree.Item1;
                if ( ent == entity)
                {
                    m_tmpTrees.Add(tree);
                }
            }

            foreach (var tre in m_tmpTrees)
            {
                RemoveUnreachableTree(tre);
            }
            m_tmpTrees.Clear();
        }

        public bool PositionIsNearTarget(Vector3D position, float radius)
        {
            Debug.Assert(HasTarget());
            if (!HasTarget()) return false;

            Vector3D targetPosition;
            float gotoRadius;
            GetTargetPosition(position, out targetPosition, out gotoRadius);

            return Vector3D.Distance(position, targetPosition) <= radius + gotoRadius;
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
                GetTargetPosition(m_bot.Navigation.PositionAndOrientation.Translation, out gotoPosition, out radius);
                m_bot.Navigation.Goto(gotoPosition, radius, m_targetEntity);
            }
        }

        public void GotoTargetNoPath(float radius, bool resetStuckDetection = true)
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
                GetTargetPosition(m_bot.Navigation.PositionAndOrientation.Translation, out gotoPosition, out r);
                m_bot.Navigation.GotoNoPath(gotoPosition, radius + r, resetStuckDetection: resetStuckDetection);
            }
        }

        public void GetTargetPosition(Vector3D startingPosition, out Vector3D targetPosition, out float radius)
        {
            targetPosition = default(Vector3D);
            radius = 0.0f;

            Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION)
            {
                targetPosition = m_targetPosition;
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
                    target = TargetCubeWorldPosition + localVec;

                    //if (MyFakes.NAVMESH_PRESUMES_DOWNWARD_GRAVITY)
                    //{
                    //    target += Vector3.Down * TargetGrid.GridSize * 0.5f;
                    //}
                }
                else if (m_currentTarget == MyAiTargetEnum.CHARACTER)
                {
                    radius = 0.65f;
                    var charPos = (m_targetEntity as MyCharacter).PositionComp;
                    target = charPos.WorldVolume.Center;
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
                    if ( m_targetPosition != Vector3D.Zero && m_targetEntity is MyFracturedPiece )
                    {
                        // can be here something different than a trunk?
                        target = m_targetPosition;
                    }
                    radius = m_targetEntity.PositionComp.LocalAABB.HalfExtents.Length();
                }

                targetPosition = target;
            }
        }

        public Vector3D GetTargetPosition(Vector3D startingPosition)
        {
            float radius;
            Vector3D targetPos;
            GetTargetPosition(startingPosition, out targetPos, out radius);
            return targetPos;
        }

        public void AimAtTarget()
        {
            //Debug.Assert(HasTarget());
            if (!HasTarget()) return;

            if (m_currentTarget == MyAiTargetEnum.POSITION || m_currentTarget == MyAiTargetEnum.VOXEL)
            {
                m_bot.Navigation.AimAt(null, m_targetPosition);
            }
            else
            {
                SetMTargetPosition(GetAimAtPosition(m_bot.Navigation.AimingPositionAndOrientation.Translation));
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
            else if (m_targetEntity != null && m_targetEntity is MyTrees)
            {
                // record of unreacheable tree
                AddUnreachableTree(m_targetEntity, m_targetTreeId, UNREACHABLE_ENTITY_TIMEOUT);
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
                            SetMTargetPosition(memoryTarget.Position.Value);
                        else
                            SetMTargetPosition(entity.PositionComp.GetPosition());

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
                        //SetMTargetPosition(Vector3D.Zero);
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
            if (entity is MyCubeBlock)
            {
                SetTargetBlock((entity as MyCubeBlock).SlimBlock);
            }
            else
            {
                if (m_targetEntity != null)
                    UnsetTargetEntity();
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
                else if (entity is MyEnvironmentItems)
                {
                    m_currentTarget = MyAiTargetEnum.ENVIRONMENT_ITEM;
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
                UnsetTargetEntity();
            }
        }

        public void SetTargetBlock(MySlimBlock slimBlock, ushort? compoundId = null)
        {
            if (m_targetEntity != slimBlock.CubeGrid)
            {
                // We don't have to unset the target, because it will be done in SetTargetEntity
                SetTargetEntity(slimBlock.CubeGrid);
            }

            m_targetCube = slimBlock.Position;
            m_currentTarget = MyAiTargetEnum.CUBE;
        }

        public MySlimBlock GetTargetBlock()
        {
            if (m_currentTarget != MyAiTargetEnum.CUBE) return null;
            if (TargetGrid == null) return null;
            return GetCubeBlock();
        }

        public void SetTargetTree(ref MyTrees.ItemInfo targetTree, long treesId)
        {
            MyEntity entity;
            if (!MyEntities.TryGetEntityById(treesId, out entity))
                return;

            UnsetTarget();

            SetMTargetPosition(targetTree.Transform.Position);
            m_targetEntity = entity;
            m_targetTreeId = targetTree.LocalId;
            SetTargetEntity(entity);
        }

        public void SetTargetPosition(Vector3D pos)
        {
            UnsetTarget();
            SetMTargetPosition(pos);
            m_currentTarget = MyAiTargetEnum.POSITION;
        }

        public void SetTargetVoxel(Vector3D pos, MyVoxelMap voxelMap)
        {
            UnsetTarget();

            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref pos, out m_targetInVoxelCoord);
            SetMTargetPosition(pos);
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
                    var charPos = (m_targetEntity as MyCharacter).PositionComp;
                    target = Vector3D.Transform(charPos.LocalVolume.Center, charPos.WorldMatrix);
                }
                else if (m_currentTarget == MyAiTargetEnum.VOXEL)
                {
                    target = m_targetPosition;
                }
                else if (m_currentTarget == MyAiTargetEnum.ENTITY)
                {
                    if (m_targetPosition != Vector3D.Zero && m_targetEntity is MyFracturedPiece)
                    {
                        // can be here something different than a trunk?
                        target = m_targetPosition;
                    }

                    //if (m_targetPosition != Vector3D.Zero)
                    //    target = m_targetPosition;
                }

                return target;
            }
        }

        public virtual bool GetRandomDirectedPosition(Vector3D initPosition, Vector3D direction, out Vector3D outPosition)
        {
            outPosition = MySession.Static.LocalCharacter.PositionComp.GetPosition();
            return true;
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
                return TargetGrid != null ? TargetGrid.GetCubeBlock(m_targetCube) : null;
            }
        }
    }
}
