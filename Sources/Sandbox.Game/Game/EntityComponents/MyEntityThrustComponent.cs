using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using VRage.Profiler;

namespace Sandbox.Game.GameSystems
{
    public abstract class MyEntityThrustComponent : MyEntityComponentBase
    {
        private class DirectionComparer : IEqualityComparer<Vector3I>
        {
            public bool Equals(Vector3I x, Vector3I y)
            {
                return x == y;
            }

            public int GetHashCode(Vector3I obj)
            {
                Debug.Assert(
                    obj == Vector3I.Forward ||
                    obj == Vector3I.Backward ||
                    obj == Vector3I.Left ||
                    obj == Vector3I.Right ||
                    obj == Vector3I.Up ||
                    obj == Vector3I.Down);
                return obj.X + (8 * obj.Y) + (64 * obj.Z);
            }
        }

        private static readonly DirectionComparer m_directionComparer = new DirectionComparer();

        public class FuelTypeData
        {
            public Dictionary<Vector3I, HashSet<MyEntity>> ThrustsByDirection;
            public Dictionary<Vector3I, float> MaxRequirementsByDirection;

            public float CurrentRequiredFuelInput;

            public Vector3 MaxNegativeThrust;
            public Vector3 MaxPositiveThrust;

            public float MinRequiredPowerInput;
            public float MaxRequiredPowerInput;

            public int ThrustCount;

            public float Efficiency;
            public float EnergyDensity;

            public Vector3 CurrentThrust;
            public Vector3 ThrustOverride;
            public float ThrustOverridePower;
        }

        public class MyConveyorConnectedGroup
        {
            public readonly List<FuelTypeData> DataByFuelType;
            public readonly MyResourceSinkComponent ResourceSink;

            public int ThrustCount;

            public Vector3 MaxNegativeThrust;
            public Vector3 MaxPositiveThrust;

            public Vector3 ThrustOverride;
            public float ThrustOverridePower;

            public readonly List<MyDefinitionId> FuelTypes;
            public readonly Dictionary<MyDefinitionId, int> FuelTypeToIndex;

            public long LastPowerUpdate;

            // FirstEndpoint is always the entity on which the resource sink of the group is stored
            public IMyConveyorEndpoint FirstEndpoint;

            public MyConveyorConnectedGroup(IMyConveyorEndpointBlock endpointBlock)
            {
                Debug.Assert(endpointBlock != null);
                FirstEndpoint = endpointBlock.ConveyorEndpoint;
                DataByFuelType = new List<FuelTypeData>();
                ResourceSink = new MyResourceSinkComponent();

                LastPowerUpdate = MySession.Static.GameplayFrameCounter;

                FuelTypes = new List<MyDefinitionId>();
                FuelTypeToIndex = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
            }

            public bool TryGetTypeIndex(ref MyDefinitionId fuelId, out int typeIndex)
            {
                typeIndex = 0;
                if (FuelTypeToIndex.Count > 1)
                {
                    if (!FuelTypeToIndex.TryGetValue(fuelId, out typeIndex))
                        return false;
                }
                return FuelTypeToIndex.Count > 0;
            }

            public int GetTypeIndex(ref MyDefinitionId fuelId)
            {
                var typeIndex = 0;
                if (FuelTypeToIndex.Count > 1)
                {
                    int fetchedValue;
                    if (FuelTypeToIndex.TryGetValue(fuelId, out fetchedValue))
                        typeIndex = fetchedValue;
                }
                return typeIndex;
            }
        }

        #region Fields

        protected float m_lastPlanetaryInfluence = -1f;
        protected bool m_lastPlanetaryInfluenceHasAtmosphere = false;
        protected float m_lastPlanetaryGravityMagnitude = 0f;
        private int m_nextPlanetaryInfluenceRecalculation = -1;
        private const int m_maxInfluenceRecalculationInterval = 10000;

        private Vector3 m_maxNegativeThrust;
        private Vector3 m_maxPositiveThrust;

        protected readonly List<FuelTypeData> m_dataByFuelType = new List<FuelTypeData>();
        private readonly MyResourceSinkComponent m_resourceSink;

        private Vector3 m_totalMaxNegativeThrust;
        private Vector3 m_totalMaxPositiveThrust;

        protected readonly List<MyDefinitionId> m_fuelTypes = new List<MyDefinitionId>();
        private readonly Dictionary<MyDefinitionId, int> m_fuelTypeToIndex = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);

        private readonly List<MyConveyorConnectedGroup> m_connectedGroups = new List<MyConveyorConnectedGroup>();
        protected ListReader<MyConveyorConnectedGroup> ConnectedGroups { get { return new ListReader<MyConveyorConnectedGroup>(m_connectedGroups); } }

        protected MyResourceSinkComponent m_lastSink;
        protected FuelTypeData m_lastFuelTypeData;
        protected MyConveyorConnectedGroup m_lastGroup;

        protected Vector3 m_totalThrustOverride;
        protected float m_totalThrustOverridePower;

        private readonly List<MyConveyorConnectedGroup> m_groupsToTrySplit = new List<MyConveyorConnectedGroup>();
        private bool m_mergeAllGroupsDirty = true;

        [ThreadStatic]
        private static List<int> m_tmpGroupIndicesPerThread;

        [ThreadStatic]
        private static List<MyTuple<MyEntity, Vector3I>> m_tmpEntitiesWithDirectionsPerThread;

        [ThreadStatic]
        private static List<MyConveyorConnectedGroup> m_tmpGroupsPerThread;

        private static List<int> m_tmpGroupIndices { get { return MyUtils.Init(ref m_tmpGroupIndicesPerThread); } }
        private static List<MyTuple<MyEntity, Vector3I>> m_tmpEntitiesWithDirections { get { return MyUtils.Init(ref m_tmpEntitiesWithDirectionsPerThread); } }
        private static List<MyConveyorConnectedGroup> m_tmpGroups { get { return MyUtils.Init(ref m_tmpGroupsPerThread); } }

        protected readonly MyConcurrentQueue<MyTuple<MyEntity, Vector3I, Func<bool>>> m_thrustEntitiesPending = new MyConcurrentQueue<MyTuple<MyEntity, Vector3I, Func<bool>>>();
        protected readonly HashSet<MyEntity> m_thrustEntitiesRemovedBeforeRegister = new HashSet<MyEntity>();
        private MyConcurrentQueue<IMyConveyorEndpointBlock> m_conveyorEndpointsPending = new MyConcurrentQueue<IMyConveyorEndpointBlock>();
        private MyConcurrentQueue<IMyConveyorSegmentBlock> m_conveyorSegmentsPending = new MyConcurrentQueue<IMyConveyorSegmentBlock>();

        /// <summary>
        /// True whenever thrust was added or removed.
        /// </summary>
        protected bool m_thrustsChanged;

        private Vector3 m_controlThrust;
        private bool m_controlThrustChanged = false;
        protected bool ControlThrustChanged { get { return m_controlThrustChanged; } set { m_controlThrustChanged = value; } }

        private long m_lastPowerUpdate;

        private Vector3? m_maxThrustOverride;

        public Vector3? MaxThrustOverride
        {
            get { return MyFakes.ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT ? m_maxThrustOverride : null; }
            set { m_maxThrustOverride = value; }
        }

        private bool m_secondFrameUpdate = false;
        private bool m_dampenersEnabledLastFrame = true;

        #endregion Fields

        #region Properties

        public new MyEntity Entity { get { return base.Entity as MyEntity; } }

        public float MaxRequiredPowerInput { get; private set; }
        public float MinRequiredPowerInput { get; private set; }

        /// <summary>
        /// For now just the maximum slowdown factor of any thruster registered to the component
        /// </summary>
        public float SlowdownFactor { get; set; }

        public int ThrustCount { get; private set; }
        public bool DampenersEnabled { get; set; }

        private int m_counter;
        /// <summary>
        /// Torque and thrust wanted by player (from input).
        /// </summary>
        public Vector3 ControlThrust
        {
            get { return m_controlThrust; }
            set
            {
                m_controlThrustChanged |= m_controlThrust != value;
                m_controlThrust = value;
            }
        }

        /// <summary>
        /// Final thrust (clamped by available power, added anti-gravity, slowdown).
        /// </summary>
        public Vector3 FinalThrust { get; private set; }

        /// <summary>
        /// Thrust wanted by AutoPilot
        /// </summary>
        public Vector3 AutoPilotControlThrust { get; set; }

        public bool AutopilotEnabled { get; set; }

        public bool Enabled { get; set; }

        #endregion Properties

        static MyEntityThrustComponent()
        {
            MyResourceDistributorComponent.InitializeMappings();
        }

        private int InitializeType(
            MyDefinitionId fuelType,
            List<FuelTypeData> dataByTypeList,
            List<MyDefinitionId> fuelTypeList,
            Dictionary<MyDefinitionId, int> fuelTypeToIndex,
            MyResourceSinkComponent resourceSink)
        {
            dataByTypeList.Add(new FuelTypeData
            {
                ThrustsByDirection = new Dictionary<Vector3I, HashSet<MyEntity>>(6, m_directionComparer),
                MaxRequirementsByDirection = new Dictionary<Vector3I, float>(6, m_directionComparer),
                CurrentRequiredFuelInput = 0.0001f, //AB: because of update of resource component
                Efficiency = 0f,
                EnergyDensity = 0f,
            });

            int typeIndex = dataByTypeList.Count - 1;
            fuelTypeToIndex.Add(fuelType, typeIndex);
            fuelTypeList.Add(fuelType);

            foreach (var direction in Base6Directions.IntDirections)
            {
                dataByTypeList[typeIndex].ThrustsByDirection[direction] = new HashSet<MyEntity>();
            }

            var sinkData = new MyResourceSinkInfo
            {
                ResourceTypeId = fuelType,
                MaxRequiredInput = 0,
                RequiredInputFunc = () => RequiredFuelInput(dataByTypeList[typeIndex])
            };

            if (fuelTypeList.Count == 1)
            {
                resourceSink.Init(MyStringHash.GetOrCompute("Thrust"), sinkData);
                resourceSink.IsPoweredChanged += Sink_IsPoweredChanged;
                resourceSink.CurrentInputChanged += Sink_CurrentInputChanged;

                AddSinkToSystems(resourceSink, Container.Entity as MyCubeGrid);
            }
            else
                resourceSink.AddType(ref sinkData);

            return typeIndex;
        }

