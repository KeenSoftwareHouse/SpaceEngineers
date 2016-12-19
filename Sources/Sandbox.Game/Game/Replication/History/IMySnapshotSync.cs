using VRage.Library.Collections;
using VRage.Library.Utils;

namespace Sandbox.Game.Replication.History
{
    public class MySnapshotSyncSetup
    {
        public bool ApplyRotation;
        public bool ApplyPhysics;
        public bool IsControlled;
    }

    public interface IMySnapshotSync
    {
        void Update(MyTimeSpan clientTimestamp, MySnapshotSyncSetup setup);
        void Write(BitStream stream);
        void Read(BitStream stream, MyTimeSpan timeStamp);
        void Reset();
    }
}
