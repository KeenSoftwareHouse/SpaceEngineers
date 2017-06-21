using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Utils;
using VRage.Network;
using VRage.ModAPI;
using VRage.Game.Entity;
using Sandbox.Engine.Multiplayer;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Profiler;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_FracturedPiece))]
    public class MyFracturedPiece : MyEntity, IMyDestroyableObject, IMyEventProxy
    {
        private static List<HkdShapeInstanceInfo> m_tmpInfos = new List<HkdShapeInstanceInfo>();

        public event Action<MyEntity> OnRemove;

        public new MyRenderComponentFracturedPiece Render { get { return (MyRenderComponentFracturedPiece)base.Render; } }
        public HkdBreakableShape Shape;

        public class HitInfo
        {
            public Vector3D Position;
            public Vector3 Impulse;
        }
        public HitInfo InitialHit;

        private float m_hitPoints;

        public List<MyDefinitionId> OriginalBlocks = new List<MyDefinitionId>();
        private List<HkdShapeInstanceInfo> m_children = new List<HkdShapeInstanceInfo>();

        private List<MyObjectBuilder_FracturedPiece.Shape> m_shapes = new List<MyObjectBuilder_FracturedPiece.Shape>();
        private List<HkdShapeInstanceInfo> m_shapeInfos = new List<HkdShapeInstanceInfo>();

        private MyTimeSpan m_markedBreakImpulse = MyTimeSpan.Zero;
        private HkEasePenetrationAction m_easePenetrationAction;

        private MyEntity3DSoundEmitter m_soundEmitter = null;
        private DateTime m_soundStart;
        private bool m_obstacleContact = false;
        private bool m_groundContact = false;
        private Sync<bool> m_fallSoundShouldPlay;
        private MySoundPair m_fallSound = null;
        private Sync<string> m_fallSoundString;
        private bool m_contactSet = false;
        public readonly SyncType SyncType;

        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }

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
            UseDamageSystem = false;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
#if !XB1 // !XB1_SYNC_NOREFLECTION
            SyncType = SyncHelpers.Compose(this);
#else // XB1
            SyncType = new SyncType(new List<SyncBase>());
            m_fallSoundShouldPlay = SyncType.CreateAndAddProp<bool>();
            m_fallSoundString = SyncType.CreateAndAddProp<string>();
