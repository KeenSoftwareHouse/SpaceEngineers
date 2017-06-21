using Havok;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRageMath;
using Sandbox.Game.EntityComponents;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Replication;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Entity;
using VRage.Profiler;

namespace Sandbox.Engine.Physics
{
    public static class MyDestructionHelper
    {
        public static readonly float MASS_REDUCTION_COEF = 1f/25f;
 
        private static List<HkdShapeInstanceInfo> m_tmpInfos = new List<HkdShapeInstanceInfo>();
        private static List<HkdShapeInstanceInfo> m_tmpInfos2 = new List<HkdShapeInstanceInfo>();

        private static bool DontCreateFracture(HkdBreakableShape breakableShape)
        {
            if (!breakableShape.IsValid())
                return false;
            return (breakableShape.UserObject & (uint)HkdBreakableShape.Flags.DONT_CREATE_FRACTURE_PIECE) != 0;
        }

        public static bool IsFixed(HkdBreakableBodyInfo breakableBodyInfo)
        {
            System.Diagnostics.Debug.Assert(m_tmpInfos2.Count == 0);
            var dbHelper = new HkdBreakableBodyHelper(breakableBodyInfo);
            dbHelper.GetChildren(m_tmpInfos2);

            foreach (var child in m_tmpInfos2)
            {
                if (IsFixed(child.Shape))
                {
                    m_tmpInfos2.Clear();
                    return true;
                }
            }

            m_tmpInfos2.Clear();
            return false;
        }

        public static bool IsFixed(HkdBreakableShape breakableShape)
        {
            System.Diagnostics.Debug.Assert(m_tmpInfos.Count == 0, "");
            if (!breakableShape.IsValid())
                return false;
            if ((breakableShape.UserObject & (uint)HkdBreakableShape.Flags.IS_FIXED) != 0)
            {
                return true;
            }
            else
            {
                Debug.Assert(m_tmpInfos.Count == 0);
                breakableShape.GetChildren(m_tmpInfos);
                foreach (var child in m_tmpInfos)
                {
                    if ((child.Shape.UserObject & (uint)HkdBreakableShape.Flags.IS_FIXED) != 0)
                    {
                        m_tmpInfos.Clear();
                        return true;
                    }
                }
                m_tmpInfos.Clear();
            }
            return false;
        }

        /// <summary>
        /// Returns true if the body does not generate fractured pieces.
        /// </summary>
        private static bool IsBodyWithoutGeneratedFracturedPieces(HkdBreakableBody b, MyCubeBlock block)
        {
            if (MyFakes.REMOVE_GENERATED_BLOCK_FRACTURES && (block == null || ContainsBlockWithoutGeneratedFracturedPieces(block)))
            {
                if (b.BreakableShape.IsCompound())
                {
                    Debug.Assert(m_tmpInfos.Count == 0);
                    b.BreakableShape.GetChildren(m_tmpInfos);
                    for (int i = m_tmpInfos.Count - 1; i >= 0; --i)
                    {
                        if (DontCreateFracture(m_tmpInfos[i].Shape))
                            m_tmpInfos.RemoveAt(i);
                        else
                            break; // Break because we know that there is block which creates fracture pieces and condition "m_tmpInfos.Count == 0" in bellow code cannot be true
                    }

                    if (m_tmpInfos.Count == 0)
                    {
                        return true;
                    }
                    m_tmpInfos.Clear();
                }
                else if (DontCreateFracture(b.BreakableShape))
                {
                    return true;
                }
            }
            return false;
        }

        public static MyFracturedPiece CreateFracturePiece(HkdBreakableBody b, ref MatrixD worldMatrix, List<MyDefinitionId> originalBlocks, MyCubeBlock block = null, bool sync = true)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "Only on server");

            if (IsBodyWithoutGeneratedFracturedPieces(b, block))
                return null;

            ProfilerShort.Begin("CreateFracturePiece");
            var fracturedPiece = MyFracturedPiecesManager.Static.GetPieceFromPool(0);
            fracturedPiece.InitFromBreakableBody(b, worldMatrix, block);
            fracturedPiece.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            //fracturedPiece.Physics.RigidBody.ContactPointCallbackDelay = 0;
            //fracturedPiece.Physics.RigidBody.ContactPointCallbackEnabled = true;
            ProfilerShort.End();

