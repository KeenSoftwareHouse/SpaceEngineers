using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using VRage.Collections;
using VRageMath;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage;
using VRageRender;
using Sandbox.Common;
using VRage.Utils;
using Sandbox.Game.Components;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Models;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Import;
using VRage.Game.Models;
using VRage.Game.ModAPI.Interfaces;
using VRageRender.Import;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Cube block which is composed of several cube blocks together with shared compound template name.
    /// </summary>
    [MyCubeBlockType(typeof(MyObjectBuilder_CompoundCubeBlock))]
    public class MyCompoundCubeBlock : MyCubeBlock, IMyDecalProxy
    {
        static List<VertexArealBoneIndexWeight> m_boneIndexWeightTmp;

        private class MyCompoundBlockPosComponent : MyCubeBlock.MyBlockPosComponent
        {
            private MyCompoundCubeBlock m_block;

            public override void OnAddedToContainer()
            {
                base.OnAddedToContainer();
                m_block = Container.Entity as MyCompoundCubeBlock;
                Debug.Assert(m_block != null);
            }

            public override void UpdateWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
            {
                m_block.UpdateBlocksWorldMatrix(ref parentWorldMatrix, source);
                base.UpdateWorldMatrix(ref parentWorldMatrix, source);
            }
        }

        private static string COMPOUND_DUMMY = "compound_";
        // IDentifier of local block ids.
        private static ushort BLOCK_IN_COMPOUND_LOCAL_ID = 0x8000;
        private static ushort BLOCK_IN_COMPOUND_LOCAL_MAX_VALUE = 0x7FFF;

        private static readonly MyStringId BUILD_TYPE_ANY = MyStringId.GetOrCompute("any");

        // Common compound block subtype name
        private static readonly string COMPOUND_BLOCK_SUBTYPE_NAME = "CompoundBlock";

        private static readonly HashSet<string> m_tmpTemplates = new HashSet<string>();
        private static readonly List<MyModelDummy> m_tmpDummies = new List<MyModelDummy>();
        private static readonly List<MyModelDummy> m_tmpOtherDummies = new List<MyModelDummy>();


        private readonly Dictionary<ushort, MySlimBlock> m_mapIdToBlock = new Dictionary<ushort, MySlimBlock>();
        // Duplicated array of blocks for getting them as list without allocation.
        private readonly List<MySlimBlock> m_blocks = new List<MySlimBlock>();

        // Server next id;
        private ushort m_nextId;
        // Local next id identifier of blocks which are set directly (not through server - i.e. generated blocks).
        private ushort m_localNextId;

        // Refreshed set of shared templates between all blocks.
        private HashSet<string> m_templates = new HashSet<string>();

        private class MyDebugRenderComponentCompoundBlock : MyDebugRenderComponent
        {
            MyCompoundCubeBlock m_compoundBlock = null;

            public MyDebugRenderComponentCompoundBlock(MyCompoundCubeBlock compoundBlock)
                : base(compoundBlock)
            {
                m_compoundBlock = compoundBlock;
            }

            public override void DebugDraw()
            {
                foreach (var block in m_compoundBlock.GetBlocks())
                    if (block.FatBlock != null)
                        block.FatBlock.DebugDraw();
            }
        }


        public MyCompoundCubeBlock() 
        {
            PositionComp = new MyCompoundBlockPosComponent();
            Render = new Components.MyRenderComponentCompoundCubeBlock();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_CompoundCubeBlock;

            if (ob.Blocks != null)
            {
                if (ob.BlockIds != null)
                {
                    Debug.Assert(ob.Blocks.Length == ob.BlockIds.Length);

                    for (int i = 0; i < ob.Blocks.Length; ++i)
                    {
                        ushort id = ob.BlockIds[i];
                        if (m_mapIdToBlock.ContainsKey(id))
                        {
                            Debug.Fail("Block with the same id found");
                            continue;
                        }

                        var blockBuilder = ob.Blocks[i];
                        object objectBlock = MyCubeBlockFactory.CreateCubeBlock(blockBuilder);
                        MySlimBlock cubeBlock = objectBlock as MySlimBlock;
                        if (cubeBlock == null)
                            cubeBlock = new MySlimBlock();

                        cubeBlock.Init(blockBuilder, cubeGrid, objectBlock as MyCubeBlock);

                        if (cubeBlock.FatBlock != null)
                        {
                            cubeBlock.FatBlock.HookMultiplayer();

                            cubeBlock.FatBlock.Hierarchy.Parent = Hierarchy;
                            m_mapIdToBlock.Add(id, cubeBlock);
                            m_blocks.Add(cubeBlock);
                        }
                    }

                    RefreshNextId();
                }
                else
                {
                    for (int i = 0; i < ob.Blocks.Length; ++i)
                    {
                        var blockBuilder = ob.Blocks[i];
                        object objectBlock = MyCubeBlockFactory.CreateCubeBlock(blockBuilder);
                        MySlimBlock cubeBlock = objectBlock as MySlimBlock;
                        if (cubeBlock == null)
                            cubeBlock = new MySlimBlock();

                        cubeBlock.Init(blockBuilder, cubeGrid, objectBlock as MyCubeBlock);
                        cubeBlock.FatBlock.HookMultiplayer();

                        cubeBlock.FatBlock.Hierarchy.Parent = Hierarchy;
                        ushort id = CreateId(cubeBlock);
                        m_mapIdToBlock.Add(id, cubeBlock);
                        m_blocks.Add(cubeBlock);
                    }
                }
            }

            Debug.Assert(m_mapIdToBlock.Count == m_blocks.Count);

            RefreshTemplates();

            AddDebugRenderComponent(new MyDebugRenderComponentCompoundBlock(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_CompoundCubeBlock objectBuilder = (MyObjectBuilder_CompoundCubeBlock)base.GetObjectBuilderCubeBlock(copy);
            if (m_mapIdToBlock.Count > 0) 
            {
                objectBuilder.Blocks = new MyObjectBuilder_CubeBlock[m_mapIdToBlock.Count];
                objectBuilder.BlockIds = new ushort[m_mapIdToBlock.Count];

                int counter = 0;
                foreach (var pair in m_mapIdToBlock)
                {
                    objectBuilder.BlockIds[counter] = pair.Key;
                    if(!copy)
                        objectBuilder.Blocks[counter] = pair.Value.GetObjectBuilder();
                    else
                        objectBuilder.Blocks[counter] = pair.Value.GetCopyObjectBuilder();
                    ++counter;
                }
            }

            Debug.Assert(objectBuilder.Blocks == null && objectBuilder.BlockIds == null || objectBuilder.Blocks.Length == objectBuilder.BlockIds.Length);

            return objectBuilder;
        }

        public override void OnAddedToScene(object source)
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.OnAddedToScene(source);
                else
                    Debug.Assert(false);
            }

            base.OnAddedToScene(source);
        }

        public override void OnRemovedFromScene(object source)
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.OnRemovedFromScene(source);
                else
                    Debug.Assert(false);
            }

            base.OnRemovedFromScene(source);
        }

        public override void UpdateVisual()
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.UpdateVisual();
                else
                    Debug.Assert(false);
            }

            base.UpdateVisual();
        }

        public override float GetMass()
        {
            float mass = 0f;
            foreach (var pair in m_mapIdToBlock)
                mass += pair.Value.GetMass();
            return mass;
        }

        private void UpdateBlocksWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
        {
            MatrixD localMatrix = MatrixD.Identity;
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                {
                    GetBlockLocalMatrixFromGridPositionAndOrientation(pair.Value, ref localMatrix);
                    MatrixD worldMatrix = localMatrix * parentWorldMatrix;
                    pair.Value.FatBlock.PositionComp.SetWorldMatrix(worldMatrix, this, true);
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        protected override void Closing()
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.Close();
                else
                    Debug.Assert(false);
            }

            base.Closing();
        }

        public override void OnCubeGridChanged(MyCubeGrid oldGrid)
        {
            base.OnCubeGridChanged(oldGrid);

            foreach (var pair in m_mapIdToBlock)
            {
                pair.Value.CubeGrid = CubeGrid;
            }
        }

        internal override void OnTransformed(ref MatrixI transform)
        {
            foreach (var pair in m_mapIdToBlock)
            {
                pair.Value.Transform(ref transform);
            }
        }

        internal override void UpdateWorldMatrix()
        {
            base.UpdateWorldMatrix();

            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.UpdateWorldMatrix();
            }
        }

        public override bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                {
                    if (pair.Value.FatBlock.ConnectionAllowed(ref otherBlockPos, ref faceNormal, def))
                        return true;
                }
            }
            return false;
        }

        public override bool ConnectionAllowed(ref Vector3I otherBlockMinPos, ref Vector3I otherBlockMaxPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            foreach (var pair in m_mapIdToBlock)
            {
                if (pair.Value.FatBlock != null)
                {
                    if (pair.Value.FatBlock.ConnectionAllowed(ref otherBlockMinPos, ref otherBlockMaxPos, ref faceNormal, def))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds block to compound (should be used on server only and for generated blocks on local).
        /// </summary>
        /// <returns>true if the block has been addded, otherwise false</returns>
        public bool Add(MySlimBlock block, out ushort id)
        {
            id = CreateId(block);
            return Add(id, block);
        }

        /// <summary>
        /// Adds block to compound (should be used for clients).
        /// </summary>
        /// <returns>true if the block has been addded, otherwise false</returns>
        public bool Add(ushort id, MySlimBlock block)
        {
            if (!CanAddBlock(block))
                return false;

            Debug.Assert(block.BlockDefinition.Size == Vector3I.One, "Only blocks with size=1 are supported inside compound block!");
            Debug.Assert(block.BlockDefinition.CubeSize == MyCubeSize.Large, "Only large blocks are supported inside compound block!");

            if (m_mapIdToBlock.ContainsKey(id))
            {
                Debug.Fail("Cannot add block with existing id to compound block!");
                return false;
            }

            m_mapIdToBlock.Add(id, block);
            m_blocks.Add(block);
            Debug.Assert(m_mapIdToBlock.Count == m_blocks.Count);

            MatrixD parentWorldMatrix = this.Parent.WorldMatrix;
            MatrixD blockLocalMatrix = MatrixD.Identity;
            GetBlockLocalMatrixFromGridPositionAndOrientation(block, ref blockLocalMatrix);
            MatrixD worldMatrix = blockLocalMatrix * parentWorldMatrix;
            block.FatBlock.PositionComp.SetWorldMatrix(worldMatrix, this, true);

            // Set parent before OnAddedToScene otherwise chhild block will be added to pruning structure
            block.FatBlock.Hierarchy.Parent = Hierarchy;

            block.FatBlock.OnAddedToScene(this);

            CubeGrid.UpdateBlockNeighbours(SlimBlock);

            RefreshTemplates();

            if (block.IsMultiBlockPart)
                CubeGrid.AddMultiBlockInfo(block);

            return true;
        }

        public bool Remove(MySlimBlock block, bool merged = false)
        {
            var pair = m_mapIdToBlock.FirstOrDefault(p => p.Value == block);
            if (pair.Value == block)
                return Remove(pair.Key, merged: merged);

            Debug.Fail("Cannot remove block from compound");
            return false;
        }

        public bool Remove(ushort blockId, bool merged = false)
        {
            MySlimBlock block;
            if (m_mapIdToBlock.TryGetValue(blockId, out block))
            {
                m_mapIdToBlock.Remove(blockId);
                m_blocks.Remove(block);
                Debug.Assert(m_mapIdToBlock.Count == m_blocks.Count);

                if (!merged)
                {
                    if (block.IsMultiBlockPart)
                        CubeGrid.RemoveMultiBlockInfo(block);

                    block.FatBlock.OnRemovedFromScene(this);
                    block.FatBlock.Close();
                }

                if (block.FatBlock.Hierarchy.Parent == Hierarchy)
                    block.FatBlock.Hierarchy.Parent = null;

                if (!merged)
                    CubeGrid.UpdateBlockNeighbours(SlimBlock);

                RefreshTemplates();
                return true;
            }

            Debug.Fail("Cannot remove block from compound");
            return false;
        }

        public bool CanAddBlock(MySlimBlock block)
        {
            if (block == null || block.FatBlock == null)
                return false;

            if (m_mapIdToBlock.ContainsValue(block))
                return false;

            if (block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock otherCompound = block.FatBlock as MyCompoundCubeBlock;
                foreach (var otherBlockInCompound in otherCompound.GetBlocks())
                {
                    if (!CanAddBlock(otherBlockInCompound.BlockDefinition, otherBlockInCompound.Orientation, multiBlockId: otherBlockInCompound.MultiBlockId))
                        return false;
                }
                return true;
            }
            else
            {
                return CanAddBlock(block.BlockDefinition, block.Orientation, multiBlockId: block.MultiBlockId);
            }
        }

        public bool CanAddBlock(MyCubeBlockDefinition definition, MyBlockOrientation? orientation, int multiBlockId = 0, bool ignoreSame = false)
        {
            Debug.Assert(definition != GetCompoundCubeBlockDefinition());

            if (!IsCompoundEnabled(definition))
                return false;

            if (MyFakes.ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES)
            {
                if (orientation == null)
                    return false;

                if (m_blocks.Count == 0)
                    return true;

                Matrix otherRotation, thisRotation;
                orientation.Value.GetMatrix(out otherRotation);

                m_tmpOtherDummies.Clear();
                GetCompoundCollisionDummies(definition, m_tmpOtherDummies);

                foreach (var block in m_blocks)
                {
                    // The same block with the same orientation
                    if (block.BlockDefinition.Id.SubtypeId == definition.Id.SubtypeId && block.Orientation == orientation.Value)
                    {
                        if (ignoreSame)
                            continue;
                        else
                            return false;
                    }

                    // Blocks from the same multiblock can be added to one compound
                    if (multiBlockId != 0 && block.MultiBlockId == multiBlockId)
                        continue;

                    if (block.BlockDefinition.IsGeneratedBlock)
                        continue;

                    m_tmpDummies.Clear();
                    GetCompoundCollisionDummies(block.BlockDefinition, m_tmpDummies);

                    block.Orientation.GetMatrix(out thisRotation);

                    if (CompoundDummiesIntersect(ref thisRotation, ref otherRotation, m_tmpDummies, m_tmpOtherDummies))
                    {
                        m_tmpDummies.Clear();
                        m_tmpOtherDummies.Clear();
                        return false;
                    }
                }

                m_tmpDummies.Clear();
                m_tmpOtherDummies.Clear();

                return true;
            }

            // Check the same block with the same orientation or same build type.
            if (orientation != null)
            {
                foreach (var pair in m_mapIdToBlock)
                {
                    if (pair.Value.BlockDefinition.Id.SubtypeId == definition.Id.SubtypeId && pair.Value.Orientation == orientation)
                        return false;
                    else if (definition.BuildType != null && pair.Value.BlockDefinition.BuildType == definition.BuildType && pair.Value.Orientation == orientation)
                        return false;
                }
            }

            // Check templates
            foreach (var template in definition.CompoundTemplates) 
            {
                if (m_templates.Contains(template)) 
                {
                    MyCompoundBlockTemplateDefinition templateDefinition = GetTemplateDefinition(template);
                    if (templateDefinition == null || templateDefinition.Bindings == null)
                    {
                        Debug.Assert(false);
                        continue;
                    }

                    MyCompoundBlockTemplateDefinition.MyCompoundBlockBinding binding = GetTemplateDefinitionBinding(templateDefinition, definition);
                    if (binding == null)
                        continue;

                    if (binding.BuildType == BUILD_TYPE_ANY)
                        return true;

                    if (!binding.Multiple)
                    {
                        bool continueNextTemplate = false;

                        foreach (var pair in m_mapIdToBlock)
                        {
                            if (pair.Value.BlockDefinition.BuildType == definition.BuildType) 
                            {
                                continueNextTemplate = true;
                                break;
                            }
                        }

                        if (continueNextTemplate)
                            continue;
                    }

                    // Check rotations
                    if (orientation != null)
                    {
                        bool continueNextTemplate = false;

                        foreach (var pair in m_mapIdToBlock)
                        {
                            MyCompoundBlockTemplateDefinition.MyCompoundBlockBinding existingBlockBinding = GetTemplateDefinitionBinding(templateDefinition, pair.Value.BlockDefinition);
                            if (existingBlockBinding == null) 
                            {
                                Debug.Assert(false);
                                continue;
                            }

                            if (existingBlockBinding.BuildType == BUILD_TYPE_ANY)
                                continue;

                            MyCompoundBlockTemplateDefinition.MyCompoundBlockRotationBinding rotBinding = GetRotationBinding(templateDefinition, definition, pair.Value.BlockDefinition);
                            if (rotBinding == null)
                                continue;

                            if (rotBinding.BuildTypeReference == definition.BuildType) 
                            {
                                if (IsRotationValid(orientation.Value, pair.Value.Orientation, rotBinding.Rotations))
                                    continue;

                                // The same build type must be checked from both sides
                                if (rotBinding.BuildTypeReference == pair.Value.BlockDefinition.BuildType)
                                {
                                    if (IsRotationValid(pair.Value.Orientation, orientation.Value, rotBinding.Rotations))
                                        continue;
                                }
                            }
                            else 
                            {
                                Debug.Assert(rotBinding.BuildTypeReference == pair.Value.BlockDefinition.BuildType);
                                if (IsRotationValid(pair.Value.Orientation, orientation.Value, rotBinding.Rotations))
                                    continue;
                            }

                            continueNextTemplate = true;
                            break;
                        }

                        if (continueNextTemplate)
                            continue;
                    }

                    return true;
                }
            }

            return false;
        }

        public static bool CanAddBlocks(MyCubeBlockDefinition definition, MyBlockOrientation orientation, MyCubeBlockDefinition otherDefinition, MyBlockOrientation otherOrientation)
        {
            Debug.Assert(MyFakes.ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES);

            if (!IsCompoundEnabled(definition) || !IsCompoundEnabled(otherDefinition))
                return false;

            if (MyFakes.ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES)
            {
                Matrix thisRotation;
                orientation.GetMatrix(out thisRotation);

                m_tmpDummies.Clear();
                GetCompoundCollisionDummies(definition, m_tmpDummies);

                Matrix otherRotation;
                otherOrientation.GetMatrix(out otherRotation);

                m_tmpOtherDummies.Clear();
                GetCompoundCollisionDummies(otherDefinition, m_tmpOtherDummies);

                bool intersect = CompoundDummiesIntersect(ref thisRotation, ref otherRotation, m_tmpDummies, m_tmpOtherDummies);
                m_tmpDummies.Clear();
                m_tmpOtherDummies.Clear();
                return !intersect;
            }

            // Note that this part (compound templates) is not implemented, because it will not be used in future (only dummies).
            return true;
        }

        private static bool CompoundDummiesIntersect(ref Matrix thisRotation, ref Matrix otherRotation, List<MyModelDummy> thisDummies, List<MyModelDummy> otherDummies)
        {
            foreach (var dummy in thisDummies)
            {
                //TODO: thisBoxHalfExtends should be dummy.Matrix.Scale * 0.5 but Scale in Matrix is bad.
                Vector3 thisBoxHalfExtends = new Vector3(dummy.Matrix.Right.Length(), dummy.Matrix.Up.Length(), dummy.Matrix.Forward.Length()) * 0.5f;
                BoundingBox thisAABB = new BoundingBox(-thisBoxHalfExtends, thisBoxHalfExtends);

                // Normalize this dummy, use inverse matrix as temporary.
                Matrix thisDummyMatrixInv = Matrix.Normalize(dummy.Matrix);

                // Rotate this dummy
                Matrix thisDummyMatrix;
                Matrix.Multiply(ref thisDummyMatrixInv, ref thisRotation, out thisDummyMatrix);
                // Create trasform to this dummy (inverse).
                Matrix.Invert(ref thisDummyMatrix, out thisDummyMatrixInv);

                //DebugDrawAABB(thisAABB, thisDummyMatrix);

                foreach (var otherDummy in otherDummies)
                {
                    //TODO: otherBoxHalfExtends should be otherDummy.Matrix.Scale * 0.5 but Scale in Matrix is bad.
                    Vector3 otherBoxHalfExtends = new Vector3(otherDummy.Matrix.Right.Length(), otherDummy.Matrix.Up.Length(), otherDummy.Matrix.Forward.Length()) * 0.5f;
                    BoundingBox otherAABB = new BoundingBox(-otherBoxHalfExtends, otherBoxHalfExtends);

                    // Store normalized dummy matrix as temporary
                    Matrix otherDummyMatrixInThis = Matrix.Normalize(otherDummy.Matrix);

                    // Rotate other dummy
                    Matrix otherDummyMatrix;
                    Matrix.Multiply(ref otherDummyMatrixInThis, ref otherRotation, out otherDummyMatrix);
                    // Transform other dummy to this dummy
                    Matrix.Multiply(ref otherDummyMatrix, ref thisDummyMatrixInv, out otherDummyMatrixInThis);

                    MyOrientedBoundingBox obb = MyOrientedBoundingBox.Create(otherAABB, otherDummyMatrixInThis);
                    //DebugDrawOBB(obb, thisDummyMatrix);
                    if (obb.Intersects(ref thisAABB))
                        return true;
                }
            }

            return false;
        }

        private void DebugDrawAABB(BoundingBox aabb, Matrix localMatrix) 
        {
            Matrix obbTransform = Matrix.CreateScale(2 * aabb.HalfExtents);

            MatrixD worldMatrix = obbTransform * (MatrixD)localMatrix;
            worldMatrix *= PositionComp.WorldMatrix;

            VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.Normalize(worldMatrix), 0.1f, false);
            VRageRender.MyRenderProxy.DebugDrawOBB(worldMatrix, Color.Green, 0.1f, false, false);
        }

        private void DebugDrawOBB(MyOrientedBoundingBox obb, Matrix localMatrix)
        {
            Matrix obbTransform = Matrix.CreateFromTransformScale(obb.Orientation, obb.Center, 2 * obb.HalfExtent);

            MatrixD worldMatrix = obbTransform * (MatrixD)localMatrix;
            worldMatrix *= PositionComp.WorldMatrix;

            VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.Normalize(worldMatrix), 0.1f, false);
            VRageRender.MyRenderProxy.DebugDrawOBB(worldMatrix, Vector3.One, 0.1f, false, false);
        }

        private bool IsRotationValid(MyBlockOrientation refOrientation, MyBlockOrientation orientation, MyBlockOrientation[] validRotations)
        {
            Debug.Assert(validRotations != null);

            // Ref matrix
            MatrixI localMatrix = new MatrixI(Vector3I.Zero, refOrientation.Forward, refOrientation.Up);
            MatrixI inverseMatrix;
            MatrixI.Invert(ref localMatrix, out inverseMatrix);
            Matrix inverseMatrixF = inverseMatrix.GetFloatMatrix();

            // Transform orientation to ref
            Base6Directions.Direction forward = Base6Directions.GetClosestDirection(Vector3.TransformNormal((Vector3)Base6Directions.GetIntVector(orientation.Forward), inverseMatrixF));
            Base6Directions.Direction up = Base6Directions.GetClosestDirection(Vector3.TransformNormal((Vector3)Base6Directions.GetIntVector(orientation.Up), inverseMatrixF));

            foreach (var validRotation in validRotations)
            {
                if (validRotation.Forward == forward && validRotation.Up == up)
                    return true;
            }

            return false;
        }

        public MySlimBlock GetBlock(ushort id)
        {
            MySlimBlock block;
            if (m_mapIdToBlock.TryGetValue(id, out block))
                return block;
            return null;
        }

        public ushort? GetBlockId(MySlimBlock block)
        {
            var pair = m_mapIdToBlock.FirstOrDefault(p => p.Value == block);
            if (pair.Value == block)
                return pair.Key;
            return null;
        }

        public ListReader<MySlimBlock> GetBlocks()
        {
			Debug.Assert(m_mapIdToBlock.Count == m_blocks.Count);
            return m_blocks;
        }

        public int GetBlocksCount()
        {
            Debug.Assert(m_mapIdToBlock.Count == m_blocks.Count);
            return m_blocks.Count;
        }

        /// <summary>
        /// Returns compound cube block builder which includes the given block.
        /// </summary>
        public static MyObjectBuilder_CompoundCubeBlock CreateBuilder(MyObjectBuilder_CubeBlock cubeBlockBuilder)
        {
            MyObjectBuilder_CompoundCubeBlock compoundCBBuilder
                = (MyObjectBuilder_CompoundCubeBlock)MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CompoundCubeBlock>(COMPOUND_BLOCK_SUBTYPE_NAME);
            compoundCBBuilder.EntityId = MyEntityIdentifier.AllocateId();
            compoundCBBuilder.Min = cubeBlockBuilder.Min;
            compoundCBBuilder.BlockOrientation = new MyBlockOrientation(ref Quaternion.Identity);
            compoundCBBuilder.ColorMaskHSV = cubeBlockBuilder.ColorMaskHSV;
            // Add block builder to compound
            compoundCBBuilder.Blocks = new MyObjectBuilder_CubeBlock[1];
            compoundCBBuilder.Blocks[0] = cubeBlockBuilder;
            return compoundCBBuilder;
        }

        /// <summary>
        /// Returns compound cube block builder which includes given blocks.
        /// </summary>
        public static MyObjectBuilder_CompoundCubeBlock CreateBuilder(List<MyObjectBuilder_CubeBlock> cubeBlockBuilders)
        {
            MyObjectBuilder_CompoundCubeBlock compoundCBBuilder
                = (MyObjectBuilder_CompoundCubeBlock)MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CompoundCubeBlock>(COMPOUND_BLOCK_SUBTYPE_NAME);
            compoundCBBuilder.EntityId = MyEntityIdentifier.AllocateId();
            compoundCBBuilder.Min = cubeBlockBuilders[0].Min;
            compoundCBBuilder.BlockOrientation = new MyBlockOrientation(ref Quaternion.Identity);
            compoundCBBuilder.ColorMaskHSV = cubeBlockBuilders[0].ColorMaskHSV;
            compoundCBBuilder.Blocks = cubeBlockBuilders.ToArray();
            return compoundCBBuilder;
        }

        public static MyCubeBlockDefinition GetCompoundCubeBlockDefinition()
        {
            MyCubeBlockDefinition blockDefinition = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_CompoundCubeBlock), COMPOUND_BLOCK_SUBTYPE_NAME));
            Debug.Assert(blockDefinition != null);
            return blockDefinition;
        }

        private static MyCompoundBlockTemplateDefinition GetTemplateDefinition(string template)
        {
            return MyDefinitionManager.Static.GetCompoundBlockTemplateDefinition(new MyDefinitionId(typeof(MyObjectBuilder_CompoundBlockTemplateDefinition), template));
        }

        private static MyCompoundBlockTemplateDefinition.MyCompoundBlockBinding GetTemplateDefinitionBinding(MyCompoundBlockTemplateDefinition templateDefinition, MyCubeBlockDefinition blockDefinition)
        {
            // first try any build type (allows more freedom)
            foreach (var binding in templateDefinition.Bindings)
            {
                if (binding.BuildType == BUILD_TYPE_ANY)
                    return binding;
            }

            foreach (var binding in templateDefinition.Bindings)
            {
                if (binding.BuildType == blockDefinition.BuildType && blockDefinition.BuildType != MyStringId.NullOrEmpty)
                    return binding;
            }

            return null;
        }

        private static MyCompoundBlockTemplateDefinition.MyCompoundBlockRotationBinding GetRotationBinding(MyCompoundBlockTemplateDefinition templateDefinition, MyCubeBlockDefinition blockDefinition1, MyCubeBlockDefinition blockDefinition2)
        {
            MyCompoundBlockTemplateDefinition.MyCompoundBlockBinding binding = GetTemplateDefinitionBinding(templateDefinition, blockDefinition1);
            if (binding == null)
                return null;

            MyCompoundBlockTemplateDefinition.MyCompoundBlockRotationBinding rotBind = GetRotationBinding(binding, blockDefinition2);
            if (rotBind != null)
                return rotBind;

            binding = GetTemplateDefinitionBinding(templateDefinition, blockDefinition2);
            if (binding == null)
                return null;

            rotBind = GetRotationBinding(binding, blockDefinition1);

            return rotBind;
        }

        private static MyCompoundBlockTemplateDefinition.MyCompoundBlockRotationBinding GetRotationBinding(MyCompoundBlockTemplateDefinition.MyCompoundBlockBinding binding, MyCubeBlockDefinition blockDefinition)
        {
            if (binding.RotationBinds != null)
            {
                foreach (var rotBind in binding.RotationBinds)
                {
                    if (rotBind.BuildTypeReference == blockDefinition.BuildType)
                        return rotBind;
                }
            }

            return null;
        }

        private void RefreshTemplates()
        {
            m_templates.Clear();

            if (MyFakes.ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES)
                return;

            foreach (var pair in m_mapIdToBlock) {
                Debug.Assert(pair.Value.BlockDefinition.CompoundTemplates != null);
                if (pair.Value.BlockDefinition.CompoundTemplates == null)
                    continue;

                if (m_templates.Count == 0)
                {
                    foreach (var template in pair.Value.BlockDefinition.CompoundTemplates)
                        m_templates.Add(template);
                }
                else
                {
                    m_tmpTemplates.Clear();
                    foreach (var template in pair.Value.BlockDefinition.CompoundTemplates)
                        m_tmpTemplates.Add(template);

                    m_templates.IntersectWith(m_tmpTemplates);
                }
            }
        }

        public override BoundingBox GetGeometryLocalBox()
        {
            BoundingBox b = BoundingBox.CreateInvalid();

            foreach (var child in GetBlocks())
            {
                if (child.FatBlock != null)
                {
                    Matrix childMatrix;
                    child.Orientation.GetMatrix(out childMatrix);
                    b.Include(child.FatBlock.Model.BoundingBox.Transform(childMatrix));
                }
            }

            return b;
        }

        private void RefreshNextId()
        {
            foreach (var pair in m_mapIdToBlock)
            {
                bool isLocal = (pair.Key & BLOCK_IN_COMPOUND_LOCAL_ID) == BLOCK_IN_COMPOUND_LOCAL_ID;
                if (isLocal)
                {
                    ushort id = (ushort)(pair.Key & ~BLOCK_IN_COMPOUND_LOCAL_ID);
                    m_localNextId = Math.Max(m_localNextId, id);
                }
                else
                {
                    ushort id = pair.Key;
                    m_nextId = Math.Max(m_nextId, id);
                }
            }

            if (m_nextId == BLOCK_IN_COMPOUND_LOCAL_MAX_VALUE)
                m_nextId = 0;
            else
                ++m_nextId;

            if (m_localNextId == BLOCK_IN_COMPOUND_LOCAL_MAX_VALUE)
                m_localNextId = 0;
            else
                ++m_localNextId;
        }

        private ushort CreateId(MySlimBlock block)
        {
            bool isLocal = block.BlockDefinition.IsGeneratedBlock;

            ushort id = 0;

            if (isLocal)
            {
                id = (ushort)(m_localNextId | BLOCK_IN_COMPOUND_LOCAL_ID);

                while (m_mapIdToBlock.ContainsKey(id))
                {
                    if (m_localNextId == BLOCK_IN_COMPOUND_LOCAL_MAX_VALUE)
                        m_localNextId = 0;
                    else
                        ++m_localNextId;

                    id = (ushort)(m_localNextId | BLOCK_IN_COMPOUND_LOCAL_ID);
                }
                ++m_localNextId;
            }
            else
            {
                id = m_nextId;

                while (m_mapIdToBlock.ContainsKey(id))
                {
                    if (m_nextId == BLOCK_IN_COMPOUND_LOCAL_MAX_VALUE)
                        m_nextId = 0;
                    else
                        ++m_nextId;

                    id = m_nextId;
                }
                ++m_nextId;
            }

            return id;
        }

        internal void DoDamage(float damage, MyStringHash damageType, MyHitInfo? hitInfo, long attackerId)
        {
            float integrity = 0;
            foreach(var block in m_mapIdToBlock)
            {
                integrity += block.Value.MaxIntegrity;
            }

            for (int i = m_blocks.Count - 1; i >= 0; --i)
            {
                var block = m_blocks[i];
                block.DoDamage(damage * (block.MaxIntegrity / integrity), damageType, hitInfo, true, attackerId);
            }
        }

        void IMyDecalProxy.AddDecals(MyHitInfo hitInfo, MyStringHash source, object customdata, IMyDecalHandler decalHandler, MyStringHash material)
        {
            Debug.Assert(m_mapIdToBlock.Count > 0);
            MyCubeGridHitInfo gridHitInfo = customdata as MyCubeGridHitInfo;
            if (gridHitInfo == null)
            {
                Debug.Fail("MyCubeGridHitInfo must not be null");
                return;
            }

            MySlimBlock block = m_mapIdToBlock.First().Value;
            MyPhysicalMaterialDefinition physicalMaterial = block.BlockDefinition.PhysicalMaterial;
            MyDecalRenderInfo renderable = new MyDecalRenderInfo();
            renderable.Position = Vector3D.Transform(hitInfo.Position, CubeGrid.PositionComp.WorldMatrixInvScaled);
            renderable.Normal = Vector3D.TransformNormal(hitInfo.Normal, CubeGrid.PositionComp.WorldMatrixInvScaled);
            renderable.RenderObjectId = CubeGrid.Render.GetRenderObjectID();

            VertexBoneIndicesWeights? boneIndicesWeights = gridHitInfo.Triangle.GetAffectingBoneIndicesWeights(ref m_boneIndexWeightTmp);
            if (boneIndicesWeights.HasValue)
            {
                renderable.BoneIndices = boneIndicesWeights.Value.Indices;
                renderable.BoneWeights = boneIndicesWeights.Value.Weights;
            }

            if (material.GetHashCode() == 0)
                renderable.Material = MyStringHash.GetOrCompute(physicalMaterial.Id.SubtypeName);
            else
                renderable.Material = material;


            var decalId = decalHandler.AddDecal(ref renderable);
            if (decalId != null)
                CubeGrid.RenderData.AddDecal(Position, gridHitInfo, decalId.Value);
        }

        public bool GetIntersectionWithLine(ref LineD line, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? t, out ushort blockId, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES, bool checkZFight = false, bool ignoreGenerated = false)
        {
            t = null;
            blockId = 0;

            double distanceSquaredInCompound = double.MaxValue;

            bool foundIntersection = false;

            foreach (var blockPair in m_mapIdToBlock)
            {
                MySlimBlock cmpSlimBlock = blockPair.Value;

				if (ignoreGenerated && cmpSlimBlock.BlockDefinition.IsGeneratedBlock)
					continue;

                VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersectionTriResult;
                if (cmpSlimBlock.FatBlock.GetIntersectionWithLine(ref line, out intersectionTriResult) && intersectionTriResult != null)
                {
                    Vector3D startToIntersection = intersectionTriResult.Value.IntersectionPointInWorldSpace - line.From;
                    double instrDistanceSq = startToIntersection.LengthSquared();
                    if (instrDistanceSq < distanceSquaredInCompound)
                    {
						if (checkZFight && distanceSquaredInCompound < instrDistanceSq + 0.001f)
							continue;

                        distanceSquaredInCompound = instrDistanceSq;
                        t = intersectionTriResult;
                        blockId = blockPair.Key;
                        foundIntersection = true;
                    }
                }
            }

            return foundIntersection;
        }

        /// <summary>
        /// Calculates intersected block with all models replaced by final models. Useful for construction/deconstruction when models are made from wooden construction.
        /// </summary>
        public bool GetIntersectionWithLine_FullyBuiltProgressModels(ref LineD line, out VRage.Game.Models.MyIntersectionResultLineTriangleEx? t, out ushort blockId, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES, bool checkZFight = false, bool ignoreGenerated = false)
        {
            t = null;
            blockId = 0;

            double distanceSquaredInCompound = double.MaxValue;

            bool foundIntersection = false;

            foreach (var blockPair in m_mapIdToBlock)
            {
                MySlimBlock cmpSlimBlock = blockPair.Value;

                if (ignoreGenerated && cmpSlimBlock.BlockDefinition.IsGeneratedBlock)
                    continue;

                MyModel collisionModel = MyModels.GetModelOnlyData(cmpSlimBlock.BlockDefinition.Model);
                if (collisionModel != null)
                {
                    VRage.Game.Models.MyIntersectionResultLineTriangleEx? intersectionTriResult = collisionModel.GetTrianglePruningStructure().GetIntersectionWithLine(
                        cmpSlimBlock.FatBlock, ref line, flags);

                    if (intersectionTriResult != null)
                    {
                        Vector3D startToIntersection = intersectionTriResult.Value.IntersectionPointInWorldSpace - line.From;
                        double instrDistanceSq = startToIntersection.LengthSquared();
                        if (instrDistanceSq < distanceSquaredInCompound)
                        {
                            if (checkZFight && distanceSquaredInCompound < instrDistanceSq + 0.001f)
                                continue;

                            distanceSquaredInCompound = instrDistanceSq;
                            t = intersectionTriResult;
                            blockId = blockPair.Key;
                            foundIntersection = true;
                        }
                    }
                }
            }

            return foundIntersection;
        }

        private static void GetBlockLocalMatrixFromGridPositionAndOrientation(MySlimBlock block, ref MatrixD localMatrix)
        {
            Matrix orientation;
            block.Orientation.GetMatrix(out orientation);

            localMatrix = orientation;
            localMatrix.Translation = block.CubeGrid.GridSize * block.Position;
        }

        private static void GetCompoundCollisionDummies(MyCubeBlockDefinition definition, List<MyModelDummy> outDummies)
        {
            var model = VRage.Game.Models.MyModels.GetModelOnlyDummies(definition.Model);
            if (model != null)
            {
                foreach (var pair in model.Dummies)
                {
                    if (pair.Key.ToLower().StartsWith(COMPOUND_DUMMY))
                        outDummies.Add(pair.Value);
                }
            }
        }

        public static bool IsCompoundEnabled(MyCubeBlockDefinition blockDefinition)
        {
            if (!MyFakes.ENABLE_COMPOUND_BLOCKS)
                return false;

            if (blockDefinition == null)
                return false;

            if (blockDefinition.CubeSize != MyCubeSize.Large)
                return false;

            if (blockDefinition.Size != Vector3I.One)
                return false;

            if (MyFakes.ENABLE_COMPOUND_BLOCK_COLLISION_DUMMIES)
                return blockDefinition.CompoundEnabled;

            return blockDefinition.CompoundTemplates != null && blockDefinition.CompoundTemplates.Length != 0;
        }
    }
}
