using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage;
using VRage.Collections;
using VRage.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Trace;
using VRage.Utils;

namespace Sandbox.Game.EntityComponents
{
    public enum MyResourceStateEnum
    {
        Ok,
        OverloadAdaptible, // some adaptible group does not have enough resources, but everything else still works fine
        OverloadBlackout,  // some non-adaptible group does not have enough resources, so it is without power
        NoPower
    }

    public class MyResourceDistributorComponent : MyEntityComponentBase
    {
        /// Note: The properties in this class will default to electricity for backwards compatibility for now

        private struct MyPhysicalDistributionGroup
        {
            public IMyConveyorEndpoint FirstEndpoint;
            public HashSet<MyResourceSinkComponent>[] SinksByPriority;
            public HashSet<MyResourceSourceComponent>[] SourcesByPriority;
            public List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] SinkSourcePairsByPriority;

            public MySinkGroupData[] SinkDataByPriority;
            public MySourceGroupData[] SourceDataByPriority;
            public MyTuple<MySinkGroupData, MySourceGroupData>[][] SinkSourcePairDataByPriority;
            public List<int>[][] StockpilingStorageIndicesByPriority;
            public List<int>[][] OtherStorageIndicesByPriority;

            public float MaxAvailableResources;
            public MyResourceStateEnum ResourceState;

            public MyPhysicalDistributionGroup(MyDefinitionId typeId, IMyConveyorEndpointBlock block)
            {
                SinksByPriority = null; SourcesByPriority = null; SinkSourcePairsByPriority = null; FirstEndpoint = null;
                SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorageIndicesByPriority = null;
                OtherStorageIndicesByPriority = null; SinkSourcePairDataByPriority = null;
                MaxAvailableResources = 0f; ResourceState = MyResourceStateEnum.NoPower;
                AllocateData();

                Init(typeId, block);
            }

            public MyPhysicalDistributionGroup(MyDefinitionId typeId, MyResourceSinkComponent tempConnectedSink)
            {
                SinksByPriority = null; SourcesByPriority = null; SinkSourcePairsByPriority = null; FirstEndpoint = null;
                SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorageIndicesByPriority = null;
                OtherStorageIndicesByPriority = null; SinkSourcePairDataByPriority = null;
                MaxAvailableResources = 0f; ResourceState = MyResourceStateEnum.NoPower;
                AllocateData();

                InitFromTempConnected(typeId, tempConnectedSink);
            }

            public MyPhysicalDistributionGroup(MyDefinitionId typeId, MyResourceSourceComponent tempConnectedSource)
            {
                SinksByPriority = null; SourcesByPriority = null; SinkSourcePairsByPriority = null; FirstEndpoint = null;
                SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorageIndicesByPriority = null;
                OtherStorageIndicesByPriority = null; SinkSourcePairDataByPriority = null;
                MaxAvailableResources = 0f; ResourceState = MyResourceStateEnum.NoPower;
                AllocateData();

                InitFromTempConnected(typeId, tempConnectedSource);
            }

            public void Init(MyDefinitionId typeId, IMyConveyorEndpointBlock block)
            {
                FirstEndpoint = block.ConveyorEndpoint;
                ClearData();

                Add(typeId, block);
            }

            public void InitFromTempConnected(MyDefinitionId typeId, MyResourceSinkComponent tempConnectedSink)
            {
                var conveyorEndPointBlock = tempConnectedSink.TemporaryConnectedEntity as IMyConveyorEndpointBlock;
                if (conveyorEndPointBlock != null)
                    FirstEndpoint = conveyorEndPointBlock.ConveyorEndpoint;

                ClearData();

                AddTempConnected(typeId, tempConnectedSink);
            }

            public void InitFromTempConnected(MyDefinitionId typeId, MyResourceSourceComponent tempConnectedSource)
            {
                var conveyorEndPointBlock = tempConnectedSource.TemporaryConnectedEntity as IMyConveyorEndpointBlock;
                if (conveyorEndPointBlock != null)
                    FirstEndpoint = conveyorEndPointBlock.ConveyorEndpoint;

                ClearData();

                AddTempConnected(typeId, tempConnectedSource);
            }

            public void Add(MyDefinitionId typeId, IMyConveyorEndpointBlock endpoint)
            {
                var componentContainer = (endpoint as IMyEntity).Components;

                var sink = componentContainer.Get<MyResourceSinkComponent>();
                var source = componentContainer.Get<MyResourceSourceComponent>();

                bool containsSink = sink != null && sink.AcceptedResources.Contains(typeId);
                bool containsSource = source != null && source.ResourceTypes.Contains(typeId);
                if (containsSink && containsSource)
                {
                    var sinkSourcePair = new MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>(sink, source);
                    SinkSourcePairsByPriority[sinkPriority][GetPriority(sink)].Add(sinkSourcePair);
                    SinkSourcePairsByPriority[sourcePriority][GetPriority(source)].Add(sinkSourcePair);
                }
                else if (containsSink)
                {
                    SinksByPriority[GetPriority(sink)].Add(sink);
                }
                else if (containsSource)
                {
                    SourcesByPriority[GetPriority(source)].Add(source);
                }
            }

            public void AddTempConnected(MyDefinitionId typeId, MyResourceSinkComponent tempConnectedSink)
            {
                bool containsSink = tempConnectedSink != null && tempConnectedSink.AcceptedResources.Contains(typeId);
                Debug.Assert(containsSink);
                if (containsSink)
                    SinksByPriority[GetPriority(tempConnectedSink)].Add(tempConnectedSink);
            }

            public void AddTempConnected(MyDefinitionId typeId, MyResourceSourceComponent tempConnectedSource)
            {
                bool containsSource = tempConnectedSource != null && tempConnectedSource.ResourceTypes.Contains(typeId);
                Debug.Assert(containsSource);
                if (containsSource)
                    SourcesByPriority[GetPriority(tempConnectedSource)].Add(tempConnectedSource);
            }

            private void AllocateData()
            {
                FirstEndpoint = null;
                SinksByPriority = new HashSet<MyResourceSinkComponent>[m_sinkGroupPrioritiesTotal];
                SourcesByPriority = new HashSet<MyResourceSourceComponent>[m_sourceGroupPrioritiesTotal];
                SinkSourcePairsByPriority = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[numPriorities][];
                SinkSourcePairsByPriority[sinkPriority] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[m_sinkGroupPrioritiesTotal];
                SinkSourcePairsByPriority[sourcePriority] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[m_sourceGroupPrioritiesTotal];
                SinkDataByPriority = new MySinkGroupData[m_sinkGroupPrioritiesTotal];
                SourceDataByPriority = new MySourceGroupData[m_sourceGroupPrioritiesTotal];
                SinkSourcePairDataByPriority = new MyTuple<MySinkGroupData, MySourceGroupData>[numPriorities][];
                SinkSourcePairDataByPriority[sinkPriority] = new MyTuple<MySinkGroupData, MySourceGroupData>[m_sinkGroupPrioritiesTotal];
                SinkSourcePairDataByPriority[sourcePriority] = new MyTuple<MySinkGroupData, MySourceGroupData>[m_sourceGroupPrioritiesTotal];
                StockpilingStorageIndicesByPriority = new List<int>[numPriorities][];
                StockpilingStorageIndicesByPriority[sinkPriority] = new List<int>[m_sinkGroupPrioritiesTotal];
                StockpilingStorageIndicesByPriority[sourcePriority] = new List<int>[m_sourceGroupPrioritiesTotal];
                OtherStorageIndicesByPriority = new List<int>[numPriorities][];
                OtherStorageIndicesByPriority[sinkPriority] = new List<int>[m_sinkGroupPrioritiesTotal];
                OtherStorageIndicesByPriority[sourcePriority] = new List<int>[m_sourceGroupPrioritiesTotal];

                for (int priorityIndex = 0; priorityIndex < m_sinkGroupPrioritiesTotal; ++priorityIndex)
                    SinksByPriority[priorityIndex] = new HashSet<MyResourceSinkComponent>();

                for (int priorityIndex = 0; priorityIndex < m_sourceGroupPrioritiesTotal; ++priorityIndex)
                    SourcesByPriority[priorityIndex] = new HashSet<MyResourceSourceComponent>();

                for (int i = 0; i < numPriorities; ++i)
                {
                    for (int priorityIndex = 0; priorityIndex < SinkSourcePairsByPriority[i].Length; ++priorityIndex)
                        SinkSourcePairsByPriority[i][priorityIndex] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>();

                    for (int priorityIndex = 0; priorityIndex < StockpilingStorageIndicesByPriority[i].Length; ++priorityIndex)
                        StockpilingStorageIndicesByPriority[i][priorityIndex] = new List<int>();

                    for (int priorityIndex = 0; priorityIndex < OtherStorageIndicesByPriority[i].Length; ++priorityIndex)
                        OtherStorageIndicesByPriority[i][priorityIndex] = new List<int>();
                }
            }

            private void ClearData()
            {
                foreach (var sinks in SinksByPriority)
                    sinks.Clear();

                foreach (var sources in SourcesByPriority)
                    sources.Clear();

                for (int i = 0; i < numPriorities; ++i)
                {
                    foreach (var sinkSourcePairs in SinkSourcePairsByPriority[i])
                        sinkSourcePairs.Clear();

                    foreach (var stockpilingStorageIndices in StockpilingStorageIndicesByPriority[i])
                        stockpilingStorageIndices.Clear();

                    foreach (var otherStorageIndices in OtherStorageIndicesByPriority[i])
                        otherStorageIndices.Clear();
                }
            }
        }

        /// <summary>
        /// Some precomputed data for each priority group.
        /// </summary>
        private struct MySinkGroupData
        {
            public bool IsAdaptible;
            public float RequiredInput;
            public float RequiredInputCumulative; // Sum of required input for this group and groups above it.
            public float RemainingAvailableResource; // Remaining resource after distributing to higher priorities.

