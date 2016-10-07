using Havok;
using Sandbox.Engine.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Replication
{
    public class MySmallObjectPhysicsStateGroup : MyEntityPhysicsStateGroupWithSupport
    {
        public MySmallObjectPhysicsStateGroup(MyEntity entity, IMyReplicable owner)
            : base(entity, owner)
        {
            m_positionValidation = ValidatePosition;
            FindSupportDelegate = () => MySupportHelper.FindSupportForCharacter(Entity);
        }

        public override bool Serialize(BitStream stream, EndpointId forClient, uint timestamp, byte packetId, int maxBitPosition)
        {
            bool lowPrecisionOrientation = true;
            bool applyWhenReading = true;
            SetSupport(FindSupportDelegate());
            if (stream.Writing)
            {
                bool moving = IsMoving(Entity);
                stream.WriteBool(moving);
                SerializeVelocities(stream, Entity, MyEntityPhysicsStateGroup.EffectiveSimulationRatio, applyWhenReading, moving);
                SerializeTransform(stream, Entity, null, lowPrecisionOrientation, applyWhenReading, moving, timestamp);
            }
            else
            {
                bool moving = stream.ReadBool();
                // reading
                SerializeServerVelocities(stream, Entity, MyEntityPhysicsStateGroup.EffectiveSimulationRatio, moving, ref Entity.m_serverLinearVelocity, ref Entity.m_serverAngularVelocity);

                applyWhenReading = SerializeServerTransform(stream, Entity, null, moving, timestamp, lowPrecisionOrientation,
                    ref Entity.m_serverPosition, ref Entity.m_serverOrientation, ref Entity.m_serverWorldMatrix, m_positionValidation);

                if (applyWhenReading && moving)
                {
                    Entity.PositionComp.SetWorldMatrix(Entity.m_serverWorldMatrix, null, true);
                    Entity.SetSpeedsAccordingToServerValues();
                }
            }

            SerializeFriction(stream, Entity);
            SerializeActive(stream, Entity);

            return true;
        }

        bool ValidatePosition(MyEntity entity, Vector3D position)
        {
            float positionTolerancy = Math.Max(entity.PositionComp.LocalAABB.HalfExtents.Max() * 0.1f, 0.1f);
            float smallSpeed = 0.1f;
            if (entity.m_serverLinearVelocity == Vector3.Zero || entity.m_serverLinearVelocity.Length() < smallSpeed)
                // some tolerancy of position for not moving objects
                positionTolerancy = Math.Max(entity.PositionComp.LocalAABB.HalfExtents.Max() * 0.5f, 1.0f);

            if ((position - entity.PositionComp.GetPosition()).Length() < positionTolerancy)
                return false;
            return true;
        }

        protected void SerializeActive(BitStream stream, MyEntity entity)
        {
            if (stream.Writing)
            {
                if (entity.Physics.RigidBody != null && entity.Physics.RigidBody.IsActive)
                    stream.WriteBool(true);
                else
                    stream.WriteBool(false);
            }
            else
            {
                // reading 
                bool isActive = stream.ReadBool();
                if (entity != null && entity.Physics != null)
                {
                    HkRigidBody rb = entity.Physics.RigidBody;
                    if (rb != null)
                    {
                        if (isActive)
                            rb.Activate();
                        else
                            rb.Deactivate();
                    }
                }
            }
        }

        protected void SerializeFriction(BitStream stream, MyEntity entity)
        {
            MyPhysicsBody pb = entity.Physics as MyPhysicsBody;
            if (pb == null || pb.RigidBody == null)
            {
                if (stream.Writing)
                    stream.WriteFloat(0);
                else
                    stream.ReadFloat();
                return;
            }
            HkRigidBody rb = pb.RigidBody;

            if (stream.Writing)
                stream.WriteFloat(rb.Friction);
            else
                rb.Friction = stream.ReadFloat();
        }

    }
}
