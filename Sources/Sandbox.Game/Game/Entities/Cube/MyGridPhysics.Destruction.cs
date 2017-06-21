#region Usings
using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Debris;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.Game.Components;
using VRage.ModAPI;
using Sandbox.Game.Entities.EnvironmentItems;
using Sandbox.Game.Components;
using Sandbox.Engine.Voxels;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.Entity;
using VRage.Profiler;

#endregion

namespace Sandbox.Game.Entities.Cube
{
    public partial class MyGridPhysics : MyPhysicsBody
    {
        private List<HkdBreakableBodyInfo> m_newBodies = new List<HkdBreakableBodyInfo>();
        private List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_tmpChildren_RemoveShapes = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_tmpChildren_CompoundIds = new List<HkdShapeInstanceInfo>();
        private static List<string> m_tmpShapeNames = new List<string>();
        private static HashSet<MySlimBlock> m_tmpBlocksToDelete = new HashSet<MySlimBlock>();
        private static HashSet<MySlimBlock> m_tmpBlocksUpdateDamage = new HashSet<MySlimBlock>();
        private static HashSet<ushort> m_tmpCompoundIds = new HashSet<ushort>();
        private static List<MyDefinitionId> m_tmpDefinitions = new List<MyDefinitionId>();

        private List<Vector3D> m_splitPosition = new List<Vector3D>();
        private List<Sandbox.Game.Entities.Cube.MySlimBlock> m_blocksToDisconnect = new List<Sandbox.Game.Entities.Cube.MySlimBlock>();
        private bool m_recreateBody;
        private Vector3 m_oldLinVel;
        private Vector3 m_oldAngVel;

