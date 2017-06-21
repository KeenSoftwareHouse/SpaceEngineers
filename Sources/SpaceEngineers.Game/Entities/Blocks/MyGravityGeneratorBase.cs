using System.Collections.Generic;
using System.Diagnostics;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Profiler;
using VRage.Sync;
using VRage.Trace;
using VRageMath;
using VRageRender;
using Sandbox.Definitions;

namespace SpaceEngineers.Game.Entities.Blocks
{
    public abstract class MyGravityGeneratorBase : MyFunctionalBlock, IMyGizmoDrawableObject, IMyGravityGeneratorBase, IMyGravityProvider
    {
        private new MyGravityGeneratorBaseDefinition BlockDefinition
        {
            get { return (MyGravityGeneratorBaseDefinition)base.BlockDefinition; }
        }

        protected Color m_gizmoColor = new Vector4(0, 0.1f, 0, 0.1f);
        protected const float m_maxGizmoDrawDistance = 1000.0f;

        private object m_locker = new object();

        protected bool m_oldEmissiveState = false;
        protected readonly Sync<float> m_gravityAcceleration;
        protected HashSet<IMyEntity> m_containedEntities = new HashSet<IMyEntity>();

        public float GravityAcceleration
        {
            get { return m_gravityAcceleration; }
            set
            {
                if (m_gravityAcceleration != value)
                {
                    m_gravityAcceleration.Value = value;
                }
            }
        }

        public abstract bool IsPositionInRange(Vector3D worldPoint);
        public abstract Vector3 GetWorldGravity(Vector3D worldPoint);

        protected abstract void UpdateText();
        protected abstract float CalculateRequiredPowerInput();
        protected abstract HkShape GetHkShape();

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            InitializeSinkComponent();
            base.Init(objectBuilder, cubeGrid);
            if (CubeGrid.CreatePhysics)
            {
            	// Put on my fake, because it does performance issues
                if (MyFakes.ENABLE_GRAVITY_PHANTOM)
                {
                        var shape = CreateFieldShape();
                        Physics = new Sandbox.Engine.Physics.MyPhysicsBody(this, RigidBodyFlag.RBF_KINEMATIC);
                        Physics.IsPhantom = true;
                        Physics.CreateFromCollisionObject(shape, PositionComp.LocalVolume.Center, WorldMatrix, null, Sandbox.Engine.Physics.MyPhysics.CollisionLayers.GravityPhantomLayer);
                        shape.Base.RemoveReference();
                        Physics.Enabled = IsWorking;
                }
                NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

                SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
                ResourceSink.Update();
            }
            m_soundEmitter = new MyEntity3DSoundEmitter(this, true);
            m_baseIdleSound.Init("BlockGravityGen");
			
        }

	    protected abstract void InitializeSinkComponent();

        protected void UpdateFieldShape()
        {
            if (MyFakes.ENABLE_GRAVITY_PHANTOM && Physics != null)
            {
                var shape = CreateFieldShape();
                Physics.RigidBody.SetShape(shape);
                shape.Base.RemoveReference();
            }

			ResourceSink.Update();
        }

        private HkBvShape CreateFieldShape()
        {
            var phantom = new HkPhantomCallbackShape(phantom_Enter, phantom_Leave);
            var detectorShape = GetHkShape();
            return new HkBvShape(detectorShape, phantom, HkReferencePolicy.TakeOwnership);
        }
        protected override bool CheckIsWorking()
        {
            return (ResourceSink != null ? ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) : true) && base.CheckIsWorking();
        }

        public MyGravityGeneratorBase()
            : base()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_gravityAcceleration = SyncType.CreateAndAddProp<float>();