        protected MyEntityThrustComponent()
        {
            m_resourceSink = new MyResourceSinkComponent();
        }

        public virtual void Init()
        {
            Enabled = true;
            ThrustCount = 0;
            DampenersEnabled = true;
            MarkDirty();
            m_lastPowerUpdate = MySession.Static.GameplayFrameCounter;
        }

        public virtual void Register(MyEntity entity, Vector3I forwardVector, Func<bool> onRegisteredCallback = null)
        {
            Debug.Assert(entity != null);
            Debug.Assert(!IsRegistered(entity, forwardVector));

            Dictionary<Vector3I, HashSet<MyEntity>> thrustsByDirection;
            MyDefinitionId fuelType = FuelType(entity);
            MyResourceSinkComponent resourceSink;
            int groupIndex = -1;
            int typeIndex = -1;
            var conveyorEndpointBlock = entity as IMyConveyorEndpointBlock;
            if (MyResourceDistributorComponent.IsConveyorConnectionRequiredTotal(ref fuelType) && conveyorEndpointBlock != null)
            {
                MyConveyorConnectedGroup group;
                FindConnectedGroups(conveyorEndpointBlock, m_connectedGroups, m_tmpGroupIndices);

                if (m_tmpGroupIndices.Count >= 1)
                {
                    // Merge all groups that were connected by this block
                    if (m_tmpGroupIndices.Count > 1)
                        MergeGroups(m_connectedGroups, m_tmpGroupIndices);

                    groupIndex = m_tmpGroupIndices[0];
                    group = m_connectedGroups[groupIndex];
                }
                // Create a new group if nothing was connected
                else
                {
                    group = new MyConveyorConnectedGroup(conveyorEndpointBlock);
                    m_connectedGroups.Add(group);
                    groupIndex = m_connectedGroups.Count - 1;
                }

                // Initialize a new type into the group if it wasn't already supported
                if (!group.TryGetTypeIndex(ref fuelType, out typeIndex))
                {
                    typeIndex = InitializeType(fuelType, group.DataByFuelType, group.FuelTypes, group.FuelTypeToIndex, group.ResourceSink);

                    if (group.FuelTypes.Count == 1)
                        entity.Components.Add(group.ResourceSink);
                }

                ++group.ThrustCount;
                ++group.DataByFuelType[typeIndex].ThrustCount;
                thrustsByDirection = group.DataByFuelType[typeIndex].ThrustsByDirection;
                resourceSink = group.ResourceSink;
                m_tmpGroupIndices.Clear();
            }
            else
            {
                if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                {
                    typeIndex = InitializeType(fuelType, m_dataByFuelType, m_fuelTypes, m_fuelTypeToIndex, m_resourceSink);

                    if (m_fuelTypes.Count == 1)
                        entity.Components.Add(m_resourceSink);
                }
                else
                {
                    // We already had this type, so we already had a sink component for the group
                    entity.Components.Remove<MyResourceSinkComponent>();
                }

                thrustsByDirection = m_dataByFuelType[typeIndex].ThrustsByDirection;
                resourceSink = m_resourceSink;
                ++m_dataByFuelType[typeIndex].ThrustCount;
            }

            m_lastSink = resourceSink;
            m_lastGroup = (groupIndex == -1 ? null : m_connectedGroups[groupIndex]);

            m_lastFuelTypeData = (groupIndex == -1 ? m_dataByFuelType[typeIndex] : m_connectedGroups[groupIndex].DataByFuelType[typeIndex]);
            thrustsByDirection[forwardVector].Add(entity);
            ++ThrustCount;
            MarkDirty();
        }

        // Called when m_thrustEntitiesPending is used to delay the registration of thrust entities.
        protected virtual bool RegisterLazy(MyEntity entity, Vector3I forwardVector, Func<bool> onRegisteredCallback)
        {
            return true;
        }

        public bool IsRegistered(MyEntity entity, Vector3I forwardVector)
        {
            Debug.Assert(entity != null);

            bool isRegistered = false;
            MyDefinitionId fuelType = FuelType(entity);
            var conveyorEndpointBlock = entity as IMyConveyorEndpointBlock;
            if (MyResourceDistributorComponent.IsConveyorConnectionRequiredTotal(ref fuelType) && conveyorEndpointBlock != null)
            {
                foreach (var group in m_connectedGroups)
                {
                    int typeIndex;
                    if (!group.TryGetTypeIndex(ref fuelType, out typeIndex))
                        continue;

                    if (group.DataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Contains(entity))
                    {
                        isRegistered = true;
                        break;
                    }
                }
            }
            else
            {
                int typeIndex;
                if (TryGetTypeIndex(ref fuelType, out typeIndex))
                    isRegistered = m_dataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Contains(entity);
            }
            return isRegistered;
        }

