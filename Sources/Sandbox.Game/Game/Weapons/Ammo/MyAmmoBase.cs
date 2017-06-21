using System.Text;
using VRageMath;

using Sandbox.Engine.Physics;

using Sandbox.Game.Entities;
using Sandbox.Engine.Utils;
using Sandbox.Game.Utils;
using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common;
using System;

using VRage.ModAPI;
using VRage.Game.Components;
using VRage;
using VRage.Game.Entity;
using VRage.Game;


namespace Sandbox.Game.Weapons
{
    public class MyAmmoBase : MyEntity
    {
        //  IMPORTANT: This class isn't realy inicialized by constructor, but by Start()
        //  So don't initialize members here, do it in Start()

        protected Vector3D m_previousPosition; //last position 
        protected Vector3D m_origin; //start position 
        protected Vector3D m_initialVelocity; //starting velocity 
        protected int m_elapsedMiliseconds;  //milliseconds from start
        protected int m_cascadeExplosionLevel; //to reduce the range of the cascaded explosions
        protected MyWeaponDefinition m_weaponDefinition;
        protected bool m_canByAffectedByExplosionForce = true;        
        //public bool IsDummy { get; set; }

        protected float m_ammoOffsetSize = 0.0f;

        protected bool m_isExploded = false;

        /// <summary>
        /// Need per frame position updates in multiplayer
        /// </summary>
        public bool GuidedInMultiplayer { get; set; }

        public int CascadedExplosionLevel { get { return m_cascadeExplosionLevel; } }

        public bool MarkedToDestroy = false;

        /// <summary>
        /// (optional) Time to activate the ammo in milliseconeds, if applicable.
        /// Now used only in universal launcher shells, but can be extended to elsewhere.
        /// </summary>
        public int? TimeToActivate { get; set; }

        public new MyPhysicsBody Physics
        {
            get { return base.Physics as MyPhysicsBody; }
            set { base.Physics = value; }
        }

        [Flags]
        public enum MyAmmoBaseFlags
        {
            None = 0,
            IsLightWeight = 1 << 1,
            SkipAcceleration = 1 << 2
        }

        public MyAmmoBase()
            :base()
        {
            Save = false;
        }

        public virtual void Init(MyWeaponPropertiesWrapper weaponProperties, string modelName, bool spherePhysics = true, bool capsulePhysics = false, bool bulletType = false)
        {
            System.Diagnostics.Debug.Assert(MyEntityIdentifier.AllocationSuspended == false, "Allocation was not suspended in MyAmmoBase.Init(...)");
            bool oldSuspend = MyEntityIdentifier.AllocationSuspended;
            MyEntityIdentifier.AllocationSuspended = true;
            base.Init(null, modelName, null, null, null);

            m_weaponDefinition = weaponProperties.WeaponDefinition;
                
            //  Collision skin
            if (spherePhysics)
            {
                this.InitSpherePhysics(MyMaterialType.AMMO, Model, 100,
                                  MyPerGameSettings.DefaultLinearDamping,
                                  MyPerGameSettings.DefaultAngularDamping, MyPhysics.CollisionLayers.AmmoLayer,
                                  bulletType ? RigidBodyFlag.RBF_BULLET : RigidBodyFlag.RBF_DEFAULT);
            }
            if (capsulePhysics)
            {
                this.InitCapsulePhysics(MyMaterialType.AMMO, new Vector3(0, 0, -Model.BoundingBox.HalfExtents.Z * 0.8f),
                    new Vector3(0, 0, Model.BoundingBox.HalfExtents.Z * 0.8f), 0.1f, 10, 0, 0, MyPhysics.CollisionLayers.AmmoLayer,
                    bulletType ? RigidBodyFlag.RBF_BULLET : RigidBodyFlag.RBF_DEFAULT);
                m_ammoOffsetSize = Model.BoundingBox.HalfExtents.Z * 0.8f + 0.1f;
            }
            else
            {
                this.InitBoxPhysics(MyMaterialType.AMMO, Model, 1,
                               MyPerGameSettings.DefaultAngularDamping, MyPhysics.CollisionLayers.AmmoLayer,
                               bulletType ? RigidBodyFlag.RBF_BULLET : RigidBodyFlag.RBF_DEFAULT);
            }  

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            Render.CastShadows = false;
            Closed = true; //Because ammobase instance is going to pool. It is started by Start()

            Physics.RigidBody.ContactPointCallbackEnabled = true;
            Physics.ContactPointCallback += listener_ContactPointCallback;

            MyEntityIdentifier.AllocationSuspended = oldSuspend;
        }

        public virtual void Start(Vector3D position, Vector3D initialVelocity, Vector3D direction, long owner)
        {
            System.Diagnostics.Debug.Assert(Closed);
            System.Diagnostics.Debug.Assert(this.EntityId == 0);
            
            Closed = false;
            GuidedInMultiplayer = false;

            this.EntityId = MyEntityIdentifier.AllocateId();
 
            m_isExploded = false;
            m_cascadeExplosionLevel = 0;
            m_origin = position + direction * m_ammoOffsetSize;
            m_previousPosition = m_origin;
            m_initialVelocity = initialVelocity;

            m_elapsedMiliseconds = 0;
            MarkedToDestroy = false;

            MatrixD ammoWorld = MatrixD.CreateWorld(m_origin, direction, Vector3D.CalculatePerpendicularVector(direction));

            PositionComp.SetWorldMatrix(ammoWorld);

            this.Physics.Clear();
            this.Physics.Enabled = true;

            MyEntities.Add(this);

            //if (owner.Physics != null)
            //    this.Physics.GroupMask = owner.Physics.GroupMask;
            //else
            //    this.Physics.GroupMask = MyGroupMask.Empty;

            //this.Physics.Enabled = true;

            this.Physics.LinearVelocity = initialVelocity;
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;

            //this.Physics.ApplyImpulse(direction * this.Physics.Mass * impulseMultiplier, position);
        }

        /// <summary>
        /// Updates resource.
        /// </summary>
        public override void UpdateBeforeSimulation()
        {
            base.UpdateBeforeSimulation();

            m_elapsedMiliseconds += VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
            m_previousPosition = this.PositionComp.WorldMatrix.Translation;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
        }

        public virtual void Explode()
        {
            if (MarkedToDestroy)
                Close();
            else
            {
                if (!m_isExploded)
                {
                    m_isExploded = true;
                }
            }
        }

        public virtual void ExplodeCascade(int level)
        {
            m_cascadeExplosionLevel = level;
            Explode();
        }

        public bool CanBeAffectedByExplosionForce()
        {
            return m_canByAffectedByExplosionForce && !m_isExploded;
        }

        protected override void Closing()
        {
            TimeToActivate = null;
        }

        void listener_ContactPointCallback(ref MyPhysics.MyContactPointEvent value)
        {
            if (value.ContactPointEvent.EventType != HkContactPointEvent.Type.ManifoldAtEndOfStep)
            {
                OnContactStart(ref value);
            }
        }

        public virtual void OnContactStart(ref MyPhysics.MyContactPointEvent value)
        {
        }
    }
}
