
using System;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Utils;

using VRage.Generics;
using VRage.Utils;
using VRageMath;
using Sandbox.Common;

using Sandbox.Game.Components;
using VRage.Game.Components;
using VRage.ModAPI;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using VRage.Game.Entity;
namespace Sandbox.Game.Entities.Debris
{

    /// <summary>
    /// Description of 
    /// </summary>
    public class MyDebrisBaseDescription
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
    public class MyDebrisBase : MyEntity
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
                var pos = ((((MyEntity)Entity).Render.GetModel().BoundingBox.Max + ((MyEntity)Entity).Render.GetModel().BoundingBox.Min) / 2);
                shape = new HkTransformShape(boxShape, ref pos, ref Quaternion.Identity);
                //shape = boxShape;
                massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(boxShape.HalfExtents, boxShape.HalfExtents.Volume * 0.5f);
                massProperties.CenterOfMass = pos;
            }

            public virtual void ScalePhysicsShape(ref HkMassProperties massProperties)
            {
                var debriModel = Entity.Render.GetModel();
                HkShape shape;
                if (debriModel.HavokCollisionShapes != null && debriModel.HavokCollisionShapes.Length > 0)
                {
                    shape = debriModel.HavokCollisionShapes[0];
                    Vector4 min, max;
                    shape.GetLocalAABB(0.1f, out min, out max);
                    Vector3 he = new Vector3((max - min) * 0.5f);
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(he, he.Volume * 50);
                    massProperties.CenterOfMass = new Vector3((min + max) * 0.5f);
                }
                else
                {
                    var transformShape = (HkTransformShape)RigidBody.GetShape();
                    var boxShape = (HkBoxShape)transformShape.ChildShape;
                    boxShape.HalfExtents = ((debriModel.BoundingBox.Max - debriModel.BoundingBox.Min) / 2) * Entity.PositionComp.Scale.Value;
                    massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(boxShape.HalfExtents, boxShape.HalfExtents.Volume * 0.5f);
                    massProperties.CenterOfMass = transformShape.Transform.Translation;
                    shape = transformShape;
                }
                RigidBody.SetShape(shape);
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
            public int LifespanInMiliseconds;
            public float RandomScale;


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
                RandomScale = MyUtils.GetRandomFloat(desc.ScaleMin, desc.ScaleMax);
                Container.Entity.PositionComp.Scale = RandomScale;
                LifespanInMiliseconds = MyUtils.GetRandomInt(desc.LifespanMinInMiliseconds, desc.LifespanMaxInMiliseconds);

                HkShape shape;
                m_massProperties = new HkMassProperties();
                m_massProperties.Mass = (float)(0.5236f * Math.Pow(2 * RandomScale, 3)) * 2600;//0.5236f = pi / 6, 2600 = default stone density
                Container.Entity.Physics = GetPhysics(RigidBodyFlag.RBF_DEBRIS);
                (Container.Entity.Physics as MyDebrisPhysics).CreatePhysicsShape(out shape, ref m_massProperties);
                (Container.Entity.Physics as MyDebrisPhysics).CreateFromCollisionObject(shape, Vector3.Zero, MatrixD.Identity, m_massProperties, MyPhysics.CollisionLayers.DebrisCollisionLayer);
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
                Start(MatrixD.CreateTranslation(position), initialVelocity, scale);
            }
            public virtual void Start(MatrixD position, Vector3D initialVelocity, float scale, bool randomRotation = true)
            {
                MyDebug.AssertDebug(!m_isStarted);
                m_createdTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                Container.Entity.PositionComp.Scale = RandomScale * scale;
                Container.Entity.WorldMatrix = position;
                (Container.Entity.Physics as MyDebrisPhysics).ScalePhysicsShape(ref m_massProperties);
                Container.Entity.Physics.Clear();
                Container.Entity.Physics.LinearVelocity = initialVelocity;
                //apply random rotation impulse
                if(randomRotation)
                    Container.Entity.Physics.AngularVelocity = new Vector3(MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian(),
                                                      MyUtils.GetRandomRadian());
                MyEntities.Add(m_entity);
                Container.Entity.Physics.Enabled = true;
                Vector3D gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(position.Translation);
                ((MyPhysicsBody)Container.Entity.Physics).RigidBody.Gravity = gravity;
                (Container.Entity.Physics as MyPhysicsBody).HavokWorld.ActiveRigidBodies.Add((Container.Entity.Physics as MyPhysicsBody).RigidBody);
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
                    if (age > LifespanInMiliseconds)
                    {
                        MarkForClose();
                        return; //dont dither 
                    }

                    float dithering = age / (float)LifespanInMiliseconds;
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