        public virtual void Unregister(MyEntity entity, Vector3I forwardVector)
        {
            Debug.Assert(Entity != null, "Thrust Component taken out of container before all thrusters were cleaned up!");
            Debug.Assert(entity != null);

            if (entity == null || Entity == null || Entity.MarkedForClose)
                return;

            if (!IsRegistered(entity, forwardVector))
            {
                m_thrustEntitiesRemovedBeforeRegister.Add(entity);
                return;
            }

            Dictionary<Vector3I, HashSet<MyEntity>> thrustsByDirection = null;
            int thrustsLeft = 0;
            MyResourceSinkComponentBase resourceSink = null;
            MyDefinitionId fuelType = FuelType(entity);
            List<FuelTypeData> typeData = null;
            int groupIndex = -1;
            int typeIndex = -1;
            var conveyorEndpointBlock = entity as IMyConveyorEndpointBlock;
            if (MyResourceDistributorComponent.IsConveyorConnectionRequiredTotal(ref fuelType) && conveyorEndpointBlock != null)
            {
                MyConveyorConnectedGroup containingGroup = TrySplitGroup(conveyorEndpointBlock);
                if (!containingGroup.TryGetTypeIndex(ref fuelType, out typeIndex))
                {
                    Debug.Fail("Removing a thruster with fuel type that isn't registered");
                    return;
                }

                if (containingGroup.DataByFuelType[typeIndex].ThrustsByDirection[forwardVector].Contains(entity))
                {
                    thrustsLeft = --containingGroup.ThrustCount;
                    resourceSink = containingGroup.ResourceSink;
                    thrustsByDirection = containingGroup.DataByFuelType[typeIndex].ThrustsByDirection;
                    typeData = containingGroup.DataByFuelType;

                    for (int foundIndex = 0; foundIndex < m_connectedGroups.Count; ++foundIndex)
                    {
                        if (m_connectedGroups[foundIndex] == containingGroup)
                        {
                            groupIndex = foundIndex;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (!TryGetTypeIndex(ref fuelType, out typeIndex))
                {
                    Debug.Fail("Removing a thruster with fuel type that isn't registered");
                    return;
                }
                resourceSink = m_resourceSink;
                thrustsByDirection = m_dataByFuelType[typeIndex].ThrustsByDirection;
                typeData = m_dataByFuelType;

                thrustsLeft = 0;
                foreach (var fuelData in m_dataByFuelType)
                    thrustsLeft += fuelData.ThrustCount;

                thrustsLeft = Math.Max(thrustsLeft - 1, 0);
            }

            if (thrustsByDirection == null)
                return;

            var group = (groupIndex != -1 ? m_connectedGroups[groupIndex] : null);

            // If the sink of this connected group is stored in the removed entity, move it to some other entity if there are any
            MoveSinkToNewEntity(entity, typeData, typeIndex, thrustsLeft, resourceSink, group);

            thrustsByDirection[forwardVector].Remove(entity);
            resourceSink.SetMaxRequiredInputByType(fuelType, resourceSink.MaxRequiredInputByType(fuelType) - PowerAmountToFuel(ref fuelType, MaxPowerConsumption(entity), group));
            if (--typeData[typeIndex].ThrustCount == 0)
            {
                typeData.RemoveAtFast(typeIndex);
                if (group != null)
                {
                    group.FuelTypes.RemoveAtFast(typeIndex);
                    group.FuelTypeToIndex.Remove(fuelType);
                }
                else
                {
                    m_fuelTypes.RemoveAtFast(typeIndex);
                    m_fuelTypeToIndex.Remove(fuelType);
                }
            }

            if (thrustsLeft == 0)
            {
                RemoveSinkFromSystems(resourceSink, Container.Entity as MyCubeGrid);
                if (groupIndex != -1)
                    m_connectedGroups.RemoveAt(groupIndex);
            }
            --ThrustCount;
            MarkDirty();
        }

        private void MoveSinkToNewEntity(MyEntity entity, List<FuelTypeData> fuelData, int typeIndex, int thrustsLeftInGroup, MyResourceSinkComponentBase resourceSink, MyConveyorConnectedGroup containingGroup)
        {
            if (!(Container.Entity is MyCubeGrid))
                return;

            var existingSink = entity.Components.Get<MyResourceSinkComponent>();
            if (existingSink == resourceSink)
            {
                entity.Components.Remove<MyResourceSinkComponent>();
                if (thrustsLeftInGroup > 0)
                {
                    foreach (var thrustSet in fuelData[typeIndex].ThrustsByDirection.Values)
                    {
                        if (thrustSet.Count <= 0)
                            continue;

                        bool sinkSet = false;
                        foreach (var thrustEntity in thrustSet)
                        {
                            if (thrustEntity == entity)
                                continue;

                            thrustEntity.Components.Add(resourceSink);
                            AddSinkToSystems(resourceSink as MyResourceSinkComponent, Entity as MyCubeGrid);
                            sinkSet = true;
                            if (containingGroup != null)
                                containingGroup.FirstEndpoint = (thrustEntity as IMyConveyorEndpointBlock).ConveyorEndpoint;
                            break;
                        }
                        if (sinkSet)
                            break;
                    }
                }
            }
        }

        private void MergeGroups(List<MyConveyorConnectedGroup> groups, List<int> connectedGroupIndices)
        {
            Debug.Assert(groups != null && groups.Count > 1 && connectedGroupIndices != null && connectedGroupIndices.Count > 1, "Can only merge two or more groups!");

            int mergeToIndex = int.MinValue;
            int mostThrusts = int.MinValue;

            // Find the largest group to merge the others into
            foreach (int groupIndex in connectedGroupIndices)
            {
                var group = groups[groupIndex];
                if (group.ThrustCount > mostThrusts)
                {
                    mergeToIndex = groupIndex;
                    mostThrusts = group.ThrustCount;
                }
            }

            var mergeToGroup = groups[mergeToIndex];
            foreach (int groupIndex in connectedGroupIndices)
            {
                if (groupIndex == mergeToIndex)
                    continue;

                var group = groups[groupIndex];
                foreach (var fuelType in group.FuelTypes)
                {
                    var fuelId = fuelType;
                    int typeIndex;
                    if (!mergeToGroup.TryGetTypeIndex(ref fuelId, out typeIndex))
                        typeIndex = InitializeType(fuelType, mergeToGroup.DataByFuelType, mergeToGroup.FuelTypes, mergeToGroup.FuelTypeToIndex, mergeToGroup.ResourceSink);

                    FuelTypeData mergeToDataByType = mergeToGroup.DataByFuelType[typeIndex];
                    FuelTypeData mergeFromDataByType = group.DataByFuelType[typeIndex];
                    mergeToDataByType.MaxRequiredPowerInput += mergeFromDataByType.MaxRequiredPowerInput;
                    mergeToDataByType.MinRequiredPowerInput += mergeFromDataByType.MinRequiredPowerInput;
                    mergeToDataByType.CurrentRequiredFuelInput += mergeFromDataByType.CurrentRequiredFuelInput;
                    mergeToDataByType.MaxNegativeThrust += mergeFromDataByType.MaxNegativeThrust;
                    mergeToDataByType.MaxPositiveThrust += mergeFromDataByType.MaxPositiveThrust;
                    mergeToDataByType.ThrustOverride += mergeFromDataByType.ThrustOverride;
                    mergeToDataByType.ThrustOverridePower += mergeFromDataByType.ThrustOverridePower;
                    mergeToDataByType.ThrustCount += mergeFromDataByType.ThrustCount;

                    foreach (var direction in Base6Directions.IntDirections)
                    {
                        float requirementFrom;
                        bool found = mergeFromDataByType.MaxRequirementsByDirection.TryGetValue(direction, out requirementFrom);
                        if (found)
                        {
                            float requirementTo;
                            found = mergeToDataByType.MaxRequirementsByDirection.TryGetValue(direction, out requirementTo);
                            if (found)
                                mergeToDataByType.MaxRequirementsByDirection[direction] = requirementTo + requirementFrom;
                            else
                                mergeToDataByType.MaxRequirementsByDirection[direction] = requirementFrom;
                        }

                        if (!mergeToDataByType.ThrustsByDirection.ContainsKey(direction))
                        {
                            mergeToDataByType.ThrustsByDirection[direction] = new HashSet<MyEntity>();
                            Debug.Fail("ThrustByDirection missing " + direction.ToString() + " direction");
                        }

                        var mergeToThrustsByDirection = mergeToDataByType.ThrustsByDirection[direction];
                        if (mergeFromDataByType.ThrustsByDirection.ContainsKey(direction))
                        {
                            foreach (var thrust in mergeFromDataByType.ThrustsByDirection[direction])
                            {
                                mergeToThrustsByDirection.Add(thrust);
                                thrust.Components.Remove<MyResourceSinkComponent>();
                            }
                        }
                        else
                        {
                            Debug.Fail("ThrustByDirection missing " + direction.ToString() + " direction");
                        }
                    }
                    mergeToGroup.ThrustCount += group.ThrustCount;
                    mergeToGroup.ThrustOverride += group.ThrustOverride;
                    mergeToGroup.ThrustOverridePower += group.ThrustOverridePower;
                    mergeToGroup.MaxNegativeThrust += group.MaxNegativeThrust;
                    mergeToGroup.MaxPositiveThrust += group.MaxPositiveThrust;

                    RemoveSinkFromSystems(group.ResourceSink, Container.Entity as MyCubeGrid);
                }
            }
            connectedGroupIndices.Sort();
            for (int connectedIndexIndex = connectedGroupIndices.Count - 1; connectedIndexIndex >= 0; --connectedIndexIndex)
            {
                if (connectedGroupIndices[connectedIndexIndex] == mergeToIndex)
                    continue;

                if (connectedGroupIndices[connectedIndexIndex] < mergeToIndex)
                    mergeToIndex--;
                groups.RemoveAtFast(connectedGroupIndices[connectedIndexIndex]);
                connectedGroupIndices.RemoveAt(connectedIndexIndex);
            }
            mergeToGroup.ResourceSink.Update();
            connectedGroupIndices[0] = mergeToIndex;
        }

        public void MergeAllGroupsDirty()
        {
            m_mergeAllGroupsDirty = true;
        }

        private void TryMergeAllGroups()
        {
            if (m_connectedGroups == null || m_connectedGroups.Count == 0)
                return;

            int groupIndex = 0;
            do
            {
                var group = m_connectedGroups[groupIndex];
                IMyConveyorEndpointBlock representativeBlock = group.FirstEndpoint != null ? group.FirstEndpoint.CubeBlock as IMyConveyorEndpointBlock : null;
                if (representativeBlock == null)
                    continue;

                FindConnectedGroups(representativeBlock, m_connectedGroups, m_tmpGroupIndices);
                if (m_tmpGroupIndices.Count > 1)
                {
                    MergeGroups(m_connectedGroups, m_tmpGroupIndices);
                    --groupIndex;
                }

                m_tmpGroupIndices.Clear();
                ++groupIndex;
            } while (groupIndex < m_connectedGroups.Count);
        }

        private static void FindConnectedGroups(IMyConveyorSegmentBlock block, List<MyConveyorConnectedGroup> groups, List<int> outConnectedGroupIndices)
        {
            Debug.Assert(groups != null);
            Debug.Assert(outConnectedGroupIndices != null && outConnectedGroupIndices.Count == 0, "FindConnectedGroups called with null or non-empty list!");
            if (block.ConveyorSegment.ConveyorLine == null)
                return;

            var segmentEndpoint = block.ConveyorSegment.ConveyorLine.GetEndpoint(0) ?? block.ConveyorSegment.ConveyorLine.GetEndpoint(1);
            if (segmentEndpoint == null)
                return;

            for (int groupIndex = 0; groupIndex < groups.Count; ++groupIndex)
            {
                var group = groups[groupIndex];
                if (MyGridConveyorSystem.Reachable(group.FirstEndpoint, segmentEndpoint))
                    outConnectedGroupIndices.Add(groupIndex);
            }
        }

        private static void FindConnectedGroups(IMyConveyorEndpointBlock block, List<MyConveyorConnectedGroup> groups, List<int> outConnectedGroupIndices)
        {
            Debug.Assert(groups != null);
            Debug.Assert(outConnectedGroupIndices != null && outConnectedGroupIndices.Count == 0, "FindConnectedGroups called with null or non-empty list!");
            Debug.Assert(block != null && block.ConveyorEndpoint != null, "Conveyor endpoint null!");
            for (int groupIndex = 0; groupIndex < groups.Count; ++groupIndex)
            {
                var group = groups[groupIndex];
                if (group.FirstEndpoint == null)
                {
                    Debug.Fail("First endpoint of group cannot be null!");
                    continue;
                }
                if (MyGridConveyorSystem.Reachable(group.FirstEndpoint, block.ConveyorEndpoint))
                    outConnectedGroupIndices.Add(groupIndex);
            }
        }

        /// <summary>
        /// Tries to split the group containing the given block at the position of the block. Leaves the block in one of the new groups (or the old one if no splits happened) and returns that group
        /// If conveyorEndpointBlock, it uses groupOverride for the group instead.
        /// </summary>
        private MyConveyorConnectedGroup TrySplitGroup(IMyConveyorEndpointBlock conveyorEndpointBlock, MyConveyorConnectedGroup groupOverride = null)
        {
            MyConveyorConnectedGroup returnGroup = null;
            Debug.Assert(conveyorEndpointBlock != null || groupOverride != null, "TrySplitGroup called with a null block!");

            var endpointEntity = conveyorEndpointBlock as MyEntity;
            returnGroup = groupOverride ?? FindEntityGroup(endpointEntity);
            if (returnGroup == null)
                return null;

            if (conveyorEndpointBlock != null && conveyorEndpointBlock.ConveyorEndpoint == returnGroup.FirstEndpoint)
            {
                if (returnGroup.ThrustCount == 1)
                    return returnGroup;

                // Move the reference endpoint and the sink to another entity in this group
                foreach (var fuelData in returnGroup.DataByFuelType)
                {
                    bool endpointMoved = false;
                    foreach (var thrustSet in fuelData.ThrustsByDirection.Values)
                    {
                        foreach (var thrustEntity in thrustSet)
                        {
                            if (thrustEntity == endpointEntity)
                                continue;

                            returnGroup.FirstEndpoint = (thrustEntity as IMyConveyorEndpointBlock).ConveyorEndpoint;
                            endpointEntity.Components.Remove<MyResourceSinkComponent>();
                            thrustEntity.Components.Add(returnGroup.ResourceSink);
                            endpointMoved = true;
                        }
                        if (endpointMoved)
                            break;
                    }
                    if (endpointMoved)
                        break;
                }
            }

            // Find all the newly disconnected blocks
            foreach (var direction in Base6Directions.IntDirections)
            {
                for (int typeIndex = 0; typeIndex < returnGroup.FuelTypes.Count; ++typeIndex)
                {
                    var fuelData = returnGroup.DataByFuelType[typeIndex];
                    foreach (var thrustEntity in fuelData.ThrustsByDirection[direction])
                    {
                        Debug.Assert(thrustEntity is IMyConveyorEndpointBlock);
                        var endpoint = (thrustEntity as IMyConveyorEndpointBlock).ConveyorEndpoint;
                        if (endpointEntity == thrustEntity) // Leave the splitting block in the original group
                            continue;

                        if (!MyGridConveyorSystem.Reachable(endpoint, returnGroup.FirstEndpoint))
                        {
                            var fuelType = FuelType(thrustEntity);
                            returnGroup.ResourceSink.SetMaxRequiredInputByType(fuelType, returnGroup.ResourceSink.MaxRequiredInputByType(fuelType) - PowerAmountToFuel(ref fuelType, MaxPowerConsumption(thrustEntity), returnGroup));
                            --fuelData.ThrustCount;
                            --returnGroup.ThrustCount;
                            m_tmpEntitiesWithDirections.Add(new MyTuple<MyEntity, Vector3I>(thrustEntity, direction));
                        }
                    }

                    foreach (var entityAndDirection in m_tmpEntitiesWithDirections)
                    {
                        fuelData.ThrustsByDirection[entityAndDirection.Item2].Remove(entityAndDirection.Item1);
                        RemoveFromGroup(entityAndDirection.Item1, returnGroup);
                    }
                }
            }

            foreach (var entityAndDirection in m_tmpEntitiesWithDirections)
            {
                MyEntity entity = entityAndDirection.Item1;
                Vector3I direction = entityAndDirection.Item2;
                var fuelType = FuelType(entity);
                bool foundGroup = false;
                foreach (var newGroup in m_tmpGroups)
                {
                    // A group connected to this entity was already created
                    if (MyGridConveyorSystem.Reachable((entity as IMyConveyorEndpointBlock).ConveyorEndpoint, newGroup.FirstEndpoint))
                    {
                        int typeIndex;
                        // Initialize a new type for this group if necessary
                        if (!newGroup.TryGetTypeIndex(ref fuelType, out typeIndex))
                            typeIndex = InitializeType(fuelType, newGroup.DataByFuelType, newGroup.FuelTypes, newGroup.FuelTypeToIndex, newGroup.ResourceSink);

                        var fuelData = newGroup.DataByFuelType[typeIndex];
                        fuelData.ThrustsByDirection[direction].Add(entity);
                        AddToGroup(entity, newGroup);
                        ++fuelData.ThrustCount;
                        ++newGroup.ThrustCount;
                        newGroup.ResourceSink.SetMaxRequiredInputByType(fuelType, newGroup.ResourceSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, MaxPowerConsumption(entity), newGroup));
                        foundGroup = true;
                        break;
                    }
                }

                if (foundGroup)
                    continue;

                var createdGroup = new MyConveyorConnectedGroup(entity as IMyConveyorEndpointBlock);
                m_tmpGroups.Add(createdGroup);
                m_connectedGroups.Add(createdGroup);

                var createdTypeIndex = InitializeType(fuelType, createdGroup.DataByFuelType, createdGroup.FuelTypes, createdGroup.FuelTypeToIndex, createdGroup.ResourceSink);
                entity.Components.Add(createdGroup.ResourceSink);

                var createdFuelData = createdGroup.DataByFuelType[createdTypeIndex];
                var otherTypeIndex = returnGroup.GetTypeIndex(ref fuelType);
                createdFuelData.Efficiency = returnGroup.DataByFuelType[otherTypeIndex].Efficiency;
                createdFuelData.EnergyDensity = returnGroup.DataByFuelType[otherTypeIndex].EnergyDensity;
                createdFuelData.ThrustsByDirection[direction].Add(entity);
                AddToGroup(entity, createdGroup);
                ++createdFuelData.ThrustCount;
                ++createdGroup.ThrustCount;
                createdGroup.ResourceSink.SetMaxRequiredInputByType(fuelType, createdGroup.ResourceSink.MaxRequiredInputByType(fuelType) + PowerAmountToFuel(ref fuelType, MaxPowerConsumption(entity), createdGroup));
            }
            m_tmpGroups.Clear();
            m_tmpEntitiesWithDirections.Clear();

            Debug.Assert(returnGroup != null, "TrySplitGroup should always return a valid group!");
            return returnGroup;
        }

        private static void AddSinkToSystems(MyResourceSinkComponent resourceSink, MyCubeGrid cubeGrid)
        {
            if (cubeGrid == null)
                return;

            var gridSystems = cubeGrid.GridSystems;
            if (gridSystems == null)
                return;

            if (gridSystems.ResourceDistributor != null)
            {
                gridSystems.ResourceDistributor.AddSink(resourceSink);
            }
        }

        private static void RemoveSinkFromSystems(MyResourceSinkComponentBase resourceSink, MyCubeGrid cubeGrid)
        {
            if (cubeGrid == null)
                return;

            var gridSystems = cubeGrid.GridSystems;
            if (gridSystems == null)
                return;

            if (gridSystems.ResourceDistributor != null)
                gridSystems.ResourceDistributor.RemoveSink(resourceSink as MyResourceSinkComponent);
        }

        private void Sink_CurrentInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            m_controlThrustChanged = true;
            if (Entity is MyCubeGrid && Entity.Physics != null && !Entity.Physics.RigidBody.IsActive)
                (Entity as MyCubeGrid).ActivatePhysics();
        }

        private void Sink_IsPoweredChanged()
        {
            MarkDirty();
            if (Entity is MyCubeGrid && Entity.Physics != null && !Entity.Physics.RigidBody.IsActive)
                (Entity as MyCubeGrid).ActivatePhysics();
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            var cubeGrid = Entity as MyCubeGrid;
            if (cubeGrid == null)
                return;

            cubeGrid.OnBlockAdded += CubeGrid_OnBlockAdded;
            cubeGrid.GridSystems.ConveyorSystem.OnBeforeRemoveSegmentBlock += ConveyorSystem_OnBeforeRemoveSegmentBlock;
            cubeGrid.GridSystems.ConveyorSystem.OnBeforeRemoveEndpointBlock += ConveyorSystem_OnBeforeRemoveEndpointBlock;
            cubeGrid.GridSystems.ConveyorSystem.ResourceSink.IsPoweredChanged += ConveyorSystem_OnPoweredChanged;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            foreach (var group in m_connectedGroups)
            {
                RemoveSinkFromSystems(group.ResourceSink, Container.Entity as MyCubeGrid);
            }

            RemoveSinkFromSystems(m_resourceSink, Container.Entity as MyCubeGrid);

            var cubeGrid = Entity as MyCubeGrid;
            if (cubeGrid == null)
                return;

            cubeGrid.OnBlockAdded -= CubeGrid_OnBlockAdded;
            cubeGrid.GridSystems.ConveyorSystem.OnBeforeRemoveSegmentBlock -= ConveyorSystem_OnBeforeRemoveSegmentBlock;
            cubeGrid.GridSystems.ConveyorSystem.OnBeforeRemoveEndpointBlock -= ConveyorSystem_OnBeforeRemoveEndpointBlock;
            cubeGrid.GridSystems.ConveyorSystem.ResourceSink.IsPoweredChanged -= ConveyorSystem_OnPoweredChanged;
        }

        public virtual void UpdateBeforeSimulation(bool updateDampeners)
        {
            if (Entity == null)
                return;

            ProfilerShort.Begin("EntityThrustComponent.UpdateBeforeSimulation");

            if (Entity.InScene)
                UpdateConveyorSystemChanges();

            if (ThrustCount == 0)
            {
                Entity.Components.Remove<MyEntityThrustComponent>();
                ProfilerShort.End();
                return;
            }

            var frameCounter = MySession.Static.GameplayFrameCounter;
            if (frameCounter >= m_nextPlanetaryInfluenceRecalculation)
                RecalculatePlanetaryInfluence();

            ProfilerShort.BeginNextBlock("RecomputeThrustParameters");
            if (m_thrustsChanged)
            {
                RecomputeThrustParameters();
                if (Entity is MyCubeGrid && Entity.Physics != null && !Entity.Physics.RigidBody.IsActive)
                    (Entity as MyCubeGrid).ActivatePhysics();
            }

            ProfilerShort.BeginNextBlock("UpdateThrusts");
            if (Enabled && Entity.Physics != null)
            {
                UpdateThrusts(updateDampeners);
                if (m_thrustsChanged)
                    RecomputeThrustParameters();
            }

            if (DampenersEnabled == false && m_dampenersEnabledLastFrame)//turn off thruster power for not overriden after dampener is disabled
            {
                foreach (var group in m_connectedGroups)
                {
                    if (group.DataByFuelType.Count > 0)
                        TurnOffThrusterFlame(group.DataByFuelType);
                }
                if (m_dataByFuelType.Count > 0)
                    TurnOffThrusterFlame(m_dataByFuelType);
            }
            m_dampenersEnabledLastFrame = DampenersEnabled;
            m_thrustsChanged = false;
            ProfilerShort.End();
        }

        private void TurnOffThrusterFlame(List<FuelTypeData> dataByFuelType)
        {
            foreach (var data in dataByFuelType)
            {
                foreach (var direction in data.ThrustsByDirection)
                {
                    foreach (var thrustEntity in direction.Value)
                    {
                        var thrust = thrustEntity as MyThrust;
                        if (thrust != null && thrust.ThrustOverride <= 0)
                            thrust.CurrentStrength = 0;
                    }
                }
            }
        }

        private void RecomputeThrustParameters() // Only gets called when m_thrustsChanged is set
        {
            m_secondFrameUpdate = true;
            if (!m_thrustsChanged && m_secondFrameUpdate)
                m_secondFrameUpdate = false;
            m_totalThrustOverride = Vector3.Zero;
            m_totalThrustOverridePower = 0;

            m_maxPositiveThrust = new Vector3();
            m_maxNegativeThrust = new Vector3();
            m_totalMaxNegativeThrust = new Vector3();
            m_totalMaxPositiveThrust = new Vector3();
            MaxRequiredPowerInput = 0.0f;
            MinRequiredPowerInput = 0.0f;
            foreach (FuelTypeData dataByType in m_dataByFuelType)
            {
                RecomputeTypeThrustParameters(dataByType);

                MaxRequiredPowerInput += dataByType.MaxRequiredPowerInput;
                MinRequiredPowerInput += dataByType.MinRequiredPowerInput;

                m_maxPositiveThrust += dataByType.MaxPositiveThrust;
                m_maxNegativeThrust += dataByType.MaxNegativeThrust;
                m_totalThrustOverride += dataByType.ThrustOverride;
                m_totalThrustOverridePower += dataByType.ThrustOverridePower;
            }

            m_totalMaxNegativeThrust += m_maxNegativeThrust;
            m_totalMaxPositiveThrust += m_maxPositiveThrust;

            foreach (var group in m_connectedGroups)
            {
                group.MaxPositiveThrust = new Vector3();
                group.MaxNegativeThrust = new Vector3();
                group.ThrustOverride = new Vector3();
                group.ThrustOverridePower = 0f;

                foreach (FuelTypeData dataByType in group.DataByFuelType)
                {
                    RecomputeTypeThrustParameters(dataByType);

                    MaxRequiredPowerInput += dataByType.MaxRequiredPowerInput;
                    MinRequiredPowerInput += dataByType.MinRequiredPowerInput;

                    group.MaxPositiveThrust += dataByType.MaxPositiveThrust;
                    group.MaxNegativeThrust += dataByType.MaxNegativeThrust;
                    group.ThrustOverride += dataByType.ThrustOverride;
                    group.ThrustOverridePower += dataByType.ThrustOverridePower;
                }

                m_totalMaxNegativeThrust += group.MaxNegativeThrust;
                m_totalMaxPositiveThrust += group.MaxPositiveThrust;
            }
        }

        public float GetMaxThrustInDirection(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                default:
                case Base6Directions.Direction.Forward:
                    return m_maxPositiveThrust.Z;
                case Base6Directions.Direction.Up:
                    return m_maxPositiveThrust.Y;
                case Base6Directions.Direction.Right:
                    return m_maxPositiveThrust.X;
                case Base6Directions.Direction.Backward:
                    return m_maxNegativeThrust.Z;
                case Base6Directions.Direction.Left:
                    return m_maxNegativeThrust.X;
                case Base6Directions.Direction.Down:
                    return m_maxNegativeThrust.Y;
            }
        }