            if (originalBlocks != null && originalBlocks.Count != 0)
            {
                fracturedPiece.OriginalBlocks.Clear();
                fracturedPiece.OriginalBlocks.AddRange(originalBlocks);

                MyPhysicalModelDefinition def;
                if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(originalBlocks[0], out def))
                    fracturedPiece.Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;
            }

            // Check valid shapes from block definitions. 
            if (MyFakes.ENABLE_FRACTURE_PIECE_SHAPE_CHECK)
                fracturedPiece.DebugCheckValidShapes();

            ProfilerShort.Begin("MyEntities.Add");
            if (MyExternalReplicable.FindByObject(fracturedPiece) == null)
                MyEntities.RaiseEntityCreated(fracturedPiece);
            MyEntities.Add(fracturedPiece);
            ProfilerShort.End();

            return fracturedPiece;
        }

        public static void FixPosition(MyFracturedPiece fp)
        {
            //return;
            ProfilerShort.Begin("FixPosition");
            var shape = fp.Physics.BreakableBody.BreakableShape;
            if (shape.GetChildrenCount() == 0)
            {
                ProfilerShort.End();
                return;
            }
            shape.GetChildren(m_tmpInfos);
            var offset = m_tmpInfos[0].GetTransform().Translation;
            if (offset.LengthSquared() < 1)
            {
                m_tmpInfos.Clear();
                ProfilerShort.End();
                return;
            }
            var lst = new List<HkdConnection>();
            var set = new HashSet<HkdBreakableShape>();
            var set2 = new HashSet<HkdBreakableShape>();
            set.Add(shape);
            shape.GetConnectionList(lst);
            fp.PositionComp.SetPosition(Vector3D.Transform(offset, fp.PositionComp.WorldMatrix));
            foreach (var child in m_tmpInfos)
            {
                var m = child.GetTransform();
                m.Translation -= offset;
                child.SetTransform(ref m);
                m_tmpInfos2.Add(child);
                HkdBreakableShape par = child.Shape;
                par.GetConnectionList(lst);
                while (par.HasParent)
                {
                    par = par.GetParent();
                    if (set.Add(par))
                        par.GetConnectionList(lst);
                    else
                    {

                    }
                }
                set2.Add(child.Shape);
            }
            m_tmpInfos.Clear();
            HkdBreakableShape compound = new HkdCompoundBreakableShape(shape, m_tmpInfos2);
            //HkMassProperties mp = new HkMassProperties();
            ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();
            compound.SetChildrenParent(compound);
            foreach (var c in lst) 
            {
                HkBaseSystem.EnableAssert(390435339, true);
                if (!set2.Contains(c.ShapeA) || !set2.Contains(c.ShapeB))
                    continue;
                var cref = c;
                compound.AddConnection(ref cref);
            }
            fp.Physics.BreakableBody.BreakableShape = compound;
            m_tmpInfos2.Clear();

            ((HkdCompoundBreakableShape)compound).RecalcMassPropsFromChildren();

            ProfilerShort.End();
        }

        /// <summary>
        /// Returns true if the block (or any block in compound) does not generate generate fractured pieces.
        /// </summary>
        private static bool ContainsBlockWithoutGeneratedFracturedPieces(MyCubeBlock block)
        {
            if (!block.BlockDefinition.CreateFracturedPieces)
                return true;

            if (block is MyCompoundCubeBlock)
            {
                foreach (var b in (block as MyCompoundCubeBlock).GetBlocks())
                    if (!b.BlockDefinition.CreateFracturedPieces)
                        return true;
            }

            if (block is MyFracturedBlock)
            {
                foreach (var def in (block as MyFracturedBlock).OriginalBlocks)
                    if (!MyDefinitionManager.Static.GetCubeBlockDefinition(def).CreateFracturedPieces)
                        return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shape">Piece takes ownership of shape so clone it first</param>
        /// <param name="worldMatrix"></param>
        /// <param name="definition"> without definition the piece wont save</param>
        /// <returns></returns>
        public static MyFracturedPiece CreateFracturePiece(HkdBreakableShape shape, ref MatrixD worldMatrix, bool isStatic, MyDefinitionId? definition, bool sync)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "Only on server");
            var fracturedPiece = CreateFracturePiece(ref shape, ref worldMatrix, isStatic);

            if (definition.HasValue)
            {
                fracturedPiece.OriginalBlocks.Clear();
                fracturedPiece.OriginalBlocks.Add(definition.Value);

                MyPhysicalModelDefinition def;
                if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(definition.Value, out def))
                    fracturedPiece.Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;
            }
            else
                fracturedPiece.Save = false;

            // Check valid shapes from block definitions. 
            if (fracturedPiece.Save && MyFakes.ENABLE_FRACTURE_PIECE_SHAPE_CHECK)
                fracturedPiece.DebugCheckValidShapes();

            ProfilerShort.Begin("MyEntities.Add");
            if (MyExternalReplicable.FindByObject(fracturedPiece) == null)
                MyEntities.RaiseEntityCreated(fracturedPiece);
            MyEntities.Add(fracturedPiece);
            ProfilerShort.End();

            return fracturedPiece;
        }

        public static MyFracturedPiece CreateFracturePiece(MyFracturedBlock fracturedBlock, bool sync)
        {
            System.Diagnostics.Debug.Assert(Sync.IsServer, "Only on server");
            var m = fracturedBlock.CubeGrid.PositionComp.WorldMatrix;
            m.Translation = fracturedBlock.CubeGrid.GridIntegerToWorld(fracturedBlock.Position);
            var fp = CreateFracturePiece(ref fracturedBlock.Shape, ref m, false);
            fp.OriginalBlocks = fracturedBlock.OriginalBlocks;

            MyPhysicalModelDefinition def;
            if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(fp.OriginalBlocks[0], out def))
                fp.Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;

            // Check valid shapes from block definitions. 
            if (MyFakes.ENABLE_FRACTURE_PIECE_SHAPE_CHECK)
                fp.DebugCheckValidShapes();

            ProfilerShort.Begin("MyEntities.Add");
            if (MyExternalReplicable.FindByObject(fp) == null)
                MyEntities.RaiseEntityCreated(fp);
            MyEntities.Add(fp);
            ProfilerShort.End();

            return fp;
        }

        public static MyFracturedPiece CreateFracturePiece(MyFractureComponentCubeBlock fractureBlockComponent, bool sync)
        {
            if (!fractureBlockComponent.Block.BlockDefinition.CreateFracturedPieces)
                return null;

            if (!fractureBlockComponent.Shape.IsValid())
            {
                Debug.Fail("Invalid shape in fracture component");
                MyLog.Default.WriteLine("Invalid shape in fracture component, Id: " + fractureBlockComponent.Block.BlockDefinition.Id.ToString() + ", closed: " + fractureBlockComponent.Block.FatBlock.Closed);
                return null;
            }

            System.Diagnostics.Debug.Assert(Sync.IsServer, "Only on server");
            var m = fractureBlockComponent.Block.FatBlock.WorldMatrix;
            var fp = CreateFracturePiece(ref fractureBlockComponent.Shape, ref m, false);
            fp.OriginalBlocks.Add(fractureBlockComponent.Block.BlockDefinition.Id);

            // Check valid shapes from block definitions. 
            if (MyFakes.ENABLE_FRACTURE_PIECE_SHAPE_CHECK)
                fp.DebugCheckValidShapes();

            MyPhysicalModelDefinition def;
            if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(fp.OriginalBlocks[0], out def))
                fp.Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;

            ProfilerShort.Begin("MyEntities.Add");
            if (MyExternalReplicable.FindByObject(fp) == null)
                MyEntities.RaiseEntityCreated(fp);
            MyEntities.Add(fp);
            ProfilerShort.End();

            return fp;
        }

        private static MyFracturedPiece CreateFracturePiece(ref HkdBreakableShape shape, ref MatrixD worldMatrix, bool isStatic)
        {
            Debug.Assert(shape.IsValid());
            ProfilerShort.Begin("CreateFracturePiece");
            var fracturedPiece = MyFracturedPiecesManager.Static.GetPieceFromPool(0);//new MyFracturedPiece();
            fracturedPiece.PositionComp.WorldMatrix = worldMatrix;
            fracturedPiece.Physics.Flags = isStatic ? RigidBodyFlag.RBF_STATIC : RigidBodyFlag.RBF_DEBRIS;
            var physicsBody = fracturedPiece.Physics as MyPhysicsBody;//new MyPhysicsBody(fracturedPiece,isFixed ?RigidBodyFlag.RBF_STATIC : RigidBodyFlag.RBF_DEBRIS);

            HkMassProperties mp = new HkMassProperties();
            shape.BuildMassProperties(ref mp);
            physicsBody.InitialSolverDeactivation = HkSolverDeactivation.High;
            physicsBody.CreateFromCollisionObject(shape.GetShape(), Vector3.Zero, worldMatrix, mp);

            physicsBody.LinearDamping = MyPerGameSettings.DefaultLinearDamping;
            physicsBody.AngularDamping = MyPerGameSettings.DefaultAngularDamping;

            System.Diagnostics.Debug.Assert(physicsBody.BreakableBody == null, "physicsBody.DestructionBody == null");
            physicsBody.BreakableBody = new HkdBreakableBody(shape, physicsBody.RigidBody, null, worldMatrix);
            physicsBody.BreakableBody.AfterReplaceBody += physicsBody.FracturedBody_AfterReplaceBody;
            ProfilerShort.End();

            ProfilerShort.Begin("Sync");
            if (fracturedPiece.SyncFlag)
            {
                fracturedPiece.CreateSync();
            }
            ProfilerShort.End();
            fracturedPiece.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            //physicsBody.RigidBody.ContactPointCallbackDelay = 0;
            //physicsBody.RigidBody.ContactPointCallbackEnabled = true;
            //fracturedPiece.Physics = physicsBody;
            //FixPosition(fracturedPiece);
            fracturedPiece.SetDataFromHavok(shape);
            ProfilerShort.Begin("AddToWorld");
            fracturedPiece.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            shape.RemoveReference();
            ProfilerShort.End();
            return fracturedPiece;
        }

        public static void TriggerDestruction(HkWorld world, HkRigidBody body, Vector3 havokPosition, float radius = 0.0005f)
        {
            Havok.HkdFractureImpactDetails details = Havok.HkdFractureImpactDetails.Create();
            details.SetBreakingBody(body);
            details.SetContactPoint(havokPosition);
            details.SetDestructionRadius(radius);
            details.SetBreakingImpulse(Sandbox.MyDestructionConstants.STRENGTH * 10);
            //details.SetParticlePosition(havokPosition);
            //details.SetParticleMass(1000000);
            details.Flag = details.Flag | Havok.HkdFractureImpactDetails.Flags.FLAG_DONT_RECURSE;

            Sandbox.Engine.Physics.MyPhysics.FractureImpactDetails destruction = new Sandbox.Engine.Physics.MyPhysics.FractureImpactDetails();
            destruction.Details = details;
            destruction.World = world;
            Sandbox.Engine.Physics.MyPhysics.EnqueueDestruction(destruction);
        }

        public static void TriggerDestruction(float destructionImpact, MyPhysicsBody body, Vector3D position, Vector3 normal, float maxDestructionRadius)
        {
            if (body.BreakableBody != null)
            {
                float collidingMass = body.Mass;// == 0 ? Mass : body.Mass; //fall on voxel
                float destructionRadius = Math.Min(destructionImpact / 8000, maxDestructionRadius);
                float destructionImpulse = MyDestructionConstants.STRENGTH + destructionImpact / 10000;
                float expandVelocity = Math.Min(destructionImpact / 10000, 3);

                MyPhysics.FractureImpactDetails destruction;
                HkdFractureImpactDetails details;
                details = HkdFractureImpactDetails.Create();
                details.SetBreakingBody(body.RigidBody);
                details.SetContactPoint(body.WorldToCluster(position));
                details.SetDestructionRadius(destructionRadius);
                details.SetBreakingImpulse(destructionImpulse);
                details.SetParticleExpandVelocity(expandVelocity);
                //details.SetParticleVelocity(contactVelocity);
                details.SetParticlePosition(body.WorldToCluster(position - normal * 0.25f));
                details.SetParticleMass(10000000);//collidingMass);
                details.ZeroCollidingParticleVelocity();
                details.Flag = details.Flag | HkdFractureImpactDetails.Flags.FLAG_DONT_RECURSE | HkdFractureImpactDetails.Flags.FLAG_TRIGGERED_DESTRUCTION;

                destruction = new MyPhysics.FractureImpactDetails();
                destruction.Details = details;
                destruction.World = body.HavokWorld;
                destruction.ContactInWorld = position;
                destruction.Entity = (MyEntity)body.Entity;

                MyPhysics.EnqueueDestruction(destruction);
            }
        }

        public static float MassToHavok(float m)
        {
            if (MyPerGameSettings.Destruction)
                return m * MASS_REDUCTION_COEF;
            else
                return m;
        }

        public static float MassFromHavok(float m)
        {
            if (MyPerGameSettings.Destruction)
                return m / MASS_REDUCTION_COEF;
            else
                return m;
        }
    }
}