        List<HkdBreakableBody> m_newBreakableBodies = new List<HkdBreakableBody>();
        public override void FracturedBody_AfterReplaceBody(ref HkdReplaceBodyEvent e)
        {
            if (!MyFakes.ENABLE_AFTER_REPLACE_BODY)
                return;

            System.Diagnostics.Debug.Assert(Sync.IsServer, "Client must not simulate destructions");

            if (!Sync.IsServer)
                return;
            Debug.Assert(!m_recreateBody, "Only one destruction per entity per frame");
            if (m_recreateBody)
                return;
            ProfilerShort.Begin("DestructionFracture.AfterReplaceBody");
            HavokWorld.DestructionWorld.RemoveBreakableBody(e.OldBody); // To remove from HkpWorld rigidBodies list
            m_oldLinVel = RigidBody.LinearVelocity;
            m_oldAngVel = RigidBody.AngularVelocity;
            MyPhysics.RemoveDestructions(RigidBody);
            e.GetNewBodies(m_newBodies);
            if (m_newBodies.Count == 0)// || e.OldBody != DestructionBody) 
            {
                ProfilerShort.End();
                return;
            }
            bool createdEffect = false;
            m_tmpBlocksToDelete.Clear();
            m_tmpBlocksUpdateDamage.Clear();
            MySlimBlock destructionSoundBlock = null;
            ProfilerShort.Begin("ProcessBodies");
            foreach (var b in m_newBodies)
            {
                if (!b.IsFracture() || (MyFakes.ENABLE_FRACTURE_COMPONENT && m_grid.BlocksCount == 1 && m_grid.IsStatic && MyDestructionHelper.IsFixed(b)))
                {
                    m_newBreakableBodies.Add(MyFracturedPiecesManager.Static.GetBreakableBody(b));
                    ProfilerShort.Begin("FindFracturedBlocks");
                    FindFracturedBlocks(b);
                    ProfilerShort.BeginNextBlock("RemoveBBfromWorld");
                    HavokWorld.DestructionWorld.RemoveBreakableBody(b);
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("CreateFracture");
                    var bBody = MyFracturedPiecesManager.Static.GetBreakableBody(b);
                    var bodyMatrix = bBody.GetRigidBody().GetRigidBodyMatrix();
                    var pos = ClusterToWorld(bodyMatrix.Translation);

                    MySlimBlock cb;
                    var shape = bBody.BreakableShape;
                    ProfilerShort.Begin("Properties");
                    HkVec3IProperty prop = shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                    if (!prop.IsValid() && shape.IsCompound())  //TODO: this is last block in grid and is fractured
                    {                                           //its compound of childs of our block shape that has position prop, 
                        prop = shape.GetChild(0).Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                    }

                    ProfilerShort.End();
                    Debug.Assert(prop.IsValid());
                    bool removePiece = false;
                    cb = m_grid.GetCubeBlock(prop.Value);
                    MyCompoundCubeBlock compoundBlock = cb != null ? cb.FatBlock as MyCompoundCubeBlock : null;

                    if (cb != null)
                    {
                        if (destructionSoundBlock == null)
                            destructionSoundBlock = cb;

                        if (!createdEffect)
                        {
                            ProfilerShort.Begin("CreateParticle");
                            AddDestructionEffect(m_grid.GridIntegerToWorld(cb.Position), Vector3.Down);
                            createdEffect = true;
                            ProfilerShort.End();
                        }

                        MatrixD m = bodyMatrix;
                        m.Translation = pos;
                        MyFracturedPiece fp = null;

                        if (MyFakes.ENABLE_FRACTURE_COMPONENT) 
                        {
                            // Get compound ids
                            Debug.Assert(m_tmpCompoundIds.Count == 0);
                            HkSimpleValueProperty compoundIdProperty = shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                            if (compoundIdProperty.IsValid())
                            {
                                m_tmpCompoundIds.Add((ushort)compoundIdProperty.ValueUInt);
                            }
                            else if (!compoundIdProperty.IsValid() && shape.IsCompound())
                            {
                                m_tmpChildren_CompoundIds.Clear();
                                shape.GetChildren(m_tmpChildren_CompoundIds);
                                foreach (var child in m_tmpChildren_CompoundIds)
                                {
                                    HkSimpleValueProperty compoundIdChildProperty = child.Shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                                    if (compoundIdChildProperty.IsValid())
                                        m_tmpCompoundIds.Add((ushort)compoundIdChildProperty.ValueUInt);
                                }
                            }

                            // Remove shapes
                            bool fracturePieceValid = true;
                            if (m_tmpCompoundIds.Count > 0) 
                            {
                                Debug.Assert(m_tmpDefinitions.Count == 0);
                                Debug.Assert(compoundBlock != null);
                                foreach (var compoundId in m_tmpCompoundIds)
                                {
                                    var blockInCompound = compoundBlock.GetBlock(compoundId);
                                    if (blockInCompound == null) 
                                    {
                                        fracturePieceValid = false;
                                        continue;
                                    }

                                    m_tmpDefinitions.Add(blockInCompound.BlockDefinition.Id);
                                    fracturePieceValid &= RemoveShapesFromFracturedBlocks(bBody, blockInCompound, compoundId, m_tmpBlocksToDelete, m_tmpBlocksUpdateDamage);
                                }
                            }
                            else 
                            {
                                Debug.Assert(compoundBlock == null);
                                m_tmpDefinitions.Add(cb.BlockDefinition.Id);
                                fracturePieceValid = RemoveShapesFromFracturedBlocks(bBody, cb, null, m_tmpBlocksToDelete, m_tmpBlocksUpdateDamage);
                            }

                            // Test invalid shapes (from block which has been removed from grid).
                            Debug.Assert(fracturePieceValid);
                            if (fracturePieceValid)
                            {
                                fp = MyDestructionHelper.CreateFracturePiece(bBody, ref m, m_tmpDefinitions, compoundBlock != null ? compoundBlock : cb.FatBlock);
                                if (fp == null)
                                    removePiece = true;
                            }
                            else
                            {
                                removePiece = true;
                            }

                            m_tmpChildren_CompoundIds.Clear();
                            m_tmpCompoundIds.Clear();
                            m_tmpDefinitions.Clear();
                        }
                        else
                        {
                            fp = MyDestructionHelper.CreateFracturePiece(bBody, ref m, null, compoundBlock != null ? compoundBlock : cb.FatBlock);
                            if (fp == null)
                                removePiece = true;
                        }
                    }
                    else
                    {
                        //Debug.Fail("Fracture piece missing block!");//safe to ignore
                        removePiece = true;
                    }

                    if (removePiece)
                    {
                        HavokWorld.DestructionWorld.RemoveBreakableBody(b);
                        MyFracturedPiecesManager.Static.ReturnToPool(bBody);
                    }
                    ProfilerShort.End();
                }
            }
            m_newBodies.Clear();
            bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

            if (destructionSoundBlock != null)
                MyAudioComponent.PlayDestructionSound(destructionSoundBlock);

            if (MyFakes.ENABLE_FRACTURE_COMPONENT)
            {
                // Cache blocks with fracture components
                FindFractureComponentBlocks();

                // Remove blocks from blocksToDelete which are marked for creating fracture component.
                foreach (var info in m_fractureBlockComponentsCache)
                    m_tmpBlocksToDelete.Remove(((MyCubeBlock)info.Entity).SlimBlock);

                // Remove blocks from update collection when the block is also in delete colection.
                foreach (var blockToDelete in m_tmpBlocksToDelete)
                    m_tmpBlocksUpdateDamage.Remove(blockToDelete);

                // Delete blocks
                foreach (var cb in m_tmpBlocksToDelete)
                {
                    // Update deleted blocks destruction damage for multiblocks
                    if (cb.IsMultiBlockPart)
                    {
                        var multiBlockInfo = cb.CubeGrid.GetMultiBlockInfo(cb.MultiBlockId);
                        // Only apply damage if there is some other block in multiblock.
                        if (multiBlockInfo != null && multiBlockInfo.Blocks.Count > 1)
                        {
                            var fractureComponent = cb.GetFractureComponent();
                            if (fractureComponent != null)
                                cb.ApplyDestructionDamage(0f);
                        }
                    }

                    if (cb.FatBlock != null)
                        cb.FatBlock.OnDestroy();

                    m_grid.RemoveBlockWithId(cb, true);
                }

                // Update blocks destruction damage
                foreach (var cb in m_tmpBlocksUpdateDamage)
                {
                    var fractureComponent = cb.GetFractureComponent();
                    if (fractureComponent != null)
                        cb.ApplyDestructionDamage(fractureComponent.GetIntegrityRatioFromFracturedPieceCounts());
                }
            }
            else
            {
                foreach (var cb in m_tmpBlocksToDelete)
                {
                    var b = m_grid.GetCubeBlock(cb.Position);
                    if (b != null)
                    {
                        if (b.FatBlock != null)
                            b.FatBlock.OnDestroy();
                        m_grid.RemoveBlock(b, true);
                    }
                }
            }

            m_grid.EnableGenerators(oldGeneratorsEnabled);

            m_tmpBlocksToDelete.Clear();
            m_tmpBlocksUpdateDamage.Clear();

            ProfilerShort.End();
            //SplitGrid(e);
            m_recreateBody = true;
            ProfilerShort.End();
        }

        /// <summary>
        /// Removes breakable shapes form fracture component.
        /// </summary>
        /// <param name="bBody">body with shapes</param>
        /// <param name="block">block</param>
        /// <param name="blocksToDelete">collection of blocks to remove from grid</param>
        /// <param name="blocksUpdateDamage">collection of blocks for updating their damage according to remaining shapes count</param>
        /// <returns>true if block was processes otherwise false (block in compound does not exist)</returns>
        private bool RemoveShapesFromFracturedBlocks(HkdBreakableBody bBody, MySlimBlock block, ushort? compoundId, HashSet<MySlimBlock> blocksToDelete, HashSet<MySlimBlock> blocksUpdateDamage)
        {
            Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT);