            public override string ToString() { return string.Format("IsAdaptible: {0}, RequiredInput: {1}, RemainingAvailableResource: {2}", IsAdaptible, RequiredInput, RemainingAvailableResource); }
        }

        private struct MySourceGroupData
        {
            public float MaxAvailableResource;
            public float UsageRatio;
            public bool InfiniteCapacity;
            public int ActiveCount;

            public override string ToString() { return string.Format("MaxAvailableResource: {0}, UsageRatio: {1}", MaxAvailableResource, UsageRatio); }
        }


        private class PerTypeData
        {
            public MySinkGroupData[] SinkDataByPriority;
            public MySourceGroupData[] SourceDataByPriority;
            public MyTuple<MySinkGroupData, MySourceGroupData>[][] SinkSourcePairDataByPriority;

            public HashSet<MyResourceSinkComponent>[] SinksByPriority;
            public HashSet<MyResourceSourceComponent>[] SourcesByPriority;
            public List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] SinkSourcePairsByPriority;

            public List<int>[][] StockpilingStorageIndicesByPriority;
            public List<int>[][] OtherStorageIndicesByPriority;

            public List<MyPhysicalDistributionGroup> DistributionGroups;
            public int DistributionGroupsInUse;

            public bool GroupsDirty;
            public bool NeedsRecompute;
            public int SourceCount;
            public float RemainingFuelTime;
            public bool RemainingFuelTimeDirty;
            public int LastFuelTimeCompute;
            public float MaxAvailableResource;
            public MyMultipleEnabledEnum SourcesEnabled;
            public bool SourcesEnabledDirty;
            public MyResourceStateEnum ResourceState;
        }

        private const int sinkPriority = 0;
        private const int sourcePriority = 1;
        private const int numPriorities = 2;

        private readonly List<PerTypeData> m_dataPerType = new List<PerTypeData>();

        private int m_allEnabledCounter = 0;

        private readonly HashSet<MyDefinitionId> m_initializedTypes = new HashSet<MyDefinitionId>();
        private int m_typeGroupCount = 0;
        private readonly Dictionary<MyDefinitionId, int> m_typeIdToIndex = new Dictionary<MyDefinitionId, int>();
        private readonly Dictionary<MyDefinitionId, bool> m_typeIdToConveyorConnectionRequired = new Dictionary<MyDefinitionId, bool>();

        /// <summary>
        /// Remaining fuel time in hours.
        /// </summary>

        #region Properties

        public bool AllEnabledRecently { get { return m_allEnabledCounter <= 30; } }
        public float MaxAvailableResource { get { return m_dataPerType[0].MaxAvailableResource; } }
        public float MaxAvailableResourceByType(MyDefinitionId resourceTypeId) { return m_dataPerType[GetTypeIndex(resourceTypeId)].MaxAvailableResource; }

        public float TotalRequiredInput { get { return TotalRequiredInputByType(m_typeIdToIndexTotal.Keys.First()); } }
        public float TotalRequiredInputByType(MyDefinitionId type) { return m_dataPerType[GetTypeIndex(type)].SinkDataByPriority.Last().RequiredInputCumulative; }

        /// <summary>
        /// For debugging purposes. Enables trace messages and watches for this instance.
        /// </summary>
        public bool ShowTrace { get; set; }

        public MyMultipleEnabledEnum SourcesEnabled { get { return SourcesEnabledByType(m_typeIdToIndexTotal.Keys.First()); } }
        public MyResourceStateEnum ResourceState { get { return ResourceStateByType(m_typeIdToIndexTotal.Keys.First()); } }

        #endregion

        private static int m_typeGroupCountTotal = -1;
        private static int m_sinkGroupPrioritiesTotal = -1;
        private static int m_sourceGroupPrioritiesTotal = -1;

        public static int SinkGroupPrioritiesTotal { get { return m_sinkGroupPrioritiesTotal; } }

        public static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private static readonly Dictionary<MyDefinitionId, int> m_typeIdToIndexTotal = new Dictionary<MyDefinitionId, int>();
        private static readonly Dictionary<MyDefinitionId, bool> m_typeIdToConveyorConnectionRequiredTotal = new Dictionary<MyDefinitionId, bool>();

        private static readonly Dictionary<MyStringHash, int> m_sourceSubtypeToPriority = new Dictionary<MyStringHash, int>();
        private static readonly Dictionary<MyStringHash, int> m_sinkSubtypeToPriority = new Dictionary<MyStringHash, int>();
        private readonly static Dictionary<MyStringHash, bool> m_sinkSubtypeToAdaptability = new Dictionary<MyStringHash, bool>();
        public static DictionaryReader<MyStringHash, int> SinkSubtypesToPriority { get { return new DictionaryReader<MyStringHash, int>(m_sinkSubtypeToPriority); } }


        private static void InitializeMappings()
        {
            var distributionGroups = MyDefinitionManager.Static.GetDefinitionsOfType<MyResourceDistributionGroupDefinition>();

            var sortedGroups = distributionGroups.OrderBy(def => def.Priority);

            if (distributionGroups.Count > 0)
            {
                m_sinkGroupPrioritiesTotal = 0;
                m_sourceGroupPrioritiesTotal = 0;
            }

            foreach (MyResourceDistributionGroupDefinition distributionGroup in sortedGroups)
            {
                if (!distributionGroup.IsSource)
                {
                    m_sinkSubtypeToPriority.Add(distributionGroup.Id.SubtypeId, m_sinkGroupPrioritiesTotal++);
                    m_sinkSubtypeToAdaptability.Add(distributionGroup.Id.SubtypeId, distributionGroup.IsAdaptible);
                }
                else // IsSource == true
                {
                    m_sourceSubtypeToPriority.Add(distributionGroup.Id.SubtypeId, m_sourceGroupPrioritiesTotal++);
                }
            }

            m_sinkGroupPrioritiesTotal = Math.Max(m_sinkGroupPrioritiesTotal, 1);
            m_sourceGroupPrioritiesTotal = Math.Max(m_sourceGroupPrioritiesTotal, 1);

            m_sinkSubtypeToPriority.Add(MyStringHash.NullOrEmpty, m_sinkGroupPrioritiesTotal - 1);
            m_sinkSubtypeToAdaptability.Add(MyStringHash.NullOrEmpty, false);
            m_sourceSubtypeToPriority.Add(MyStringHash.NullOrEmpty, m_sourceGroupPrioritiesTotal - 1);

            m_typeGroupCountTotal = 0;
            m_typeIdToIndexTotal.Add(ElectricityId, m_typeGroupCountTotal++);   // Electricity is always in game for now (needed in ME for jetpack)
            m_typeIdToConveyorConnectionRequiredTotal.Add(ElectricityId, false);

            var gasTypes = MyDefinitionManager.Static.GetDefinitionsOfType<MyGasProperties>();  // Get viable fuel types from some definition?
            foreach (var gasDefinition in gasTypes)
            {
                m_typeIdToIndexTotal.Add(gasDefinition.Id, m_typeGroupCountTotal++);
                m_typeIdToConveyorConnectionRequiredTotal.Add(gasDefinition.Id, true);  // MK: TODO: Read this from definition
            }
        }

        private void InitializeNewType(MyDefinitionId typeId)
        {
            m_typeIdToIndex.Add(typeId, m_typeGroupCount++);
            m_typeIdToConveyorConnectionRequired.Add(typeId, IsConveyorConnectionRequiredTotal(typeId));

            var sinksByPriority = new HashSet<MyResourceSinkComponent>[m_sinkGroupPrioritiesTotal];
            var sourceByPriority = new HashSet<MyResourceSourceComponent>[m_sourceGroupPrioritiesTotal];
            var sinkSourcePairsByPriority = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[numPriorities][];
            sinkSourcePairsByPriority[sinkPriority] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[m_sinkGroupPrioritiesTotal];
            sinkSourcePairsByPriority[sourcePriority] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[m_sourceGroupPrioritiesTotal];
            var stockpilingStorageIndicesByPriority = new List<int>[numPriorities][];
            stockpilingStorageIndicesByPriority[sinkPriority] = new List<int>[m_sinkGroupPrioritiesTotal];
            stockpilingStorageIndicesByPriority[sourcePriority] = new List<int>[m_sourceGroupPrioritiesTotal];
            var otherStorageIndicesByPriority = new List<int>[numPriorities][];
            otherStorageIndicesByPriority[sinkPriority] = new List<int>[m_sinkGroupPrioritiesTotal];
            otherStorageIndicesByPriority[sourcePriority] = new List<int>[m_sourceGroupPrioritiesTotal];

            for (int priorityIndex = 0; priorityIndex < sinksByPriority.Length; ++priorityIndex)
                sinksByPriority[priorityIndex] = new HashSet<MyResourceSinkComponent>();

            for (int priorityIndex = 0; priorityIndex < sourceByPriority.Length; ++priorityIndex)
                sourceByPriority[priorityIndex] = new HashSet<MyResourceSourceComponent>();

            for (int i = 0; i < numPriorities; ++i)
            { 
                for (int priorityIndex = 0; priorityIndex < sinkSourcePairsByPriority[i].Length; ++priorityIndex)
                    sinkSourcePairsByPriority[i][priorityIndex] = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>();

                for (int priorityIndex = 0; priorityIndex < stockpilingStorageIndicesByPriority[i].Length; ++priorityIndex)
                    stockpilingStorageIndicesByPriority[i][priorityIndex] = new List<int>();

                for (int priorityIndex = 0; priorityIndex < otherStorageIndicesByPriority[i].Length; ++priorityIndex)
                    otherStorageIndicesByPriority[i][priorityIndex] = new List<int>();
            }

            List<MyPhysicalDistributionGroup> distributionGroups = null;
            int distributionGroupsInUse = 0;

            MySinkGroupData[] sinkGroupDataByPriority = null;
            MySourceGroupData[] sourceGroupDatabyPriority = null;
            MyTuple<MySinkGroupData, MySourceGroupData>[][] sinkSourcePairDataBySourcePriority = null;

            if (IsConveyorConnectionRequired(typeId))
            {
                distributionGroups = new List<MyPhysicalDistributionGroup>();
            }
            else
            {
                sinkGroupDataByPriority = new MySinkGroupData[m_sinkGroupPrioritiesTotal];
                sourceGroupDatabyPriority = new MySourceGroupData[m_sourceGroupPrioritiesTotal];
                sinkSourcePairDataBySourcePriority = new MyTuple<MySinkGroupData, MySourceGroupData>[numPriorities][];
                sinkSourcePairDataBySourcePriority[sinkPriority] = new MyTuple<MySinkGroupData, MySourceGroupData>[m_sinkGroupPrioritiesTotal];
                sinkSourcePairDataBySourcePriority[sourcePriority] = new MyTuple<MySinkGroupData, MySourceGroupData>[m_sourceGroupPrioritiesTotal];
            }

            m_dataPerType.Add(new PerTypeData
            {
                SinkDataByPriority = sinkGroupDataByPriority,
                SourceDataByPriority = sourceGroupDatabyPriority,
                SinkSourcePairDataByPriority = sinkSourcePairDataBySourcePriority,
                SinksByPriority = sinksByPriority,
                SourcesByPriority = sourceByPriority,
                SinkSourcePairsByPriority = sinkSourcePairsByPriority,
                StockpilingStorageIndicesByPriority = stockpilingStorageIndicesByPriority,
                OtherStorageIndicesByPriority = otherStorageIndicesByPriority,
                DistributionGroups = distributionGroups,
                DistributionGroupsInUse = distributionGroupsInUse,
                NeedsRecompute = true,
                GroupsDirty = true,
                SourceCount = 0,
                RemainingFuelTime = 0,
                RemainingFuelTimeDirty = true,
                LastFuelTimeCompute = 0,
                MaxAvailableResource = 0,
                SourcesEnabled = MyMultipleEnabledEnum.NoObjects,
                SourcesEnabledDirty = true,
                ResourceState = MyResourceStateEnum.NoPower,
            });

            m_initializedTypes.Add(typeId);
        }

        public MyResourceDistributorComponent()
        {
            if (m_sinkGroupPrioritiesTotal < 0 || m_sourceGroupPrioritiesTotal < 0)
                InitializeMappings();
        }

        #region Add and remove

        public void AddSink(MyResourceSinkComponent sink)
        {
            Debug.Assert(sink != null);

            foreach (var typeId in sink.AcceptedResources)
            {
                if (!m_initializedTypes.Contains(typeId))
                    InitializeNewType(typeId);

                var sinksOfType = GetSinksOfType(typeId, sink.Group);
                Debug.Assert(MatchesAdaptability(sinksOfType, sink), "All sinks in the same group must have same adaptability.");
                Debug.Assert(!sinksOfType.Contains(sink));
                int typeIndex = GetTypeIndex(typeId);

                MyResourceSourceComponent matchingSource = null;
                if (sink.Container != null)
                {
                    var sourceDataByPriority = m_dataPerType[typeIndex].SourcesByPriority;
                    for (int priorityIndex = 0; priorityIndex < sourceDataByPriority.Length; ++priorityIndex)
                    {
                        var sources = sourceDataByPriority[priorityIndex];
                        foreach (var source in sources)
                        {
                            if (source.Container == null)
                                continue;

                            var sinkInContainer = source.Container.Get<MyResourceSinkComponent>();
                            if (sinkInContainer == sink)
                            {
                                m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority][GetPriority(sink)].Add(MyTuple.Create(sink, source));
                                m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][priorityIndex].Add(MyTuple.Create(sink, source));
                                matchingSource = source;
                                break;
                            }
                        }
                        if (matchingSource == null)
                            continue;

                        sources.Remove(matchingSource);
                        break;
                    }
                }

                if (matchingSource == null)
                    sinksOfType.Add(sink);

                m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            }

            sink.RequiredInputChanged += Sink_RequiredInputChanged;
            sink.ResourceAvailable += Sink_IsResourceAvailable;
            sink.OnAddType += Sink_OnAddType;
        }

        public void RemoveSink(MyResourceSinkComponent sink, bool resetSinkInput = true, bool markedForClose = false)
        {
            if (markedForClose)
                return;
            Debug.Assert(sink != null);

            foreach (var typeId in sink.AcceptedResources)
            {
                var sinksOfType = GetSinksOfType(typeId, sink.Group);
                int typeIndex = GetTypeIndex(typeId);

                if (!sinksOfType.Remove(sink))
                {
                    int foundSourceIndex = -1;
                    int foundSourcePriorityIndex = -1;
                    var sinkSourcePairsBySourcePriority = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority];
                    for (int priorityIndex = 0; priorityIndex < sinkSourcePairsBySourcePriority.Length; ++priorityIndex)
                    {
                        var sinkSourcePairs = sinkSourcePairsBySourcePriority[priorityIndex];
                        for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                        {
                            if (sinkSourcePairs[pairIndex].Item1 != sink)
                                continue;

                            foundSourceIndex = pairIndex;
                            foundSourcePriorityIndex = priorityIndex;
                            break;
                        }
                    }

                    if (foundSourceIndex != -1)
                    {
                        var matchingSource = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][foundSourcePriorityIndex][foundSourceIndex].Item2;
                        m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][foundSourcePriorityIndex].RemoveAtFast(foundSourceIndex);
                        m_dataPerType[typeIndex].SourcesByPriority[sourcePriority].Add(matchingSource);

                        var sinkSourcePairs = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority][GetPriority(sink)];
                        for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                        {
                            if (sinkSourcePairs[pairIndex].Item1 == sink)
                            {
                                sinkSourcePairs.RemoveAtFast(pairIndex);
                                break;
                            }
                        }
                    }
                }

                m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
                if (resetSinkInput)
                    sink.SetInputFromDistributor(typeId, 0.0f, IsAdaptible(sink));
            }

            sink.OnAddType -= Sink_OnAddType;
            sink.RequiredInputChanged -= Sink_RequiredInputChanged;
            sink.ResourceAvailable -= Sink_IsResourceAvailable;
        }

        public void AddSource(MyResourceSourceComponent source)
        {
            Debug.Assert(source != null);

            foreach (var typeId in source.ResourceTypes)
            {
                if (!m_initializedTypes.Contains(typeId))
                    InitializeNewType(typeId);

                var sourcesOfType = GetSourcesOfType(typeId, source.Group);
                int typeIndex = GetTypeIndex(typeId);
                Debug.Assert(!sourcesOfType.Contains(source));
                Debug.Assert(MatchesInfiniteCapacity(sourcesOfType, source), "All producers in the same group must have same 'infinite capacity' state.");

                MyResourceSinkComponent matchingSink = null;
                if (source.Container != null)
                {
                    var sinkDataByPriority = m_dataPerType[typeIndex].SinksByPriority;
                    for (int priorityIndex = 0; priorityIndex < sinkDataByPriority.Length; ++priorityIndex)
                    {
                        var sinks = sinkDataByPriority[priorityIndex];
                        foreach (var sink in sinks)
                        {
                            if (sink.Container == null)
                                continue;

                            var sourceInContainer = sink.Container.Get<MyResourceSourceComponent>();
                            if (sourceInContainer == source)
                            {
                                m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority][priorityIndex].Add(MyTuple.Create(sink, source));
                                m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][GetPriority(source)].Add(MyTuple.Create(sink, source));
                                matchingSink = sink;
                                break;
                            }
                        }
                        if (matchingSink == null)
                            continue;

                        sinks.Remove(matchingSink);
                        break;

                    }
                }

                if (matchingSink == null)
                    sourcesOfType.Add(source);

                m_dataPerType[typeIndex].NeedsRecompute = true;
                ++m_dataPerType[typeIndex].SourceCount;

                if (m_dataPerType[typeIndex].SourceCount == 1)
                {
                    // This is the only source we have, so the state of all is the same as of this one.
                    m_dataPerType[typeIndex].SourcesEnabled = (source.Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
                }
                else if ((m_dataPerType[typeIndex].SourcesEnabled == MyMultipleEnabledEnum.AllEnabled && !source.Enabled) || (m_dataPerType[typeIndex].SourcesEnabled == MyMultipleEnabledEnum.AllDisabled && source.Enabled))
                {
                    m_dataPerType[typeIndex].SourcesEnabled = MyMultipleEnabledEnum.Mixed;
                }
                m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            }
            source.HasCapacityRemainingChanged += source_HasRemainingCapacityChanged;
            source.MaxOutputChanged += source_MaxOutputChanged;
            source.ProductionEnabledChanged += source_ProductionEnabledChanged;
        }

        public void RemoveSource(MyResourceSourceComponent source)
        {
            Debug.Assert(source != null);

            foreach (var typeId in source.ResourceTypes)
            {
                var sourcesOfType = GetSourcesOfType(typeId, source.Group);
                var typeIndex = GetTypeIndex(typeId);

                if (!sourcesOfType.Remove(source))
                {
                    int foundSinkIndex = -1;
                    int foundSinkPriorityIndex = -1;
                    var sinkSourcePairsBySourcePriority = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority];
                    for (int priorityIndex = 0; priorityIndex < sinkSourcePairsBySourcePriority.Length; ++priorityIndex)
                    {
                        var sinkSourcePairs = sinkSourcePairsBySourcePriority[priorityIndex];
                        for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                        {
                            if (sinkSourcePairs[pairIndex].Item2 != source)
                                continue;

                            foundSinkIndex = pairIndex;
                            foundSinkPriorityIndex = priorityIndex;
                            break;
                        }
                    }

                    if (foundSinkIndex != -1)
                    {
                        var matchingSink = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority][foundSinkPriorityIndex][foundSinkIndex].Item1;
                        m_dataPerType[typeIndex].SinkSourcePairsByPriority[sinkPriority][foundSinkPriorityIndex].RemoveAtFast(foundSinkIndex);
                        m_dataPerType[typeIndex].SinksByPriority[foundSinkPriorityIndex].Add(matchingSink);

                        var sinkSourcePairs = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][GetPriority(source)];
                        for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                        {
                            if (sinkSourcePairs[pairIndex].Item2 == source)
                            {
                                sinkSourcePairs.RemoveAtFast(pairIndex);
                                break;
                            }
                        }
                    }
                }

                m_dataPerType[typeIndex].NeedsRecompute = true;

                --m_dataPerType[typeIndex].SourceCount;
                if (m_dataPerType[typeIndex].SourceCount == 0)
                {
                    m_dataPerType[typeIndex].SourcesEnabled = MyMultipleEnabledEnum.NoObjects;
                }
                else if (m_dataPerType[typeIndex].SourceCount == 1)
                {
                    var firstSourceOfType = GetFirstSourceOfType(typeId);
                    if (firstSourceOfType != null)
                        ChangeSourcesState(typeId, (firstSourceOfType.Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled, MySession.LocalPlayerId);
                }
                else if (m_dataPerType[typeIndex].SourcesEnabled == MyMultipleEnabledEnum.Mixed)
                {
                    // We were in mixed state and need to check whether we still are.
                    m_dataPerType[typeIndex].SourcesEnabledDirty = true;
                }
                m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            }
            source.ProductionEnabledChanged -= source_ProductionEnabledChanged;
            source.MaxOutputChanged -= source_MaxOutputChanged;
            source.HasCapacityRemainingChanged -= source_HasRemainingCapacityChanged;

        }

        public int GetSourceCount(MyDefinitionId resourceTypeId, MyStringHash sourceGroupType)
        {
            int additionalCount = 0;
            int typeIndex = GetTypeIndex(resourceTypeId);
            int priorityIndex = m_sourceSubtypeToPriority[sourceGroupType];
            foreach (var pair in m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][priorityIndex])
                if (pair.Item2.CurrentOutputByType(resourceTypeId) > 0) ++additionalCount;

            return m_dataPerType[typeIndex].SourceDataByPriority[priorityIndex].ActiveCount + additionalCount;
        }

        #endregion

        public void UpdateBeforeSimulation10()
        {
            foreach (var typeId in m_typeIdToIndex.Keys)
            {
                int typeIndex = GetTypeIndex(typeId);
                if (m_dataPerType[typeIndex].NeedsRecompute)
                    RecomputeResourceDistribution(typeId);
                m_dataPerType[typeIndex].LastFuelTimeCompute += 10;
            }

            if (ShowTrace)
                UpdateTrace();

            m_allEnabledCounter += 10;
        }

        /// <summary>
        /// Computes number of groups that have enough energy to work.
        /// </summary>
        public void UpdateHud(MyHudSinkGroupInfo info)
        {
            bool isWorking = true;
            int workingGroupCount = 0;
            int i = 0;
            var typeIndex = GetTypeIndex(ElectricityId);
            for (; i < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++i)
            {
                if (isWorking &&
                    m_dataPerType[typeIndex].SinkDataByPriority[i].RemainingAvailableResource < m_dataPerType[typeIndex].SinkDataByPriority[i].RequiredInput &&
                    !m_dataPerType[typeIndex].SinkDataByPriority[i].IsAdaptible)
                    isWorking = false;

                if (isWorking)
                    ++workingGroupCount;

                info.SetGroupDeficit(i, Math.Max(m_dataPerType[typeIndex].SinkDataByPriority[i].RequiredInput - m_dataPerType[typeIndex].SinkDataByPriority[i].RemainingAvailableResource, 0.0f));
            }

            info.WorkingGroupCount = workingGroupCount;
        }

        public void ChangeSourcesState(MyDefinitionId resourceTypeId, MyMultipleEnabledEnum state, long playerId)
        {
            int typeIndex = GetTypeIndex(resourceTypeId);
            MyDebug.AssertDebug(state != MyMultipleEnabledEnum.Mixed, "You must NOT use this property to set mixed state.");
            MyDebug.AssertDebug(state != MyMultipleEnabledEnum.NoObjects, "You must NOT use this property to set state without any objects.");
            // You cannot change the state when there are no objects.
            if (m_dataPerType[typeIndex].SourcesEnabled == state || m_dataPerType[typeIndex].SourcesEnabled == MyMultipleEnabledEnum.NoObjects)
                return;

            m_dataPerType[typeIndex].SourcesEnabled = state;
            bool enabled = (state == MyMultipleEnabledEnum.AllEnabled);
            foreach (var group in m_dataPerType[typeIndex].SourcesByPriority)
            {
                foreach (var source in group)
                {
                    source.MaxOutputChanged -= source_MaxOutputChanged;
                    source.ProductionEnabledChanged -= source_ProductionEnabledChanged;
                    source.Enabled = enabled;
                    source.ProductionEnabledChanged += source_ProductionEnabledChanged;
                    source.MaxOutputChanged += source_MaxOutputChanged;
                }
            }

            // recomputing power distribution here caused problems with 
            // connecting to ship connector. The bug caused immediate disconnect
            // in only special cases.

            //RecomputeResourceDistribution();
            m_dataPerType[typeIndex].SourcesEnabledDirty = false;
            m_dataPerType[typeIndex].NeedsRecompute = true;
            m_allEnabledCounter = 0;
        }

        #region Private methods

        private float ComputeRemainingFuelTime(MyDefinitionId resourceTypeId)
        {
            ProfilerShort.Begin("MyResourceDistributor.ComputeRemainingFuelTime()");
            try
            {
                var typeIndex = GetTypeIndex(resourceTypeId);
                if (m_dataPerType[typeIndex].MaxAvailableResource == 0.0f)
                    return 0.0f;

                float powerInUse = 0.0f;
                foreach (MySinkGroupData groupData in m_dataPerType[typeIndex].SinkDataByPriority)
                {
                    if (groupData.RemainingAvailableResource >= groupData.RequiredInput)
                        powerInUse += groupData.RequiredInput;
                    else if (groupData.IsAdaptible)
                        powerInUse += groupData.RemainingAvailableResource;
                    else
                        break;
                }
                foreach (var sinkSourcePairData in m_dataPerType[typeIndex].SinkSourcePairDataByPriority[sinkPriority])
                {
                    if (sinkSourcePairData.Item1.RemainingAvailableResource >= sinkSourcePairData.Item1.RequiredInput)
                        powerInUse += sinkSourcePairData.Item1.RequiredInput;
                    else if (sinkSourcePairData.Item1.IsAdaptible)
                        powerInUse += sinkSourcePairData.Item1.RemainingAvailableResource;
                    else
                        break;
                }

                bool hasInfiniteProducer = false;
                bool hasAnyOtherProducer = false;
                float remainingCapacity = 0.0f;
                for (int i = 0; i < m_dataPerType[typeIndex].SourcesByPriority.Length; ++i)
                {
                    MySourceGroupData groupData = m_dataPerType[typeIndex].SourceDataByPriority[i];
                    if (groupData.UsageRatio <= 0f)
                        continue;

                    if (groupData.InfiniteCapacity)
                    {
                        hasInfiniteProducer = true;
                        // ignore power from infinite capacity group
                        powerInUse -= groupData.UsageRatio * groupData.MaxAvailableResource;
                        continue;
                    }

                    var group = m_dataPerType[typeIndex].SourcesByPriority[i];
                    foreach (MyResourceSourceComponent producer in group)
                    {
                        if (!producer.Enabled || !producer.ProductionEnabledByType(resourceTypeId))
                            continue;

                        hasAnyOtherProducer = true;
                        remainingCapacity += producer.RemainingCapacityByType(resourceTypeId);
                    }
                }

                for (int i = 0; i < m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority].Length; ++i)
                {
                    MySourceGroupData groupData = m_dataPerType[typeIndex].SinkSourcePairDataByPriority[sourcePriority][i].Item2;
                    if (groupData.UsageRatio <= 0f)
                        continue;

                    if (groupData.InfiniteCapacity)
                    {
                        hasInfiniteProducer = true;
                        // ignore power from infinite capacity group
                        powerInUse -= groupData.UsageRatio * groupData.MaxAvailableResource;
                        continue;
                    }

                    var group = m_dataPerType[typeIndex].SinkSourcePairsByPriority[sourcePriority][i];
                    foreach (var sinkSourcePair in group)
                    {
                        if (!sinkSourcePair.Item2.Enabled || !sinkSourcePair.Item2.ProductionEnabledByType(resourceTypeId))
                            continue;

                        hasAnyOtherProducer = true;
                        remainingCapacity += sinkSourcePair.Item2.RemainingCapacityByType(resourceTypeId);
                    }
                }

                if (hasInfiniteProducer && !hasAnyOtherProducer)
                    return float.PositiveInfinity;

                float remainingFuelTime = 0f;
                if (powerInUse > 0f)
                    remainingFuelTime = remainingCapacity / powerInUse;

                return remainingFuelTime;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void RefreshSourcesEnabled(MyDefinitionId resourceTypeId)
        {
            ProfilerShort.Begin("MyResourceDistributor.RefreshSourcesEnabled");
            int typeIndex;
            if (!TryGetTypeIndex(resourceTypeId, out typeIndex))
                return;

            m_dataPerType[typeIndex].SourcesEnabledDirty = false;
            // Simplest method for now. If it takes too long at some point, we can change it.

            if (m_dataPerType[typeIndex].SourceCount == 0)
            {
                m_dataPerType[typeIndex].SourcesEnabled = MyMultipleEnabledEnum.NoObjects;
                ProfilerShort.End();
                return;
            }

            bool allOn = true;
            bool allOff = true;
            foreach (var resourceSources in m_dataPerType[typeIndex].SourcesByPriority)
            {
                foreach (MyResourceSourceComponent source in resourceSources)
                {
                    allOn = allOn && source.Enabled;
                    allOff = allOff && !source.Enabled;
                    if (!allOn && !allOff)
                    {
                        m_dataPerType[typeIndex].SourcesEnabled = MyMultipleEnabledEnum.Mixed;
                        ProfilerShort.End();
                        return;
                    }
                }
            }
            m_dataPerType[typeIndex].SourcesEnabled = (allOn) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            ProfilerShort.End();
        }

        internal void CubeGrid_OnBlockAddedOrRemoved(MySlimBlock addedBlock)
        {
            var fatblock = addedBlock.FatBlock;
            if (fatblock == null)
                return;

            var conveyorEndpointBlock = fatblock as IMyConveyorEndpointBlock;
            var conveyorSegmentBlock = fatblock as IMyConveyorSegmentBlock;
            if (conveyorEndpointBlock != null || conveyorSegmentBlock != null)
            {
                foreach (var typeData in m_dataPerType) // TODO: Build the groups incrementally and update only the group that was affected when something changes
                {
                    typeData.GroupsDirty = true;
                }
            }
        }

        private void RecomputeResourceDistribution(MyDefinitionId typeId)
        {
            ProfilerShort.Begin("MyResourceDistributor.RecomputeResourceDistribution");

            int typeIndex = GetTypeIndex(typeId);

            if (!IsConveyorConnectionRequired(typeId))
            {
                ProfilerShort.Begin("ComputeInitial");
                ComputeInitialDistributionData(
                    typeId,
                    m_dataPerType[typeIndex].SinkDataByPriority,
                    m_dataPerType[typeIndex].SourceDataByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairDataByPriority,
                    m_dataPerType[typeIndex].SinksByPriority,
                    m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairsByPriority,
                    m_dataPerType[typeIndex].StockpilingStorageIndicesByPriority,
                    m_dataPerType[typeIndex].OtherStorageIndicesByPriority,
                    out m_dataPerType[typeIndex].MaxAvailableResource);
                ProfilerShort.BeginNextBlock("RecomputeDistribution");
                m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
                    typeId,
                    0,
                    m_dataPerType[typeIndex].SinkDataByPriority,
                    m_dataPerType[typeIndex].SourceDataByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairDataByPriority,
                    m_dataPerType[typeIndex].SinksByPriority,
                    m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairsByPriority,
                    m_dataPerType[typeIndex].StockpilingStorageIndicesByPriority,
                    m_dataPerType[typeIndex].OtherStorageIndicesByPriority,
                    m_dataPerType[typeIndex].MaxAvailableResource);
                ProfilerShort.End();
            }
            else
            {
                ProfilerShort.Begin("RecreatePhysical" + typeId);
                if (m_dataPerType[typeIndex].GroupsDirty)
                {
                    m_dataPerType[typeIndex].DistributionGroupsInUse = 0;
                    RecreatePhysicalDistributionGroups(typeId, m_dataPerType[typeIndex].SinksByPriority, m_dataPerType[typeIndex].SourcesByPriority, m_dataPerType[typeIndex].SinkSourcePairsByPriority);
                }

                MyResourceStateEnum totalState = MyResourceStateEnum.Ok;
                int powerOutCounter = 0;
                for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
                {
                    MyPhysicalDistributionGroup group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];

                    ProfilerShort.BeginNextBlock("ComputeInitial");
                    ComputeInitialDistributionData(
                    typeId,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    group.SinkSourcePairDataByPriority,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairsByPriority,
                    group.StockpilingStorageIndicesByPriority,
                    group.OtherStorageIndicesByPriority,
                    out group.MaxAvailableResources);

                    ProfilerShort.BeginNextBlock("RecomputeDistribution");
                    group.ResourceState = RecomputeResourceDistributionPartial(
                    typeId,
                    0,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    group.SinkSourcePairDataByPriority,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairsByPriority,
                    group.StockpilingStorageIndicesByPriority,
                    group.OtherStorageIndicesByPriority,
                    group.MaxAvailableResources);

                    m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;
                    //if (group.ResourceState == MyResourceStateEnum.NoPower && ++powerOutCounter == m_dataPerType[typeIndex].DistributionGroupsInUse)
                    //	totalState = MyResourceStateEnum.NoPower;
                }
                ProfilerShort.End();

            }

            m_dataPerType[typeIndex].NeedsRecompute = false;

            ProfilerShort.End();
        }

        void RecreatePhysicalDistributionGroups(
            MyDefinitionId typeId,
            HashSet<MyResourceSinkComponent>[] allSinksByPriority,
            HashSet<MyResourceSourceComponent>[] allSourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] allSinkSourcePairsByPriority)
        {
            foreach (var sinks in allSinksByPriority)
            {
                foreach (var sink in sinks)
                {
                    if (sink.Entity == null)
                    {
                        if (sink.TemporaryConnectedEntity == null)
                            continue;

                        SetEntityGroupForTempConnected(typeId, sink);
                        continue;
                    }

                    SetEntityGroup(typeId, sink.Entity);
                }
            }

            foreach (var sources in allSourcesByPriority)
            {
                foreach (var source in sources)
                {
                    if (source.Entity == null)
                    {
                        if (source.TemporaryConnectedEntity == null)
                            continue;

                        SetEntityGroupForTempConnected(typeId, source);
                        continue;
                    }

                    SetEntityGroup(typeId, source.Entity);
                }
            }

            foreach (var sinkSourcePairs in allSinkSourcePairsByPriority[sinkPriority])
            {
                foreach (var sinkSourcePair in sinkSourcePairs)
                {
                    if (sinkSourcePair.Item1.Entity != null)
                        SetEntityGroup(typeId, sinkSourcePair.Item1.Entity);
                }
            }
        }

        void SetEntityGroup(MyDefinitionId typeId, IMyEntity entity)
        {
            var block = entity as IMyConveyorEndpointBlock;
            if (block == null)
                return;

            int typeIndex = GetTypeIndex(typeId);
            bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            {
                if (!MyGridConveyorSystem.Pathfinding.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
                    continue;

                m_dataPerType[typeIndex].DistributionGroups[groupIndex].Add(typeId, block);
                found = true;
                break;
            }

            if (!found)
            {
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, block));
                else
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1].Init(typeId, block);
            }
        }

        void SetEntityGroupForTempConnected(MyDefinitionId typeId, MyResourceSinkComponent sink)
        {
            var block = sink.TemporaryConnectedEntity as IMyConveyorEndpointBlock;

            int typeIndex = GetTypeIndex(typeId);
            bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            {
                if (block == null || !MyGridConveyorSystem.Pathfinding.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
                {
                    bool addToGroup = false;
                    if (block == null)
                    {
                        foreach (var sources in m_dataPerType[typeIndex].DistributionGroups[groupIndex].SourcesByPriority)
                        {
                            foreach (var groupSource in sources)
                            {
                                if (sink.TemporaryConnectedEntity == groupSource.TemporaryConnectedEntity)
                                {
                                    addToGroup = true;
                                    break;
                                }
                            }
                            if (addToGroup)
                                break;
                        }
                    }
                    if (!addToGroup)
                        continue;
                }

                m_dataPerType[typeIndex].DistributionGroups[groupIndex].AddTempConnected(typeId, sink);
                found = true;
                break;
            }

            if (!found)
            {
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, sink));
                else
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1].InitFromTempConnected(typeId, sink);
            }
        }

        void SetEntityGroupForTempConnected(MyDefinitionId typeId, MyResourceSourceComponent source)
        {
            var block = source.TemporaryConnectedEntity as IMyConveyorEndpointBlock;

            int typeIndex = GetTypeIndex(typeId);
            bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            {
                if (block == null || !MyGridConveyorSystem.Pathfinding.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
                {
                    bool addToGroup = false;
                    if (block == null)
                    {
                        foreach (var sinks in m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinksByPriority)
                        {
                            foreach (var groupSink in sinks)
                            {
                                if (source.TemporaryConnectedEntity == groupSink.TemporaryConnectedEntity)
                                {
                                    addToGroup = true;
                                    break;
                                }
                            }
                            if (addToGroup)
                                break;
                        }
                    }
                    if (!addToGroup)
                        continue;
                }

                m_dataPerType[typeIndex].DistributionGroups[groupIndex].AddTempConnected(typeId, source);
                found = true;
                break;
            }

            if (!found)
            {
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, source));
                else
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1].InitFromTempConnected(typeId, source);
            }
        }

        private void ComputeInitialDistributionData(
            MyDefinitionId typeId,
            MySinkGroupData[] sinkDataByPriority,
            MySourceGroupData[] sourceDataByPriority,
            MyTuple<MySinkGroupData, MySourceGroupData>[][] sinkSourcePairDataByPriority,
            HashSet<MyResourceSinkComponent>[] sinksByPriority,
            HashSet<MyResourceSourceComponent>[] sourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] sinkSourcePairsByPriority,
            List<int>[][] stockpilingStorageIndicesByPriority,
            List<int>[][] otherStorageIndicesByPriority,
            out float maxAvailableResource)
        {
            // Clear state of all sources and sinks. Also find out how 
            // much of resource is available for distribution.
            maxAvailableResource = 0.0f;

            Debug.Assert(sourceDataByPriority.Length == sourcesByPriority.Length);
            for (int i = 0; i < sourceDataByPriority.Length; ++i)
            {
                var resourceSources = sourcesByPriority[i];
                MySourceGroupData sourceGroupData = sourceDataByPriority[i];
                sourceGroupData.MaxAvailableResource = 0f;
                foreach (MyResourceSourceComponent source in resourceSources)
                {
                    if (!source.Enabled || !source.HasCapacityRemainingByType(typeId))
                        continue;

                    sourceGroupData.MaxAvailableResource += source.MaxOutputByType(typeId);
                    sourceGroupData.InfiniteCapacity = source.IsInfiniteCapacity;
                }
                maxAvailableResource += sourceGroupData.MaxAvailableResource;
                sourceDataByPriority[i] = sourceGroupData;
            }

            float requiredInputCumulative = 0.0f;
            for (int i = 0; i < sinksByPriority.Length; ++i)
            {
                float requiredInput = 0.0f;
                bool isAdaptible = true;
                foreach (MyResourceSinkComponent sink in sinksByPriority[i])
                {
                    requiredInput += sink.RequiredInputByType(typeId);
                    isAdaptible = isAdaptible && IsAdaptible(sink);
                }
                sinkDataByPriority[i].RequiredInput = requiredInput;
                sinkDataByPriority[i].IsAdaptible = isAdaptible;

                requiredInputCumulative += requiredInput;
                sinkDataByPriority[i].RequiredInputCumulative = requiredInputCumulative;
            }

            PrepareSinkSourceData(typeId,
                sinkSourcePairDataByPriority,
                sinkSourcePairsByPriority,
                stockpilingStorageIndicesByPriority,
                otherStorageIndicesByPriority);

            for (int i = 0; i < sinkSourcePairsByPriority[sourcePriority].Length; ++i)
                maxAvailableResource += sinkSourcePairDataByPriority[sourcePriority][i].Item2.MaxAvailableResource;
        }

        private void PrepareSinkSourceData(
            MyDefinitionId typeId,
            MyTuple<MySinkGroupData, MySourceGroupData>[][] sinkSourcePairDataByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] sinkSourcePairsByPriority,
            List<int>[][] stockpilingStorageIndicesByPriority,
            List<int>[][] otherStorageIndicesByPriority)
        {
            for (int i = 0; i < sinkSourcePairsByPriority[sinkPriority].Length; ++i)
            {
                var sinkSourcePairs = sinkSourcePairsByPriority[sinkPriority][i];
                var sinkSourcePairData = sinkSourcePairDataByPriority[sinkPriority][i];
                var stockpilingStorageIndices = stockpilingStorageIndicesByPriority[sinkPriority][i];
                var otherStorageIndices = otherStorageIndicesByPriority[sinkPriority][i];

                stockpilingStorageIndices.Clear();
                otherStorageIndices.Clear();
                sinkSourcePairData.Item1.IsAdaptible = true;
                sinkSourcePairData.Item1.RequiredInput = 0f;
                sinkSourcePairData.Item1.RequiredInputCumulative = 0f;
                //sinkSourcePairData.Item2.MaxAvailableResource = 0f;
                for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                {
                    var sinkSourcePair = sinkSourcePairs[pairIndex];
                    bool productionEnabled = sinkSourcePair.Item2.ProductionEnabledByType(typeId);
                    bool isStockpiling = sinkSourcePair.Item2.Enabled && !productionEnabled && sinkSourcePair.Item1.RequiredInputByType(typeId) > 0;
                    sinkSourcePairData.Item1.IsAdaptible = sinkSourcePairData.Item1.IsAdaptible && IsAdaptible(sinkSourcePair.Item1);
                    sinkSourcePairData.Item1.RequiredInput += sinkSourcePair.Item1.RequiredInputByType(typeId);
                    if (isStockpiling)
                        sinkSourcePairData.Item1.RequiredInputCumulative += sinkSourcePair.Item1.RequiredInputByType(typeId);

                    //sinkSourcePairData.Item2.InfiniteCapacity = float.IsInfinity(sinkSourcePair.Item2.RemainingCapacityByType(typeId));
                    if (isStockpiling)
                        stockpilingStorageIndices.Add(pairIndex);
                    else
                    {
                        otherStorageIndices.Add(pairIndex);
                        //if (sinkSourcePair.Item2.Enabled && productionEnabled)
                        //    sinkSourcePairData.Item2.MaxAvailableResource += sinkSourcePair.Item2.MaxOutputByType(typeId);
                    }
                }

                sinkSourcePairDataByPriority[sinkPriority][i] = sinkSourcePairData;
            }
            for (int i = 0; i < sinkSourcePairsByPriority[sourcePriority].Length; ++i)
            {
                var sinkSourcePairs = sinkSourcePairsByPriority[sourcePriority][i];
                var sinkSourcePairData = sinkSourcePairDataByPriority[sourcePriority][i];
                var stockpilingStorageIndices = stockpilingStorageIndicesByPriority[sourcePriority][i];
                var otherStorageIndices = otherStorageIndicesByPriority[sourcePriority][i];

                stockpilingStorageIndices.Clear();
                otherStorageIndices.Clear();
                //sinkSourcePairData.Item1.IsAdaptible = true;
                //sinkSourcePairData.Item1.RequiredInput = 0f;
                //sinkSourcePairData.Item1.RequiredInputCumulative = 0f;
                sinkSourcePairData.Item2.MaxAvailableResource = 0f;
                for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
                {
                    var sinkSourcePair = sinkSourcePairs[pairIndex];
                    bool productionEnabled = sinkSourcePair.Item2.ProductionEnabledByType(typeId);
                    bool isStockpiling = sinkSourcePair.Item2.Enabled && !productionEnabled && sinkSourcePair.Item1.RequiredInputByType(typeId) > 0;
                    //sinkSourcePairData.Item1.IsAdaptible = sinkSourcePairData.Item1.IsAdaptible && IsAdaptible(sinkSourcePair.Item1);
                    //sinkSourcePairData.Item1.RequiredInput += sinkSourcePair.Item1.RequiredInputByType(typeId);
                    //if (isStockpiling)
                    //    sinkSourcePairData.Item1.RequiredInputCumulative += sinkSourcePair.Item1.RequiredInputByType(typeId);

                    sinkSourcePairData.Item2.InfiniteCapacity = float.IsInfinity(sinkSourcePair.Item2.RemainingCapacityByType(typeId));
                    if (isStockpiling)
                        stockpilingStorageIndices.Add(pairIndex);
                    else
                    {
                        otherStorageIndices.Add(pairIndex);
                        if (sinkSourcePair.Item2.Enabled && productionEnabled)
                            sinkSourcePairData.Item2.MaxAvailableResource += sinkSourcePair.Item2.MaxOutputByType(typeId);
                    }
                }

                sinkSourcePairDataByPriority[sourcePriority][i] = sinkSourcePairData;
            }
        }

        /// <summary>
        /// Recomputes power distribution in subset of all priority groups (in range
        /// from startPriorityIdx until the end). Passing index 0 recomputes all priority groups.
        /// </summary>
        private MyResourceStateEnum RecomputeResourceDistributionPartial(
            MyDefinitionId typeId,
            int startPriorityIdx,
            MySinkGroupData[] sinkDataByPriority,
            MySourceGroupData[] sourceDataByPriority,
            MyTuple<MySinkGroupData, MySourceGroupData>[][] sinkSourcePairDataByPriority,
            HashSet<MyResourceSinkComponent>[] sinksByPriority,
            HashSet<MyResourceSourceComponent>[] sourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>[][] sinkSourcePairsByPriority,
            List<int>[][] stockpilingStorageIndicesByPriority,
            List<int>[][] otherStorageIndicesByPriority,
            float availableResource)
        {
            ProfilerShort.Begin("MyResourceDistributor.RecomputeResourceDistributionPartial");
            ProfilerShort.Begin("Non-zero inputs");
            float totalAvailableResource = availableResource;
            int sinkPriorityIndex = startPriorityIdx;
            for (; sinkPriorityIndex < sinksByPriority.Length; ++sinkPriorityIndex)
            {
                sinkDataByPriority[sinkPriorityIndex].RemainingAvailableResource = availableResource;

                if (sinkDataByPriority[sinkPriorityIndex].RequiredInput <= availableResource)
                {
                    // Run everything in the group at max.
                    availableResource -= sinkDataByPriority[sinkPriorityIndex].RequiredInput;
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex])
                        sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), sinkDataByPriority[sinkPriorityIndex].IsAdaptible);
                }
                else if (sinkDataByPriority[sinkPriorityIndex].IsAdaptible && availableResource > 0.0f)
                {
                    // Distribute power in this group based on ratio of its requirement vs. group requirement.
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex])
                    {
                        float ratio = sink.RequiredInputByType(typeId) / sinkDataByPriority[sinkPriorityIndex].RequiredInput;
                        sink.SetInputFromDistributor(typeId, ratio * availableResource, true);
                    }
                    availableResource = 0.0f;
                }
                else
                {
                    // Not enough power for this group and members can't adapt.
                    // None of the lower priority groups will get any power either.
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex])
                        sink.SetInputFromDistributor(typeId, 0.0f, sinkDataByPriority[sinkPriorityIndex].IsAdaptible);
                    sinkDataByPriority[sinkPriorityIndex].RemainingAvailableResource = availableResource;
                    ++sinkPriorityIndex; // move on to next group
                    break;
                }
            }
            ProfilerShort.End();
            ProfilerShort.Begin("Zero inputs");
            // Set remaining data.
            for (; sinkPriorityIndex < sinkDataByPriority.Length; ++sinkPriorityIndex)
            {
                sinkDataByPriority[sinkPriorityIndex].RemainingAvailableResource = 0.0f;
                foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex])
                    sink.SetInputFromDistributor(typeId, 0.0f, sinkDataByPriority[sinkPriorityIndex].IsAdaptible);
            }
            ProfilerShort.End();
            float consumptionForNonStorage = totalAvailableResource - availableResource + (startPriorityIdx != 0 ? sinkDataByPriority[0].RemainingAvailableResource - sinkDataByPriority[startPriorityIdx].RemainingAvailableResource : 0f);

            float totalAvailableResourcesForStockpiles = Math.Max(totalAvailableResource - consumptionForNonStorage, 0);
            float availableResourcesForStockpiles = totalAvailableResourcesForStockpiles;
            ProfilerShort.Begin("Stockpiles");
            sinkPriorityIndex = startPriorityIdx;
            for (; sinkPriorityIndex < stockpilingStorageIndicesByPriority[sinkPriority].Length; ++sinkPriorityIndex)
            {
                var sinkSourceData = sinkSourcePairDataByPriority[sinkPriority][sinkPriorityIndex];
                var sinkSourcePairs = sinkSourcePairsByPriority[sinkPriority][sinkPriorityIndex];
                var stockpilingStorageList = stockpilingStorageIndicesByPriority[sinkPriority][sinkPriorityIndex];

                float stockpilesRequiredInput = sinkSourceData.Item1.RequiredInputCumulative;
                if (stockpilesRequiredInput <= availableResourcesForStockpiles)
                {
                    availableResourcesForStockpiles -= stockpilesRequiredInput;
                    foreach (int pairIndex in stockpilingStorageList)
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), true);
                    }
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStockpiles;
                }
                else
                {
                    foreach (int pairIndex in stockpilingStorageList)
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        float ratio = sink.RequiredInputByType(typeId) / stockpilesRequiredInput;
                        sink.SetInputFromDistributor(typeId, ratio * totalAvailableResourcesForStockpiles, true);
                    }
                    availableResourcesForStockpiles = 0f;
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStockpiles;
                }

                sinkSourcePairDataByPriority[sinkPriority][sinkPriorityIndex] = sinkSourceData;
            }
            ProfilerShort.End();

            float totalSinkSourceMaxAvailableResource = 0.0f;
            foreach (var sinkSourcePairData in sinkSourcePairDataByPriority[sourcePriority])
                totalSinkSourceMaxAvailableResource += sinkSourcePairData.Item2.MaxAvailableResource;

            ProfilerShort.Begin("Non-stockpiling storage");
            float consumptionForStockpiles = totalAvailableResourcesForStockpiles - availableResourcesForStockpiles;
            float totalAvailableResourcesForStorage = Math.Max(totalAvailableResource - totalSinkSourceMaxAvailableResource - consumptionForNonStorage - consumptionForStockpiles, 0);
            float availableResourcesForStorage = totalAvailableResourcesForStorage;
            sinkPriorityIndex = startPriorityIdx;
            for (; sinkPriorityIndex < otherStorageIndicesByPriority[sinkPriority].Length; ++sinkPriorityIndex)
            {
                var sinkSourceData = sinkSourcePairDataByPriority[sinkPriority][sinkPriorityIndex];
                var sinkSourcePairs = sinkSourcePairsByPriority[sinkPriority][sinkPriorityIndex];
                var otherStorageList = otherStorageIndicesByPriority[sinkPriority][sinkPriorityIndex];

                float ordinaryStorageRequiredInput = sinkSourceData.Item1.RequiredInput - sinkSourceData.Item1.RequiredInputCumulative;
                if (ordinaryStorageRequiredInput <= availableResourcesForStorage)
                {
                    availableResourcesForStorage -= ordinaryStorageRequiredInput;
                    foreach (int pairIndex in otherStorageList)
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), true);
                    }
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStorage;
                }
                else
                {
                    foreach (int pairIndex in otherStorageList)
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        float ratio = sink.RequiredInputByType(typeId) / ordinaryStorageRequiredInput;
                        sink.SetInputFromDistributor(typeId, ratio * availableResourcesForStorage, true);
                    }
                    availableResourcesForStorage = 0f;
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStorage;
                }

                sinkSourcePairDataByPriority[sinkPriority][sinkPriorityIndex] = sinkSourceData;
            }
            ProfilerShort.End();

            ProfilerShort.Begin("Sources");
            int sourcePriorityIndex = 0;
            float consumptionForStorage = totalAvailableResourcesForStorage - availableResourcesForStorage;
            float consumptionForNonStorageAndStockpiles = consumptionForStockpiles + consumptionForNonStorage;
            float remainingConsumptionForStorage = consumptionForStorage;
            //float totalRemainingConsumption = consumptionForNonStorageAndStockpiles + consumptionForStorage;
            for (; sourcePriorityIndex < sourcesByPriority.Length; ++sourcePriorityIndex)
            {
                var sinkSourceData = sinkSourcePairDataByPriority[sourcePriority][sourcePriorityIndex];

                if (sinkSourceData.Item2.MaxAvailableResource > 0)
                {
                    float amountToSupply = consumptionForNonStorageAndStockpiles;
                    sinkSourceData.Item2.UsageRatio = Math.Min(1f, amountToSupply / sinkSourceData.Item2.MaxAvailableResource);
                    consumptionForNonStorageAndStockpiles -= Math.Min(amountToSupply, sinkSourceData.Item2.MaxAvailableResource);
                }
                else
                    sinkSourceData.Item2.UsageRatio = 0f;

                sinkSourceData.Item2.ActiveCount = 0;
                foreach (int pairIndex in otherStorageIndicesByPriority[sourcePriority][sourcePriorityIndex])
                {
                    var source = sinkSourcePairsByPriority[sourcePriority][sourcePriorityIndex][pairIndex].Item2;
                    if (!source.Enabled || !source.ProductionEnabledByType(typeId) || !source.HasCapacityRemainingByType(typeId))
                        continue;

                    ++sinkSourceData.Item2.ActiveCount;
                    ProfilerShort.Begin("Set CurrentOutput");
                    source.SetOutputByType(typeId, sinkSourceData.Item2.UsageRatio * source.MaxOutputByType(typeId));
                    ProfilerShort.End();
                }

                if (sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource > 0f)
                {
                    float amountToSupply = Math.Max(consumptionForNonStorageAndStockpiles + remainingConsumptionForStorage, 0f);
                    sourceDataByPriority[sourcePriorityIndex].UsageRatio = Math.Min(1f, amountToSupply / sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource);
                    consumptionForNonStorageAndStockpiles -= Math.Min(amountToSupply, sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource);
                    if (consumptionForNonStorageAndStockpiles < 0)
                    {
                        remainingConsumptionForStorage += consumptionForNonStorageAndStockpiles;
                        consumptionForNonStorageAndStockpiles = 0;
                    }
                }
                else
                    sourceDataByPriority[sourcePriorityIndex].UsageRatio = 0f;

                sourceDataByPriority[sourcePriorityIndex].ActiveCount = 0;
                foreach (MyResourceSourceComponent source in sourcesByPriority[sourcePriorityIndex])
                {
                    if (!source.Enabled || !source.HasCapacityRemainingByType(typeId))
                        continue;

                    ++sourceDataByPriority[sourcePriorityIndex].ActiveCount;
                    ProfilerShort.Begin("Set CurrentOutput");
                    source.SetOutputByType(typeId, sourceDataByPriority[sourcePriorityIndex].UsageRatio * source.MaxOutputByType(typeId));
                    ProfilerShort.End();
                }

                sinkSourcePairDataByPriority[sourcePriority][sourcePriorityIndex] = sinkSourceData;
            }

            MyResourceStateEnum resultState;
            if (totalAvailableResource == 0.0f)
                resultState = MyResourceStateEnum.NoPower;
            else if (sinkDataByPriority[m_sinkGroupPrioritiesTotal - 1].RequiredInputCumulative > totalAvailableResource)
            {
                MySinkGroupData lastGroup = sinkDataByPriority.Last();
                if (lastGroup.IsAdaptible && lastGroup.RemainingAvailableResource != 0.0f)
                    resultState = MyResourceStateEnum.OverloadAdaptible;
                else
                    resultState = MyResourceStateEnum.OverloadBlackout;
            }
            else
                resultState = MyResourceStateEnum.Ok;
            ProfilerShort.End();

            ProfilerShort.End();

            return resultState;
        }

        /// <summary>
        /// Mostly debug method to verify that all members of the group have 
        /// same ability to adapt as given sink.
        /// </summary>
        private bool MatchesAdaptability(HashSet<MyResourceSinkComponent> group, MyResourceSinkComponent referenceSink)
        {
            var referenceAdaptibility = IsAdaptible(referenceSink);
            foreach (MyResourceSinkComponent sink in group)
            {
                var isAdaptible = IsAdaptible(sink);
                if (isAdaptible != referenceAdaptibility)
                    return false;
            }
            return true;
        }

        private bool MatchesInfiniteCapacity(HashSet<MyResourceSourceComponent> group, MyResourceSourceComponent producer)
        {
            foreach (MyResourceSourceComponent source in group)
            {
                if (producer.IsInfiniteCapacity != source.IsInfiniteCapacity)
                    return false;
            }
            return true;
        }

        [Conditional("DEBUG")]
        private void UpdateTrace()
        {
            for (int typeIndex = 0; typeIndex < m_typeGroupCount; ++typeIndex)
            {
                for (int i = 0; i < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++i)
                {
                    var data = m_dataPerType[typeIndex].SinkDataByPriority[i];
                    MyTrace.Watch(String.Format("Data[{0}][{1}].RemainingAvailableResource", typeIndex, i), data.RemainingAvailableResource);
                }
                for (int i = 0; i < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++i)
                {
                    var data = m_dataPerType[typeIndex].SinkDataByPriority[i];
                    MyTrace.Watch(String.Format("Data[{0}][{1}].RequiredInput", typeIndex, i), data.RequiredInput);
                }
                for (int i = 0; i < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++i)
                {
                    var data = m_dataPerType[typeIndex].SinkDataByPriority[i];
                    MyTrace.Watch(String.Format("Data[{0}][{1}].IsAdaptible", typeIndex, i), data.IsAdaptible);
                }

                int j = 0;
                foreach (var group in m_dataPerType[typeIndex].SourcesByPriority)
                    foreach (var producer in group)
                    {
                        ++j;
                        MyTrace.Watch(String.Format("Producer[{0}][{1}].IsTurnedOn", typeIndex, j), producer.Enabled);
                        MyTrace.Watch(String.Format("Producer[{0}][{1}].HasRemainingCapacity", typeIndex, j), producer.HasCapacityRemaining);
                        MyTrace.Watch(String.Format("Producer[{0}][{1}].CurrentOutput", typeIndex, j), producer.CurrentOutput);
                    }
            }
        }

        private HashSet<MyResourceSinkComponent> GetSinksOfType(MyDefinitionId typeId, MyStringHash groupType)
        {
            return m_dataPerType[GetTypeIndex(typeId)].SinksByPriority[m_sinkSubtypeToPriority[groupType]];
        }

        private HashSet<MyResourceSourceComponent> GetSourcesOfType(MyDefinitionId typeId, MyStringHash groupType)
        {
            return m_dataPerType[GetTypeIndex(typeId)].SourcesByPriority[m_sourceSubtypeToPriority[groupType]];
        }

        private MyResourceSourceComponent GetFirstSourceOfType(MyDefinitionId resourceTypeId)
        {
            var typeIndex = GetTypeIndex(resourceTypeId);
            for (int i = 0; i < m_dataPerType[typeIndex].SourcesByPriority.Length; ++i)
            {
                if (m_dataPerType[typeIndex].SourcesByPriority[i].Count > 0)
                    return m_dataPerType[typeIndex].SourcesByPriority[i].First();
            }
            return null;
        }

        public MyMultipleEnabledEnum SourcesEnabledByType(MyDefinitionId resourceTypeId)
        {
            int typeIndex;
            if (!TryGetTypeIndex(resourceTypeId, out typeIndex))
                return MyMultipleEnabledEnum.NoObjects;

            if (m_dataPerType[typeIndex].SourcesEnabledDirty)
                RefreshSourcesEnabled(resourceTypeId);

            return m_dataPerType[typeIndex].SourcesEnabled;
        }

        public float RemainingFuelTimeByType(MyDefinitionId resourceTypeId)
        {
            int typeIndex;
            if (!TryGetTypeIndex(resourceTypeId, out typeIndex))
                return 0f;

            if (!m_dataPerType[typeIndex].RemainingFuelTimeDirty || m_dataPerType[typeIndex].LastFuelTimeCompute <= 30)
                return m_dataPerType[typeIndex].RemainingFuelTime;

            m_dataPerType[typeIndex].RemainingFuelTime = ComputeRemainingFuelTime(resourceTypeId);
            m_dataPerType[typeIndex].LastFuelTimeCompute = 0;
            return m_dataPerType[typeIndex].RemainingFuelTime;
        }

        public MyResourceStateEnum ResourceStateByType(MyDefinitionId typeId)
        {
            var typeIndex = GetTypeIndex(typeId);
            if (m_dataPerType[typeIndex].NeedsRecompute)
                RecomputeResourceDistribution(typeId);

            return m_dataPerType[typeIndex].ResourceState;
        }

        private bool TryGetTypeIndex(MyDefinitionId typeId, out int typeIndex)
        {
            typeIndex = 0;
            if (m_typeGroupCount == 0)
                return false;
            else if (m_typeGroupCount > 1)
                return m_typeIdToIndex.TryGetValue(typeId, out typeIndex);

            return true;
        }

        private int GetTypeIndex(MyDefinitionId typeId)
        {
            var typeIndex = 0;
            if (m_typeGroupCount > 1)
                typeIndex = m_typeIdToIndex[typeId];
            return typeIndex;
        }

        private static int GetTypeIndexTotal(MyDefinitionId typeId)
        {
            var typeIndex = 0;
            if (m_typeGroupCountTotal > 1)
                typeIndex = m_typeIdToIndexTotal[typeId];
            return typeIndex;
        }

        public static bool IsConveyorConnectionRequiredTotal(MyDefinitionId typeId)
        {
            return m_typeIdToConveyorConnectionRequiredTotal[typeId];
        }

        private bool IsConveyorConnectionRequired(MyDefinitionId typeId)
        {
            return m_typeIdToConveyorConnectionRequired[typeId];
        }

        static internal int GetPriority(MyResourceSinkComponent sink)
        {
            return m_sinkSubtypeToPriority[sink.Group];
        }

        static internal int GetPriority(MyResourceSourceComponent source)
        {
            return m_sourceSubtypeToPriority[source.Group];
        }

        private bool IsAdaptible(MyResourceSinkComponent sink)
        {
            return m_sinkSubtypeToAdaptability[sink.Group];
        }

        #endregion

        #region Event handlers

        private void Sink_OnAddType(MyResourceSinkComponent sink)
        {
            RemoveSink(sink, false);
            AddSink(sink);
        }

        private void Sink_RequiredInputChanged(MyDefinitionId changedResourceTypeId, MyResourceSinkComponent changedSink, float oldRequirement, float newRequirement)
        {
            var typeIndex = GetTypeIndex(changedResourceTypeId);
            if (m_dataPerType[typeIndex].NeedsRecompute)
            {
                RecomputeResourceDistribution(changedResourceTypeId);
                return;
            }

            int groupId = GetPriority(changedSink);

            if (!IsConveyorConnectionRequired(changedResourceTypeId))
            {
                // Go over all priorities, starting from the changedSink.
                MyDebug.AssertDebug(m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput >= 0.0f);
                m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput = 0.0f;
                foreach (MyResourceSinkComponent sink in m_dataPerType[typeIndex].SinksByPriority[groupId])
                    m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput += sink.RequiredInputByType(changedResourceTypeId);

                // Update cumulative requirements.
                float cumulative = (groupId != 0) ? m_dataPerType[typeIndex].SinkDataByPriority[groupId - 1].RequiredInputCumulative : 0.0f;
                for (int index = groupId; index < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++index)
                {
                    cumulative += m_dataPerType[typeIndex].SinkDataByPriority[index].RequiredInput;
                    m_dataPerType[typeIndex].SinkDataByPriority[index].RequiredInputCumulative = cumulative;
                }

                PrepareSinkSourceData(changedResourceTypeId,
                    m_dataPerType[typeIndex].SinkSourcePairDataByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairsByPriority,
                    m_dataPerType[typeIndex].StockpilingStorageIndicesByPriority,
                    m_dataPerType[typeIndex].OtherStorageIndicesByPriority);

                m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
                    changedResourceTypeId,
                    groupId,
                    m_dataPerType[typeIndex].SinkDataByPriority,
                    m_dataPerType[typeIndex].SourceDataByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairDataByPriority,
                    m_dataPerType[typeIndex].SinksByPriority,
                    m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].SinkSourcePairsByPriority,
                    m_dataPerType[typeIndex].StockpilingStorageIndicesByPriority,
                    m_dataPerType[typeIndex].OtherStorageIndicesByPriority,
                    m_dataPerType[typeIndex].SinkDataByPriority[groupId].RemainingAvailableResource);
            }
            else
            {
                for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
                {
                    if (!m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinksByPriority[groupId].Contains(changedSink) && m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinkSourcePairsByPriority[sinkPriority][groupId].TrueForAll((pair) => pair.Item1 != changedSink))
                        continue;

                    var group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];

                    MyDebug.AssertDebug(group.SinkDataByPriority[groupId].RequiredInput >= 0.0f);
                    group.SinkDataByPriority[groupId].RequiredInput = 0.0f;
                    foreach (MyResourceSinkComponent sink in group.SinksByPriority[groupId])
                        group.SinkDataByPriority[groupId].RequiredInput += sink.RequiredInputByType(changedResourceTypeId);

                    float cumulative = (groupId != 0) ? group.SinkDataByPriority[groupId - 1].RequiredInputCumulative : 0.0f;
                    for (int index = groupId; index < group.SinkDataByPriority.Length; ++index)
                    {
                        cumulative += group.SinkDataByPriority[index].RequiredInput;
                        group.SinkDataByPriority[index].RequiredInputCumulative = cumulative;
                    }

                    PrepareSinkSourceData(changedResourceTypeId,
                        group.SinkSourcePairDataByPriority,
                        group.SinkSourcePairsByPriority,
                        group.StockpilingStorageIndicesByPriority,
                        group.OtherStorageIndicesByPriority);

                    group.ResourceState = RecomputeResourceDistributionPartial(
                    changedResourceTypeId,
                    groupId,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    group.SinkSourcePairDataByPriority,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairsByPriority,
                    group.StockpilingStorageIndicesByPriority,
                    group.OtherStorageIndicesByPriority,
                    group.MaxAvailableResources);

                    m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;

                    break;
                }
            }
        }

        private float Sink_IsResourceAvailable(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver)
        {
            var typeIndex = GetTypeIndex(resourceTypeId);
            int groupIndex = m_sinkSubtypeToPriority[receiver.Group];
            return m_dataPerType[typeIndex].SinkDataByPriority[groupIndex].RemainingAvailableResource - m_dataPerType[typeIndex].SinkDataByPriority[groupIndex].RequiredInput;
        }

        private void source_HasRemainingCapacityChanged(MyDefinitionId changedResourceTypeId, MyResourceSourceComponent source)
        {
            int typeIndex = GetTypeIndex(changedResourceTypeId);
            m_dataPerType[typeIndex].NeedsRecompute = true;
            m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
        }

        private void source_MaxOutputChanged(MyDefinitionId changedResourceTypeId, float oldOutput, MyResourceSourceComponent obj)
        {
            int typeIndex = GetTypeIndex(changedResourceTypeId);
            m_dataPerType[typeIndex].NeedsRecompute = true;
            m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            m_dataPerType[typeIndex].SourcesEnabledDirty = true;

            // Don't wait for next update with few sources.
            // Also ensures that when character enters cockpit, his battery is
            // turned off right away without waiting for update.
            if (m_dataPerType[typeIndex].SourceCount == 1)
                RecomputeResourceDistribution(changedResourceTypeId);
        }

        private void source_ProductionEnabledChanged(MyDefinitionId changedResourceTypeId, MyResourceSourceComponent obj)
        {
            int typeIndex = GetTypeIndex(changedResourceTypeId);
            m_dataPerType[typeIndex].NeedsRecompute = true;
            m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            m_dataPerType[typeIndex].SourcesEnabledDirty = true;

            if (m_dataPerType[typeIndex].SourceCount == 1)
                RecomputeResourceDistribution(changedResourceTypeId);
        }

        #endregion

        public override string ComponentTypeDebugString { get { return "Resource Distributor"; } }
    }
}
