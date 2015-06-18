using Havok;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Components;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_FracturedPiece))]
    public class MyFracturedPiece : MyEntity, IMyDestroyableObject
    {
        public class HitInfo
        {
            public Vector3D Position;
            public Vector3 Impulse;
        }

        public new MyRenderComponentFracturedPiece Render { get { return (MyRenderComponentFracturedPiece)base.Render; } }
        public HkdBreakableShape Shape;

        public HitInfo InitialHit;

        private static List<HkdShapeInstanceInfo> m_tmpInfos = new List<HkdShapeInstanceInfo>();

        private float m_hitPoints;
        public MyFracturedPiece()
            : base()
        {
            //EntityId = MyEntityIdentifier.AllocateId();

            //TODO: Synchronize through manager to avoid performance hits
            SyncFlag = true;

            base.PositionComp = new MyFracturePiecePositionComponent();
            base.Render = new MyRenderComponentFracturedPiece();
            base.Render.NeedsDraw = true;
            base.Render.PersistentFlags = MyPersistentEntityFlags2.Enabled;
            AddDebugRenderComponent(new MyFracturedPieceDebugDraw(this));
        }

        public List<MyDefinitionId> OriginalBlocks = new List<MyDefinitionId>();
        private List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var old = base.GetObjectBuilder(copy);
            var ob = old as MyObjectBuilder_FracturedPiece;
            foreach (var def in OriginalBlocks)
                ob.BlockDefinitions.Add(def);
            if (Physics == null)
            {
                Debug.Assert(m_shapes.Count > 0, "Saving invalid piece!");
                foreach (var shape in m_shapes)
                    ob.Shapes.Add(new MyObjectBuilder_FracturedPiece.Shape() { Name = shape.Name, Orientation = shape.Orientation });
                return ob;
            }
            if (Physics.BreakableBody.BreakableShape.IsCompound() || string.IsNullOrEmpty(Physics.BreakableBody.BreakableShape.Name))
            {
                Physics.BreakableBody.BreakableShape.GetChildren(m_children);
                if (m_children.Count == 0)
                {
                    Debug.Fail("Saiving invalid piece!");
                    return ob;
                }
                foreach (var child in m_children)
                {
                    var shape = new MyObjectBuilder_FracturedPiece.Shape()
                    {
                        Name = child.ShapeName,
                        Orientation = Quaternion.CreateFromRotationMatrix(child.GetTransform().GetOrientation()),
                        Fixed = MyDestructionHelper.IsFixed(child.Shape)
                    };
                    ob.Shapes.Add(shape);
                }

                if (Physics.IsInWorld)
                {
                    var fixedPosition = Physics.ClusterToWorld(Vector3.Transform(m_children[0].GetTransform().Translation, Physics.RigidBody.GetRigidBodyMatrix()));
                    var posOr = ob.PositionAndOrientation.Value;
                    posOr.Position = fixedPosition;
                    ob.PositionAndOrientation = posOr;
                }
                m_children.Clear();
            }
            else
            {
                ob.Shapes.Add(new MyObjectBuilder_FracturedPiece.Shape() { Name = Physics.BreakableBody.BreakableShape.Name });
            }
            Debug.Assert(ob.Shapes.Count > 0, "Saiving invalid piece!");
            return ob;
        }

        List<MyObjectBuilder_FracturedPiece.Shape> m_shapes = new List<MyObjectBuilder_FracturedPiece.Shape>();
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            var ob = objectBuilder as MyObjectBuilder_FracturedPiece;
            if (ob.Shapes.Count == 0)
            {
                Debug.Fail("Invalid fracture piece! Dont call init without valid OB. Use pool/noinit.");
                throw new Exception("Fracture piece has no shapes."); //throwing exception, otherwise there is fp with null physics which can mess up somwhere else
            }

            foreach (var shape in ob.Shapes)
            {
                Render.AddPiece(shape.Name, Matrix.CreateFromQuaternion(shape.Orientation));
            }
            OriginalBlocks.Clear();
            foreach (var def in ob.BlockDefinitions)
            {
                string model = null;
                MyPhysicalModelDefinition mdef;
                if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(def, out mdef))
                    model = mdef.Model;
                MyCubeBlockDefinition blockDef = null;
                MyDefinitionManager.Static.TryGetDefinition<MyCubeBlockDefinition>(def, out blockDef);

                if (model == null)
                {
                    Debug.Fail("Fracture piece Definition not found");
                    continue;
                }

                model = mdef.Model;
                if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, mdef, false, Vector3.One);
                var shape = MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                var si = new HkdShapeInstanceInfo(shape, null, null);
                m_children.Add(si);
                shape.GetChildren(m_children);

                if (blockDef != null && blockDef.BuildProgressModels != null)
                {
                    foreach (var progress in blockDef.BuildProgressModels)
                    {
                        model = progress.File;
                        if (MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                            MyDestructionData.Static.LoadModelDestruction(model, blockDef, false, Vector3.One);
                        shape = MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                        si = new HkdShapeInstanceInfo(shape, null, null);
                        m_children.Add(si);
                        shape.GetChildren(m_children);
                    }
                }

                OriginalBlocks.Add(def);
            }
            m_shapes.AddRange(ob.Shapes);

            Vector3? offset = null;
            int shapeAtZero = 0;
            for (int i = 0; i < m_children.Count; i++)
            {
                var child = m_children[i];
                Func<MyObjectBuilder_FracturedPiece.Shape, bool> x = s => s.Name == child.ShapeName;
                var result = m_shapes.Where(x);
                if (result.Count() > 0)
                {
                    var found = result.First();
                    var m = Matrix.CreateFromQuaternion(found.Orientation);
                    if (!offset.HasValue && found.Name == m_shapes[0].Name)
                    {
                        offset = child.GetTransform().Translation;
                        shapeAtZero = m_shapeInfos.Count;
                    }
                    m.Translation = child.GetTransform().Translation;
                    var si = new HkdShapeInstanceInfo(child.Shape.Clone(), m);
                    if(found.Fixed)
                        si.Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                    m_shapeInfos.Add(si);
                    m_shapes.Remove(found);
                }
                else
                {
                    child.GetChildren(m_children);
                }
            }

            if (m_shapeInfos.Count == 0)
            {
                Debug.Fail("No relevant shape was found for fractured piece. It was probably reexported and names changed.");
                //HkdShapeInstanceInfo si = new HkdShapeInstanceInfo(new HkdBreakableShape((HkShape)new HkBoxShape(Vector3.One)), Matrix.Identity);
                //m_shapeInfos.Add(si);
                throw new Exception("No relevant shape was found for fractured piece. It was probably reexported and names changed.");
            }

            if (offset.HasValue)
            {
                for (int i = 0; i < m_shapeInfos.Count; i++)
                {
                    var m = m_shapeInfos[i].GetTransform();
                    m.Translation -= offset.Value;
                    m_shapeInfos[i].SetTransform(ref m);
                }
                {
                    var m = m_shapeInfos[shapeAtZero].GetTransform();
                    m.Translation = Vector3.Zero;
                    m_shapeInfos[shapeAtZero].SetTransform(ref m);
                }
            }

            if (m_shapeInfos.Count > 0)
            {
                if (m_shapeInfos.Count == 1)
                    Shape = m_shapeInfos[0].Shape;
                else
                {
                    Shape = new HkdCompoundBreakableShape(null, m_shapeInfos);
                    ((HkdCompoundBreakableShape)Shape).RecalcMassPropsFromChildren();
                }
                Shape.SetStrenght(MyDestructionConstants.STRENGTH);
                var mp = new HkMassProperties();
                Shape.BuildMassProperties(ref mp);
                Shape.SetChildrenParent(Shape);
                Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DEBRIS);
                Physics.InitialSolverDeactivation = HkSolverDeactivation.Medium;
                Physics.CreateFromCollisionObject(Shape.GetShape(), Vector3.Zero, PositionComp.WorldMatrix, mp);
                Physics.BreakableBody = new HkdBreakableBody(Shape, Physics.RigidBody, MyPhysics.SingleWorld.DestructionWorld, (Matrix)PositionComp.WorldMatrix);
                Physics.BreakableBody.AfterReplaceBody += Physics.FracturedBody_AfterReplaceBody;

                var rigidBody = Physics.RigidBody;
                bool isFixed = MyDestructionHelper.IsFixed(Physics.BreakableBody.BreakableShape);
                if (isFixed)
                {
                    rigidBody.UpdateMotionType(HkMotionType.Fixed);
                    rigidBody.LinearVelocity = Vector3.Zero;
                    rigidBody.AngularVelocity = Vector3.Zero;
                }

                //Cannot set keyframed in constructor, because Havok does not allow set CoM on kinematic object..
                if(!Sync.IsServer)
                    Physics.RigidBody.UpdateMotionType(HkMotionType.Keyframed);
                Physics.Enabled = true;
            }
            m_children.Clear();
            m_shapeInfos.Clear();
        }


        internal void InitFromBreakableBody(HkdBreakableBody b, MatrixD worldMatrix, MyCubeBlock block)
        {
            ProfilerShort.Begin("RemoveGen&SetFixed");

            OriginalBlocks.Clear();
            if (block != null)
            {
                if (block is MyCompoundCubeBlock)
                {
                    foreach (var block2 in (block as MyCompoundCubeBlock).GetBlocks())
                        OriginalBlocks.Add(block2.BlockDefinition.Id);
                }
                else if (block is MyFracturedBlock)
                {
                    OriginalBlocks.AddRange((block as MyFracturedBlock).OriginalBlocks);
                }
                else
                {
                    OriginalBlocks.Add(block.BlockDefinition.Id);
                }
            }

            var rigidBody = b.GetRigidBody();
            bool isFixed = MyDestructionHelper.IsFixed(b.BreakableShape);
            if (isFixed)
            {
                rigidBody.UpdateMotionType(HkMotionType.Fixed);
                rigidBody.LinearVelocity = Vector3.Zero;
                rigidBody.AngularVelocity = Vector3.Zero;
            }

            ProfilerShort.Begin("Sync");
            if (SyncFlag)
            {
                CreateSync();
            }
            ProfilerShort.End();

            PositionComp.WorldMatrix = worldMatrix;
            Physics.Flags = isFixed ? RigidBodyFlag.RBF_STATIC : RigidBodyFlag.RBF_DEBRIS;
            Physics.BreakableBody = b;
            rigidBody.UserObject = Physics;
            if (!isFixed)
            {
                rigidBody.Motion.SetDeactivationClass(HkSolverDeactivation.High);
                rigidBody.EnableDeactivation = true;
                if (MyFakes.REDUCE_FRACTURES_COUNT)
                {
                    if (b.BreakableShape.Volume < 1 && MyRandom.Instance.Next(6) > 1)
                        rigidBody.Layer = MyFracturedPiecesManager.FakePieceLayer;
                    else
                        rigidBody.Layer = MyPhysics.DefaultCollisionLayer;
                }
                else
                    rigidBody.Layer = MyPhysics.DefaultCollisionLayer;
            }
            else
                rigidBody.Layer = MyPhysics.StaticCollisionLayer;
            Physics.BreakableBody.AfterReplaceBody += Physics.FracturedBody_AfterReplaceBody;

            if(OriginalBlocks.Count > 0)
            {
                MyPhysicalModelDefinition def;
                if(MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(OriginalBlocks[0],out def))
                    Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;
            }

            ProfilerShort.BeginNextBlock("Enable");
            Physics.Enabled = true;
            MyDestructionHelper.FixPosition(this);
            SetDataFromHavok(b.BreakableShape);
            var coml = b.GetRigidBody().CenterOfMassLocal;
            var comw = b.GetRigidBody().CenterOfMassWorld;
            var com = b.BreakableShape.CoM;
            b.GetRigidBody().CenterOfMassLocal = com;
            b.BreakableShape.RemoveReference();
            ProfilerShort.End();

        }

        /// <summary>
        /// Sets model from havok to render component of this entity.
        /// </summary>
        public void SetDataFromHavok(HkdBreakableShape shape)
        {
            ProfilerShort.Begin("FP.SetDataFromHavok");
            Shape = shape;
            Shape.AddReference();
            if (Render != null)
            {
                if (shape.IsCompound() || string.IsNullOrEmpty(shape.Name))
                {
                    shape.GetChildren(m_shapeInfos);
                    Debug.Assert(m_shapeInfos.Count > 0);
                    foreach (var shapeInstanceInfo in m_shapeInfos)
                    {
                        //System.Diagnostics.Debug.Assert(shapeInstanceInfo.IsValid(), "Invalid shapeInstanceInfo!");
                        if (shapeInstanceInfo.IsValid())
                        {
                            Render.AddPiece(shapeInstanceInfo.ShapeName, shapeInstanceInfo.GetTransform());
                        }
                    }

                    m_shapeInfos.Clear();
                }
                else
                    Render.AddPiece(shape.Name, Matrix.Identity);
            }
            ProfilerShort.End();

            m_hitPoints = Shape.Volume * 100;
        }

        List<HkdShapeInstanceInfo> m_shapeInfos = new List<HkdShapeInstanceInfo>();

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            Physics.Enabled = true;
            Physics.RigidBody.Activate();
            Physics.RigidBody.ContactPointCallbackDelay = 0;
            Physics.RigidBody.ContactSoundCallbackEnabled = true;

            //return;
            if (InitialHit != null)
            {
                Physics.ApplyImpulse(InitialHit.Impulse, Physics.CenterOfMassWorld);

                MyPhysics.FractureImpactDetails fid = new Sandbox.Engine.Physics.MyPhysics.FractureImpactDetails();
                fid.Entity = this;
                fid.World = Physics.HavokWorld;
                fid.ContactInWorld = InitialHit.Position;

                HkdFractureImpactDetails details = HkdFractureImpactDetails.Create();
                details.SetBreakingBody(Physics.RigidBody);
                details.SetContactPoint(Physics.WorldToCluster(InitialHit.Position));
                details.SetDestructionRadius(0.05f);
                details.SetBreakingImpulse(30000);
                details.SetParticleVelocity(InitialHit.Impulse);
                details.SetParticlePosition(Physics.WorldToCluster(InitialHit.Position));
                details.SetParticleMass(500);

                fid.Details = details;
                MyPhysics.EnqueueDestruction(fid);
            }
        }

        protected override void Closing()
        {
            base.Closing();
            //if(Shape.IsValid())
            //    Shape.RemoveReference();
        }

        class MyFracturedPieceDebugDraw : MyDebugRenderComponentBase
        {
            MyFracturedPiece m_piece;
            public MyFracturedPieceDebugDraw(MyFracturedPiece piece)
            {
                m_piece = piece;
            }

            public override bool DebugDraw()
            {
                if (MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES)
                {
                    VRageRender.MyRenderProxy.DebugDrawAxis(m_piece.WorldMatrix, 1, false);
                }
                return false;
            }

            public override void DebugDrawInvalidTriangles()
            {
            }
        }

        public void OnDestroy()
        {
            if (Sync.IsServer)
            {
                MyFracturedPiecesManager.Static.RemoveFracturePiece(this, 2);
            }
        }

        public void DoDamage(float damage, Common.ObjectBuilders.Definitions.MyDamageType damageType, bool sync, MyHitInfo? hitInfo)
        {
            if (Sync.IsServer)
            {
                m_hitPoints -= damage;
                if (m_hitPoints <= 0)
                {
                    MyFracturedPiecesManager.Static.RemoveFracturePiece(this, 2);
                }
            }
        }

        public float Integrity
        {
            get { return m_hitPoints; }
        }

    }
}