#endif // XB1
            m_fallSoundShouldPlay.Value = false;
            m_fallSoundString.Value = "";
            m_fallSoundString.ValueChanged += (x) => SetFallSound();
        }

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

                int childrenCount = m_children.Count;
                for (int i=0; i<childrenCount; ++i)
                {
                    var child = m_children[i];
                    if (string.IsNullOrEmpty(child.ShapeName))
                    {
                        child.GetChildren(m_children);
                    }
                }

                foreach (var child in m_children)
                {
                    var shapeName = child.ShapeName;
                    if (string.IsNullOrEmpty(shapeName))
                        continue;

                    var shape = new MyObjectBuilder_FracturedPiece.Shape()
                    {
                        Name = shapeName,
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

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            var ob = objectBuilder as MyObjectBuilder_FracturedPiece;
            if (ob.Shapes.Count == 0)
            {
                return;
                //Debug.Fail("Invalid fracture piece! Dont call init without valid OB. Use pool/noinit.");
                //throw new Exception("Fracture piece has no shapes."); //throwing exception, otherwise there is fp with null physics which can mess up somwhere else
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
                if (VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                    MyDestructionData.Static.LoadModelDestruction(model, mdef, Vector3.One);
                var shape = VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
                var si = new HkdShapeInstanceInfo(shape, null, null);
                m_children.Add(si);
                shape.GetChildren(m_children);

                if (blockDef != null && blockDef.BuildProgressModels != null)
                {
                    foreach (var progress in blockDef.BuildProgressModels)
                    {
                        model = progress.File;
                        if (VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes == null)
                            MyDestructionData.Static.LoadModelDestruction(model, blockDef, Vector3.One);
                        shape = VRage.Game.Models.MyModels.GetModelOnlyData(model).HavokBreakableShapes[0];
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
                List<string> shapesToLoad = new List<string>();
                foreach (var obShape in ob.Shapes)
                    shapesToLoad.Add(obShape.Name);

                var shapesStr = shapesToLoad.Aggregate((str1, str2) => str1 + ", " + str2);
                var blocksStr = OriginalBlocks.Aggregate("", (str, defId) => str + ", " + defId.ToString());
                var failMsg = "No relevant shape was found for fractured piece. It was probably reexported and names changed. Shapes: " + shapesStr + ". Original blocks: " + shapesStr;

                Debug.Fail(failMsg);
                //HkdShapeInstanceInfo si = new HkdShapeInstanceInfo(new HkdBreakableShape((HkShape)new HkBoxShape(Vector3.One)), Matrix.Identity);
                //m_shapeInfos.Add(si);
                throw new Exception(failMsg);
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
                Physics.CanUpdateAccelerations = true;
                Physics.InitialSolverDeactivation = HkSolverDeactivation.High;
                Physics.CreateFromCollisionObject(Shape.GetShape(), Vector3.Zero, PositionComp.WorldMatrix, mp);
                Physics.BreakableBody = new HkdBreakableBody(Shape, Physics.RigidBody, null, (Matrix)PositionComp.WorldMatrix);
                Physics.BreakableBody.AfterReplaceBody += Physics.FracturedBody_AfterReplaceBody;

                if (OriginalBlocks.Count > 0)
                {
                    MyPhysicalModelDefinition def;
                    if (MyDefinitionManager.Static.TryGetDefinition<MyPhysicalModelDefinition>(OriginalBlocks[0], out def))
                        Physics.MaterialType = def.PhysicalMaterial.Id.SubtypeId;
                }


                var rigidBody = Physics.RigidBody;
                bool isFixed = MyDestructionHelper.IsFixed(Physics.BreakableBody.BreakableShape);
                if (isFixed)
                {
                    rigidBody.UpdateMotionType(HkMotionType.Fixed);
                    rigidBody.LinearVelocity = Vector3.Zero;
                    rigidBody.AngularVelocity = Vector3.Zero;
                }


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
                        rigidBody.Layer = MyPhysics.CollisionLayers.DefaultCollisionLayer;
                }
                else
                    rigidBody.Layer = MyPhysics.CollisionLayers.DefaultCollisionLayer;
            }
            else
                rigidBody.Layer = MyPhysics.CollisionLayers.StaticCollisionLayer;
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

            var grav = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.GetPosition());
            Physics.RigidBody.Gravity = grav;
        }

        public void RegisterObstacleContact(ref HkContactPointEvent e)
        {
            if (m_obstacleContact == false && m_fallSoundShouldPlay.Value == true && (DateTime.UtcNow - m_soundStart).TotalSeconds >= 1f)
            {
                m_obstacleContact = true;
            }
        }

        private void SetFallSound()
        {
            if (OriginalBlocks != null && OriginalBlocks[0].TypeId.ToString().Equals("MyObjectBuilder_Tree"))
            {
                m_fallSound = new MySoundPair(m_fallSoundString.Value);
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public void StartFallSound(string sound)
        {
            m_groundContact = false;
            m_obstacleContact = false;
            m_fallSoundString.Value = sound;
            m_soundStart = DateTime.UtcNow;
            m_fallSoundShouldPlay.Value = true;
            if (m_contactSet == false && (MySandboxGame.IsDedicated || MyMultiplayer.Static == null || MyMultiplayer.Static.IsServer))
                Physics.RigidBody.ContactSoundCallback += RegisterObstacleContact;
            m_contactSet = true;
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            if (m_markedBreakImpulse != MyTimeSpan.Zero)
                UnmarkEntityBreakable(true);

            //restart falling sound if it starts moving again
            if (m_fallSoundShouldPlay.Value == false && this.Physics.LinearVelocity.LengthSquared() > 25f && (DateTime.UtcNow - m_soundStart).TotalSeconds >= 1f)
            {
                m_fallSoundShouldPlay.Value = true;
                m_obstacleContact = false;
                m_groundContact = false;
            }
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            //Sound control
            if (MySandboxGame.IsDedicated == false)
            {
                if (m_fallSoundShouldPlay.Value == true)
                {
                    if (m_soundEmitter == null)
                    {
                        m_soundEmitter = new MyEntity3DSoundEmitter(this);
                        //NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
                    }
                    if (m_soundEmitter.IsPlaying == false && m_fallSound != null && m_fallSound != MySoundPair.Empty)
                        m_soundEmitter.PlaySound(m_fallSound, true, true);
                }
                else
                {
                    if (m_soundEmitter != null && m_soundEmitter.IsPlaying)
                        m_soundEmitter.StopSound(false);
                }
            }

            //contact was made
            if (m_obstacleContact && m_groundContact == false)
            {
                if (Physics.LinearVelocity.Y > 0f || Physics.LinearVelocity.LengthSquared() < 9f)
                {
                    m_groundContact = true;
                    m_fallSoundShouldPlay.Value = false;//start crash sound
                    m_soundStart = DateTime.UtcNow;
                }
                else
                {
                    m_obstacleContact = false;//scratching
                }
            }
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (m_soundEmitter != null)
            {
                m_soundEmitter.Update();
                if (m_soundEmitter.IsPlaying && (DateTime.UtcNow - m_soundStart).TotalSeconds >= 15f)//stop falling sound if it playing too long
                    m_fallSoundShouldPlay.Value = false;
            }

			var grav = MyGravityProviderSystem.CalculateTotalGravityInPoint(PositionComp.GetPosition());
            Physics.RigidBody.Gravity = grav;
        }
        private void UnmarkEntityBreakable(bool checkTime)
        {
            if (m_markedBreakImpulse != MyTimeSpan.Zero && (!checkTime || MySandboxGame.Static.UpdateTime - m_markedBreakImpulse > MyTimeSpan.FromSeconds(1.5)))
            {
                m_markedBreakImpulse = MyTimeSpan.Zero;
                if (Physics != null && Physics.HavokWorld != null)
                {
                    Physics.HavokWorld.BreakOffPartsUtil.UnmarkEntityBreakable(Physics.RigidBody);
                    if (checkTime)
                        CreateEasyPenetrationAction(1f);
                }
            }
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            MyCubeBlockDefinition firstBlockDef = null;
            if (Physics.HavokWorld != null && OriginalBlocks.Count != 0 && MyDefinitionManager.Static.TryGetCubeBlockDefinition(OriginalBlocks[0], out firstBlockDef) && firstBlockDef.CubeSize == MyCubeSize.Large)
            {
                var impulse = Physics.Mass * 0.4f;
                Physics.HavokWorld.BreakOffPartsUtil.MarkEntityBreakable(Physics.RigidBody, impulse);
                m_markedBreakImpulse = MySandboxGame.Static.UpdateTime;
            }
        }

        public override void OnRemovedFromScene(object source)
        {
            base.OnRemovedFromScene(source);

            UnmarkEntityBreakable(false);
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);

            if (OnRemove != null)
                OnRemove(this);
        }


        private void CreateEasyPenetrationAction(float duration)
        {
            if (Physics != null && Physics.RigidBody != null)
            {
                m_easePenetrationAction = new HkEasePenetrationAction(Physics.RigidBody, duration);
                m_easePenetrationAction.InitialAllowedPenetrationDepthMultiplier = 5f;
                m_easePenetrationAction.InitialAdditionalAllowedPenetrationDepth = 2f;
            }
        }

        class MyFracturedPieceDebugDraw : MyDebugRenderComponentBase
        {
            MyFracturedPiece m_piece;
            public MyFracturedPieceDebugDraw(MyFracturedPiece piece)
            {
                m_piece = piece;
            }

            public override void DebugDraw()
            {
                if (MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES)
                {
                    VRageRender.MyRenderProxy.DebugDrawAxis(m_piece.WorldMatrix, 1, false);

                    if (m_piece.Physics != null && m_piece.Physics.RigidBody != null)
                    {
                        MyPhysicsBody pb = m_piece.Physics as MyPhysicsBody;
                        HkRigidBody rb = pb.RigidBody;

                        Vector3 center = pb.ClusterToWorld(rb.CenterOfMassWorld);// tohle uz je pozice stredu rigid body
                        BoundingBoxD bbox = new BoundingBoxD(center - Vector3D.One * 0.1f, center + Vector3D.One * 0.1f);

                        if (!Sync.IsServer)
                        {
                            if (m_piece.m_serverPosition != Vector3.Zero)
                            {
                                BoundingBoxD bbox1 = new BoundingBoxD(m_piece.m_serverPosition - Vector3D.One * 0.1f, m_piece.GetBaseEntity().m_serverPosition + Vector3D.One * 0.1f);
                                VRageRender.MyRenderProxy.DebugDrawAABB(bbox1, Color.Yellow, 1.0f, 1.0f, false);

                                BoundingBoxD bbox2 = new BoundingBoxD(m_piece.PositionComp.WorldMatrix.Translation - Vector3D.One * 0.1f, m_piece.PositionComp.WorldMatrix.Translation + Vector3D.One * 0.1f);
                                VRageRender.MyRenderProxy.DebugDrawAABB(bbox2, Color.YellowGreen, 1.0f, 1.0f, false);

                                VRageRender.MyRenderProxy.DebugDrawLine3D(m_piece.GetBaseEntity().m_serverPosition, m_piece.PositionComp.WorldMatrix.Translation, Color.Yellow, Color.YellowGreen, false);
                            }
                        }

                        string str = String.Format("{0}\n, {1}\n{2}", rb.GetMotionType(), pb.Friction, pb.Entity.EntityId.ToString().Substring(0,5));
                        VRageRender.MyRenderProxy.DebugDrawText3D(center, str, Color.White, 0.6f, false);
                    }
                }
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

        public bool DoDamage(float damage, MyStringHash damageType, bool sync, MyHitInfo? hitInfo, long attackerId)
        {
            if (Sync.IsServer)
            {
                MyDamageInformation info = new MyDamageInformation(false, damage, damageType, attackerId);
                if (UseDamageSystem)
                    MyDamageSystem.Static.RaiseBeforeDamageApplied(this, ref info);

                m_hitPoints -= info.Amount;

                if (UseDamageSystem)
                    MyDamageSystem.Static.RaiseAfterDamageApplied(this, info);

                if (m_hitPoints <= 0)
                {
                    MyFracturedPiecesManager.Static.RemoveFracturePiece(this, 2);

                    if (UseDamageSystem)
                        MyDamageSystem.Static.RaiseDestroyed(this, info);
                }
            }
            return true;
        }

        public float Integrity
        {
            get { return m_hitPoints; }
        }

        public bool UseDamageSystem { get; private set; }

        public void DebugCheckValidShapes()
        {
            bool hasBlock = false;
            HashSet<Tuple<string, float>> shapeNamesAndProgress = new HashSet<Tuple<string, float>>();
            HashSet<Tuple<string, float>> shapeNamesAndProgressInShape = new HashSet<Tuple<string, float>>();
            foreach (var defId in OriginalBlocks)
            {
                MyCubeBlockDefinition blockDef;
                if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDef))
                {
                    hasBlock = true;
                    MyFracturedBlock.GetAllBlockBreakableShapeNames(blockDef, shapeNamesAndProgress);
                }
            }

            MyFracturedBlock.GetAllBlockBreakableShapeNames(Shape, shapeNamesAndProgressInShape, 0);

            // Check
            foreach (var tupleInShape in shapeNamesAndProgressInShape)
            {
                bool found = false;
                foreach (var tuple in shapeNamesAndProgress)
                {
                    if (tupleInShape.Item1 == tuple.Item1)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && hasBlock && !tupleInShape.Item1.ToLower().Contains("compound"))
                {
                    Debug.Fail("Found shape which is not in any definition: " + tupleInShape.Item1);
                }
            }
        }
    }
}
