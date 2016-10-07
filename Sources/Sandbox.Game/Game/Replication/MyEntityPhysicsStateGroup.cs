using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;
using VRageMath.PackedVector;
using Havok;
using VRage.Game.Entity;

namespace Sandbox.Game.Replication
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    public class MyEntityPhysicsStateGroup : IMyStateGroup
    {
        protected Func<MyEntity, Vector3D, bool> m_positionValidation;
        public delegate void MovedDelegate(ref MatrixD oldTransform, ref MatrixD newTransform);
        public delegate void VelocityDelegate(ref Vector3 oldVelocity,ref Vector3 newVelocity);

        protected class PrioritySettings
        {
            public int AcceleratingUpdateCount = 4;
            public int LinearMovingUpdateCount = 20;
            public int StoppedUpdateCount = 60;
            public float AcceleratingPriority = 1;
            public float LinearMovingPriority = 0.33f;
            public float StoppedPriority = 0.13f;
            public int StopAfterUpdateCount = 60; // 60 frames not moving is switched to stopped

            public static readonly PrioritySettings Default = new PrioritySettings();
        }

        public readonly MyEntity Entity;
        public readonly IMyReplicable OwnerReplicable;

        protected readonly PrioritySettings m_prioritySettings = new PrioritySettings();
        protected uint m_lastMovementFrame = 0;
        protected bool m_lowPrecisionOrientation;

        static MatrixD m_readMatrix = new MatrixD();
        static Quaternion m_readQuaternion = new Quaternion();
        static Vector3D m_readTranslation = new Vector3D();
        static Vector3 m_readLinearVelocity = new Vector3();
        static Vector3 m_readAngularVelocity = new Vector3();

        public const int NUM_DECIMAL_PRECISION = 3;
        static protected float PRECISION = 1/(float)Math.Pow(10,NUM_DECIMAL_PRECISION);
        const float epsilonSq = 0.05f * 0.05f;

        protected virtual bool IsMoving(MyEntity entity)
        {
            // Never know if somebody is moving entity when physics is null
            return entity.Physics == null
                || Vector3.IsZero(entity.Physics.LinearVelocity, PRECISION) == false
                || Vector3.IsZero(entity.Physics.AngularVelocity, PRECISION) == false;
        }

        protected bool IsAccelerating
        {
            get
            {
                // Never know if somebody is moving entity when physics is null
              
                return Entity.Physics == null
                    || Entity.Physics.LinearAcceleration.LengthSquared() > epsilonSq
                    || Entity.Physics.AngularAcceleration.LengthSquared() > epsilonSq;
            }
        }

        protected virtual bool IsControlledLocally
        {
            get 
            {
                var player = GetControllingPlayer();
                return player != null && player.Client == Sync.Clients.LocalClient; 
            }
        }

        public static float EffectiveSimulationRatio
        {
            get { return (float)Math.Round(MathHelper.Clamp(MyPhysics.SimulationRatio, 0.001f, 10),3); }
        }

        public MovedDelegate MoveHandler { get { return OnMoved; } }
        protected VelocityDelegate VelocityHandler { get { return OnVelocityChanged; } }

        public virtual StateGroupEnum GroupType { get { return StateGroupEnum.Physics; } }

        /// <summary>
        /// Event which occurs when entity is moved by network.
        /// </summary>
        public event MovedDelegate OnMoved;
        public event VelocityDelegate OnVelocityChanged;

        public MyEntityPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
        {
            Entity = entity;
            OwnerReplicable = ownerReplicable;
        }

        public void CreateClientData(MyClientStateBase forClient)
        {
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
        }

        public void ClientUpdate(uint timestamp)
        {
        }

        public virtual void Destroy()
        {
        }

        /// <summary>
        /// Takes into account:
        /// Static body (zero priority for static),
        /// ResponsibilityForUpdate by client (zero priority for not responsible),
        /// otherwise returns OwnerReplicable priority.
        /// </summary>
        protected float GetBasicPhysicsPriority(MyClientInfo client)
        {
            // Called only on server
            if (Entity.Physics == null || Entity.Physics.IsStatic)
                return 0;
                        
            return client.GetPriority(OwnerReplicable);
        }

        public static bool ResponsibleForUpdate(MyEntity entity,EndpointId endpointId)
        {
            if (Sync.Players == null)
                return false;

            var controllingPlayer = Sync.Players.GetControllingPlayer(entity);
            if (controllingPlayer == null)
            {
                // TODO: Move to subclass?
                var character = entity as MyCharacter;
                if (character != null && character.CurrentRemoteControl != null)
                {
                    controllingPlayer = Sync.Players.GetControllingPlayer(character.CurrentRemoteControl as MyEntity);
                }
            }

            if (controllingPlayer == null)
            {
                // IsGameServer
                return endpointId.Value == Sync.ServerId;
            }
            else
            {
                return controllingPlayer.Client.SteamUserId == endpointId.Value;
            }
        }
        
        /// <summary>
        /// Gets priority scale and update rate based on prioritySettings.
        /// </summary>
        protected float GetMovementScale(PrioritySettings prioritySettings, out float updateOncePer)
        {
            float result;

            if(IsMoving(Entity))
            {
                m_lastMovementFrame = MyMultiplayer.Static.FrameCounter;
            }

            bool isStopped = (MyMultiplayer.Static.FrameCounter - m_lastMovementFrame) > prioritySettings.StopAfterUpdateCount;
            
            if(IsAccelerating)
            {
                updateOncePer = prioritySettings.AcceleratingUpdateCount;
                result = prioritySettings.AcceleratingPriority;
            }
            else if(isStopped)
            {
                updateOncePer = prioritySettings.StoppedUpdateCount;
                result = prioritySettings.StoppedPriority;
            }
            else
            {
                // Linearly moving or not moving and waiting for being stopped (StopAfterUpdateCount)
                updateOncePer = prioritySettings.LinearMovingUpdateCount;
                result = prioritySettings.LinearMovingPriority;
            }

            if (MyFakes.NEW_POS_UPDATE_TIMING)
            {
                // Slower physics = more updates so clients have fluent object movement
                updateOncePer *= MyPhysics.SimulationRatio;
            }
            return result;
        }
        
        float IMyStateGroup.GetGroupPriority(int frameCountWithoutSync, MyClientInfo client)
        {
            return GetGroupPriority(frameCountWithoutSync, client, m_prioritySettings);
        }

        protected virtual float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client, PrioritySettings settings)
        {
            float priority = GetBasicPhysicsPriority(client);
            if (priority <= 0)
                return 0;

            float updateFrameCount;
            priority *= GetMovementScale(settings, out updateFrameCount);
            
            return MyReplicationHelpers.RampPriority(priority, frameCountWithoutSync, updateFrameCount);
        }

        public virtual MyPlayer GetControllingPlayer()
        {
            return Sync.Players == null ? null : Sync.Players.GetControllingPlayer(Entity);
        }

        public virtual bool Serialize(BitStream stream, EndpointId forClient,uint timestamp, byte packetId, int maxBitPosition)
        {
            bool moving = false;
            if (stream.Writing)
            {
                moving = IsMoving(Entity);
                stream.WriteBool(moving);
            }
            else
            {
                moving = stream.ReadBool();
            }

            // When controlled by local player, don't apply what came from server
            SerializeTransform(stream, Entity, null, m_lowPrecisionOrientation, !IsControlledLocally, moving, timestamp, null, MoveHandler);
            SerializeVelocities(stream, Entity, EffectiveSimulationRatio, !IsControlledLocally, moving,VelocityHandler);

            return true;
        }

        /// <summary>
        /// Serializes transform into 10 to 30.5 bytes.
        /// </summary>
        protected static bool SerializeTransform(BitStream stream, MyEntity entity, Vector3D? deltaPosBase, bool lowPrecisionOrientation, bool applyWhenReading, bool movingOnServer, uint timeStamp, Func<MyEntity, Vector3D, bool> posValidation = null, MovedDelegate moveHandler = null)
        {
            stream.Serialize(ref timeStamp);
            if(stream.Writing)
            {
                WriteTransform(stream, entity, deltaPosBase, lowPrecisionOrientation);
                return true;
            }
            else
            {
                bool apply = ReadTransform(stream, entity, deltaPosBase, applyWhenReading,movingOnServer, ref m_readTranslation, ref m_readQuaternion, ref m_readMatrix, posValidation, moveHandler);
                if (apply && applyWhenReading)
                {
                    var old = entity.PositionComp.WorldMatrix;
                    entity.PositionComp.SetWorldMatrix(m_readMatrix, null);

                    if (moveHandler != null)
                    {
                        moveHandler(ref old, ref m_readMatrix);
                    }
                }
                return apply;
            }
        }

        static void WriteTransform(BitStream stream, MyEntity entity, Vector3D? deltaPosBase, bool lowPrecisionOrientation)
        {
            var matrix = entity.WorldMatrix;
            stream.WriteBool(deltaPosBase == null);
            if (deltaPosBase == null)
            {
                stream.Write(matrix.Translation); // 24 B
            }
            else
            {
                stream.Write((Vector3)(matrix.Translation - deltaPosBase.Value)); // 6 B
            }
            var orientation = Quaternion.CreateFromForwardUp(matrix.Forward, matrix.Up);
            stream.WriteBool(lowPrecisionOrientation);
            if (lowPrecisionOrientation)
            {
                stream.WriteQuaternionNormCompressed(orientation); // 29b
            }
            else
            {
                stream.WriteQuaternionNorm(orientation); // 52b
            }
        }

        static  bool ReadTransform(BitStream stream, MyEntity entity, Vector3D? deltaPosBase, bool applyWhenReading, bool movingOnServer, ref Vector3D outPosition, ref Quaternion outOrientation, ref MatrixD outWorldMartix, Func<MyEntity, Vector3D, bool> posValidation = null, MovedDelegate moveHandler = null)
        {
            Vector3D position;
            if (stream.ReadBool())
            {
                position = stream.ReadVector3D(); // 24 B
            }
            else
            {
                Vector3 pos = stream.ReadVector3(); // 6 B
                if (deltaPosBase != null)
                {
                    position = pos + deltaPosBase.Value;
                }
                else
                {
                    position = pos;
                }
            }
            Quaternion orientation;
            bool lowPrecisionOrientation = stream.ReadBool();
            if (lowPrecisionOrientation)
            {
                orientation = stream.ReadQuaternionNormCompressed(); // 29b
            }
            else
            {
                orientation = stream.ReadQuaternionNorm(); // 52b
            }

            if (entity != null)
            {
                double delta = (entity.WorldMatrix.Translation - position).LengthSquared();
                if (applyWhenReading && (movingOnServer || delta > 0.1 * 0.1) && (posValidation == null || posValidation(entity, position)))
                {
                    MatrixD matrix = MatrixD.CreateFromQuaternion(orientation);
                    if (matrix.IsValid())
                    {
                        matrix.Translation = position;

                        outPosition = matrix.Translation;
                        outOrientation = orientation;
                        outWorldMartix = matrix;
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        protected bool SerializeServerTransform(BitStream stream, MyEntity entity, Vector3D? deltaPosBase, bool movingOnServer, uint timeStamp, bool lowPrecisionOrientation, ref Vector3D outPosition, ref Quaternion outOrientation, ref MatrixD outWorldMartix, Func<MyEntity, Vector3D, bool> posValidation = null)
        {
            stream.Serialize(ref timeStamp);
            if (stream.Writing)
            {
                WriteTransform(stream, entity, deltaPosBase, lowPrecisionOrientation);
                return true;
            }
            else
            {
                bool apply = ReadTransform(stream, entity, deltaPosBase, true, movingOnServer,ref outPosition, ref outOrientation, ref outWorldMartix, posValidation);
                return apply;
            }
        }        
        /// <summary>
        /// Serializes velocities into 12 bytes.
        /// </summary>
        protected static void SerializeVelocities(BitStream stream, MyEntity entity, float simulationRatio, bool applyWhenReading, bool movingOnServer, VelocityDelegate velocityHandler = null)
        {
            if (stream.Writing)
            {
                WriteVelocities(stream, entity, simulationRatio, movingOnServer);
            }
            else
            {
                ReadVelocities(stream, entity, simulationRatio,movingOnServer, ref m_readLinearVelocity, ref m_readAngularVelocity);

                float linearVelocityDiff = 0.0f;
                float angularVelocityDiff = 0.0f;

                if (entity != null && entity.Physics != null)
                {
                    linearVelocityDiff = (entity.Physics.LinearVelocity - m_readLinearVelocity).LengthSquared();
                    angularVelocityDiff = (entity.Physics.AngularVelocity - m_readAngularVelocity).LengthSquared();
                }

                if ((linearVelocityDiff > 0.001f || angularVelocityDiff > 0.001f || applyWhenReading) && entity.Physics != null)
                {
                    Vector3 oldLinear = entity.Physics.LinearVelocity;
                    entity.Physics.LinearVelocity = Vector3.Round(m_readLinearVelocity,2);
                    entity.Physics.AngularVelocity = Vector3.Round(m_readAngularVelocity,2);
                    entity.Physics.UpdateAccelerations();

                    if (velocityHandler != null && MyFakes.COMPENSATE_SPEED_WITH_SUPPORT)
                    {
                        velocityHandler(ref oldLinear, ref m_readLinearVelocity);
                    }
                }
            }
        }

        protected void SerializeServerVelocities(BitStream stream, MyEntity entity, float simulationRatio, bool movingOnServer, ref Vector3 outLinearVelocity, ref Vector3 outAngularVelocity)
        {
            if (stream.Writing)
            {
                WriteVelocities(stream, entity, simulationRatio, movingOnServer);
            }
            else
            {
                ReadVelocities(stream, entity, simulationRatio,movingOnServer, ref outLinearVelocity, ref outAngularVelocity);
            }
        }

        static void WriteVelocities(BitStream stream, MyEntity entity, float simulationRatio, bool moving)
        {
            Vector3 linear = entity.Physics != null ? entity.Physics.LinearVelocity * simulationRatio : Vector3.Zero;
            Vector3 angular = entity.Physics != null ? entity.Physics.AngularVelocity * simulationRatio : Vector3.Zero;
            if (moving)
            {
                stream.Write(linear); // 6B
                stream.Write(angular); // 6B
            }
        }

        static void ReadVelocities(BitStream stream, MyEntity entity, float simulationRatio, bool movingOnServer, ref Vector3 outLinearVelocity, ref Vector3 outAngularVelocity)
        {
            Vector3 linear = Vector3.Zero;
            Vector3 angular = Vector3.Zero;
            if (movingOnServer)
            {
                linear = stream.ReadVector3(); // 6B
                angular = stream.ReadVector3(); // 6B
            }

            outLinearVelocity = linear / simulationRatio;
            outAngularVelocity = angular / simulationRatio;
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
        }

        public void ForceSend(MyClientStateBase clientData)
        {

        }
    }
}
