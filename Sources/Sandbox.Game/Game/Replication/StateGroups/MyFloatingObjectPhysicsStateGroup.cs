using Sandbox.Game.Entities;
using VRage.Library.Utils;
using VRage.Network;

namespace Sandbox.Game.Replication.StateGroups
{
    public class MyFloatingObjectPhysicsStateGroup : MyEntityPhysicsStateGroup
    {
        public override StateGroupEnum GroupType { get { return StateGroupEnum.FloatingObjectPhysics; } }

        private readonly History.MyPredictedSnapshotSyncSetup m_Settings = new History.MyPredictedSnapshotSyncSetup()
        {
            ApplyRotation = false,
            ApplyPhysics = true,
            MaxPositionFactor = 100.0f,
            MaxLinearFactor = 100.0f,
            MaxRotationFactor = 100.0f,
            IterationsFactor = 1.0f,
        };

        public MyFloatingObjectPhysicsStateGroup(MyFloatingObject entity, IMyReplicable owner)
            : base(entity, owner)
        {
            m_prioritySettings.AcceleratingPriority /= 2;
            m_prioritySettings.LinearMovingPriority /= 2;
            m_prioritySettings.StoppedPriority /= 2;

            m_prioritySettings.AcceleratingUpdateCount *= 2;
            m_prioritySettings.LinearMovingUpdateCount *= 2;
            m_prioritySettings.StoppedUpdateCount *= 2;
        }

        public override void ClientUpdate(MyTimeSpan clientTimestamp)
        {
            SnapshotSync.Update(clientTimestamp, m_Settings);
        }
    }
}