        private void RecomputeTypeThrustParameters(FuelTypeData fuelData)
        {
            fuelData.MaxRequiredPowerInput = 0f;
            fuelData.MinRequiredPowerInput = 0f;
            fuelData.MaxPositiveThrust = new Vector3();
            fuelData.MaxNegativeThrust = new Vector3();
            fuelData.MaxRequirementsByDirection.Clear();
            fuelData.ThrustOverride = new Vector3();
            fuelData.ThrustOverridePower = 0f;

            foreach (var dir in fuelData.ThrustsByDirection)
            {
                if (!fuelData.MaxRequirementsByDirection.ContainsKey(dir.Key))
                    fuelData.MaxRequirementsByDirection[dir.Key] = 0f;

                float maxRequiredPower = 0;
                foreach (MyEntity thrustEntity in dir.Value)
                {
                    if (RecomputeOverriddenParameters(thrustEntity, fuelData))
                        continue;

                    if (!IsUsed(thrustEntity))
                        continue;

                    var forceMagnitude = ForceMagnitude(thrustEntity, m_lastPlanetaryInfluence, m_lastPlanetaryInfluenceHasAtmosphere);
                    var forceMultiplier = CalculateForceMultiplier(thrustEntity, m_lastPlanetaryInfluence, m_lastPlanetaryInfluenceHasAtmosphere);
                    float consumptionMultiplier = CalculateConsumptionMultiplier(thrustEntity, m_lastPlanetaryGravityMagnitude);

                    if (thrustEntity is MyThrust && !(thrustEntity as MyThrust).IsPowered)
                    {
                        fuelData.MaxPositiveThrust += 0;
                        fuelData.MaxNegativeThrust += 0;
                    }
                    else
                    {
                    fuelData.MaxPositiveThrust += Vector3.Clamp(-dir.Key * forceMagnitude, Vector3.Zero, Vector3.PositiveInfinity);
                    fuelData.MaxNegativeThrust += -Vector3.Clamp(-dir.Key * forceMagnitude, Vector3.NegativeInfinity, Vector3.Zero);
                    }

                    maxRequiredPower += MaxPowerConsumption(thrustEntity) * forceMultiplier * consumptionMultiplier;
                    fuelData.MinRequiredPowerInput += MinPowerConsumption(thrustEntity) * consumptionMultiplier;
                }
                fuelData.MaxRequirementsByDirection[dir.Key] += maxRequiredPower;
            }

            fuelData.MaxRequiredPowerInput += Math.Max(fuelData.MaxRequirementsByDirection[Vector3I.Forward], fuelData.MaxRequirementsByDirection[Vector3I.Backward]);
            fuelData.MaxRequiredPowerInput += Math.Max(fuelData.MaxRequirementsByDirection[Vector3I.Left], fuelData.MaxRequirementsByDirection[Vector3I.Right]);
            fuelData.MaxRequiredPowerInput += Math.Max(fuelData.MaxRequirementsByDirection[Vector3I.Up], fuelData.MaxRequirementsByDirection[Vector3I.Down]);
        }

