using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using VRage.Library.Collections;
using VRage.Network;
using VRageMath;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Profiler;
using System.Collections.Generic;

namespace Sandbox.Game.Replication.StateGroups
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    public class MyEntityPhysicsStateGroup : IMyStateGroup
    {
        public MyEntity Entity { get; private set; }
        public IMyReplicable Owner { get; private set; }

        public virtual StateGroupEnum GroupType { get { return StateGroupEnum.Physics; } }

        private Dictionary<EndpointId, MyTimeSpan> m_lastWrittenTimeStamps = new Dictionary<EndpointId, MyTimeSpan>();


        private readonly History.MyPredictedSnapshotSyncSetup m_Settings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = true,
            ApplyPhysics = true,
            IsControlled = false,
            MaxPositionFactor = 100.0f,
            MaxLinearFactor = 100.0f,
            MaxRotationFactor = 100.0f,
            IterationsFactor = 1.0f,
        };

        private readonly History.MyPredictedSnapshotSyncSetup m_ControlledSettings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = true,
            ApplyPhysics = true,
            IsControlled = true,
            MaxPositionFactor = 100.0f,
            MaxLinearFactor = 100.0f,
            MaxRotationFactor = 100.0f,
            IterationsFactor = 1.0f,
        };
        /*private readonly History.MyPredictedSnapshotSyncSetup m_Settings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = true,
            ApplyPhysics = true,
            MaxPositionFactor = 4.0f,
            MaxLinearFactor = 5.0f,
            MaxRotationFactor = 20.0f,
            IterationsFactor = 0.25f
        };*/
        protected readonly History.IMySnapshotSync SnapshotSync;

        protected bool IsControlledLocally
        {
            get
            {
                return MySession.Static.TopMostControlledEntity == Entity;
            }
        }

        public MyEntityPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
        {
            Entity = entity;
            Owner = ownerReplicable;
            if (MyFakes.MULTIPLAYER_CLIENT_PHYSICS)
                SnapshotSync = new History.MyPredictedSnapshotSync(Entity);
            else SnapshotSync = new History.MyAnimatedSnapshotSync(Entity);

        }

        public void CreateClientData(MyClientStateBase forClient)
        {
        }

        public void DestroyClientData(MyClientStateBase forClient)
        {
        }

        public virtual void Destroy()
        {
            SnapshotSync.Reset();
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
        }

        public void ForceSend(MyClientStateBase clientData)
        {

        }

        public void TimestampReset(MyTimeSpan timestamp)
        {
            SnapshotSync.Reset();
        }

        public virtual void ClientUpdate(MyTimeSpan clientTimestamp)
        {
            if (IsControlledLocally)
                SnapshotSync.Update(clientTimestamp, m_ControlledSettings);
            else
                SnapshotSync.Update(clientTimestamp, m_Settings);
        }

        public virtual void Serialize(BitStream stream, EndpointId forClient, MyTimeSpan timeStamp, byte packetId, int maxBitPosition)
        {
            //System.Diagnostics.Debug.Assert((m_lastWrittenTimeStamps.ContainsKey(forClient) && m_lastWrittenTimeStamps[forClient] != timeStamp) || !m_lastWrittenTimeStamps.ContainsKey(forClient), "Sending same data twice");

            if (stream.Writing)
            {
                SnapshotSync.Write(stream);
            }
            else
            {
                SnapshotSync.Read(stream, timeStamp);
            }

            if (stream.BitPosition <= maxBitPosition)
            {
                m_lastWrittenTimeStamps[forClient] = timeStamp;
            }
        }

        /// to be cleaned up !!!!!!!!!!!!!!!!!!
        /////////////////////////////////////////////////
        
        /// PRIORITIES
        /////////////////////////////////////////////////
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

        protected readonly PrioritySettings m_prioritySettings = new PrioritySettings();
        protected uint m_lastMovementFrame = 0;

        public const int NUM_DECIMAL_PRECISION = 3;
        protected static float PRECISION = 1 / (float)Math.Pow(10, NUM_DECIMAL_PRECISION);
        const float EPSILON_SQ = 0.05f * 0.05f;

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
                    || Entity.Physics.LinearAcceleration.LengthSquared() > EPSILON_SQ
                    || Entity.Physics.AngularAcceleration.LengthSquared() > EPSILON_SQ;
            }
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

            return client.GetPriority(Owner);
        }

        /// <summary>
        /// Gets priority scale and update rate based on prioritySettings.
        /// </summary>
        protected float GetMovementScale(PrioritySettings prioritySettings, out float updateOncePer)
        {
            float result;

            if (IsMoving(Entity))
            {
                m_lastMovementFrame = MyMultiplayer.Static.FrameCounter;
            }

            bool isStopped = (MyMultiplayer.Static.FrameCounter - m_lastMovementFrame) > prioritySettings.StopAfterUpdateCount;

            if (IsAccelerating)
            {
                updateOncePer = prioritySettings.AcceleratingUpdateCount;
                result = prioritySettings.AcceleratingPriority;
            }
            else if (isStopped)
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
            return result;
        }

        float IMyStateGroup.GetGroupPriority(int frameCountWithoutSync, MyClientInfo client)
        {
            return GetGroupPriority(frameCountWithoutSync, client, m_prioritySettings);            
        }

        protected virtual float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client, PrioritySettings settings)
        {
            if (Entity.MarkedForClose)
                return 0;

            ProfilerShort.Begin("MyEntityPhysicsStateGroup::GetGroupPriority");
            float updateFrameCount;
            float priority;
            if (ResponsibleForUpdate(Entity, client.EndpointId))
            {
                if (client.PriorityMultiplier < 1.0f)
                {
                    priority = 1.0f;
                    updateFrameCount = 1.0f;
                }
                else
                {
                    ProfilerShort.End();
                    return 1000;
                }
            }
            else
            {
                priority = GetBasicPhysicsPriority(client);
                if (priority <= 0)
                {
                    ProfilerShort.End();
                    return 0;
                }

                priority *= GetMovementScale(settings, out updateFrameCount);
            }
            if (client.PriorityMultiplier > 0)
                updateFrameCount /= client.PriorityMultiplier;
            else
            {
                ProfilerShort.End();
                return 0;
            }

            float adjustedPriority = MyReplicationHelpers.RampPriority(priority, frameCountWithoutSync, updateFrameCount);
            
            ProfilerShort.End();
            return adjustedPriority;
        }

        public static bool ResponsibleForUpdate(MyEntity entity, EndpointId endpointId)
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

        public bool IsStillDirty(EndpointId forClient)
        {
            return true;
        }
    }
}
