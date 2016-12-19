using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Character;
using System;
using VRage.Game.Entity;
using VRage.Library.Collections;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Profiler;
using VRageMath;

namespace Sandbox.Game.Replication.StateGroups
{
    /// <summary>
    /// Responsible for synchronizing entity physics over network
    /// </summary>
    public class MyCharacterPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        public new MyCharacter Entity { get { return (MyCharacter)base.Entity; } }

        private readonly History.MyPredictedSnapshotSyncSetup m_ControlledJetPackSettings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = false,
            ApplyPhysics = true,
            IsControlled = true,
            MaxPositionFactor = 10.0f,
            MaxLinearFactor = 1.0f,
            MaxRotationFactor = 1.0f,
            IterationsFactor = 0.5f,
        };
        private readonly History.MyPredictedSnapshotSyncSetup m_ControlledSettings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = false,
            ApplyPhysics = false,
            IsControlled = true,
            MaxPositionFactor = 100.0f,
            MaxLinearFactor = 100.0f,
            MaxRotationFactor = 100.0f,
            IterationsFactor = 1.0f,
        };
        private readonly History.MyPredictedSnapshotSyncSetup m_Settings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = true,
            ApplyPhysics = true,
            IsControlled = false,
            MaxPositionFactor = 100.0f,
            MaxLinearFactor = 100.0f,
            MaxRotationFactor = 180.0f,
            IterationsFactor = 0.25f,
        };
        public MyCharacterPhysicsStateGroup(MyEntity entity, IMyReplicable ownerReplicable)
            : base(entity, ownerReplicable)
        {
            // This is 9 bits per component which is more than enough (512 discrete values per-axis)
            //FindSupportDelegate = () => MySupportHelper.FindSupportForCharacter(Entity);
            

        }

        public override void ClientUpdate(MyTimeSpan clientTimestamp)
        {
            if (IsControlledLocally)
            {
                if (Entity.JetpackRunning)
                    SnapshotSync.Update(clientTimestamp, m_ControlledJetPackSettings);
                else 
                    SnapshotSync.Update(clientTimestamp, m_ControlledSettings);
                
            }
            else SnapshotSync.Update(clientTimestamp, m_Settings);
        }

        public override void Serialize(BitStream stream, EndpointId forClient, MyTimeSpan timeStamp, byte packetId, int maxBitPosition)
        {
            base.Serialize(stream, forClient, timeStamp, packetId, maxBitPosition);

            if (stream.Writing)
            {
                MyCharacterNetState charNetState;
                Entity.GetNetState(out charNetState);
                charNetState.Serialize(stream);
            }
            else
            {
                var charNetState = new MyCharacterNetState(stream);
                if (!IsControlledLocally)
                {
                    Entity.SetNetState(ref charNetState, true);
                }
            }
        }

        readonly PrioritySettings m_highQuality = new PrioritySettings()
        {
            // Send position updates for close characters every other frame
            AcceleratingUpdateCount = 2,
            LinearMovingUpdateCount = 2,
            LinearMovingPriority = 1.0f,
        };

        protected override bool IsMoving(MyEntity entity)
        {
            // Never know if somebody is moving entity when physics is null
            return Entity.Physics == null
                || Vector3.IsZero(entity.Physics.LinearVelocity, PRECISION) == false
                || Vector2.IsZero(Entity.RotationIndicator, PRECISION) == false
                || Entity.IsRotating 
                || Math.Abs(Entity.RollIndicator - 0.0f) > 0.001f
                || Entity.HeadMoved;
        }

        protected override float GetGroupPriority(int frameCountWithoutSync, MyClientInfo client, PrioritySettings settings)
        {
            ProfilerShort.Begin("MyCharacterPhysicsStateGroup::GetGroupPriority");
            const float highQualityDistance = 20; // under 8m, character physics sync gets high priority to have smooth movement

            MyMultiplayer.GetReplicationServer().AddToDirtyGroups(this);

            if (ResponsibleForUpdate(Entity, client.EndpointId))
            {
                if (client.PriorityMultiplier >= 1.0f)
                {
                    ProfilerShort.End();
                    return 1000.0f;
                }
            }

            var clientPos = ((MyClientState)client.State).Position;
            var characterPos = Entity.PositionComp.GetPosition();
            bool isHighQuality = Vector3D.DistanceSquared(clientPos, characterPos) < highQualityDistance * highQualityDistance;

            var priority = base.GetGroupPriority(frameCountWithoutSync, client, isHighQuality ? m_highQuality : settings);
            ProfilerShort.End();
            return priority;
        }

        public static void ReadAndSkipCharacterState(BitStream stream)
        {
            new MyCharacterNetState(stream);
        }

        public bool IsAlwaysDirty
        {
            get { return true; }
        }
    }
}