#endif // XB1
            m_gravityAcceleration.ValueChanged += (x) => AccelerationChanged();
        }

        void AccelerationChanged()
        {
            ResourceSink.Update();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            MyGravityProviderSystem.AddGravityGenerator(this);
            UpdateEmissivity();

			if(ResourceSink != null)
				ResourceSink.Update();
        }
        public override void OnRemovedFromScene(object source)
        {
            MyGravityProviderSystem.RemoveGravityGenerator(this);
            base.OnRemovedFromScene(source);
        }

        public override void OnBuildSuccess(long builtBy)
        {
			ResourceSink.Update();
            base.OnBuildSuccess(builtBy);
        }

        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();
			
			if(ResourceSink != null)
				ResourceSink.Update();

            if (IsWorking)
            {
                foreach (IMyEntity entityInterface in m_containedEntities)
                {
                    MyEntity entity = entityInterface as MyEntity;
                    MyCharacter character = entity as MyCharacter;
                    IMyVirtualMass mass = entity as IMyVirtualMass;
					
					var naturalGravityMultiplier = MyGravityProviderSystem.CalculateHighestNaturalGravityMultiplierInPoint(entity.WorldMatrix.Translation);
					var gravity = GetWorldGravity(entity.WorldMatrix.Translation) * MyGravityProviderSystem.CalculateArtificialGravityStrengthMultiplier(naturalGravityMultiplier);

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
                        character == null) 
                    {
                        if (entity.Physics.RigidBody != null && entity.Physics.RigidBody.IsActive)
                        {
                            //<ib.change> increase gravity for floating objects
                            //entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * entity.Physics.RigidBody.Mass, null, null);
                            if (entity is MyFloatingObject)
                            {
                                var floatingEntity = entity as MyFloatingObject;
                                float w = (floatingEntity.HasConstraints()) ? 2.0f : 1.0f;
                                entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, w * gravity * entity.Physics.RigidBody.Mass, null, null);                                
                            }
                            else
                            {
                                entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * entity.Physics.RigidBody.Mass, null, null);
                            }                   
                            
                        }
                    }
                }
            }
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
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

		protected void Receiver_RequiredInputChanged(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver, float oldRequirement, float newRequirement)
        {
            UpdateText();
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        void phantom_Enter(HkPhantomCallbackShape sender, HkRigidBody body)
        {
            ProfilerShort.Begin("GravityEnter");
            var entity = MyPhysicsExtensions.GetEntity(body, 0);// jn: TODO we should collect bodies not entities
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
            ProfilerShort.End();
        }

        void phantom_Leave(HkPhantomCallbackShape sender, HkRigidBody body)
        {
            ProfilerShort.Begin("GravityLeave");
            var entity = MyPhysicsExtensions.GetEntity(body, 0);// jn: TODO we should collect bodies not entities

            lock (m_locker)
            {
                if (entity != null)
                {
                    m_containedEntities.Remove(entity);
                    MyTrace.Send(TraceWindow.EntityId, string.Format("Entity left gravity field, entity: {0}", entity));
                }
            }
            ProfilerShort.End();
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
            return Sandbox.Game.Entities.Cube.MyRadioAntenna.IsRecievedByPlayer(this);
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

		public float GetGravityMultiplier(Vector3D worldPoint)
		{
			return (IsPositionInRange(worldPoint) ? 1.0f : 0.0f);
		}

        #region ModAPI
        float ModAPI.IMyGravityGeneratorBase.Gravity
        {
            get { return GravityAcceleration / MyGravityProviderSystem.G; }
            set { GravityAcceleration = value * MyGravityProviderSystem.G; }
        }

        float ModAPI.IMyGravityGeneratorBase.GravityAcceleration
        {
            get { return GravityAcceleration; }
            set { GravityAcceleration = value; }
        }

        float ModAPI.Ingame.IMyGravityGeneratorBase.Gravity
        {
            get { return GravityAcceleration / MyGravityProviderSystem.G; }
            set { GravityAcceleration = MathHelper.Clamp(value * MyGravityProviderSystem.G, BlockDefinition.MinGravityAcceleration, BlockDefinition.MaxGravityAcceleration); }
        }
        
        float ModAPI.Ingame.IMyGravityGeneratorBase.GravityAcceleration
        {
            get { return GravityAcceleration; }
            set { GravityAcceleration = MathHelper.Clamp(value, BlockDefinition.MinGravityAcceleration, BlockDefinition.MaxGravityAcceleration); }
        }

        bool ModAPI.Ingame.IMyGravityGeneratorBase.IsPositionInRange(Vector3D worldPoint)
        {
            return IsPositionInRange(worldPoint);
        }

        Vector3 ModAPI.Ingame.IMyGravityGeneratorBase.GetWorldGravity(Vector3D worldPoint)
        {
            return GetWorldGravity(worldPoint);
        }

        #endregion ModAPI
    }
}
