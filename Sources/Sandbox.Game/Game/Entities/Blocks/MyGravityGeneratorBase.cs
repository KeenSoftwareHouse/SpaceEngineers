using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Components;
using VRage.ModAPI;
using VRage.Trace;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    abstract class MyGravityGeneratorBase : MyFunctionalBlock, IMyPowerConsumer, IMyGizmoDrawableObject, IMyGravityGeneratorBase, IMyGravityProvider
    {
        protected Color m_gizmoColor = new Vector4(0, 0.1f, 0, 0.1f);
        protected const float m_maxGizmoDrawDistance = 1000.0f;

        private object m_locker = new object();

        protected bool m_oldEmissiveState = false;
        protected float m_gravityAcceleration = MyGravityProviderSystem.G;
        protected HashSet<IMyEntity> m_containedEntities = new HashSet<IMyEntity>();

        public float GravityAcceleration
        {
            get { return m_gravityAcceleration; }
            set
            {
                if (m_gravityAcceleration != value)
                {
                    m_gravityAcceleration = value;
                    PowerReceiver.Update();
                    RaisePropertiesChanged();
                }
            }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        public abstract bool IsPositionInRange(Vector3D worldPoint);
        public abstract Vector3 GetWorldGravity(Vector3D worldPoint);

        protected abstract void UpdateText();
        protected abstract float CalculateRequiredPowerInput();
        protected abstract HkShape GetHkShape();

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);
            if (CubeGrid.CreatePhysics)
            {
                MyGravityProviderSystem.AddGravityGenerator(this);

            	// Put on my fake, because it does performance issues
                if (MyFakes.ENABLE_GRAVITY_PHANTOM)
                {
                
                        var shape = CreateFieldShape();
                        Physics = new Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
                        Physics.IsPhantom = true;
                        Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center, WorldMatrix, null, Sandbox.Engine.Physics.MyPhysics.GravityPhantomLayer);
                        shape.Base.RemoveReference();
                        Physics.Enabled = IsWorking;
                }
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            }
        }

        protected void UpdateFieldShape()
        {
            if (MyFakes.ENABLE_GRAVITY_PHANTOM)
            {
                var shape = CreateFieldShape();
                Physics.RigidBody.SetShape(shape);
                shape.Base.RemoveReference();
            }

            PowerReceiver.Update();
        }

        private HkBvShape CreateFieldShape()
        {
            var phantom = new HkPhantomCallbackShape(phantom_Enter, phantom_Leave);
            var detectorShape = GetHkShape();
            return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
        }
        protected override bool CheckIsWorking()
        {
            return PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        public MyGravityGeneratorBase()
            : base()
        {
            m_baseIdleSound.Init("BlockGravityGen");
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
            PowerReceiver.Update();
        }

        public override void OnBuildSuccess(long builtBy)
        {
            PowerReceiver.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            PowerReceiver.Update();

            if (IsWorking)
            {
                foreach (IMyEntity entityInterface in m_containedEntities)
                {
                    MyEntity entity = entityInterface as MyEntity;
                    MyCharacter character = entity as MyCharacter;
                    IMyVirtualMass mass = entity as IMyVirtualMass;

                    var gravity = GetWorldGravity(entity.WorldMatrix.Translation);

                    if (mass != null && entity.Physics.RigidBody.IsActive)
                    {
                        if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_MISCELLANEOUS)
                        {
                            MyRenderProxy.DebugDrawSphere(entity.WorldMatrix.Translation, 0.2f, mass.IsWorking ? Color.Blue : Color.Red, 1.0f, false);
                        }
                        if (mass.IsWorking && entity.Physics.RigidBody.IsActive)
                            ((IMyEntity)mass.CubeGrid).Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * mass.VirtualMass, entity.WorldMatrix.Translation, null);
                    }
                    else if (!entity.Physics.IsKinematic && 
                        !entity.Physics.IsStatic &&
                        entity.Physics.RigidBody2 == null && //jn: TODO this is actualy check for large grid
                        (character == null || character.IsDead)) 
                    {
                        if (entity.Physics.RigidBody != null && entity.Physics.RigidBody.IsActive)
                            entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * entity.Physics.RigidBody.Mass, null, null);
                    }
                }
            }
        }

        protected override void Closing()
        {
            MyGravityProviderSystem.RemoveGravityGenerator(this);
            base.Closing();
        }

        protected override void OnEnabledChanged()
        {
            PowerReceiver.Update();
            base.OnEnabledChanged();
        }

        private void UpdateEmissivity()
        {
            if (IsWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Cyan, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        protected void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();

            Debug.Assert(Physics != null);
            if (Physics != null)
                Physics.Enabled = IsWorking;
            UpdateEmissivity();
            UpdateText();
        }


        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        protected void Receiver_RequiredInputChanged(MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
        }

        void phantom_Enter(HkPhantomCallbackShape sender, HkRigidBody body)
        {
            var entity = body.GetEntity(0);// jn: TODO we should collect bodies not entities
            // HACK: disabled gravity for ships (there may be more changes so I won't add Entity.RespectsGravity now)
            lock (m_locker)
            {
                if (entity != null && !(entity is MyCubeGrid))
                {
                    MyTrace.Send(TraceWindow.EntityId, string.Format("Entity entered gravity field, entity: {0}", entity));
                    m_containedEntities.Add(entity);

                    if (entity.Physics.HasRigidBody)
                        ((MyPhysicsBody)entity.Physics).RigidBody.Activate();
                }
            }
        }

        void phantom_Leave(HkPhantomCallbackShape sender, HkRigidBody body)
        {
            var entity = body.GetEntity(0);// jn: TODO we should collect bodies not entities

            lock (m_locker)
            {
                if (entity != null)
                {
                    m_containedEntities.Remove(entity);
                    MyTrace.Send(TraceWindow.EntityId, string.Format("Entity left gravity field, entity: {0}", entity));
                }
            }
        }
        public Color GetGizmoColor()
        {
            return m_gizmoColor;
        }

        public bool CanBeDrawed()
        {
            if (false == MyCubeGrid.ShowGravityGizmos || false == ShowOnHUD || false == IsWorking || false == HasLocalPlayerAccess() ||
                GetDistanceBetweenCameraAndBoundingSphere() > m_maxGizmoDrawDistance)
            {
                return false;
            }
            return Entities.Cube.MyRadioAntenna.IsRecievedByPlayer(this);
        }
        public MatrixD GetWorldMatrix()
        {
            return WorldMatrix;
        }

        public virtual BoundingBox? GetBoundingBox()
        {
            return null;
        }

        public virtual float GetRadius()
        {
            return -1;
        }
        public Vector3 GetPositionInGrid()
        {
            return this.Position;
        }

        
        public bool EnableLongDrawDistance()
        {
            return false;
        }


        public Vector3 GetWorldGravityGrid(Vector3D worldPoint)
        {
            return Vector3.Zero;
        }

        public bool IsPositionInRangeGrid(Vector3D worldPoint)
        {
            return false;
        }
    }
}
