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
using Sandbox.Common.ModAPI;
using VRage.Components;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.Common;
using VRage.Utils;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Cube block which is composed of several cube blocks together with shared compound template name.
    /// </summary>
    [MyCubeBlockType(typeof(MyObjectBuilder_CompoundCubeBlock))]
    public class MyCompoundCubeBlock : MyCubeBlock
    {
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

        // IDentifier of local block ids.
        private static ushort BLOCK_IN_COMPOUND_LOCAL_ID = 0xF000;

        private static readonly string BUILD_TYPE_ANY = "any";

        // Common compound block subtype name
        private static readonly string COMPOUND_BLOCK_SUBTYPE_NAME = "CompoundBlock";

        private static HashSet<string> m_tmpTemplates = new HashSet<string>();

        private readonly Dictionary<ushort, MySlimBlock> m_blocks = new Dictionary<ushort, MySlimBlock>();
        // Server next id;
        private ushort m_nextId;
        // Local next id identifier of blocks which are set directly (not through server - i.e. generated blocks).
        private ushort m_localNextId;

        // Refreshed set of shared templates between all blocks.
        private HashSet<string> m_templates = new HashSet<string>();


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
                        if (m_blocks.ContainsKey(id))
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

                        cubeBlock.FatBlock.Hierarchy.Parent = Hierarchy;
                        m_blocks.Add(id, cubeBlock);
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

                        cubeBlock.FatBlock.Hierarchy.Parent = Hierarchy;
                        ushort id = CreateId(cubeBlock);
                        m_blocks.Add(id, cubeBlock);
                    }
                }
             }

            RefreshTemplates();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_CompoundCubeBlock objectBuilder = (MyObjectBuilder_CompoundCubeBlock)base.GetObjectBuilderCubeBlock(copy);
            if (m_blocks.Count > 0) 
            {
                objectBuilder.Blocks = new MyObjectBuilder_CubeBlock[m_blocks.Count];
                objectBuilder.BlockIds = new ushort[m_blocks.Count];

                int counter = 0;
                foreach (var pair in m_blocks)
                {
                    objectBuilder.BlockIds[counter] = pair.Key;
                    objectBuilder.Blocks[counter] = pair.Value.GetObjectBuilder();
                    ++counter;
                }
            }

            Debug.Assert(objectBuilder.Blocks == null && objectBuilder.BlockIds == null || objectBuilder.Blocks.Length == objectBuilder.BlockIds.Length);

            return objectBuilder;
        }

        public override void OnAddedToScene(object source)
        {
            foreach (var pair in m_blocks)
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
            foreach (var pair in m_blocks)
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
            foreach (var pair in m_blocks)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.UpdateVisual();
                else
                    Debug.Assert(false);
            }

            base.UpdateVisual();
        }

        internal override float GetMass()
        {
            float mass = 0f;
            foreach (var pair in m_blocks)
                mass += pair.Value.GetMass();
            return mass;
        }

        private void UpdateBlocksWorldMatrix(ref MatrixD parentWorldMatrix, object source = null)
        {
            MatrixD localMatrix = MatrixD.Identity;
            foreach (var pair in m_blocks)
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
            foreach (var pair in m_blocks)
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

            foreach (var pair in m_blocks)
            {
                pair.Value.CubeGrid = CubeGrid;
            }
        }

        internal override void OnAddedNeighbours()
        {
            //foreach (var block in m_blocks)
            //{
            //    block.RemoveNeighbours();
            //    block.AddNeighbours();
            //}
        }

        internal override void OnRemovedNeighbours()
        {
            //foreach (var block in m_blocks)
            //{
            //    block.RemoveNeighbours();
            //}
        }

        internal override void OnTransformed(ref MatrixI transform)
        {
            foreach (var pair in m_blocks)
            {
                pair.Value.Transform(ref transform);
            }
        }

        internal override void UpdateWorldMatrix()
        {
            base.UpdateWorldMatrix();

            foreach (var pair in m_blocks)
            {
                if (pair.Value.FatBlock != null)
                    pair.Value.FatBlock.UpdateWorldMatrix();
            }
        }

        public override bool ConnectionAllowed(ref Vector3I otherBlockPos, ref Vector3I faceNormal, MyCubeBlockDefinition def)
        {
            foreach (var pair in m_blocks)
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
            foreach (var pair in m_blocks)
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

            // block.AddNeighbours();

            Debug.Assert(block.BlockDefinition.Size == Vector3I.One, "Only blocks with size=1 are supported!");
            if (m_blocks.ContainsKey(id))
            {
                Debug.Fail("Cannot add block with existing id to compound block!");
                return false;
            }

            m_blocks.Add(id, block);

            MatrixD parentWorldMatrix = this.Parent.WorldMatrix;
            MatrixD blockLocalMatrix = MatrixD.Identity;
            GetBlockLocalMatrixFromGridPositionAndOrientation(block, ref blockLocalMatrix);
            MatrixD worldMatrix = blockLocalMatrix * parentWorldMatrix;
            block.FatBlock.PositionComp.SetWorldMatrix(worldMatrix, this, true);

            block.FatBlock.OnAddedToScene(this);

            block.FatBlock.Hierarchy.Parent = Hierarchy;

            CubeGrid.UpdateBlockNeighbours(SlimBlock);

            RefreshTemplates();

            return true;
        }

        public bool Remove(MySlimBlock block, bool removeFromScene = true)
        {
            var pair = m_blocks.FirstOrDefault(p => p.Value == block);
            if (pair.Value == block)
                return Remove(pair.Key, removeFromScene);

            Debug.Fail("Cannot remove block from compound");
            return false;
        }

        public bool Remove(ushort blockId, bool removeFromScene = true)
        {
            MySlimBlock block;
            if (m_blocks.TryGetValue(blockId, out block))
            {
                m_blocks.Remove(blockId);
                if (removeFromScene)
                {
                    block.FatBlock.OnRemovedFromScene(this);
                    block.FatBlock.Close();
                }

                if (block.FatBlock.Hierarchy.Parent == Hierarchy)
                    block.FatBlock.Hierarchy.Parent = null;

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

            if (m_blocks.ContainsValue(block))
                return false;

            if (block.FatBlock is MyCompoundCubeBlock)
            {
                MyCompoundCubeBlock otherCompound = block.FatBlock as MyCompoundCubeBlock;
                foreach (var otherBlockInCompound in otherCompound.GetBlocks())
                {
                    if (!CanAddBlock(otherBlockInCompound.BlockDefinition, otherBlockInCompound.Orientation))
                        return false;
                }
                return true;
            }
            else
            {
                return CanAddBlock(block.BlockDefinition, block.Orientation);
            }
        }

        public bool CanAddBlock(MyCubeBlockDefinition definition, MyBlockOrientation? orientation)
        {
            Debug.Assert(definition != GetCompoundCubeBlockDefinition());

            if (definition == null || definition.CompoundTemplates == null || definition.CompoundTemplates.Length == 0)
                return false;

            // Only blocks with size 1 are supported now
            if (definition.Size != Vector3I.One)
            {
                Debug.Assert(definition.Size == Vector3I.One, "Only blocks with size=1 are supported!");
                return false;
            }

            // Check the same block with the same orientation or same build type.
            if (orientation != null)
            {
                foreach (var pair in m_blocks)
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

                        foreach (var pair in m_blocks)
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

                        foreach (var pair in m_blocks)
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
            if (m_blocks.TryGetValue(id, out block))
                return block;
            return null;
        }

        public ushort? GetBlockId(MySlimBlock block)
        {
            var pair = m_blocks.FirstOrDefault(p => p.Value == block);
            if (pair.Value == block)
                return pair.Key;
            return null;
        }

        public List<MySlimBlock> GetBlocks()
        {
            return m_blocks.Values.ToList();
        }

        public List<MySlimBlock> GetBlocks(List<MySlimBlock> blocks)
        {
            foreach (var pair in m_blocks)
                blocks.Add(pair.Value);

            return blocks;
        }

        public int GetBlocksCount()
        {
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
                if (binding.BuildType == blockDefinition.BuildType && blockDefinition.BuildType != null)
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
            foreach (var pair in m_blocks) {
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
            foreach (var pair in m_blocks)
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

            if (m_nextId == 0xEFFF)
                m_nextId = 0;
            else
                ++m_nextId;

            if (m_localNextId == 0x0FFF)
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

                while (m_blocks.ContainsKey(id))
                {
                    if (m_localNextId == 0x0FFF)
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

                while (m_blocks.ContainsKey(id))
                {
                    if (m_nextId == 0xEFFF)
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
            foreach(var block in m_blocks)
            {
                integrity += block.Value.MaxIntegrity;
            }

            if (hitInfo.HasValue)
            {
                Debug.Assert(m_blocks.Count > 0);
                CubeGrid.RenderData.AddDecal(Position, Vector3D.Transform(hitInfo.Value.Position, CubeGrid.PositionComp.WorldMatrixInvScaled),
                    Vector3D.TransformNormal(hitInfo.Value.Normal, CubeGrid.PositionComp.WorldMatrixInvScaled), m_blocks.First().Value.BlockDefinition.PhysicalMaterial.DamageDecal);
            }

            foreach (var block in m_blocks)
            {
                block.Value.DoDamage(damage * (block.Value.MaxIntegrity / integrity), damageType, true, hitInfo, false, attackerId);
            }
        }

        public bool GetIntersectionWithLine(ref LineD line, out MyIntersectionResultLineTriangleEx? t, out ushort blockId, IntersectionFlags flags = IntersectionFlags.ALL_TRIANGLES, bool checkZFight = false, bool ignoreGenerated = false)
        {
            t = null;
            blockId = 0;

            double distanceSquaredInCompound = double.MaxValue;

            bool foundIntersection = false;

            foreach (var blockPair in m_blocks)
            {
                MySlimBlock cmpSlimBlock = blockPair.Value;

				if (ignoreGenerated && cmpSlimBlock.BlockDefinition.IsGeneratedBlock)
					continue;

                MyIntersectionResultLineTriangleEx? intersectionTriResult;
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

        private static void GetBlockLocalMatrixFromGridPositionAndOrientation(MySlimBlock block, ref MatrixD localMatrix)
        {
            Matrix orientation;
            block.Orientation.GetMatrix(out orientation);

            localMatrix = orientation;
            localMatrix.Translation = block.CubeGrid.GridSize * block.Position;
        }
    }
}