        protected virtual void UpdateThrusts(bool applyDampeners)
        {
            ProfilerShort.Begin("Compute Thrust");
            for (int i = 0; i < m_dataByFuelType.Count; i++)
            {
                FuelTypeData fuelData = m_dataByFuelType[i];

                if (AutopilotEnabled)
                    ComputeAiThrust(AutoPilotControlThrust, fuelData);
                else
                    ComputeBaseThrust(ref m_controlThrust, fuelData, applyDampeners);
            }

            for (int i = 0; i < m_connectedGroups.Count; i++)
            {
                MyConveyorConnectedGroup group = m_connectedGroups[i];

                for (int j = 0; j < group.DataByFuelType.Count; j++)
                {
                    FuelTypeData fuelData = group.DataByFuelType[j];

                    if (AutopilotEnabled)
                        ComputeAiThrust(AutoPilotControlThrust, fuelData);
                    else
                        ComputeBaseThrust(ref m_controlThrust, fuelData, applyDampeners);
                }
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Thrust strength and modifiers");
            FinalThrust = new Vector3();

            for (int typeIndex = 0; typeIndex < m_dataByFuelType.Count; ++typeIndex)
            {
                MyDefinitionId fuelType = m_fuelTypes[typeIndex];
                var fuelData = m_dataByFuelType[typeIndex];

                ProfilerShort.Begin("UpdatePowerAndThrustStrength");
                if (( Entity.Physics.RigidBody == null || Entity.Physics.RigidBody.IsActive || m_thrustsChanged))
                    UpdatePowerAndThrustStrength(fuelData.CurrentThrust, fuelType, null, true);

                ProfilerShort.End();
                Vector3 thrustBeforeApply;
                var maxThrust = (m_maxPositiveThrust + m_maxNegativeThrust);
                thrustBeforeApply.X = maxThrust.X != 0 ? fuelData.CurrentThrust.X * (fuelData.MaxPositiveThrust.X + fuelData.MaxNegativeThrust.X) / maxThrust.X : 0f;
                thrustBeforeApply.Y = maxThrust.Y != 0 ? fuelData.CurrentThrust.Y * (fuelData.MaxPositiveThrust.Y + fuelData.MaxNegativeThrust.Y) / maxThrust.Y : 0f;
                thrustBeforeApply.Z = maxThrust.Z != 0 ? fuelData.CurrentThrust.Z * (fuelData.MaxPositiveThrust.Z + fuelData.MaxNegativeThrust.Z) / maxThrust.Z : 0f;
                Vector3 finalThrust = ApplyThrustModifiers(ref fuelType, ref thrustBeforeApply, ref m_totalThrustOverride, m_resourceSink);
                FinalThrust += finalThrust;
            }

            for (int i = 0; i < m_connectedGroups.Count; i++)
            {
                MyConveyorConnectedGroup group = m_connectedGroups[i];

                for (int typeIndex = 0; typeIndex < group.DataByFuelType.Count; ++typeIndex)
                {
                    MyDefinitionId fuelType = group.FuelTypes[typeIndex];
                    FuelTypeData fuelData = group.DataByFuelType[typeIndex];

                    ProfilerShort.Begin("UpdatePowerAndThrustStrength");
                    if ((Entity.Physics.RigidBody == null || Entity.Physics.RigidBody.IsActive || m_thrustsChanged))
                        UpdatePowerAndThrustStrength(fuelData.CurrentThrust, fuelType, group, true);

                    ProfilerShort.End();
                    Vector3 thrustBeforeApply;
                    var maxThrust = (group.MaxPositiveThrust + group.MaxNegativeThrust);
                    thrustBeforeApply.X = maxThrust.X != 0 ? fuelData.CurrentThrust.X * (fuelData.MaxPositiveThrust.X + fuelData.MaxNegativeThrust.X) / maxThrust.X : 0f;
                    thrustBeforeApply.Y = maxThrust.Y != 0 ? fuelData.CurrentThrust.Y * (fuelData.MaxPositiveThrust.Y + fuelData.MaxNegativeThrust.Y) / maxThrust.Y : 0f;
                    thrustBeforeApply.Z = maxThrust.Z != 0 ? fuelData.CurrentThrust.Z * (fuelData.MaxPositiveThrust.Z + fuelData.MaxNegativeThrust.Z) / maxThrust.Z : 0f;
                    Vector3 finalThrust = ApplyThrustModifiers(ref fuelType, ref thrustBeforeApply, ref group.ThrustOverride, group.ResourceSink);
                    FinalThrust += finalThrust;
                }
            }

            ProfilerShort.End();

            m_controlThrustChanged = false;
            //m_thrustsChanged = false;
        }

        public Vector3 GetAutoPilotThrustForDirection(Vector3 direction)
        {
            foreach (FuelTypeData fuelData in m_dataByFuelType)
            {
                ComputeAiThrust(AutoPilotControlThrust, fuelData);
            }
            foreach (MyConveyorConnectedGroup group in m_connectedGroups)
            {
                foreach (FuelTypeData fuelData in group.DataByFuelType)
                {
                    ComputeAiThrust(AutoPilotControlThrust, fuelData);
                }
            }

            var finalThrust = new Vector3();
            for (int typeIndex = 0; typeIndex < m_dataByFuelType.Count; ++typeIndex)
            {
                MyDefinitionId fuelType = m_fuelTypes[typeIndex];
                var fuelData = m_dataByFuelType[typeIndex];

                ProfilerShort.Begin("UpdatePowerAndThrustStrength");
                UpdatePowerAndThrustStrength(fuelData.CurrentThrust, fuelType, null, true);
                ProfilerShort.End();
                Vector3 thrustBeforeApply;
                var maxThrust = (m_maxPositiveThrust + m_maxNegativeThrust);
                thrustBeforeApply.X = maxThrust.X != 0 ? fuelData.CurrentThrust.X * (fuelData.MaxPositiveThrust.X + fuelData.MaxNegativeThrust.X) / maxThrust.X : 0f;
                thrustBeforeApply.Y = maxThrust.Y != 0 ? fuelData.CurrentThrust.Y * (fuelData.MaxPositiveThrust.Y + fuelData.MaxNegativeThrust.Y) / maxThrust.Y : 0f;
                thrustBeforeApply.Z = maxThrust.Z != 0 ? fuelData.CurrentThrust.Z * (fuelData.MaxPositiveThrust.Z + fuelData.MaxNegativeThrust.Z) / maxThrust.Z : 0f;

                finalThrust += ApplyThrustModifiers(ref fuelType, ref thrustBeforeApply, ref m_totalThrustOverride, m_resourceSink);
            }
            foreach (var group in m_connectedGroups)
            {
                for (int typeIndex = 0; typeIndex < group.DataByFuelType.Count; ++typeIndex)
                {
                    MyDefinitionId fuelType = group.FuelTypes[typeIndex];
                    var fuelData = group.DataByFuelType[typeIndex];

                    ProfilerShort.Begin("UpdatePowerAndThrustStrength");
                    UpdatePowerAndThrustStrength(fuelData.CurrentThrust, fuelType, group, true);
                    ProfilerShort.End();
                    Vector3 thrustBeforeApply;
                    var maxThrust = (group.MaxPositiveThrust + group.MaxNegativeThrust);
                    thrustBeforeApply.X = maxThrust.X != 0 ? fuelData.CurrentThrust.X * (fuelData.MaxPositiveThrust.X + fuelData.MaxNegativeThrust.X) / maxThrust.X : 0f;
                    thrustBeforeApply.Y = maxThrust.Y != 0 ? fuelData.CurrentThrust.Y * (fuelData.MaxPositiveThrust.Y + fuelData.MaxNegativeThrust.Y) / maxThrust.Y : 0f;
                    thrustBeforeApply.Z = maxThrust.Z != 0 ? fuelData.CurrentThrust.Z * (fuelData.MaxPositiveThrust.Z + fuelData.MaxNegativeThrust.Z) / maxThrust.Z : 0f;

                    finalThrust += ApplyThrustModifiers(ref fuelType, ref thrustBeforeApply, ref group.ThrustOverride, group.ResourceSink);
                }
            }
            m_controlThrustChanged = false;

            return finalThrust;
        }

        private void ComputeBaseThrust(ref Vector3 controlThrust, FuelTypeData fuelData, bool applyDampeners)
        {
            if (Entity.Physics == null)
            {
                fuelData.CurrentThrust = Vector3.Zero;
                return;
            }

            ProfilerShort.Begin("ComputeBaseThrust A");

            Matrix invWorldRot = Entity.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            const float stoppingTime = 0.5f;

            Vector3 gravityVector = Entity.Physics.Gravity * stoppingTime;

            bool applyLocalVelocity = applyDampeners;

            Vector3 localVelocity = Vector3.Transform((applyLocalVelocity ? Entity.Physics.LinearVelocity : Vector3.Zero) + gravityVector, invWorldRot);
            Vector3 positiveControl = Vector3.Clamp(controlThrust, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(controlThrust, -Vector3.One, Vector3.Zero);
            Vector3 slowdownControl = Vector3.Zero;

            if (DampenersEnabled && (Entity.Physics.RigidBody == null || Entity.Physics.RigidBody.IsActive || controlThrust != Vector3.Zero))
            {
                slowdownControl = Vector3.IsZeroVector(controlThrust, 0.001f) * Vector3.IsZeroVector(fuelData.ThrustOverride);

				// Get maximal thrust available on Grid in velocity direction.
                Vector3 maxDirThrust = Vector3.Zero;
                if (localVelocity.X > 0)
                    maxDirThrust.X = m_totalMaxNegativeThrust.X;
                else if (localVelocity.X < 0)
                    maxDirThrust.X = m_totalMaxPositiveThrust.X;

                if (localVelocity.Y > 0)
                    maxDirThrust.Y = m_totalMaxNegativeThrust.Y;
                else if (localVelocity.Y < 0)
                    maxDirThrust.Y = m_totalMaxPositiveThrust.Y;

                if (localVelocity.Z > 0)
                    maxDirThrust.Z = m_totalMaxNegativeThrust.Z;
                else if (localVelocity.Z < 0)
                    maxDirThrust.Z = m_totalMaxPositiveThrust.Z;

                // Get maximal thrust available on Thrust group in velocity direction.
                // Not all groups has to have thrust available in desired direction.
                Vector3 maxDirFuelDataThrust = Vector3.Zero;
                if (localVelocity.X > 0)
                    maxDirFuelDataThrust.X = fuelData.MaxNegativeThrust.X;
                else if (localVelocity.X < 0)
                    maxDirFuelDataThrust.X = fuelData.MaxPositiveThrust.X;

                if (localVelocity.Y > 0)
                    maxDirFuelDataThrust.Y = fuelData.MaxNegativeThrust.Y;
                else if (localVelocity.Y < 0)
                    maxDirFuelDataThrust.Y = fuelData.MaxPositiveThrust.Y;

                if (localVelocity.Z > 0)
                    maxDirFuelDataThrust.Z = fuelData.MaxNegativeThrust.Z;
                else if (localVelocity.Z < 0)
                    maxDirFuelDataThrust.Z = fuelData.MaxPositiveThrust.Z;
                Vector3 ratioOfTotal = maxDirFuelDataThrust / maxDirThrust;
                if (!ratioOfTotal.X.IsValid())
                    ratioOfTotal.X = 1;
                if (!ratioOfTotal.Y.IsValid())
                    ratioOfTotal.Y = 1;
                if (!ratioOfTotal.Z.IsValid())
                    ratioOfTotal.Z = 1;

                slowdownControl *= ratioOfTotal;
            }

            ProfilerShort.End();
            ProfilerShort.Begin("ComputeBaseThrust B");

            Vector3 thrust = negativeControl * fuelData.MaxNegativeThrust + positiveControl * fuelData.MaxPositiveThrust;
            thrust = Vector3.Clamp(thrust, -fuelData.MaxNegativeThrust, fuelData.MaxPositiveThrust);

            Vector3 slowdownAcceleration = (-localVelocity / stoppingTime);
            Vector3 slowdownThrust = slowdownAcceleration * CalculateMass() * slowdownControl;

            
            ProfilerShort.End();
            ProfilerShort.Begin("ComputeBaseThrust C");

            if (!Vector3.IsZero(slowdownThrust))
            {
                m_controlThrustChanged = true;
            }

            thrust = Vector3.Clamp(thrust + slowdownThrust, -fuelData.MaxNegativeThrust * SlowdownFactor, fuelData.MaxPositiveThrust * SlowdownFactor);

            fuelData.CurrentThrust = thrust;
            ProfilerShort.End();
        }

        private void ComputeAiThrust(Vector3 direction, FuelTypeData fuelData)
        {
            Matrix invWorldRot = Entity.PositionComp.WorldMatrixNormalizedInv.GetOrientation();

            Vector3 positiveControl = Vector3.Clamp(direction, Vector3.Zero, Vector3.One);
            Vector3 negativeControl = Vector3.Clamp(direction, -Vector3.One, Vector3.Zero);

            Vector3 positiveGravity = Vector3.Clamp(-Vector3.Transform(Entity.Physics.Gravity, ref invWorldRot) * Entity.Physics.Mass, Vector3.Zero, Vector3.PositiveInfinity);
            Vector3 negativeGravity = Vector3.Clamp(-Vector3.Transform(Entity.Physics.Gravity, ref invWorldRot) * Entity.Physics.Mass, Vector3.NegativeInfinity, Vector3.Zero);

            Vector3 maxPositiveThrust = MaxThrustOverride != null ? MaxThrustOverride.Value * Vector3I.Sign(fuelData.MaxPositiveThrust) : fuelData.MaxPositiveThrust;
            Vector3 maxNegativeThrust = MaxThrustOverride != null ? MaxThrustOverride.Value * Vector3I.Sign(fuelData.MaxNegativeThrust) : fuelData.MaxNegativeThrust;

            Vector3 maxPositiveThrustWithGravity = Vector3.Clamp((maxPositiveThrust - positiveGravity), Vector3.Zero, Vector3.PositiveInfinity);
            Vector3 maxNegativeThrustWithGravity = Vector3.Clamp((maxNegativeThrust + negativeGravity), Vector3.Zero, Vector3.PositiveInfinity);

            Vector3 maxPositiveControl = maxPositiveThrustWithGravity * positiveControl;
            Vector3 maxNegativeControl = maxNegativeThrustWithGravity * -negativeControl;

            float max = Math.Max(maxPositiveControl.Max(), maxNegativeControl.Max());

            Vector3 thrust = Vector3.Zero;
            if (max > 0.001f)
            {
                Vector3 optimalPositive = positiveControl * maxPositiveControl;
                Vector3 optimalNegative = -negativeControl * maxNegativeControl;

                Vector3 optimalPositiveRatio = maxPositiveThrustWithGravity / optimalPositive;
                Vector3 optimalNegativeRatio = maxNegativeThrustWithGravity / optimalNegative;

                if (!optimalPositiveRatio.X.IsValid())
                    optimalPositiveRatio.X = 1;
                if (!optimalPositiveRatio.Y.IsValid())
                    optimalPositiveRatio.Y = 1;
                if (!optimalPositiveRatio.Z.IsValid())
                    optimalPositiveRatio.Z = 1;

                if (!optimalNegativeRatio.X.IsValid())
                    optimalNegativeRatio.X = 1;
                if (!optimalNegativeRatio.Y.IsValid())
                    optimalNegativeRatio.Y = 1;
                if (!optimalNegativeRatio.Z.IsValid())
                    optimalNegativeRatio.Z = 1;

                thrust = -optimalNegative * optimalNegativeRatio + optimalPositive * optimalPositiveRatio;
                thrust += positiveGravity + negativeGravity;
                thrust = Vector3.Clamp(thrust, -maxNegativeThrust, maxPositiveThrust);
            }

            float STOPPING_TIME = MyFakes.ENABLE_VR_REMOTE_CONTROL_WAYPOINTS_FAST_MOVEMENT ? 0.25f : 0.5f;
            Vector3 localVelocity = Vector3.Transform(Entity.Physics.LinearVelocity + Entity.Physics.Gravity / 2.0f, ref invWorldRot);

            Vector3D velocityToCancel;
            if (!Vector3.IsZero(direction))
            {
                Vector3D normalizedDir = Vector3.Normalize(direction);
                velocityToCancel = Vector3.Reject(localVelocity, normalizedDir);
            }
            else
            {
                velocityToCancel = localVelocity;
            }

            var slowdownAcceleration = -velocityToCancel / STOPPING_TIME;
            var slowdownThrust = slowdownAcceleration * Entity.Physics.Mass;
            thrust = Vector3.Clamp(thrust + slowdownThrust, -maxNegativeThrust * SlowdownFactor, maxPositiveThrust * SlowdownFactor);

            fuelData.CurrentThrust = thrust;
        }

        private void FlipNegativeInfinity(ref Vector3 v)
        {
            if (float.IsNegativeInfinity(v.X)) v.X = float.PositiveInfinity;
            if (float.IsNegativeInfinity(v.Y)) v.Y = float.PositiveInfinity;
            if (float.IsNegativeInfinity(v.Z)) v.Z = float.PositiveInfinity;
        }

        virtual protected Vector3 ApplyThrustModifiers(ref MyDefinitionId fuelType, ref Vector3 thrust, ref Vector3 thrustOverride, MyResourceSinkComponentBase resourceSink)
        {
            thrust += thrustOverride;
            thrust *= resourceSink.SuppliedRatioByType(fuelType);
            thrust *= MyFakes.THRUST_FORCE_RATIO;

            return thrust;
        }

        private void UpdatePowerAndThrustStrength(Vector3 thrust, MyDefinitionId fuelType, MyConveyorConnectedGroup group, bool updateThrust)
        {
            if (!m_controlThrustChanged && DampenersEnabled)//&& (MySession.Static.GameplayFrameCounter - (group != null ? group.LastPowerUpdate : m_lastPowerUpdate) < 337)
                return;

            //if ((Container.Entity.Physics as Sandbox.Engine.Physics.MyPhysicsBody).IsWelded)
            //    thrust = Vector3.TransformNormal(thrust, (Container.Entity.Physics as Sandbox.Engine.Physics.MyPhysicsBody).WeldInfo.Transform);
            int typeIndex;
            MyResourceSinkComponent resourceSink;
            FuelTypeData fuelData;
            float thrustOverridePower;

            if (group == null)
            {
                typeIndex = GetTypeIndex(ref fuelType);
                resourceSink = m_resourceSink;
                fuelData = m_dataByFuelType[typeIndex];
                thrustOverridePower = m_totalThrustOverridePower;
                m_lastPowerUpdate = MySession.Static.GameplayFrameCounter;
            }
            else
            {
                typeIndex = group.GetTypeIndex(ref fuelType);
                resourceSink = group.ResourceSink;
                fuelData = group.DataByFuelType[typeIndex];
                thrustOverridePower = group.ThrustOverridePower;
                group.LastPowerUpdate = MySession.Static.GameplayFrameCounter;
            }

            Vector3 thrustPositive = thrust / (fuelData.MaxPositiveThrust + 0.0000001f);
            Vector3 thrustNegative = -thrust / (fuelData.MaxNegativeThrust + 0.0000001f);
            thrustPositive = Vector3.Clamp(thrustPositive, Vector3.Zero, Vector3.One);
            thrustNegative = Vector3.Clamp(thrustNegative, Vector3.Zero, Vector3.One);
            //if (!Vector3.IsZero(ControlThrust))
            //    Debugger.Break();
            // Calculate ratio of usage for different directions.

            float requiredPower = 0f;
            if (Enabled)
            {
                requiredPower += (thrustPositive.X > 0) ? thrustPositive.X * GetMaxPowerRequirement(fuelData, ref Vector3I.Left) : 0;
                requiredPower += (thrustPositive.Y > 0) ? thrustPositive.Y * GetMaxPowerRequirement(fuelData, ref Vector3I.Down) : 0;
                requiredPower += (thrustPositive.Z > 0) ? thrustPositive.Z * GetMaxPowerRequirement(fuelData, ref Vector3I.Forward) : 0;
                requiredPower += (thrustNegative.X > 0) ? thrustNegative.X * GetMaxPowerRequirement(fuelData, ref Vector3I.Right) : 0;
                requiredPower += (thrustNegative.Y > 0) ? thrustNegative.Y * GetMaxPowerRequirement(fuelData, ref Vector3I.Up) : 0;
                requiredPower += (thrustNegative.Z > 0) ? thrustNegative.Z * GetMaxPowerRequirement(fuelData, ref Vector3I.Backward) : 0;
                requiredPower += thrustOverridePower;
                requiredPower = Math.Max(requiredPower, fuelData.MinRequiredPowerInput);
            }
            SetRequiredFuelInput(ref fuelType, PowerAmountToFuel(ref fuelType, requiredPower, group), group);

            resourceSink.Update();

            ProfilerShort.Begin("Update thrust strengths");
            if (updateThrust)
            {
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Left], thrustPositive.X);
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Down], thrustPositive.Y);
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Forward], thrustPositive.Z);
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Right], thrustNegative.X);
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Up], thrustNegative.Y);
                UpdateThrustStrength(fuelData.ThrustsByDirection[Vector3I.Backward], thrustNegative.Z);
            }
            ProfilerShort.End();
        }

        private void RecalculatePlanetaryInfluence()
        {
            BoundingBoxD box = Entity.PositionComp.WorldAABB;
            var planet = MyGamePruningStructure.GetClosestPlanet(ref box);

            float multiplier = 0;
            if (planet != null)
            {
                multiplier = planet.GetAirDensity(box.Center);

                m_lastPlanetaryInfluenceHasAtmosphere = planet.HasAtmosphere;
                m_lastPlanetaryGravityMagnitude = planet.Components.Get<MyGravityProviderComponent>().GetGravityMultiplier(Entity.PositionComp.WorldMatrix.Translation);

                m_nextPlanetaryInfluenceRecalculation = MySession.Static.GameplayFrameCounter + Math.Min(100, m_maxInfluenceRecalculationInterval);
            }
            else
            {
                m_nextPlanetaryInfluenceRecalculation = MySession.Static.GameplayFrameCounter + Math.Min(1000, m_maxInfluenceRecalculationInterval);
            }

            if (m_lastPlanetaryInfluence != multiplier)
            {
                MarkDirty();
                m_lastPlanetaryInfluence = multiplier;
            }
        }

        private void UpdateConveyorSystemChanges()
        {
            while (m_thrustEntitiesPending.Count > 0)
            {
                MyTuple<MyEntity, Vector3I, Func<bool>> thrustTuple = m_thrustEntitiesPending.Dequeue();
                if (IsThrustEntityType(thrustTuple.Item1))
                {
                    if (m_thrustEntitiesRemovedBeforeRegister.Contains(thrustTuple.Item1))
                    {
                        m_thrustEntitiesRemovedBeforeRegister.Remove(thrustTuple.Item1);
                        continue;
                    }
                    RegisterLazy(thrustTuple.Item1, thrustTuple.Item2, thrustTuple.Item3);
                }
            }

            while (m_conveyorSegmentsPending.Count > 0)
            {
                IMyConveyorSegmentBlock conveyorSegmentBlock = m_conveyorSegmentsPending.Dequeue();
                FindConnectedGroups(conveyorSegmentBlock, m_connectedGroups, m_tmpGroupIndices);

                if (m_tmpGroupIndices.Count > 1)
                    MergeGroups(m_connectedGroups, m_tmpGroupIndices);
                m_tmpGroupIndices.Clear();
            }

            while (m_conveyorEndpointsPending.Count > 0)
            {
                IMyConveyorEndpointBlock conveyorEndpointBlock = m_conveyorEndpointsPending.Dequeue();
                FindConnectedGroups(conveyorEndpointBlock, m_connectedGroups, m_tmpGroupIndices);

                if (m_tmpGroupIndices.Count > 1)
                    MergeGroups(m_connectedGroups, m_tmpGroupIndices);
                m_tmpGroupIndices.Clear();
            }

            foreach (var group in m_groupsToTrySplit)
            {
                TrySplitGroup(null, group);
            }
            m_groupsToTrySplit.Clear();

            if (m_mergeAllGroupsDirty)
            {
                TryMergeAllGroups();
                m_mergeAllGroupsDirty = false;
            }
        }

        private void ConveyorSystem_OnPoweredChanged()
        {
            MergeAllGroupsDirty();
        }

        /// <summary>
        /// Finds the resource sink that should handle the power consumption of thrustEntity
        /// </summary>
        public MyResourceSinkComponent ResourceSink(MyEntity thrustEntity)
        {
            MyConveyorConnectedGroup group = FindEntityGroup(thrustEntity);

            return group == null ? m_resourceSink : group.ResourceSink;
        }

        public void ResourceSinks(HashSet<MyResourceSinkComponent> outResourceSinks)
        {
            if (m_resourceSink != null)
                outResourceSinks.Add(m_resourceSink);

            foreach (var group in m_connectedGroups)
            {
                if (group.ResourceSink != null)
                    outResourceSinks.Add(group.ResourceSink);
            }
        }

        private MyConveyorConnectedGroup FindEntityGroup(MyEntity thrustEntity)
        {
            MyConveyorConnectedGroup entityGroup = null;
            if (!IsThrustEntityType(thrustEntity))
            {
                IMyConveyorEndpoint entityEndpoint = null;
                var endpointBlock = thrustEntity as IMyConveyorEndpointBlock;
                var segmentBlock = thrustEntity as IMyConveyorSegmentBlock;
                if (endpointBlock != null)
                    entityEndpoint = endpointBlock.ConveyorEndpoint;
                else if (segmentBlock != null && segmentBlock.ConveyorSegment.ConveyorLine != null)
                    entityEndpoint = segmentBlock.ConveyorSegment.ConveyorLine.GetEndpoint(0) ?? segmentBlock.ConveyorSegment.ConveyorLine.GetEndpoint(1);

                if (entityEndpoint != null)
                {
                    foreach (var group in m_connectedGroups)
                    {
                        if (!MyGridConveyorSystem.Reachable(group.FirstEndpoint, entityEndpoint))
                            continue;

                        entityGroup = group;
                        break;
                    }
                }
            }
            else if (MyResourceDistributorComponent.IsConveyorConnectionRequiredTotal(FuelType(thrustEntity)))
            {
                MyDefinitionId fuelType = FuelType(thrustEntity);
                foreach (var group in m_connectedGroups)
                {
                    int typeIndex;
                    if (!group.TryGetTypeIndex(ref fuelType, out typeIndex))
                        continue;

                    foreach (var thrustSet in group.DataByFuelType[typeIndex].ThrustsByDirection.Values)
                    {
                        if (thrustSet.Contains(thrustEntity))
                        {
                            entityGroup = group;
                            break;
                        }
                    }

                    if (entityGroup != null)
                        break;
                }
            }

            return entityGroup;
        }

        protected float GetMaxPowerRequirement(FuelTypeData typeData, ref Vector3I direction)
        {
            return typeData.MaxRequirementsByDirection[direction];
        }

        public void MarkDirty()
        {
            m_thrustsChanged = true;
            m_controlThrustChanged = true;
        }

        private static float RequiredFuelInput(FuelTypeData typeData)
        {
            return typeData.CurrentRequiredFuelInput;
        }

        internal void SetRequiredFuelInput(ref MyDefinitionId fuelType, float newFuelInput, MyConveyorConnectedGroup group)
        {
            int typeIndex = 0;
            if (group == null && !TryGetTypeIndex(ref fuelType, out typeIndex))
                return;
            else if (group != null && !group.TryGetTypeIndex(ref fuelType, out typeIndex))
                return;

            var typeData = (group != null ? group.DataByFuelType : m_dataByFuelType);
            typeData[typeIndex].CurrentRequiredFuelInput = newFuelInput;
        }

        protected float PowerAmountToFuel(ref MyDefinitionId fuelType, float powerAmount, MyConveyorConnectedGroup group)
        {
            int typeIndex = 0;
            if (group == null && !TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;
            else if (group != null && !group.TryGetTypeIndex(ref fuelType, out typeIndex))
                return 0f;

            var dataByFuelType = group != null ? group.DataByFuelType : m_dataByFuelType;
            return powerAmount / (dataByFuelType[typeIndex].Efficiency * dataByFuelType[typeIndex].EnergyDensity);
        }

        private bool TryGetTypeIndex(ref MyDefinitionId fuelId, out int typeIndex)
        {
            typeIndex = 0;
            if (m_fuelTypeToIndex.Count > 1)
            {
                if (!m_fuelTypeToIndex.TryGetValue(fuelId, out typeIndex))
                    return false;
            }
            return m_fuelTypeToIndex.Count > 0;
        }

        public bool IsThrustPoweredByType(MyEntity thrustEntity, ref MyDefinitionId fuelId)
        {
            var resourceSink = ResourceSink(thrustEntity);
            return resourceSink.IsPoweredByType(fuelId);
        }

        protected int GetTypeIndex(ref MyDefinitionId fuelId)
        {
            var typeIndex = 0;
            if (m_fuelTypeToIndex.Count > 1)
            {
                int fetchedValue;
                if (m_fuelTypeToIndex.TryGetValue(fuelId, out fetchedValue))
                    typeIndex = fetchedValue;
            }
            return typeIndex;
        }

        // Makes sure changes in the grid conveyor system outside of the thruster blocks is kept track of
        private void CubeGrid_OnBlockAdded(MySlimBlock addedBlock)
        {
            var fatblock = addedBlock.FatBlock;
            if (fatblock == null)
                return;

            var conveyorEndpointBlock = fatblock as IMyConveyorEndpointBlock;
            var conveyorSegmentBlock = fatblock as IMyConveyorSegmentBlock;
            if (conveyorEndpointBlock != null && !IsThrustEntityType(conveyorEndpointBlock as MyEntity))
                m_conveyorEndpointsPending.Enqueue(conveyorEndpointBlock);
            else if (conveyorSegmentBlock != null)
                m_conveyorSegmentsPending.Enqueue(conveyorSegmentBlock);
        }

        private void ConveyorSystem_OnBeforeRemoveSegmentBlock(IMyConveyorSegmentBlock conveyorSegmentBlock)
        {
            if (conveyorSegmentBlock == null)
                return;

            var group = FindEntityGroup(conveyorSegmentBlock as MyEntity);

            if (group != null)
                m_groupsToTrySplit.Add(group);
        }

        private void ConveyorSystem_OnBeforeRemoveEndpointBlock(IMyConveyorEndpointBlock conveyorEndpointBlock)
        {
            if (conveyorEndpointBlock == null || !IsThrustEntityType(conveyorEndpointBlock as MyEntity))
                return;

            var group = FindEntityGroup(conveyorEndpointBlock as MyEntity);
            if (group != null)
                m_groupsToTrySplit.Add(group);
        }

        protected abstract void UpdateThrustStrength(HashSet<MyEntity> entities, float thrustForce);

        protected abstract bool RecomputeOverriddenParameters(MyEntity thrustEntity, FuelTypeData fuelData);

        protected abstract bool IsUsed(MyEntity thrustEntity);

        protected abstract float ForceMagnitude(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere);

        protected abstract float CalculateForceMultiplier(MyEntity thrustEntity, float planetaryInfluence, bool inAtmosphere);

        protected abstract float CalculateConsumptionMultiplier(MyEntity thrustEntity, float naturalGravityStrength);

        protected abstract float MaxPowerConsumption(MyEntity thrustEntity);

        protected abstract float MinPowerConsumption(MyEntity thrustEntity);

        protected abstract MyDefinitionId FuelType(MyEntity thrustEntity);

        protected abstract bool IsThrustEntityType(MyEntity thrustEntity);

        protected abstract void RemoveFromGroup(MyEntity thrustEntity, MyConveyorConnectedGroup group);

        protected abstract void AddToGroup(MyEntity thrustEntity, MyConveyorConnectedGroup group);

        public override string ComponentTypeDebugString { get { return "Thrust Component"; } }

        public float GetLastThrustMultiplier(MyEntity thrustEntity)
        {
            return CalculateForceMultiplier(thrustEntity, m_lastPlanetaryInfluence, m_lastPlanetaryInfluenceHasAtmosphere);
        }

        protected virtual float CalculateMass()
        {
            return Entity.Physics.Mass;
        }

        public bool HasPower
        {
            get
            {
                return m_resourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
            }
        }

        public bool HasThrustersInAllDirections(MyDefinitionId fuelId)
        {
            int fuelIndex;
            if (m_fuelTypeToIndex.TryGetValue(fuelId, out fuelIndex))
            {
                var data = m_dataByFuelType[fuelIndex];
                var flag = true;
                flag &= data.ThrustsByDirection[Vector3I.Backward].Count > 0;
                flag &= data.ThrustsByDirection[Vector3I.Forward].Count > 0;
                flag &= data.ThrustsByDirection[Vector3I.Up].Count > 0;
                flag &= data.ThrustsByDirection[Vector3I.Down].Count > 0;
                flag &= data.ThrustsByDirection[Vector3I.Left].Count > 0;
                flag &= data.ThrustsByDirection[Vector3I.Right].Count > 0;

                return flag;
            }

            return false;
        }
    }
}