            // Block can be removed when the removed shape is the last one!
            MyFractureComponentCubeBlock fractureComponent = block.GetFractureComponent();
            if (fractureComponent != null)
            {
                bool removeBlock = false;
                var bShape = bBody.BreakableShape;
                if (IsBreakableShapeCompound(bShape))
                {
                    m_tmpShapeNames.Clear();
                    m_tmpChildren_RemoveShapes.Clear();

                    bShape.GetChildren(m_tmpChildren_RemoveShapes);
                    var shapesCount = m_tmpChildren_RemoveShapes.Count;
                    for (int i = 0; i < shapesCount; ++i)
                    {
                        var child = m_tmpChildren_RemoveShapes[i];
                        if (string.IsNullOrEmpty(child.ShapeName))
                            child.Shape.GetChildren(m_tmpChildren_RemoveShapes);
                    }

                    m_tmpChildren_RemoveShapes.ForEach(delegate(HkdShapeInstanceInfo c)
                    {
                        var shapeName = c.ShapeName;
                        if (!string.IsNullOrEmpty(shapeName))
                            m_tmpShapeNames.Add(shapeName);
                    });

                    if (m_tmpShapeNames.Count != 0)
                    {
                        removeBlock = fractureComponent.RemoveChildShapes(m_tmpShapeNames);
                        MySyncDestructions.RemoveShapesFromFractureComponent(block.CubeGrid.EntityId, block.Position, compoundId ?? 0xFFFF, m_tmpShapeNames);
                    }

                    m_tmpChildren_RemoveShapes.Clear();
                    m_tmpShapeNames.Clear();
                }
                else
                {
                    var name = bBody.BreakableShape.Name;
                    removeBlock = fractureComponent.RemoveChildShapes(new string[] { name });

                    MySyncDestructions.RemoveShapeFromFractureComponent(block.CubeGrid.EntityId, block.Position, compoundId ?? 0xFFFF, name);
                }

                if (removeBlock)
                    blocksToDelete.Add(block);
                else
                    blocksUpdateDamage.Add(block);
            }
            else
            {
                blocksToDelete.Add(block);
            }

            return true;
        }

        private static bool IsBreakableShapeCompound(HkdBreakableShape shape)
        {
            return (string.IsNullOrEmpty(shape.Name) || shape.IsCompound() || shape.GetChildrenCount() > 0);
        }

        public List<MyFracturedBlock.Info> GetFracturedBlocks() { Debug.Assert(!MyFakes.ENABLE_FRACTURE_COMPONENT); return m_fractureBlocksCache; }
        private List<MyFracturedBlock.Info> m_fractureBlocksCache = new List<MyFracturedBlock.Info>();
        Dictionary<Vector3I, List<HkdShapeInstanceInfo>> m_fracturedBlocksShapes = new Dictionary<Vector3I, List<HkdShapeInstanceInfo>>();

