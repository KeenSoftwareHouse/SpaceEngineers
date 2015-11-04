using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Collections;
using VRage.Network;

namespace Sandbox.Game.Entities
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    class MyEntityPhysicsStateGroup : IMyStateGroup
    {
        protected class PrioritySettings
        {
            public int AcceleratingUpdateCount = 4;
            public int LinearMovingUpdateCount = 20;
            public int StoppedUpdateCount = 60;
            public float AcceleratingPriority = 1;
            public float LinearMovingPriority = 0.33f;
            public float StoppedPriority = 0.13f;
            public int StopAfterUpdateCount = 60; // 60 frames not moving is switched to stopped
            public bool CompensateSlowSimSpeed = true;

            public static readonly PrioritySettings Default = new PrioritySettings();
        }

        public readonly MyEntity Entity;
        public readonly IMyReplicable OwnerReplicable;

        protected readonly PrioritySettings m_prioritySettings = new PrioritySettings();
        protected uint m_lastMovementFrame = 0;

        public StateGroupEnum GroupType { get { return StateGroupEnum.Physics; } }

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

        public void ClientUpdate()
        {
        }

        /// <summary>
        /// Takes into account:
        /// Static body (zero priority for static),
        /// ResponsibilityForUpdate by client (zero priority for not responsible),
        /// otherwise returns OwnerReplicable priority.
        /// </summary>
        protected float GetBasicPhysicsPriority(MyClientStateBase client)
        {
            // Called only on server
            if (Entity.Physics.IsStatic)
                return 0;

            // TODO: Rewrite and move 'ResponsibleForUpdate' to this class (when on trunk)
            var sync = (MySyncEntity)Entity.SyncObject;
            if (sync.ResponsibleForUpdate(client.GetClient()))
                return 0;

            return OwnerReplicable.GetPriority(client);
        }
        
        /// <summary>
        /// Gets priority scale and update rate based on prioritySettings.
        /// </summary>
        protected float GetMovementScale(PrioritySettings prioritySettings, out float updateOncePer)
        {
            float result;

            var sync = (MySyncEntity)Entity.SyncObject;
            if(sync.IsMoving)
            {
                m_lastMovementFrame = MyMultiplayer.Static.FrameCounter;
            }

            bool isStopped = (MyMultiplayer.Static.FrameCounter - m_lastMovementFrame) > prioritySettings.StopAfterUpdateCount;
            
            if(sync.IsAccelerating)
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

        /// <summary>
        /// Ramps the priority up or down based on how often it should send updates and when it was last sent.
        /// Returns zero when it was sent recently (not more than 'updateOncePer' before).
        /// </summary>
        protected float RampPriority(float priority, int frameCountWithoutSync, float updateOncePer)
        {
            // Ramp-up priority when without sync for a longer time than it should be
            if (frameCountWithoutSync >= updateOncePer)
            {
                // 0 is on time, 1 is delayed by a regular update time, 2 is delayed by twice the regular update time
                float lateRatio = (frameCountWithoutSync - updateOncePer) / updateOncePer;

                // When object is delayed by more than regular update time, start ramping priority by 50% per regular update time
                // E.g. object should be update once per 4 frame
                // 8 frames without update, priority *= 1.0f (stays same)
                // 12 frames without update, priority *= 1.5f
                // 16 frames without update, priority *= 2.0f
                if (lateRatio > 1)
                {
                    float ramp = (lateRatio - 1) * 0.5f;
                    priority *= ramp;
                }
                return priority;
            }
            else
            {
                return 0;
            }
        }

        float IMyStateGroup.GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client)
        {
            return GetGroupPriority(frameCountWithoutSync, client, m_prioritySettings);
        }

        protected virtual float GetGroupPriority(int frameCountWithoutSync, MyClientStateBase client, PrioritySettings settings)
        {
            float priority = GetBasicPhysicsPriority(client);
            if (priority <= 0)
                return 0;

            float updateFrameCount;
            priority *= GetMovementScale(settings, out updateFrameCount);
            
            return RampPriority(priority, frameCountWithoutSync, updateFrameCount);
        }

        public virtual void Serialize(BitStream stream, MyClientStateBase forClient, byte packetId, int maxBitPosition)
        {
            MyNetworkClient client = null;
            if (forClient != null)
                Sync.Clients.TryGetClient(forClient.EndpointId.Value, out client);

            // TODO: Rewrite and move 'SerializePhysics' to this class (when on trunk)
            ((MySyncEntity)Entity.SyncObject).SerializePhysics(stream, client, false);
        }

        public void OnAck(MyClientStateBase forClient, byte packetId, bool delivered)
        {
        }

        // Physics state
        //void SerializePhysics(BitStream stream)
        //{
        //    if (stream.Reading)
        //    {
        //        Matrix world = Matrix.Identity;
        //        bool moving = false;
        //        Vector3 linearVelocity = Vector3.Zero;
        //        Vector3 angularVelocity = Vector3.Zero;

        //        stream.SerializePositionOrientation(ref world);
        //        stream.Serialize(ref moving);
        //        if (moving)
        //        {
        //            stream.Serialize(ref linearVelocity);
        //            stream.Serialize(ref angularVelocity);
        //        }

        //        PositionComp.SetWorldMatrix(world);
        //        Physics.LinearVelocity = linearVelocity;
        //        Physics.AngularVelocity = angularVelocity;
        //    }
        //    else
        //    {
        //        Matrix world = PositionComp.WorldMatrix; // TODO:SK use just position + orientation
        //        bool moving = Physics.LinearVelocity != Vector3.Zero || Physics.AngularVelocity != Vector3.Zero;
        //        Vector3 linearVelocity = Physics.LinearVelocity;
        //        Vector3 angularVelocity = Physics.AngularVelocity;

        //        stream.SerializePositionOrientation(ref world);
        //        stream.Serialize(ref moving);
        //        if (moving)
        //        {
        //            stream.Serialize(ref linearVelocity);
        //            stream.Serialize(ref angularVelocity);
        //        }
        //    }
        //}
    }
}
