using Havok;
using Sandbox.Common;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Components;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Engine.Physics
{
    static class MyPhysicsHelper
    {
        #region Body Methods

        public static void InitSpherePhysics(this IMyEntity entity, MyStringHash materialType, Vector3 sphereCenter, float sphereRadius, float mass, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            mass = (rbFlag & RigidBodyFlag.RBF_STATIC) != 0 ? 0 : mass;

            var physics = new Sandbox.Engine.Physics.MyPhysicsBody(entity, rbFlag)
            {
                MaterialType = materialType,
                AngularDamping = angularDamping,
                LinearDamping = linearDamping
            };

            var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(sphereRadius, mass);

            HkSphereShape shape = new HkSphereShape(sphereRadius);
            physics.CreateFromCollisionObject((HkShape)shape, sphereCenter, entity.PositionComp.WorldMatrix, massProperties);

            shape.Base.RemoveReference();
            entity.Physics = physics;
        }

        public static void InitSpherePhysics(this IMyEntity entity, MyStringHash materialType, MyModel model, float mass, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            Debug.Assert(model != null);
            entity.InitSpherePhysics(materialType, model.BoundingSphere.Center, model.BoundingSphere.Radius, mass, linearDamping, angularDamping, collisionLayer, rbFlag);
        }

        public static void InitBoxPhysics(this IMyEntity entity, MyStringHash materialType, Vector3 center, Vector3 size, float mass, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            System.Diagnostics.Debug.Assert(size.Length() > 0);

            mass = (rbFlag & RigidBodyFlag.RBF_STATIC) != 0 ? 0 : mass;

            var massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(size / 2, mass);

            var physics = new Sandbox.Engine.Physics.MyPhysicsBody(entity, rbFlag)
            {
                MaterialType = materialType,
                AngularDamping = angularDamping,
                LinearDamping = linearDamping
            };

            HkBoxShape shape = new HkBoxShape(size * 0.5f);
            physics.CreateFromCollisionObject((HkShape)shape, center, entity.PositionComp.WorldMatrix, massProperties);
            shape.Base.RemoveReference();

            entity.Physics = physics;
        }

        internal static void InitBoxPhysics(this IMyEntity entity, Matrix worldMatrix, MyStringHash materialType, Vector3 center, Vector3 size, float mass, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            mass = (rbFlag & RigidBodyFlag.RBF_STATIC) != 0 ? 0 : mass;

            var massProperties = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(size / 2, mass);

            var physicsBody = new Sandbox.Engine.Physics.MyPhysicsBody(null, rbFlag)
            {
                MaterialType = materialType,
                AngularDamping = angularDamping,
                LinearDamping = linearDamping
            };

            //BoxShape shape = new BoxShape(size * 0.5f);
            HkBoxShape shape = new HkBoxShape(size * 0.5f);
            physicsBody.CreateFromCollisionObject((HkShape)shape, center, worldMatrix, massProperties);
            shape.Base.RemoveReference();

            entity.Physics = physicsBody;
            //return physicsBody;
        }

        public static void InitBoxPhysics(this IMyEntity entity, MyStringHash materialType, MyModel model, float mass, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            Debug.Assert(model != null);
            var center = model.BoundingBox.Center;
            var size = model.BoundingBoxSize;
            entity.InitBoxPhysics(materialType, center, size, mass, 0, angularDamping, collisionLayer, rbFlag);
        }

        public static void InitCharacterPhysics(this IMyEntity entity, MyStringHash materialType, Vector3 center, float characterWidth, float characterHeight, float crouchHeight, float ladderHeight, float headSize, float headHeight, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag, float mass, bool isOnlyVertical, float maxSlope, bool networkProxy)
        {
            var physics = new Sandbox.Engine.Physics.MyPhysicsBody(entity, rbFlag)
            {
                MaterialType = materialType,
                AngularDamping = angularDamping,
                LinearDamping = linearDamping
            };

            //BoxShape shape = new BoxShape(SharpDXHelper.ToSharpDX(size * 0.5f));
            //this.m_physics.CreateFromCollisionObject(shape, center, WorldMatrix);
            physics.CreateCharacterCollision(center, characterWidth, characterHeight, crouchHeight, ladderHeight, headSize, headHeight, entity.PositionComp.WorldMatrix, mass, collisionLayer, isOnlyVertical, maxSlope, networkProxy);
            entity.Physics = physics;
        }


        public static void InitCapsulePhysics(this IMyEntity entity, MyStringHash materialType, Vector3 vertexA, Vector3 vertexB, float radius, float mass, float linearDamping, float angularDamping, ushort collisionLayer, RigidBodyFlag rbFlag)
        {
            mass = (rbFlag & RigidBodyFlag.RBF_STATIC) != 0 ? 0 : mass;

            var physics = new Sandbox.Engine.Physics.MyPhysicsBody(entity, rbFlag)
            {
                MaterialType = materialType,
                AngularDamping = angularDamping,
                LinearDamping = linearDamping
            };

            var massProperties = HkInertiaTensorComputer.ComputeSphereVolumeMassProperties(radius, mass);

            physics.ReportAllContacts = true;
            HkCapsuleShape shape = new HkCapsuleShape(vertexA, vertexB, radius);
            physics.CreateFromCollisionObject((HkShape)shape, (vertexA + vertexB) / 2, entity.PositionComp.WorldMatrix, massProperties);
            shape.Base.RemoveReference();
            entity.Physics = physics;
        }


        #endregion

    }
}