        public List<MyFractureComponentBase.Info> GetFractureBlockComponents() { Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT); return m_fractureBlockComponentsCache; }
        public void ClearFractureBlockComponents() { m_fractureBlockComponentsCache.Clear(); }
        private List<MyFractureComponentBase.Info> m_fractureBlockComponentsCache = new List<MyFractureComponentBase.Info>();
        Dictionary<MySlimBlock, List<HkdShapeInstanceInfo>> m_fracturedSlimBlocksShapes = new Dictionary<MySlimBlock, List<HkdShapeInstanceInfo>>();

        void BreakableBody_AfterControllerOperation(HkdBreakableBody b)
        {
            ProfilerShort.Begin("BreakableBody_AfterControllerOperation");
            if (m_recreateBody)
                b.BreakableShape.SetStrenghtRecursively(MyDestructionConstants.STRENGTH, 0.7f);
            ProfilerShort.End();
        }

        void BreakableBody_BeforeControllerOperation(HkdBreakableBody b)
        {
            ProfilerShort.Begin("BreakableBody_AfterControllerOperation");
            if (m_recreateBody)
                b.BreakableShape.SetStrenghtRecursively(float.MaxValue, 0.7f);
            ProfilerShort.End();
        }

        void RigidBody_ContactPointCallback_Destruction(ref HkContactPointEvent value)
        {
            ProfilerShort.Begin("Grid Contact counter");
            ProfilerShort.End();
            MyGridContactInfo info = new MyGridContactInfo(ref value, m_grid);

            if (info.IsKnown)
                return;

            var myEntity = info.CurrentEntity;//value.Base.BodyA.GetEntity() == m_grid.Components ? value.Base.BodyA.GetEntity() : value.Base.BodyB.GetEntity();
            if (myEntity == null || myEntity.Physics == null || myEntity.Physics.RigidBody == null)
            {
                return;
            }
            var myBody = myEntity.Physics.RigidBody;

            // CH: DEBUG
            var physicsBody1 = value.GetPhysicsBody(0);
            var physicsBody2 = value.GetPhysicsBody(1);
            if (physicsBody1 == null || physicsBody2 == null)
                return;

            var entity1 = physicsBody1.Entity;
            var entity2 = physicsBody2.Entity;
            if (entity1 == null || entity2 == null || entity1.Physics == null || entity2.Physics == null)
                return;

            if (entity1 is MyFracturedPiece && entity2 is MyFracturedPiece)
                return;

            var rigidBody1 = value.Base.BodyA;
            var rigidBody2 = value.Base.BodyB;

            info.HandleEvents();
            if (rigidBody1.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT) || rigidBody2.HasProperty(HkCharacterRigidBody.MANIPULATED_OBJECT))
                return;

            if (info.CollidingEntity is Sandbox.Game.Entities.Character.MyCharacter || info.CollidingEntity == null || info.CollidingEntity.MarkedForClose)
                return;

            var grid1 = entity1 as MyCubeGrid;
            var grid2 = entity2 as MyCubeGrid;

            // CH: TODO: This is a hack Instead, the IMyDestroyableObject should be used and the subpart DoDamage code could delegate it to the grid
            // The thing is, this approach would probably need a rewrite of this whole method...
            if (grid2 == null && entity2 is MyEntitySubpart)
            {
                while (entity2 != null && !(entity2 is MyCubeGrid))
                {
                    entity2 = entity2.Parent;
                }

                if (entity2 != null)
                {
                    physicsBody2 = entity2.Physics as MyPhysicsBody;
                    rigidBody2 = physicsBody2.RigidBody;
                    grid2 = entity2 as MyCubeGrid;
                }
            }

            if (grid1 != null && grid2 != null && (MyCubeGridGroups.Static.Physical.GetGroup(grid1) == MyCubeGridGroups.Static.Physical.GetGroup(grid2)))
                return;

            ProfilerShort.Begin("Grid contact point callback");

            {
                var vel = Math.Abs(value.SeparatingVelocity);
                bool enoughSpeed = vel > 3;
                //float dot = Vector3.Dot(Vector3.Normalize(LinearVelocity), Vector3.Normalize(info.CollidingEntity.Physics.LinearVelocity));


                Vector3 velocity1 = rigidBody1.GetVelocityAtPoint(info.Event.ContactPoint.Position);
                Vector3 velocity2 = rigidBody2.GetVelocityAtPoint(info.Event.ContactPoint.Position);

                float speed1 = velocity1.Length();
                float speed2 = velocity2.Length();

                Vector3 dir1 = speed1 > 0 ? Vector3.Normalize(velocity1) : Vector3.Zero;
                Vector3 dir2 = speed2 > 0 ? Vector3.Normalize(velocity2) : Vector3.Zero;

                float mass1 = MyDestructionHelper.MassFromHavok(rigidBody1.Mass);
                float mass2 = MyDestructionHelper.MassFromHavok(rigidBody2.Mass);

                float impact1 = speed1 * mass1;
                float impact2 = speed2 * mass2;

                float dot1withNormal = speed1 > 0 ? Vector3.Dot(dir1, value.ContactPoint.Normal) : 0;
                float dot2withNormal = speed2 > 0 ? Vector3.Dot(dir2, value.ContactPoint.Normal) : 0;

                speed1 *= Math.Abs(dot1withNormal);
                speed2 *= Math.Abs(dot2withNormal);

                bool is1Static = mass1 == 0;
                bool is2Static = mass2 == 0;

                bool is1Small = entity1 is MyFracturedPiece || (grid1 != null && grid1.GridSizeEnum == MyCubeSize.Small);
                bool is2Small = entity2 is MyFracturedPiece || (grid2 != null && grid2.GridSizeEnum == MyCubeSize.Small);


                float dot = Vector3.Dot(dir1, dir2);

                float maxDestructionRadius = 0.5f;

                impact1 *= info.ImpulseMultiplier;
                impact2 *= info.ImpulseMultiplier;

                MyHitInfo hitInfo = new MyHitInfo();
                var hitPos = info.ContactPosition;
                hitInfo.Normal = value.ContactPoint.Normal;

                //direct hit
                if (dot1withNormal < 0.0f)
                {
                    if (entity1 is MyFracturedPiece)
                        impact1 /= 10;

                    impact1 *= Math.Abs(dot1withNormal); //respect angle of hit

                    if ((impact1 > 2000 && speed1 > 2 && !is2Small) ||
                        (impact1 > 500 && speed1 > 10)) //must be fast enought to destroy fracture piece (projectile)
                    {  //1 is big hitting

                        if (is2Static || impact1 / impact2 > 10)
                        {
                            hitInfo.Position = hitPos + 0.1f * hitInfo.Normal;
                            impact1 -= mass1;

                            if (Sync.IsServer && impact1 > 0)
                            {
                                if (grid1 != null)
                                {
                                    var blockPos = GetGridPosition(value.ContactPoint, rigidBody1, grid1, 0);
                                    grid1.DoDamage(impact1, hitInfo, blockPos, grid2 != null ? grid2.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact1, (MyPhysicsBody)entity1.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);
                                hitInfo.Position = hitPos - 0.1f * hitInfo.Normal;
                                if (grid2 != null)
                                {
                                    var blockPos = GetGridPosition(value.ContactPoint, rigidBody2, grid2, 1);
                                    grid2.DoDamage(impact1, hitInfo, blockPos, grid1 != null ? grid1.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact1, (MyPhysicsBody)entity2.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);

                                ReduceVelocities(info);
                            }

                            MyDecals.HandleAddDecal(entity1, hitInfo);
                            MyDecals.HandleAddDecal(entity2, hitInfo);
                        }
                    }
                }

                if (dot2withNormal < 0.0f)
                {
                    if (entity2 is MyFracturedPiece)
                        impact2 /= 10;

                    impact2 *= Math.Abs(dot2withNormal); //respect angle of hit

                    if (impact2 > 2000 && speed2 > 2 && !is1Small ||
                        (impact2 > 500 && speed2 > 10)) //must be fast enought to destroy fracture piece (projectile)
                    {  //2 is big hitting

                        if (is1Static || impact2 / impact1 > 10)
                        {
                            hitInfo.Position = hitPos + 0.1f * hitInfo.Normal;
                            impact2 -= mass2;

                            if (Sync.IsServer && impact2 > 0)
                            {
                                if (grid1 != null)
                                {
                                    var blockPos = GetGridPosition(value.ContactPoint, rigidBody1, grid1, 0);
                                    grid1.DoDamage(impact2, hitInfo, blockPos, grid2 != null ? grid2.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact2, (MyPhysicsBody)entity1.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);
                                hitInfo.Position = hitPos - 0.1f * hitInfo.Normal;
                                if (grid2 != null)
                                {
                                    var blockPos = GetGridPosition(value.ContactPoint, rigidBody2, grid2, 1);
                                    grid2.DoDamage(impact2, hitInfo, blockPos, grid1 != null ? grid1.EntityId : 0);
                                }
                                else
                                    MyDestructionHelper.TriggerDestruction(impact2, (MyPhysicsBody)entity2.Physics, info.ContactPosition, value.ContactPoint.Normal, maxDestructionRadius);

                                ReduceVelocities(info);
                            }

                            MyDecals.HandleAddDecal(entity1, hitInfo);
                            MyDecals.HandleAddDecal(entity2, hitInfo);
                        }
                    }
                }

                //float destructionImpact = vel * (MyDestructionHelper.MassFromHavok(Mass) + MyDestructionHelper.MassFromHavok(info.CollidingEntity.Physics.Mass));
                //destructionImpact *= info.ImpulseMultiplier;

                //if (destructionImpact > 2000 && enoughSpeed)
                //{
                //    CreateDestructionFor(destructionImpact, LinearVelocity + info.CollidingEntity.Physics.LinearVelocity, this, info, value.ContactPoint.Normal);
                //    CreateDestructionFor(destructionImpact, LinearVelocity + info.CollidingEntity.Physics.LinearVelocity, info.CollidingEntity.Physics, info, value.ContactPoint.Normal);

                //    ReduceVelocities(info);
                //}
            }

            ProfilerShort.End();
        }

        private HkShape CreateBreakableBody(HkShape shape, HkMassProperties? massProperties)
        {
            ProfilerShort.Begin("CreateGridBody");

            HkdBreakableShape breakable;
            HkMassProperties massProps = massProperties.HasValue ? massProperties.Value : new HkMassProperties();

            if (!Shape.BreakableShape.IsValid())
                Shape.CreateBreakableShape();

            breakable = Shape.BreakableShape;

            if (!breakable.IsValid())
            {
                breakable = new HkdBreakableShape(shape);
                if (massProperties.HasValue)
                {
                    var mp = massProperties.Value;
                    breakable.SetMassProperties(ref mp);
                }
                else
                    breakable.SetMassRecursively(50);
            }
            else
                breakable.BuildMassProperties(ref massProps);

            shape = breakable.GetShape(); //doesnt add reference
            HkRigidBodyCinfo rbInfo = new HkRigidBodyCinfo();
            rbInfo.AngularDamping = m_angularDamping;
            rbInfo.LinearDamping = m_linearDamping;
            rbInfo.SolverDeactivation = m_grid.IsStatic ? InitialSolverDeactivation : HkSolverDeactivation.Low;
            rbInfo.ContactPointCallbackDelay = ContactPointDelay;
            rbInfo.Shape = shape;
            rbInfo.SetMassProperties(massProps);
            //rbInfo.Position = Entity.PositionComp.GetPosition(); //obsolete with large worlds?
            GetInfoFromFlags(rbInfo, Flags);
            if (m_grid.IsStatic)
            {
                rbInfo.MotionType = HkMotionType.Dynamic;
                rbInfo.QualityType = HkCollidableQualityType.Moving;
            }
            HkRigidBody rb = new HkRigidBody(rbInfo);
            if (m_grid.IsStatic)
            {
                rb.UpdateMotionType(HkMotionType.Fixed);
            }
            rb.EnableDeactivation = true;
            BreakableBody = new HkdBreakableBody(breakable, rb, null, Matrix.Identity);
            //DestructionBody.ConnectToWorld(HavokWorld, 0.05f);

            BreakableBody.AfterReplaceBody += FracturedBody_AfterReplaceBody;

            //RigidBody.SetWorldMatrix(Entity.PositionComp.WorldMatrix);
            //breakable.Dispose();

            ProfilerShort.End();
            return shape;
        }

        private void FindFracturedBlocks(HkdBreakableBodyInfo b)
        {
            ProfilerShort.Begin("DBHelper");
            var dbHelper = new HkdBreakableBodyHelper(b);
            ProfilerShort.BeginNextBlock("GetRBMatrix");
            var bodyMatrix = dbHelper.GetRigidBodyMatrix();
            ProfilerShort.BeginNextBlock("SearchChildren");
            dbHelper.GetChildren(m_children);
            foreach (var child in m_children)
            {
                if (!child.IsFracturePiece())
                    continue;
                //var blockPosWorld = ClusterToWorld(Vector3.Transform(child.GetTransform().Translation, bodyMatrix));
                var bShape = child.Shape;
                HkVec3IProperty pProp = bShape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                var blockPos = pProp.Value; //Vector3I.Round(child.GetTransform().Translation / m_grid.GridSize);
                if (!m_grid.CubeExists(blockPos))
                {
                    //Debug.Fail("FindFracturedBlocks:Fracture piece missing block");//safe to ignore
                    continue;
                }

                if (MyFakes.ENABLE_FRACTURE_COMPONENT)
                {
                    var block = m_grid.GetCubeBlock(blockPos);
                    if (block == null)
                        continue;

                    if (!FindFractureComponentBlocks(block, child))
                        continue;
                }
                else
                {
                    if (!m_fracturedBlocksShapes.ContainsKey(blockPos))
                        m_fracturedBlocksShapes[blockPos] = new List<HkdShapeInstanceInfo>();
                    m_fracturedBlocksShapes[blockPos].Add(child);
                }
            }
            ProfilerShort.BeginNextBlock("CreateFreacturedBlocks");
            if (!MyFakes.ENABLE_FRACTURE_COMPONENT)
            {
                foreach (var key in m_fracturedBlocksShapes.Keys)
                {
                    HkdBreakableShape shape;
                    var shapeList = m_fracturedBlocksShapes[key];
                    foreach (var s in shapeList)
                    {
                        var matrix = s.GetTransform();
                        matrix.Translation = Vector3.Zero;
                        s.SetTransform(ref matrix);
                    }
                    ProfilerShort.Begin("CreateShape");
                    HkdBreakableShape compound = new HkdCompoundBreakableShape(null, shapeList);
                    ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                    var mp = new HkMassProperties();
                    compound.BuildMassProperties(ref mp);
                    shape = compound;
                    var sh = compound.GetShape();
                    shape = new HkdBreakableShape(sh, ref mp);
                    //shape.SetMassProperties(mp); //important! pass mp to constructor
                    foreach (var si in shapeList)
                    {
                        var siRef = si;
                        shape.AddShape(ref siRef);
                    }
                    compound.RemoveReference();
                    ProfilerShort.BeginNextBlock("Connect");
                    //shape.SetChildrenParent(shape);
                    ConnectPiecesInBlock(shape, shapeList);
                    ProfilerShort.End();

                    var info = new MyFracturedBlock.Info()
                    {
                        Shape = shape,
                        Position = key,
                        Compound = true,
                    };
                    var originalBlock = m_grid.GetCubeBlock(key);
                    if (originalBlock == null)
                    {
                        //Debug.Fail("Missing fracture piece original block.");//safe to ignore
                        shape.RemoveReference();
                        continue;
                    }
                    Debug.Assert(originalBlock != null);
                    if (originalBlock.FatBlock is MyFracturedBlock)
                    {
                        var fractured = originalBlock.FatBlock as MyFracturedBlock;
                        info.OriginalBlocks = fractured.OriginalBlocks;
                        info.Orientations = fractured.Orientations;
                        info.MultiBlocks = fractured.MultiBlocks;
                    }
                    else if (originalBlock.FatBlock is MyCompoundCubeBlock)
                    {
                        info.OriginalBlocks = new List<MyDefinitionId>();
                        info.Orientations = new List<MyBlockOrientation>();
                        MyCompoundCubeBlock compoundBlock = originalBlock.FatBlock as MyCompoundCubeBlock;
                        bool hasMultiBlockPart = false;
                        var blocksInCompound = compoundBlock.GetBlocks();
                        foreach (var block in blocksInCompound)
                        {
                            info.OriginalBlocks.Add(block.BlockDefinition.Id);
                            info.Orientations.Add(block.Orientation);

                            hasMultiBlockPart = hasMultiBlockPart || block.IsMultiBlockPart;
                        }

                        if (hasMultiBlockPart)
                        {
                            info.MultiBlocks = new List<MyFracturedBlock.MultiBlockPartInfo>();

                            foreach (var block in blocksInCompound)
                            {
                                if (block.IsMultiBlockPart)
                                    info.MultiBlocks.Add(new MyFracturedBlock.MultiBlockPartInfo() { MultiBlockDefinition = block.MultiBlockDefinition.Id, MultiBlockId = block.MultiBlockId });
                                else
                                    info.MultiBlocks.Add(null);
                            }
                        }
                    }
                    else
                    {
                        info.OriginalBlocks = new List<MyDefinitionId>();
                        info.Orientations = new List<MyBlockOrientation>();
                        info.OriginalBlocks.Add(originalBlock.BlockDefinition.Id);
                        info.Orientations.Add(originalBlock.Orientation);

                        if (originalBlock.IsMultiBlockPart)
                        {
                            info.MultiBlocks = new List<MyFracturedBlock.MultiBlockPartInfo>();
                            info.MultiBlocks.Add(new MyFracturedBlock.MultiBlockPartInfo() { MultiBlockDefinition = originalBlock.MultiBlockDefinition.Id, MultiBlockId = originalBlock.MultiBlockId });
                        }
                    }
                    m_fractureBlocksCache.Add(info);
                }
            }
            m_fracturedBlocksShapes.Clear();
            m_children.Clear();

            ProfilerShort.End();
        }

        /// <summary>
        /// Searches for all children shapes of the block (or block inside compound block). The given block is always the block from grid (not from compound block).
        /// </summary>
        /// <param name="block">grid block</param>
        /// <param name="shapeInst">shape instnce for the block</param>
        /// <returns></returns>
        private bool FindFractureComponentBlocks(MySlimBlock block, HkdShapeInstanceInfo shapeInst)
        {
            Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT);

            var bShape = shapeInst.Shape;
            if (IsBreakableShapeCompound(bShape))
            {
                bool anyAdded = false;
                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                bShape.GetChildren(children);

                foreach (var child in children)
                    anyAdded |= FindFractureComponentBlocks(block, child);

                return anyAdded;
            }

            ushort? blockIdInCompound = null;
            if (bShape.HasProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID))
            {
                HkSimpleValueProperty blockIdInCompoundProperty = bShape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                blockIdInCompound = (ushort)blockIdInCompoundProperty.ValueUInt;
            }

            var compoundBlock = block.FatBlock as MyCompoundCubeBlock;
            if (compoundBlock != null)
            {
                if (blockIdInCompound != null)
                {
                    var blockInCompound = compoundBlock.GetBlock(blockIdInCompound.Value);
                    if (blockInCompound != null)
                    {
                        block = blockInCompound;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.Fail("Breakable shape has no compoundId");
                    return false;
                }
            }

            if (!m_fracturedSlimBlocksShapes.ContainsKey(block))
                m_fracturedSlimBlocksShapes[block] = new List<HkdShapeInstanceInfo>();
            m_fracturedSlimBlocksShapes[block].Add(shapeInst);

            return true;
        }

        /// <summary>
        /// Searches for blocks which will create fracture components from cached m_fracturedSlimBlocksShapes
        /// </summary>
        private void FindFractureComponentBlocks()
        {
            Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT);

            foreach (var pair in m_fracturedSlimBlocksShapes)
            {
                var slimBlock = pair.Key;
                var shapeList = pair.Value;

                if (slimBlock.FatBlock.Components.Has<MyFractureComponentBase>())
                {
                    // Block has fracture component - ignore
                    continue;
                }
                else
                {
                    int totalBreakableShapesCountForModel = slimBlock.GetTotalBreakableShapeChildrenCount();
                    Debug.Assert(shapeList.Count <= totalBreakableShapesCountForModel);
                    // No removed pieces? Then ignore.
                    if (slimBlock.BlockDefinition.CreateFracturedPieces && totalBreakableShapesCountForModel == shapeList.Count)
                        continue;

                    foreach (var s in shapeList)
                    {
                        s.SetTransform(ref Matrix.Identity);
                    }
                    ProfilerShort.Begin("CreateShapeComponent");
                    HkdBreakableShape compound = new HkdCompoundBreakableShape(null, shapeList);
                    ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
                    var mp = new HkMassProperties();
                    compound.BuildMassProperties(ref mp);
                    HkdBreakableShape shape = compound;
                    var sh = compound.GetShape();
                    shape = new HkdBreakableShape(sh, ref mp);
                    //shape.SetMassProperties(mp); //important! pass mp to constructor
                    foreach (var si in shapeList)
                    {
                        var siRef = si;
                        shape.AddShape(ref siRef);
                    }
                    compound.RemoveReference();

                    ProfilerShort.BeginNextBlock("Connect");
                    //shape.SetChildrenParent(shape);
                    ConnectPiecesInBlock(shape, shapeList);

                    MyFractureComponentBase.Info info = new MyFractureComponentBase.Info()
                    {
                        Entity = slimBlock.FatBlock,
                        Shape = shape,
                        Compound = true
                    };

                    m_fractureBlockComponentsCache.Add(info);

                    ProfilerShort.End();
                }
            }

            m_fracturedSlimBlocksShapes.Clear();
        }

        private static void ConnectPiecesInBlock(HkdBreakableShape parent, List<HkdShapeInstanceInfo> shapeList)
        {
            for (int i = 0; i < shapeList.Count; i++)
            {
                for (int j = 0; j < shapeList.Count; j++)
                {
                    if (i == j) continue;
                    MyGridShape.ConnectShapesWithChildren(parent, shapeList[i].Shape, shapeList[j].Shape);
                }
            }
        }

        private List<HkdShapeInstanceInfo> m_childList = new List<HkdShapeInstanceInfo>();
        private void RecreateBreakableBody()
        {
            ProfilerShort.Begin("RecreateBody");
            bool wasfixed = RigidBody.IsFixedOrKeyframed;
            var layer = RigidBody.Layer;

            var world = ((MyGridPhysics) m_grid.Physics).HavokWorld;

            if (false)//m_newBreakableBodies.Count == 1) //jn: keeps crashing now, putting aside for release
            {
                ProfilerShort.Begin("NewReplace");
                ProfilerShort.Begin("Close");
                BreakableBody.BreakableShape.ClearConnectionsRecursive();
                BreakableBody.BreakableShape.RemoveReference();
                CloseRigidBody();
                ProfilerShort.BeginNextBlock("2");
                BreakableBody = m_newBreakableBodies[0];
                Debug.Assert(RigidBody.Layer == layer, "New body has different layer!!");
                RigidBody.UserObject = this;
                RigidBody.ContactPointCallbackEnabled = true;
                RigidBody.ContactSoundCallbackEnabled = true;
                RigidBody.ContactPointCallback += RigidBody_ContactPointCallback_Destruction;
                BreakableBody.AfterReplaceBody += FracturedBody_AfterReplaceBody;
                BreakableBody.BreakableShape.AddReference();
                ProfilerShort.BeginNextBlock("ReplaceFractures");
                BreakableBody.BreakableShape.GetChildren(m_childList);
                for (int i = 0; i < m_childList.Count; i++) //remove fractures
                {
                    var child = m_childList[i];
                    Debug.Assert(((HkVec3IProperty)child.Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION)).IsValid());
                    if (child.Shape.IsFracturePiece())
                    {
                        //int j = 0;
                        //for (; j < BreakableBody.BreakableShape.GetChildrenCount(); j++)
                        //    if (BreakableBody.BreakableShape.GetChild(j).Shape == child.Shape)
                        //        break;
                        //Debug.Assert(child.Shape == BreakableBody.BreakableShape.GetChild(j).Shape && j >= i);
                        //child.Shape.SetFlagRecursively(HkdBreakableShape.Flags.DONT_CREATE_FRACTURE_PIECE);

                        //BreakableBody.BreakableShape.RemoveChild(j);

                        //child.Shape.RemoveReference();
                        //m_childList[i].Shape.RemoveReference();
                        //m_childList[i].RemoveReference();
                        m_childList.RemoveAt(i);
                        i--;
                    }
                }
                foreach (var fb in m_fractureBlocksCache) //add fractures grouped by block
                {
                    Debug.Assert(fb.Shape.IsValid() && !((HkVec3IProperty)fb.Shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION)).IsValid());
                    fb.Shape.SetPropertyRecursively(HkdBreakableShape.PROPERTY_GRID_POSITION, new HkVec3IProperty(fb.Position));
                    Matrix m = Matrix.Identity;
                    m.Translation = fb.Position * m_grid.GridSize;
                    fb.Shape.SetChildrenParent(fb.Shape);
                    var si = new HkdShapeInstanceInfo(fb.Shape, m);
                    //BreakableBody.BreakableShape.AddShape(ref si);
                    //si.RemoveReference();
                    m_childList.Add(si);
                }
                BreakableBody.BreakableShape.ReplaceChildren(m_childList);
                for (int i = m_childList.Count - m_fractureBlocksCache.Count; i < m_childList.Count; i++)
                    m_childList[i].RemoveReference();
                m_childList.Clear();
                ProfilerShort.BeginNextBlock("Connections");
                BreakableBody.BreakableShape.SetChildrenParent(BreakableBody.BreakableShape);
                Shape.BreakableShape = BreakableBody.BreakableShape;
                Shape.UpdateDirtyBlocks(m_dirtyCubesInfo.DirtyBlocks, false);
                Shape.CreateConnectionToWorld(BreakableBody, world);
                if (wasfixed && m_grid.GridSizeEnum == MyCubeSize.Small)
                {
                    if (MyCubeGridSmallToLargeConnection.Static.TestGridSmallToLargeConnection(m_grid))
                    {
                        RigidBody.UpdateMotionType(HkMotionType.Fixed);
                        RigidBody.Quality = HkCollidableQualityType.Fixed;
                    }
                }
                ProfilerShort.BeginNextBlock("Add");
                HavokWorld.DestructionWorld.AddBreakableBody(BreakableBody);
                ProfilerShort.End();
                ProfilerShort.End();
            }
            else
            { //Old body is removed so create new one (should use matching one from new bodies in final version)
                foreach (var b in m_newBreakableBodies)
                {
                    MyFracturedPiecesManager.Static.ReturnToPool(b);
                }
                ProfilerShort.Begin("OldReplace");
                ProfilerShort.Begin("GetPhysicsBody");
                var ph = BreakableBody.GetRigidBody();
                var linVel = ph.LinearVelocity;
                var angVel = ph.AngularVelocity;
                ph = null;
                ProfilerShort.End();
                if (m_grid.BlocksCount > 0)
                {
                    ProfilerShort.Begin("Refresh");
                    Shape.RefreshBlocks(RigidBody, RigidBody2, m_dirtyCubesInfo, BreakableBody);
                    ProfilerShort.BeginNextBlock("NewGridBody");
                    CloseRigidBody();
                    var s = (HkShape)m_shape;
                    CreateBody(ref s, null);
                    RigidBody.Layer = layer;
                    RigidBody.ContactPointCallbackEnabled = true;
                    RigidBody.ContactSoundCallbackEnabled = true;
                    RigidBody.ContactPointCallback += RigidBody_ContactPointCallback_Destruction;
                    BreakableBody.BeforeControllerOperation += BreakableBody_BeforeControllerOperation;
                    BreakableBody.AfterControllerOperation += BreakableBody_AfterControllerOperation;
                    Matrix m = Entity.PositionComp.WorldMatrix;
                    m.Translation = WorldToCluster(Entity.PositionComp.GetPosition());
                    RigidBody.SetWorldMatrix(m);
                    RigidBody.UserObject = this;
                    Entity.Physics.LinearVelocity = m_oldLinVel;
                    Entity.Physics.AngularVelocity = m_oldAngVel;
                    m_grid.DetectDisconnectsAfterFrame();
                    Shape.CreateConnectionToWorld(BreakableBody, world);
                    HavokWorld.DestructionWorld.AddBreakableBody(BreakableBody);
                    ProfilerShort.End();
                }
                else
                {
                    ProfilerShort.Begin("GridClose");
                    m_grid.Close();
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }
            m_newBreakableBodies.Clear();
            m_fractureBlocksCache.Clear();
            ProfilerShort.End();
        }

        //private void SplitGrid(HkdReplaceBodyEvent e)
        //{
        //    //Grid splitting WIP
        //    ProfilerShort.Begin("Destruction.SplitGrid");
        //    if (m_grid.GetBlocks().Count == 1)
        //    {
        //        ProfilerShort.End();
        //        return;
        //    }
        //    //TODO: Cluster to world
        //    var mat = RigidBody.GetWorldMatrix();
        //    var conn = e.GetBrokenConnections();
        //    for (int i = 0; i < conn.ConnectedBodiesCount; i++) //Get broken connections positions
        //    {
        //        for (int j = 0; j < conn.GetConnectedBody(i).ConnectionsCount; j++)
        //        {
        //            Vector3D t = conn.GetConnectedBody(i).GetConnectionInfo(j).Connection.PivotA;
        //            m_splitPosition.Add(t / m_grid.GridSize);
        //        }
        //    }
        //    if (Entity is MyCubeGrid && m_splitPosition.Count > 1)
        //    {
        //        var grid = Entity as MyCubeGrid;
        //        for (int i = 0; i < m_splitPosition.Count; i += 2) //positions should come in pairs (since both A->B and B->A connections broke and we got beginning pivots from both)
        //        {
        //            Sandbox.Game.Entities.Cube.MySlimBlock a = grid.GetCubeBlock(Vector3I.Round(m_splitPosition[i]));
        //            Sandbox.Game.Entities.Cube.MySlimBlock b = grid.GetCubeBlock(Vector3I.Round(m_splitPosition[i + 1]));
        //            if (a == null || b == null)
        //                continue;
        //            if (a == b)
        //            {
        //                //m_grid.RemoveBlock(a);
        //                //m_blocksToDisconnect.Remove(a);
        //                DisconnectBlock(a);
        //                //m_tmpLst3.Add(a);
        //                continue;
        //            }
        //            var ab = b.Position - a.Position;
        //            var ba = a.Position - b.Position;
        //            AddFaces(a, ab);
        //            AddFaces(b, ba);
        //            if (!m_blocksToDisconnect.Contains(a))
        //                m_blocksToDisconnect.Add(a);
        //            if (!m_blocksToDisconnect.Contains(b))
        //                m_blocksToDisconnect.Add(b);
        //        }
        //        foreach (var b in m_blocksToDisconnect)
        //            grid.UpdateBlockNeighbours(b);
        //        foreach (var b in m_blocksToDisconnect)
        //            b.DisconnectFaces.Clear();
        //    }
        //    m_blocksToDisconnect.Clear();
        //    m_splitPosition.Clear();
        //    ProfilerShort.End();
        //}
        public bool CheckLastDestroyedBlockFracturePieces()
        {
            Debug.Assert(MyFakes.ENABLE_FRACTURE_COMPONENT);

            if (!Sync.IsServer)
                return false;

            if (m_grid.BlocksCount == 1 && !m_grid.IsStatic)
            {
                var block = m_grid.GetBlocks().First();
                if (block.FatBlock != null)
                {
                    var compoundBlock = block.FatBlock as MyCompoundCubeBlock;

                    if (compoundBlock != null)
                    {
                        bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

                        bool allHaveFracComp = true;
                        var blocksInCompound = new List<MySlimBlock>(compoundBlock.GetBlocks());
                        foreach (var blockInCompound in blocksInCompound)
                        {
                            allHaveFracComp = allHaveFracComp && blockInCompound.FatBlock.Components.Has<MyFractureComponentBase>();
                        }

                        if (allHaveFracComp)
                        {
                            foreach (var blockInCompound in blocksInCompound)
                            {
                                var fractureComponent = blockInCompound.GetFractureComponent();
                                var compoundId = compoundBlock.GetBlockId(blockInCompound);
                                Debug.Assert(fractureComponent != null);
                                if (fractureComponent != null)
                                    MyDestructionHelper.CreateFracturePiece(fractureComponent, true);

                                m_grid.RemoveBlockWithId(blockInCompound.Position, compoundId.Value, true);
                            }
                        }

                        m_grid.EnableGenerators(oldGeneratorsEnabled);

                        return allHaveFracComp;
                    }
                    else
                    {
                        var fractureComponent = block.GetFractureComponent();
                        if (fractureComponent != null)
                        {
                            bool oldGeneratorsEnabled = m_grid.EnableGenerators(false);

                            MyDestructionHelper.CreateFracturePiece(fractureComponent, true);
                            m_grid.RemoveBlock(block, true);

                            m_grid.EnableGenerators(oldGeneratorsEnabled);
                        }

                        return fractureComponent != null;
                    }
                }
            }

            return false;
        }

        internal ushort? GetContactCompoundId(Vector3I position, Vector3D constactPos)
        {
            List<HkdBreakableShape> inersectingShapes = new List<HkdBreakableShape>();
            GetRigidBodyMatrix(out m_bodyMatrix);
            Quaternion bodyRot = Quaternion.CreateFromRotationMatrix(m_bodyMatrix);

            Debug.Assert(BreakableBody != null, "BreakableBody was null in GetContactCounpoundId!");
            if (BreakableBody == null)
            {
                MyLog.Default.WriteLine("BreakableBody was null in GetContactCounpoundId!");
            }

            Debug.Assert(HavokWorld.DestructionWorld != null, "HavokWorld.DestructionWorld was null in GetContactCompoundId!");
            if (HavokWorld.DestructionWorld == null)
            {
                MyLog.Default.WriteLine("HavokWorld.DestructionWorld was null in GetContactCompoundId!");
            }

            HkDestructionUtils.FindAllBreakableShapesIntersectingSphere(HavokWorld.DestructionWorld, BreakableBody, bodyRot, m_bodyMatrix.Translation,
                WorldToCluster(constactPos), 0.1f, inersectingShapes);

            ushort? compoundId = null;

            foreach (var shape in inersectingShapes)
            {
                if (!shape.IsValid())
                    continue;

                HkVec3IProperty propGridPos = shape.GetProperty(HkdBreakableShape.PROPERTY_GRID_POSITION);
                if (!propGridPos.IsValid() || position != propGridPos.Value)
                    continue;

                HkSimpleValueProperty propCompoundId = shape.GetProperty(HkdBreakableShape.PROPERTY_BLOCK_COMPOUND_ID);
                if (!propCompoundId.IsValid())
                    continue;

                compoundId = (ushort)propCompoundId.ValueUInt;
                break;
            }

            return compoundId;
        }
    }
}