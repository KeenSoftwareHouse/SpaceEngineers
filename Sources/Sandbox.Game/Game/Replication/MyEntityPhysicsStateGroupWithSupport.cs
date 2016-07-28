using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.ModAPI;
using VRage.Network;
using VRageMath;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// This takes care of support, on what the entity is standing on.
    /// </summary>
    public class MyEntityPhysicsStateGroupWithSupport : MyEntityPhysicsStateGroup
    {
        private MyEntityPhysicsStateGroup m_supportPhysics;
        private MovedDelegate m_onSupportMove;
        private VelocityDelegate m_onSupportVelocityChanged;
        public Func<MyEntityPhysicsStateGroup> FindSupportDelegate;

        public MyEntityPhysicsStateGroup DebugSupport
        {
            get { return m_supportPhysics; }
        }

        public MyEntityPhysicsStateGroupWithSupport(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
            m_onSupportMove = OnSupportMove;
            m_onSupportVelocityChanged = OnSupportVelocityChanged;
            if (Sync.IsServer)
                OnMoved += PhysicsStateGroup_OnMoved;
        }

        bool NearEqual(ref Quaternion q1, ref Quaternion q2)
        {
            return SharpDX.MathUtil.NearEqual(q1.X, q2.X)
                && SharpDX.MathUtil.NearEqual(q1.Y, q2.Y)
                && SharpDX.MathUtil.NearEqual(q1.Z, q2.Z)
                && SharpDX.MathUtil.NearEqual(q1.W, q2.W);
        }

        void PhysicsStateGroup_OnMoved(ref MatrixD oldTransform, ref MatrixD newTransform)
        {
            Quaternion q1, q2;
            Quaternion.CreateFromRotationMatrix(ref oldTransform, out q1);
            Quaternion.CreateFromRotationMatrix(ref newTransform, out q2);

            if (!NearEqual(ref q1, ref q2))
                m_lastMovementFrame = MyMultiplayer.Static.FrameCounter;
        }

        public override void Destroy()
        {
            if (m_supportPhysics != null)
            {
                m_supportPhysics.OnVelocityChanged -= m_onSupportVelocityChanged;
                m_supportPhysics.OnMoved -= m_onSupportMove;
                m_supportPhysics = null;
            }
            base.Destroy();
        }

        void OnSupportVelocityChanged(ref Vector3 oldVelocity,ref Vector3 newVelocity)
        {
            if (MyFakes.COMPENSATE_SPEED_WITH_SUPPORT)
            {
                if (Entity == null || Entity.PositionComp == null || Entity.Closed)
                {
                    Debug.Fail("Moving support for closed entity");
                    return;
                }
               Entity.Physics.LinearVelocity += newVelocity - oldVelocity;
            }
        }

        void OnSupportMove(ref MatrixD oldSupportTransform, ref MatrixD newSupportTransform)
        {
            // Comment this to update support even on other clients
            //if (!IsControlledLocally)
            //return;

            if (Entity == null || Entity.PositionComp == null || Entity.Closed)
            {
                Debug.Fail("Moving support for closed entity");
                return;
            }
            var old = Entity.WorldMatrix;
            MatrixD local = old * MatrixD.Invert(oldSupportTransform);

            MatrixD newTransform = local * newSupportTransform;
            Entity.PositionComp.SetWorldMatrix(newTransform, null, true);
        }

        public void SetSupport(MyEntityPhysicsStateGroup physicsGroup)
        {
            if (m_supportPhysics != physicsGroup)
            {
                if (m_supportPhysics != null)
                {
                    m_supportPhysics.OnMoved -= m_onSupportMove;
                    m_supportPhysics.OnVelocityChanged -= m_onSupportVelocityChanged;
                }
                m_supportPhysics = physicsGroup;
                if (m_supportPhysics != null)
                {
                    m_supportPhysics.OnMoved += m_onSupportMove;
                    m_supportPhysics.OnVelocityChanged += m_onSupportVelocityChanged;
                }
            }
        }

        /// <summary>
        /// Serializes physics and takes into account support (what's entity standing on)
        /// </summary>
        private void SerializePhysicsWithSupport(BitStream stream, EndpointId forClient,uint timestamp, byte packetId, int maxBitPosition)
        {
            if (stream.Writing)
            {
                // TODO: only prototype implementation
                SetSupport(FindSupportDelegate());

                stream.WriteBool(m_supportPhysics != null);
                if (m_supportPhysics != null)
                {
                    stream.WriteInt64(m_supportPhysics.Entity.EntityId);
                    var localToParent = Entity.WorldMatrix * MatrixD.Invert(m_supportPhysics.Entity.PositionComp.WorldMatrix);
                    stream.Write(localToParent.Translation);
                    stream.Write(Quaternion.CreateFromForwardUp(localToParent.Forward, localToParent.Up).ToVector4());
                    bool moving = IsMoving(Entity);
                    stream.WriteBool(moving);

                    SerializeVelocities(stream, Entity, EffectiveSimulationRatio, !IsControlledLocally, moving);
                }
                else
                {
                    base.Serialize(stream, forClient,timestamp, packetId, maxBitPosition);
                }
            }
            else
            {
                if (stream.ReadBool())
                {
                    MyEntity support;
                    bool apply = MyEntities.TryGetEntityById(stream.ReadInt64(), out support) && !IsControlledLocally;

                    var pos = stream.ReadVector3D();
                    var orient = Quaternion.FromVector4(stream.ReadVector4());

                    if (apply)
                    {
                        var old = Entity.PositionComp.WorldMatrix;

                        MatrixD localToParent = MatrixD.CreateFromQuaternion(orient);
                        localToParent.Translation = pos;
                        MatrixD matrix = localToParent * support.WorldMatrix;
                        Entity.PositionComp.SetWorldMatrix(matrix, null, true);

                        SetSupport(MySupportHelper.FindPhysics(support));

                        var handler = MoveHandler;
                        if (handler != null)
                        {
                            handler(ref old, ref matrix);
                        }
                    }
                    else
                    {
                        SetSupport(null);
                    }
                    bool moving = stream.ReadBool();
                    SerializeVelocities(stream, Entity, EffectiveSimulationRatio, apply, moving);

                }
                else
                {
                    SetSupport(null);
                    base.Serialize(stream, forClient, timestamp, packetId, maxBitPosition);
                }
            }
        }

        public override bool Serialize(BitStream stream, EndpointId forClient,uint timestamp, byte packetId, int maxBitPosition)
        {
            if (MyFakes.ENABLE_MULTIPLAYER_ENTITY_SUPPORT)
            {
                SerializePhysicsWithSupport(stream, forClient,timestamp, packetId, maxBitPosition);
            }
            else
            {
                base.Serialize(stream, forClient,timestamp, packetId, maxBitPosition);
            }

            return true;
        }
    }
}
