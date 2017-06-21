using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.AI
{
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_AutomaticBehaviour : MyObjectBuilder_Base
    {
        public struct DroneTargetSerializable
        {
            public long TargetId;
            public int Priority;
            public DroneTargetSerializable(long targetId, int priority)
            {
                TargetId = targetId;
                Priority = priority;
            }
        }

        public bool NeedUpdate = true;
        public bool IsActive = true;
        public bool CollisionAvoidance = true;
        public int PlayerPriority = 10;
        public float MaxPlayerDistance = 10000f;
        public bool CycleWaypoints = false;
        public long CurrentTarget = 0;
        public List<DroneTargetSerializable> TargetList = null;
        public List<long> WaypointList = null;
        public TargetPrioritization PrioritizationStyle = TargetPrioritization.PriorityRandom;
    }

    public enum TargetPrioritization
    {
        HightestPriorityFirst,
        ClosestFirst,
        PriorityRandom,
        Random
    }
}