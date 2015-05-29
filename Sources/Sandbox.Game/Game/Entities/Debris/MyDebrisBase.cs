﻿
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
            private ModAPI.IMyEntity Entity1;
            private RigidBodyFlag rigidBodyFlag;

            public MyDebrisPhysics(ModAPI.IMyEntity Entity1, RigidBodyFlag rigidBodyFlag) 
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
                Entity.PositionComp.Scale = m_randomScale;
                m_lifespanInMiliseconds = MyUtils.GetRandomInt(desc.LifespanMinInMiliseconds, desc.LifespanMaxInMiliseconds);

                HkShape shape;
                m_massProperties = new HkMassProperties();
                m_massProperties.Mass = 50;
                Entity.Physics = GetPhysics(RigidBodyFlag.RBF_DEBRIS);
                (Entity.Physics as MyDebrisPhysics).CreatePhysicsShape(out shape, ref m_massProperties);
                (Entity.Physics as MyDebrisPhysics).CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, m_massProperties, MyPhysics.DebrisCollisionLayer);
                Entity.Physics.Enabled = false;
                shape.RemoveReference();

                m_entity.Save = false;
                Entity.Physics.PlayCollisionCueEnabled = true;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                m_onCloseCallback = desc.OnCloseAction;
            }

            protected virtual MyPhysicsComponentBase GetPhysics(RigidBodyFlag rigidBodyFlag)
            {
                return new MyDebrisPhysics(Entity, rigidBodyFlag);
            }

            /// <summary>
            /// Called to clean up resources before pool is destroyed. Think of this as finalization.
            /// </summary>
            public virtual void Free()
            {
                if (Entity.Physics != null)
                {
                    Entity.Physics.Close();
                    Entity.Physics = null;
                }
            }

            /// <summary>
            /// Initialization of each debris instance when it is taken out of the pool.
            /// </summary>
            public virtual void Start(Vector3D position, Vector3D initialVelocity, float scale)
            {
                MyDebug.AssertDebug(!m_isStarted);
                m_createdTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                Entity.PositionComp.Scale = m_randomScale * scale;
                Entity.WorldMatrix = MatrixD.CreateTranslation(position);
                (Entity.Physics as MyDebrisPhysics).ScalePhysicsShape(ref m_massProperties);
                Entity.Physics.Clear();
                Entity.Physics.LinearVelocity = initialVelocity;
                //apply random rotation impulse
                Entity.Physics.AngularVelocity = new Vector3(MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian());
                MyEntities.Add(m_entity);
                Entity.Physics.Enabled = true;
                m_isStarted = true;
            }

            public override void OnAddedToContainer(MyComponentContainer container)
            {
                base.OnAddedToContainer(container);
                m_debris = container.Entity as MyDebrisBase;
            }

            public override void UpdateAfterSimulation()
            {
                
                base.UpdateAfterSimulation();
                if (m_isStarted)
                {
                    int age = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_createdTime;
                    if (age > m_lifespanInMiliseconds)
                        MarkForClose();
                }
            }

            public override void Close()
            {
                MyDebug.AssertDebug(m_isStarted);
                MyEntities.Remove(m_debris);
                MyEntities.RemoveFromClosedEntities(m_debris);

                if (Entity.Physics != null)
                    Entity.Physics.Enabled = false;
                else
                    MySandboxGame.Log.WriteLine("WARNING: Closing debris which no longer has its Physics.");
                Entity.WorldMatrix = Matrix.Identity;
                m_isStarted = false;
                if (m_onCloseCallback != null)
                    m_onCloseCallback(m_debris);
            }
        }

    }
}