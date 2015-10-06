
using System;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Utils;

using VRage.Generics;
using VRage.Utils;
using VRageMath;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Game.Components;
using VRage.Components;
using VRage.ModAPI;
using Sandbox.Game.GameSystems;
namespace Sandbox.Game.Entities.Debris
{

    /// <summary>
    /// Description of 
    /// </summary>
    class MyDebrisBaseDescription
    {
        public string Model;
        public float ScaleMin;
        public float ScaleMax;
        public int LifespanMinInMiliseconds;
        public int LifespanMaxInMiliseconds;
        public Action<MyDebrisBase> OnCloseAction;
    }

    /// <summary>
    /// Single physical debris object. These are stored in a pool and are not meant to be created
    /// all the time, but only pulled out and returned back to the pool. Therefore, we do not use constructor
    /// to initialize an instance.
    /// </summary>
    class MyDebrisBase : MyEntity
    {
        private MyDebrisBaseLogic m_debrisLogic;
        public MyDebrisBaseLogic Debris { get { return m_debrisLogic; } }

        public MyDebrisBase()
        {
            Components.ComponentAdded += Components_ComponentAdded;
            GameLogic = new MyDebrisBaseLogic();
        }

        void Components_ComponentAdded(Type arg1, MyComponentBase arg2)
        {
            if (arg1 == typeof(MyGameLogicComponent))
                m_debrisLogic = arg2 as MyDebrisBaseLogic;
        }


        public class MyDebrisPhysics : MyPhysicsBody
        {
            private IMyEntity Entity1;
            private RigidBodyFlag rigidBodyFlag;

            public MyDebrisPhysics(IMyEntity Entity1, RigidBodyFlag rigidBodyFlag) 
               : base(Entity1, rigidBodyFlag)
            {
            }
            public virtual void CreatePhysicsShape(out HkShape shape, ref HkMassProperties massProperties)
            {
                var boxShape = new HkBoxShape(((((MyEntity)Entity).Render.GetModel().BoundingBox.Max - ((MyEntity)Entity).Render.GetModel().BoundingBox.Min) / 2) * Entity.PositionComp.Scale.Value);
                shape = boxShape;
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(boxShape.HalfExtents, massProperties.Mass);
            }

            public virtual void ScalePhysicsShape(ref HkMassProperties massProperties)
            {
                var shape = RigidBody.GetShape();
                var boxShape = (HkBoxShape)shape;
                boxShape.HalfExtents = ((((MyEntity)Entity).Render.GetModel().BoundingBox.Max - ((MyEntity)Entity).Render.GetModel().BoundingBox.Min) / 2) * Entity.PositionComp.Scale.Value;
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(boxShape.HalfExtents, massProperties.Mass);

                RigidBody.SetShape(boxShape);
                RigidBody.SetMassProperties(ref massProperties);
                RigidBody.UpdateShape();
            }
        }

        public class MyDebrisBaseLogic : MyEntityGameLogic
        {
            MyDebrisBase m_debris;
            /// <summary>
            ///  We cannot use OnClose event from entity to deallocate pool objects,
            ///  so this callback solves that (although it isn't quite as pretty). 
            /// </summary>
            private Action<MyDebrisBase> m_onCloseCallback;

            private bool m_isStarted;
            private int m_createdTime;
            private int m_lifespanInMiliseconds;
            private float m_randomScale;


            protected HkMassProperties m_massProperties;

            /// <summary>
            /// One time initialization for debris entity. These are settings that do not change 
            /// when this debris entity is pulled from the pool.
            /// Also note that this is the only way Debris should be initialized. It calls other Init methods with 
            /// correct arguments.
            /// </summary>
            public virtual void Init(MyDebrisBaseDescription desc)
            {
                base.Init(null, desc.Model, null, 1.0f);
                m_randomScale = MyUtils.GetRandomFloat(desc.ScaleMin, desc.ScaleMax);
                Container.Entity.PositionComp.Scale = m_randomScale;
                m_lifespanInMiliseconds = MyUtils.GetRandomInt(desc.LifespanMinInMiliseconds, desc.LifespanMaxInMiliseconds);

                HkShape shape;
                m_massProperties = new HkMassProperties();
                m_massProperties.Mass = 50;
                Container.Entity.Physics = GetPhysics(RigidBodyFlag.RBF_DEBRIS);
                (Container.Entity.Physics as MyDebrisPhysics).CreatePhysicsShape(out shape, ref m_massProperties);
                (Container.Entity.Physics as MyDebrisPhysics).CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, m_massProperties, MyPhysics.DebrisCollisionLayer);
                Container.Entity.Physics.Enabled = false;
                shape.RemoveReference();

                m_entity.Save = false;
                Container.Entity.Physics.PlayCollisionCueEnabled = true;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                m_onCloseCallback = desc.OnCloseAction;
            }

            protected virtual MyPhysicsComponentBase GetPhysics(RigidBodyFlag rigidBodyFlag)
            {
                return new MyDebrisPhysics(Container.Entity, rigidBodyFlag);
            }

            /// <summary>
            /// Called to clean up resources before pool is destroyed. Think of this as finalization.
            /// </summary>
            public virtual void Free()
            {
                if (Container.Entity.Physics != null)
                {
                    Container.Entity.Physics.Close();
                    Container.Entity.Physics = null;
                }
            }

            /// <summary>
            /// Initialization of each debris instance when it is taken out of the pool.
            /// </summary>
            public virtual void Start(Vector3D position, Vector3D initialVelocity, float scale)
            {
                MyDebug.AssertDebug(!m_isStarted);
                m_createdTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                Container.Entity.PositionComp.Scale = m_randomScale * scale;
                Container.Entity.WorldMatrix = MatrixD.CreateTranslation(position);
                (Container.Entity.Physics as MyDebrisPhysics).ScalePhysicsShape(ref m_massProperties);
                Container.Entity.Physics.Clear();
                Container.Entity.Physics.LinearVelocity = initialVelocity;
                //apply random rotation impulse
                Container.Entity.Physics.AngularVelocity = new Vector3(MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian());
                MyEntities.Add(m_entity);
                Container.Entity.Physics.Enabled = true;
                m_isStarted = true;
            }

            public override void OnAddedToContainer()
            {
                base.OnAddedToContainer();
                m_debris = Container.Entity as MyDebrisBase;
            }

            public override void UpdateAfterSimulation()
            {       
                base.UpdateAfterSimulation();
                if (m_isStarted)
                {
                    int age = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_createdTime;
                    if (age > m_lifespanInMiliseconds)
                        MarkForClose();
                    float dithering = age / (float)m_lifespanInMiliseconds;
                    float ditherStart = 3.0f / 4.0f;
                    if (dithering > ditherStart)
                    {
                        VRageRender.MyRenderProxy.UpdateRenderEntity((uint)this.Container.Entity.Render.GetRenderObjectID(), null, null, (dithering - ditherStart) / (1.0f - ditherStart));
                    }
                }
            }

            public override void Close()
            {
                MyDebug.AssertDebug(m_isStarted);
                MyEntities.Remove(m_debris);
                MyEntities.RemoveFromClosedEntities(m_debris);

                if (Container.Entity.Physics != null)
                    Container.Entity.Physics.Enabled = false;
                else
                    MySandboxGame.Log.WriteLine("WARNING: Closing debris which no longer has its Physics.");
                Container.Entity.WorldMatrix = Matrix.Identity;
                m_isStarted = false;
                if (m_onCloseCallback != null)
                    m_onCloseCallback(m_debris);
            }
        }

    }
}