using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Profiler;
using VRage.Trace;
using VRage.Utils;
using VRage.Game.ModAPI;

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
        public static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        public static readonly MyDefinitionId HydrogenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Hydrogen");
        public static readonly MyDefinitionId OxygenId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");


		private struct MyPhysicalDistributionGroup
		{
			public IMyConveyorEndpoint FirstEndpoint;
			public HashSet<MyResourceSinkComponent>[] SinksByPriority;
			public HashSet<MyResourceSourceComponent>[] SourcesByPriority;
		    public List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> SinkSourcePairs;

			public MySinkGroupData[] SinkDataByPriority;
			public MySourceGroupData[] SourceDataByPriority;
			public MyTuple<MySinkGroupData, MySourceGroupData> InputOutputData;
			public List<int> StockpilingStorage;
			public List<int> OtherStorage;

			public float MaxAvailableResources;
			public MyResourceStateEnum ResourceState;

			public MyPhysicalDistributionGroup(MyDefinitionId typeId, IMyConveyorEndpointBlock block)
			{
			    SinksByPriority = null; SourcesByPriority = null; SinkSourcePairs = null; FirstEndpoint = null;
				SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorage = null; OtherStorage = null;
				InputOutputData = new MyTuple<MySinkGroupData, MySourceGroupData>();
				MaxAvailableResources = 0f; ResourceState = MyResourceStateEnum.NoPower;
				AllocateData();

				Init(typeId, block);
			}

            public MyPhysicalDistributionGroup(MyDefinitionId typeId, MyResourceSinkComponent tempConnectedSink)
            {
                SinksByPriority = null; SourcesByPriority = null; SinkSourcePairs = null; FirstEndpoint = null;
				SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorage = null; OtherStorage = null;
				InputOutputData = new MyTuple<MySinkGroupData, MySourceGroupData>();
				MaxAvailableResources = 0f; ResourceState = MyResourceStateEnum.NoPower;
                AllocateData();

                InitFromTempConnected(typeId, tempConnectedSink);
            }

            public MyPhysicalDistributionGroup(MyDefinitionId typeId, MyResourceSourceComponent tempConnectedSource)
            {
                SinksByPriority = null; SourcesByPriority = null; SinkSourcePairs = null; FirstEndpoint = null;
                SinkDataByPriority = null; SourceDataByPriority = null; StockpilingStorage = null; OtherStorage = null;
                InputOutputData = new MyTuple<MySinkGroupData, MySourceGroupData>();
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
                if(conveyorEndPointBlock != null)
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
                if (FirstEndpoint == null)
                    FirstEndpoint = endpoint.ConveyorEndpoint;

				var componentContainer = (endpoint as IMyEntity).Components;

                var sink = componentContainer.Get<MyResourceSinkComponent>();
                var source = componentContainer.Get<MyResourceSourceComponent>();

                bool containsSink = sink != null && sink.AcceptedResources.Contains(typeId);
                bool containsSource = source != null && source.ResourceTypes.Contains(typeId);
                if (containsSink && containsSource)
                {
                    SinkSourcePairs.Add(new MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>(sink, source));
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
                SinkSourcePairs = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>();
				SinkDataByPriority = new MySinkGroupData[m_sinkGroupPrioritiesTotal];
				SourceDataByPriority = new MySourceGroupData[m_sourceGroupPrioritiesTotal];
				StockpilingStorage = new List<int>();
				OtherStorage = new List<int>();

                for (int priorityIndex = 0; priorityIndex < m_sinkGroupPrioritiesTotal; ++priorityIndex)
                    SinksByPriority[priorityIndex] = new HashSet<MyResourceSinkComponent>();

                for (int priorityIndex = 0; priorityIndex < m_sourceGroupPrioritiesTotal; ++priorityIndex)
                    SourcesByPriority[priorityIndex] = new HashSet<MyResourceSourceComponent>();
		    }

		    private void ClearData()
		    {
                foreach (var sinks in SinksByPriority)
                    sinks.Clear();

                foreach (var sources in SourcesByPriority)
                    sources.Clear();

                SinkSourcePairs.Clear();

				StockpilingStorage.SetSize(0);
				OtherStorage.SetSize(0);
		    }
		}

        #region Local structs and classes

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
            public MyDefinitionId TypeId;
			public MySinkGroupData[] SinkDataByPriority;
			public MySourceGroupData[] SourceDataByPriority;
			public MyTuple<MySinkGroupData, MySourceGroupData> InputOutputData;

			public HashSet<MyResourceSinkComponent>[] SinksByPriority;
			public HashSet<MyResourceSourceComponent>[] SourcesByPriority;
			public List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> InputOutputList;

			public List<int> StockpilingStorageIndices;
			public List<int> OtherStorageIndices;

            public List<MyPhysicalDistributionGroup> DistributionGroups;
            public int DistributionGroupsInUse;

		    public bool GroupsDirty;
			public bool NeedsRecompute;
			public int SourceCount;
			public float RemainingFuelTime;
			public bool RemainingFuelTimeDirty;
			public float MaxAvailableResource;
			public MyMultipleEnabledEnum SourcesEnabled;
			public bool SourcesEnabledDirty;
			public MyResourceStateEnum ResourceState;
		}

        #endregion

		private int m_typeGroupCount = 0;

		private readonly List<PerTypeData> m_dataPerType = new List<PerTypeData>();
		private readonly HashSet<MyDefinitionId> m_initializedTypes = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
		private readonly Dictionary<MyDefinitionId, int> m_typeIdToIndex = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
		private readonly Dictionary<MyDefinitionId, bool> m_typeIdToConveyorConnectionRequired = new Dictionary<MyDefinitionId, bool>(MyDefinitionId.Comparer);

        private readonly HashSet<MyDefinitionId> m_typesToRemove = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);

        private readonly MyConcurrentHashSet<MyResourceSinkComponent> m_sinksToAdd = new MyConcurrentHashSet<MyResourceSinkComponent>();
        private readonly MyConcurrentHashSet<MyTuple<MyResourceSinkComponent, bool>> m_sinksToRemove = new MyConcurrentHashSet<MyTuple<MyResourceSinkComponent, bool>>();
        private readonly MyConcurrentHashSet<MyResourceSourceComponent> m_sourcesToAdd = new MyConcurrentHashSet<MyResourceSourceComponent>();
        private readonly MyConcurrentHashSet<MyResourceSourceComponent> m_sourcesToRemove = new MyConcurrentHashSet<MyResourceSourceComponent>();
        private readonly MyConcurrentDictionary<MyDefinitionId, int> m_changedTypes = new MyConcurrentDictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);

	    /// <summary>
	    /// Remaining fuel time in hours.
	    /// </summary>

	    #region Properties

	    /// <summary>
	    /// For debugging purposes. Enables trace messages and watches for this instance.
	    /// </summary>
        public static bool ShowTrace = false;

        public string DebugName;

	    public MyMultipleEnabledEnum SourcesEnabled { get { return SourcesEnabledByType(m_typeIdToIndexTotal.Keys.First()); } }
	    public MyResourceStateEnum ResourceState { get { return ResourceStateByType(m_typeIdToIndexTotal.Keys.First()); } }

	    #endregion

		private static int m_typeGroupCountTotal = -1;
		private static int m_sinkGroupPrioritiesTotal = -1;
		private static int m_sourceGroupPrioritiesTotal = -1;

		public static int SinkGroupPrioritiesTotal { get { return m_sinkGroupPrioritiesTotal; } }

		private static readonly Dictionary<MyDefinitionId, int> m_typeIdToIndexTotal = new Dictionary<MyDefinitionId, int>(MyDefinitionId.Comparer);
        private static readonly Dictionary<MyDefinitionId, bool> m_typeIdToConveyorConnectionRequiredTotal = new Dictionary<MyDefinitionId, bool>(MyDefinitionId.Comparer);

        private static readonly Dictionary<MyStringHash, int> m_sourceSubtypeToPriority = new Dictionary<MyStringHash, int>(MyStringHash.Comparer);
        private static readonly Dictionary<MyStringHash, int> m_sinkSubtypeToPriority = new Dictionary<MyStringHash, int>(MyStringHash.Comparer);
        private readonly static Dictionary<MyStringHash, bool> m_sinkSubtypeToAdaptability = new Dictionary<MyStringHash, bool>(MyStringHash.Comparer);
		public static DictionaryReader<MyStringHash, int> SinkSubtypesToPriority { get { return new DictionaryReader<MyStringHash, int>(m_sinkSubtypeToPriority); } }

	    internal static void InitializeMappings()
	    {
            lock (m_typeIdToIndexTotal)
            {
                if (m_sinkGroupPrioritiesTotal >= 0 || m_sourceGroupPrioritiesTotal >= 0)
                    return;

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
                m_typeIdToIndexTotal.Add(ElectricityId, m_typeGroupCountTotal++);	// Electricity is always in game for now (needed in ME for jetpack)
                m_typeIdToConveyorConnectionRequiredTotal.Add(ElectricityId, false);

                var gasTypes = MyDefinitionManager.Static.GetDefinitionsOfType<MyGasProperties>();	// Get viable fuel types from some definition?
                foreach (var gasDefinition in gasTypes)
                {
                    m_typeIdToIndexTotal.Add(gasDefinition.Id, m_typeGroupCountTotal++);
                    m_typeIdToConveyorConnectionRequiredTotal.Add(gasDefinition.Id, true);	// MK: TODO: Read this from definition
                }
            }
	    }

		private void InitializeNewType(ref MyDefinitionId typeId)
		{
			m_typeIdToIndex.Add(typeId, m_typeGroupCount++);
			m_typeIdToConveyorConnectionRequired.Add(typeId, IsConveyorConnectionRequiredTotal(ref typeId));

            var sinksByPriority = new HashSet<MyResourceSinkComponent>[m_sinkGroupPrioritiesTotal];
            var sourceByPriority = new HashSet<MyResourceSourceComponent>[m_sourceGroupPrioritiesTotal];
            var inputOutputList = new List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>>();

            for (int priorityIndex = 0; priorityIndex < sinksByPriority.Length; ++priorityIndex)
                sinksByPriority[priorityIndex] = new HashSet<MyResourceSinkComponent>();

            for (int priorityIndex = 0; priorityIndex < sourceByPriority.Length; ++priorityIndex)
                sourceByPriority[priorityIndex] = new HashSet<MyResourceSourceComponent>();

            List<MyPhysicalDistributionGroup> distributionGroups = null;
		    int distributionGroupsInUse = 0;

		    MySinkGroupData[] sinkGroupDataByPriority = null;
		    MySourceGroupData[] sourceGroupDatabyPriority = null;
		    List<int> stockpilingStorageIndices = null;
		    List<int> otherStorageIndices = null;

		    if (IsConveyorConnectionRequired(ref typeId))
		    {
                distributionGroups = new List<MyPhysicalDistributionGroup>();
		    }
		    else
		    {
                sinkGroupDataByPriority = new MySinkGroupData[m_sinkGroupPrioritiesTotal];
                sourceGroupDatabyPriority = new MySourceGroupData[m_sourceGroupPrioritiesTotal];
                stockpilingStorageIndices = new List<int>();
                otherStorageIndices = new List<int>();
		    }

            m_dataPerType.Add(new PerTypeData
            {
                TypeId = typeId,
                SinkDataByPriority = sinkGroupDataByPriority,
                SourceDataByPriority = sourceGroupDatabyPriority,
                InputOutputData = new MyTuple<MySinkGroupData, MySourceGroupData>(),
                SinksByPriority = sinksByPriority,
                SourcesByPriority = sourceByPriority,
                InputOutputList = inputOutputList,
                StockpilingStorageIndices = stockpilingStorageIndices,
                OtherStorageIndices = otherStorageIndices,
                DistributionGroups = distributionGroups,
                DistributionGroupsInUse = distributionGroupsInUse,
                NeedsRecompute = true,
                GroupsDirty = true,
                SourceCount = 0,
                RemainingFuelTime = 0,
                RemainingFuelTimeDirty = true,
                MaxAvailableResource = 0,
                SourcesEnabled = MyMultipleEnabledEnum.NoObjects,
                SourcesEnabledDirty = true,
                ResourceState = MyResourceStateEnum.NoPower,
            });

			m_initializedTypes.Add(typeId);
		}

		public MyResourceDistributorComponent(string debugName)
	    {
			InitializeMappings();
            DebugName = debugName;
	    }

	    #region Add and remove

        private void RemoveTypesFromChanges(ListReader<MyDefinitionId> types)
        {
            foreach(var resourceType in types)
            {
                int existingCount;
                if (m_changedTypes.TryGetValue(resourceType, out existingCount))
                    m_changedTypes[resourceType] = existingCount - 1;
            }
        }

	    public void AddSink(MyResourceSinkComponent sink)
	    {
		    Debug.Assert(sink != null);

            MyTuple<MyResourceSinkComponent, bool> foundTuple = default(MyTuple<MyResourceSinkComponent, bool>);
            foreach(var sinkPair in m_sinksToRemove)
            {
                if(sinkPair.Item1 == sink)
                {
                    foundTuple = sinkPair;
                    break;
                }
            }

            lock (m_sinksToRemove)
            {
                if (foundTuple.Item1 != null)
                {
                    m_sinksToRemove.Remove(foundTuple);
                    RemoveTypesFromChanges(foundTuple.Item1.AcceptedResources);
                    return;
                }
            }
            lock (m_sinksToAdd)
            {
                m_sinksToAdd.Add(sink);
            }

            foreach (var resourceType in sink.AcceptedResources)
            {
                int existingCount;
                if (!m_changedTypes.TryGetValue(resourceType, out existingCount))
                    m_changedTypes.Add(resourceType, 1);
                else
                    m_changedTypes[resourceType] = existingCount+1;
            }
	    }

	    public void RemoveSink(MyResourceSinkComponent sink, bool resetSinkInput = true, bool markedForClose = false)
	    {
		    if (markedForClose)
			    return;

		    Debug.Assert(sink != null);

            lock (m_sinksToAdd)
            {
                if (m_sinksToAdd.Contains(sink))
                {
                    m_sinksToAdd.Remove(sink);
                    RemoveTypesFromChanges(sink.AcceptedResources);
                    return;
                }
            }

            lock (m_sinksToRemove)
            {
                m_sinksToRemove.Add(MyTuple.Create(sink, true));
            }

            foreach (var resourceType in sink.AcceptedResources)
            {
                int existingCount;
                if (!m_changedTypes.TryGetValue(resourceType, out existingCount))
                    m_changedTypes.Add(resourceType, 1);
                else
                    m_changedTypes[resourceType] = existingCount+1;
            }
	    }

	    public void AddSource(MyResourceSourceComponent source)
	    {
		    Debug.Assert(source != null);

            lock (m_sourcesToRemove)
            {
                if (m_sourcesToRemove.Contains(source))
                {
                    m_sourcesToRemove.Remove(source);
                    RemoveTypesFromChanges(source.ResourceTypes);
                    return;
                }
            }

            lock (m_sourcesToAdd)
            {
                m_sourcesToAdd.Add(source);
            }

            foreach (var resourceType in source.ResourceTypes)
            {
                int existingCount;
                if (!m_changedTypes.TryGetValue(resourceType, out existingCount))
                    m_changedTypes.Add(resourceType, 1);
                else
                    m_changedTypes[resourceType] = existingCount+1;
            }
	    }

	    public void RemoveSource(MyResourceSourceComponent source)
	    {
		    Debug.Assert(source != null);

            lock (m_sourcesToAdd)
            {
                if (m_sourcesToAdd.Contains(source))
                {
                    m_sourcesToAdd.Remove(source);
                    RemoveTypesFromChanges(source.ResourceTypes);
                    return;
                }
            }

            lock (m_sourcesToRemove)
            {
                m_sourcesToRemove.Add(source);
            }

            foreach (var resourceType in source.ResourceTypes)
            {
                int existingCount;
                if (!m_changedTypes.TryGetValue(resourceType, out existingCount))
                    m_changedTypes.Add(resourceType, 1);
                else
                    m_changedTypes[resourceType] = existingCount+1;
            }
	    }

        private void AddSinkLazy(MyResourceSinkComponent sink)
        {
            foreach (var acceptedResourceId in sink.AcceptedResources)
		    {
                MyDefinitionId typeId = acceptedResourceId;
				if(!m_initializedTypes.Contains(typeId))
					InitializeNewType(ref typeId);

				var sinksOfType = GetSinksOfType(ref typeId, sink.Group);
                if (sinksOfType == null)
                {
                    Debug.Fail("SinksOfType is null on add of " + typeId.ToString());
                    continue;
                }
				//Debug.Assert(MatchesAdaptability(sinksOfType, sink), "All sinks in the same group must have same adaptability.");
				Debug.Assert(!sinksOfType.Contains(sink));
			    int typeIndex = GetTypeIndex(ref typeId);

                MyResourceSourceComponent matchingSource = null;
		        if (sink.Container != null)
		        {
		            foreach (var sources in m_dataPerType[typeIndex].SourcesByPriority)
		            {
		                foreach (var source in sources)
		                {
		                    if (source.Container == null)
		                        continue;

                            var sinkInContainer = source.Container.Get<MyResourceSinkComponent>();
		                    if (sinkInContainer == sink)
		                    {
								m_dataPerType[typeIndex].InputOutputList.Add(MyTuple.Create(sink, source));
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

                if(matchingSource == null)
		            sinksOfType.Add(sink);

			    m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].GroupsDirty = true;
			    m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
		    }

		    sink.RequiredInputChanged += Sink_RequiredInputChanged;
		    sink.ResourceAvailable += Sink_IsResourceAvailable;
	        sink.OnAddType += Sink_OnAddType;
            sink.OnRemoveType += Sink_OnRemoveType;
        }

        private void RemoveSinkLazy(MyResourceSinkComponent sink, bool resetSinkInput = true)
        {
            foreach (var acceptedResourceId in sink.AcceptedResources)
		    {
                MyDefinitionId typeId = acceptedResourceId;
			    var sinksOfType = GetSinksOfType(ref typeId, sink.Group);
                if (sinksOfType == null)
                {
                    //Debug.Fail("SinksOfType is null on removal of " + typeId.ToString());
                    continue;
                }
			    int typeIndex = GetTypeIndex(ref typeId);

                if (!sinksOfType.Remove(sink))
                {
                    int foundIndex = -1;
                    for(int pairIndex = 0; pairIndex < m_dataPerType[typeIndex].InputOutputList.Count; ++pairIndex)
		            {
                        if (m_dataPerType[typeIndex].InputOutputList[pairIndex].Item1 != sink)
		                    continue;

		                foundIndex = pairIndex;
		                break;
		            }

                    if (foundIndex != -1)
                    {
                        var matchingSource = m_dataPerType[typeIndex].InputOutputList[foundIndex].Item2;
                        m_dataPerType[typeIndex].InputOutputList.RemoveAtFast(foundIndex);
                        m_dataPerType[typeIndex].SourcesByPriority[GetPriority(matchingSource)].Add(matchingSource);
                    }
                }

			    m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].GroupsDirty = true;
				m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
				if (resetSinkInput)
					sink.SetInputFromDistributor(typeId, 0.0f, IsAdaptible(sink), false);
		    }

            sink.OnRemoveType -= Sink_OnRemoveType;
	        sink.OnAddType -= Sink_OnAddType;
		    sink.RequiredInputChanged -= Sink_RequiredInputChanged;
		    sink.ResourceAvailable -= Sink_IsResourceAvailable;
        }

        private void AddSourceLazy(MyResourceSourceComponent source)
        {
            foreach (var resourceType in source.ResourceTypes)
		    {
                var typeId = resourceType;
				if(!m_initializedTypes.Contains(typeId))
					InitializeNewType(ref typeId);

			    var sourcesOfType = GetSourcesOfType(ref typeId, source.Group);
                if (sourcesOfType == null)
                {
                    Debug.Fail("SourcesOfType is null on add of " + typeId.ToString());
                    continue;
                }
			    int typeIndex = GetTypeIndex(ref typeId);
				Debug.Assert(!sourcesOfType.Contains(source));
				Debug.Assert(MatchesInfiniteCapacity(sourcesOfType, source), "All producers in the same group must have same 'infinite capacity' state.");

                MyResourceSinkComponent matchingSink = null;
                if (source.Container != null)
                {
                    foreach (var sinks in m_dataPerType[typeIndex].SinksByPriority)
                    {
                        foreach (var sink in sinks)
                        {
                            if (sink.Container == null)
                                continue;

                            var sourceInContainer = sink.Container.Get<MyResourceSourceComponent>();
                            if (sourceInContainer == source)
                            {
                                m_dataPerType[typeIndex].InputOutputList.Add(MyTuple.Create(sink, source));
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
                m_dataPerType[typeIndex].GroupsDirty = true;
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

        private void RemoveSourceLazy(MyResourceSourceComponent source)
        {
            foreach (var resourceTypeId in source.ResourceTypes)
		    {
                MyDefinitionId typeId = resourceTypeId;
			    var sourcesOfType = GetSourcesOfType(ref typeId, source.Group);
                if (sourcesOfType == null)
                {
                    Debug.Fail("SourcesOfType is null on removal of " + typeId.ToString());
                    continue;
                }
			    var typeIndex = GetTypeIndex(ref typeId);

                if (!sourcesOfType.Remove(source))
                {
                    int foundIndex = -1;
                    for (int pairIndex = 0; pairIndex < m_dataPerType[typeIndex].InputOutputList.Count; ++pairIndex)
                    {
                        if (m_dataPerType[typeIndex].InputOutputList[pairIndex].Item2 != source)
                            continue;

                        foundIndex = pairIndex;
                        break;
                    }

                    if (foundIndex != -1)
                    {
                        var matchingSink = m_dataPerType[typeIndex].InputOutputList[foundIndex].Item1;
                        m_dataPerType[typeIndex].InputOutputList.RemoveAtFast(foundIndex);
                        m_dataPerType[typeIndex].SinksByPriority[GetPriority(matchingSink)].Add(matchingSink);
                    }
                }

			    m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].GroupsDirty = true;

			    --m_dataPerType[typeIndex].SourceCount;
				if (m_dataPerType[typeIndex].SourceCount == 0)
				{
					m_dataPerType[typeIndex].SourcesEnabled= MyMultipleEnabledEnum.NoObjects;
				}
				else if (m_dataPerType[typeIndex].SourceCount== 1)
				{
				    var firstSourceOfType = GetFirstSourceOfType(ref typeId);
                    if (firstSourceOfType != null)
                        ChangeSourcesState(typeId, (firstSourceOfType.Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled, MySession.Static.LocalPlayerId);
                    else
                    {
                        // Bug Fix - When battery is on grid it somehow leaves Sources = 1 even tought that no sources are present
                        --m_dataPerType[typeIndex].SourceCount;
                        m_dataPerType[typeIndex].SourcesEnabled = MyMultipleEnabledEnum.NoObjects;
                    }
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
	        int typeIndex;
            if (!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
                return 0;

            int priorityIndex = m_sourceSubtypeToPriority[sourceGroupType];
            foreach(var pair in m_dataPerType[typeIndex].InputOutputList)
                if (pair.Item2.Group == sourceGroupType && pair.Item2.CurrentOutputByType(resourceTypeId) > 0) ++additionalCount;

            return m_dataPerType[typeIndex].SourceDataByPriority[priorityIndex].ActiveCount + additionalCount;
	    }

	    #endregion

	    public void UpdateBeforeSimulation()
	    {
            CheckDistributionSystemChanges();

		    foreach (var typeId in m_typeIdToIndex.Keys)
		    {
                MyDefinitionId localTypeId = typeId;
                int typeIndex = GetTypeIndex(ref localTypeId);
			    if (NeedsRecompute(ref localTypeId))
                    RecomputeResourceDistribution(ref localTypeId, false);
		    }

            foreach (var typeToRemove in m_typesToRemove)
            {
                var tmpType = typeToRemove;
                RemoveType(ref tmpType);
            }

		    if (ShowTrace)
			    UpdateTrace();
	    }

	    /// <summary>
	    /// Computes number of groups that have enough energy to work.
	    /// </summary>
	    public void UpdateHud(MyHudSinkGroupInfo info)
	    {
		    bool isWorking = true;
		    int workingGroupCount = 0;
		    int i = 0;
		    int typeIndex;
            if (!TryGetTypeIndex(ElectricityId, out typeIndex))
                return;
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
			MyDebug.AssertDebug(state != MyMultipleEnabledEnum.Mixed, "You must NOT use this property to set mixed state.");
			MyDebug.AssertDebug(state != MyMultipleEnabledEnum.NoObjects, "You must NOT use this property to set state without any objects.");
            int typeIndex;
            if (TryGetTypeIndex(resourceTypeId, out typeIndex) == false)
            {
                return;
            }
			// You cannot change the state when there are no objects.
			if (m_dataPerType[typeIndex].SourcesEnabled == state || m_dataPerType[typeIndex] .SourcesEnabled == MyMultipleEnabledEnum.NoObjects)
				return;

			m_dataPerType[typeIndex].SourcesEnabled = state;
			bool enabled = (state == MyMultipleEnabledEnum.AllEnabled);
            IMyFaction faction1 = MySession.Static.Factions.TryGetPlayerFaction(playerId);
			foreach (var group in m_dataPerType[typeIndex].SourcesByPriority)
			{
				foreach (var source in group)
				{
                    //Trash send playerId = -1
                    if (playerId >= 0 && source.Entity != null)
                    {
                        MyFunctionalBlock fb = source.Entity as MyFunctionalBlock;
                        if (fb != null && fb.OwnerId != 0)
                        {
                            IMyFaction faction2 = MySession.Static.Factions.TryGetPlayerFaction(fb.OwnerId);
                            if (faction2 != null && MySession.Static.Factions.GetRelationBetweenFactions(faction1 != null ? faction1.FactionId : 0, faction2.FactionId) == MyRelationsBetweenFactions.Enemies)
                                continue;
                        }
                    }
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
		}

	    #region Private methods

	    private float ComputeRemainingFuelTime(MyDefinitionId resourceTypeId)
	    {
		    ProfilerShort.Begin("MyResourceDistributor.ComputeRemainingFuelTime()");
		    try
		    {
			    var typeIndex = GetTypeIndex(ref resourceTypeId);
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
		        if (m_dataPerType[typeIndex].InputOutputData.Item1.RemainingAvailableResource > m_dataPerType[typeIndex].InputOutputData.Item1.RequiredInput)
		            powerInUse += m_dataPerType[typeIndex].InputOutputData.Item1.RequiredInput;
		        else
		            powerInUse += m_dataPerType[typeIndex].InputOutputData.Item1.RemainingAvailableResource;

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
					    powerInUse -= groupData.UsageRatio*groupData.MaxAvailableResource;
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

		        if (m_dataPerType[typeIndex].InputOutputData.Item2.UsageRatio > 0)
		        {
		            foreach (var sinkSourcePair in m_dataPerType[typeIndex].InputOutputList)
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
				    remainingFuelTime = remainingCapacity/powerInUse;

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
	        if (!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
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
	                typeData.NeedsRecompute = true;
	            }
	        }
	    }

        private void CheckDistributionSystemChanges()
        {
            if (m_sinksToRemove.Count > 0)
            {
                lock (m_sinksToRemove)
                {
                    foreach (var sinkResetPair in m_sinksToRemove)
                    {
                        RemoveSinkLazy(sinkResetPair.Item1, sinkResetPair.Item2);

                        foreach (var resourceType in sinkResetPair.Item1.AcceptedResources)
                            m_changedTypes[resourceType] = m_changedTypes[resourceType] - 1;
                    }
                    m_sinksToRemove.Clear();
                }
            }

            if (m_sourcesToRemove.Count > 0)
            {
                lock (m_sourcesToRemove)
                {
                    foreach (var sourceToRemove in m_sourcesToRemove)
                    {
                        RemoveSourceLazy(sourceToRemove);

                        foreach (var resourceType in sourceToRemove.ResourceTypes)
                            m_changedTypes[resourceType] = m_changedTypes[resourceType] - 1;
                    }
                    m_sourcesToRemove.Clear();
                }
            }

            if (m_sourcesToAdd.Count > 0)
            {
                lock (m_sourcesToAdd)
                {
                    foreach (var sourceToAdd in m_sourcesToAdd)
                    {
                        AddSourceLazy(sourceToAdd);

                        foreach (var resourceType in sourceToAdd.ResourceTypes)
                            m_changedTypes[resourceType] = m_changedTypes[resourceType] - 1;
                    }
                    m_sourcesToAdd.Clear();
                }
            }

            if (m_sinksToAdd.Count > 0)
            {
                lock (m_sinksToAdd)
                {
                    foreach (var sinkToAdd in m_sinksToAdd)
                    {
                        AddSinkLazy(sinkToAdd);

                        foreach (var resourceType in sinkToAdd.AcceptedResources)
                            m_changedTypes[resourceType] = m_changedTypes[resourceType] - 1;
                    }
                    m_sinksToAdd.Clear();
                }
            }
        }

	    private void RecomputeResourceDistribution(ref MyDefinitionId typeId, bool updateChanges = true)
	    {
		    ProfilerShort.Begin("MyResourceDistributor.RecomputeResourceDistribution");

            if(updateChanges)
                CheckDistributionSystemChanges();

		    int typeIndex = GetTypeIndex(ref typeId);

            if (m_dataPerType[typeIndex].SinksByPriority.Length == 0 &&
                m_dataPerType[typeIndex].SourcesByPriority.Length == 0 &&
                m_dataPerType[typeIndex].InputOutputList.Count == 0)
            {
                m_typesToRemove.Add(typeId);
                ProfilerShort.End();
                return;
            }

			if (!IsConveyorConnectionRequired(ref typeId))
		    {
                ProfilerShort.Begin("ComputeInitial");
			    ComputeInitialDistributionData(
					ref typeId,
					m_dataPerType[typeIndex].SinkDataByPriority,
					m_dataPerType[typeIndex].SourceDataByPriority,
                    ref m_dataPerType[typeIndex].InputOutputData,
					m_dataPerType[typeIndex].SinksByPriority,
					m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].InputOutputList,
					m_dataPerType[typeIndex].StockpilingStorageIndices,
					m_dataPerType[typeIndex].OtherStorageIndices,
					out m_dataPerType[typeIndex].MaxAvailableResource);

                ProfilerShort.BeginNextBlock("RecomputeDistribution");
				m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
					ref typeId, 
					0,
					m_dataPerType[typeIndex].SinkDataByPriority,
					m_dataPerType[typeIndex].SourceDataByPriority,
                    ref m_dataPerType[typeIndex].InputOutputData,
					m_dataPerType[typeIndex].SinksByPriority,
					m_dataPerType[typeIndex].SourcesByPriority, 
                    m_dataPerType[typeIndex].InputOutputList,
					m_dataPerType[typeIndex].StockpilingStorageIndices,
					m_dataPerType[typeIndex].OtherStorageIndices,
					m_dataPerType[typeIndex].MaxAvailableResource);
                ProfilerShort.End();
		    }
		    else
		    {
                ProfilerShort.Begin("RecreatePhysical " + typeId);
		        if (m_dataPerType[typeIndex].GroupsDirty)
		        {
                    m_dataPerType[typeIndex].GroupsDirty = false;
		            m_dataPerType[typeIndex].DistributionGroupsInUse = 0;
		            RecreatePhysicalDistributionGroups(ref typeId, m_dataPerType[typeIndex].SinksByPriority, m_dataPerType[typeIndex].SourcesByPriority, m_dataPerType[typeIndex].InputOutputList);
		        }

                m_dataPerType[typeIndex].MaxAvailableResource = 0;

                for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
			    {
                    MyPhysicalDistributionGroup group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];

                    ProfilerShort.BeginNextBlock("ComputeInitial");
					ComputeInitialDistributionData(
					ref typeId,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    ref group.InputOutputData,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairs,
                    group.StockpilingStorage,
                    group.OtherStorage,
                    out group.MaxAvailableResources);

                    ProfilerShort.BeginNextBlock("RecomputeDistribution");
                    group.ResourceState = RecomputeResourceDistributionPartial(
					ref typeId,
					0,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    ref group.InputOutputData,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairs,
                    group.StockpilingStorage,
                    group.OtherStorage,
                    group.MaxAvailableResources);

                    m_dataPerType[typeIndex].MaxAvailableResource += group.MaxAvailableResources;

                    m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;
			    }

                MyResourceStateEnum resultState;
                if (m_dataPerType[typeIndex].MaxAvailableResource == 0.0f)
                    resultState = MyResourceStateEnum.NoPower;
                else 
                {
                    resultState = MyResourceStateEnum.Ok;

                    for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
                    {
                        if ( m_dataPerType[typeIndex].DistributionGroups[groupIndex].ResourceState == MyResourceStateEnum.OverloadAdaptible)
                        {
                            resultState = MyResourceStateEnum.OverloadAdaptible;
                            break;
                        }
                        if ( m_dataPerType[typeIndex].DistributionGroups[groupIndex].ResourceState == MyResourceStateEnum.OverloadBlackout)
                        {
                            resultState = MyResourceStateEnum.OverloadAdaptible;
                            break;
                        }
                    }
                }

                m_dataPerType[typeIndex].ResourceState = resultState;

                ProfilerShort.End();

		    }

   		    m_dataPerType[typeIndex].NeedsRecompute = false;

		    ProfilerShort.End();
	    }

	    void RecreatePhysicalDistributionGroups(
            ref MyDefinitionId typeId,
            HashSet<MyResourceSinkComponent>[] allSinksByPriority,
            HashSet<MyResourceSourceComponent>[] allSourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> allSinkSources)
	    {
            foreach (var sinks in allSinksByPriority)
            {
                foreach (var sink in sinks)
                {
                    if (sink.Entity == null)
                    {
                        if (sink.TemporaryConnectedEntity == null)
                            continue;

                        SetEntityGroupForTempConnected(ref typeId, sink);
                        continue;
                    }

                    SetEntityGroup(ref typeId, sink.Entity);
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

                        SetEntityGroupForTempConnected(ref typeId, source);
                        continue;
                    }

                    SetEntityGroup(ref typeId, source.Entity);
                }
            }

            foreach (var sinkSourcePair in allSinkSources)
            {
                if (sinkSourcePair.Item1.Entity != null)
                    SetEntityGroup(ref typeId, sinkSourcePair.Item1.Entity);
            }
	    }

		void SetEntityGroup(ref MyDefinitionId typeId, IMyEntity entity)
		{
			var block = entity as IMyConveyorEndpointBlock;
			if (block == null)
				return;

		    int typeIndex = GetTypeIndex(ref typeId);
			bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
			{
                if (!MyGridConveyorSystem.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
					continue;

                var group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];
                group.Add(typeId, block);
                m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;
				found = true;
				break;
			}

			if (!found)
			{
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, block));
                else
                {
                    var group = m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1];
                    group.Init(typeId, block);
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1] = group;
                }
			}
		}

        void SetEntityGroupForTempConnected(ref MyDefinitionId typeId, MyResourceSinkComponent sink)
        {
            var block = sink.TemporaryConnectedEntity as IMyConveyorEndpointBlock;

            int typeIndex = GetTypeIndex(ref typeId);
            bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            {
                if (block == null || !MyGridConveyorSystem.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
                {
                    bool addToGroup = false;
                    if (block == null)
                    {
                        foreach (var sources in m_dataPerType[typeIndex].DistributionGroups[groupIndex].SourcesByPriority)
                        {
                            foreach(var groupSource in sources)
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
                    if(!addToGroup)
                        continue;
                }

                var group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];
                group.AddTempConnected(typeId, sink);
                m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;
                found = true;
                break;
            }

            if (!found)
            {
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, sink));
                else
                {
                    var group = m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1];
                    group.InitFromTempConnected(typeId, sink);
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1] = group;
                }
            }
        }

        void SetEntityGroupForTempConnected(ref MyDefinitionId typeId, MyResourceSourceComponent source)
        {
            var block = source.TemporaryConnectedEntity as IMyConveyorEndpointBlock;

            int typeIndex = GetTypeIndex(ref typeId);
            bool found = false;
            for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            {
                if (block == null || !MyGridConveyorSystem.Reachable(m_dataPerType[typeIndex].DistributionGroups[groupIndex].FirstEndpoint, block.ConveyorEndpoint))
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
                var group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];
                group.AddTempConnected(typeId, source);
                m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;
                found = true;
                break;
            }

            if (!found)
            {
                if (++m_dataPerType[typeIndex].DistributionGroupsInUse > m_dataPerType[typeIndex].DistributionGroups.Count)
                    m_dataPerType[typeIndex].DistributionGroups.Add(new MyPhysicalDistributionGroup(typeId, source));
                else
                {
                    var group = m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1];
                    group.InitFromTempConnected(typeId, source);
                    m_dataPerType[typeIndex].DistributionGroups[m_dataPerType[typeIndex].DistributionGroupsInUse - 1] = group;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="sinkDataByPriority"></param>
        /// <param name="sourceDataByPriority"></param>
        /// <param name="sinkSourceData"></param>
        /// <param name="sinksByPriority"></param>
        /// <param name="sourcesByPriority"></param>
        /// <param name="sinkSourcePairs"></param>
        /// <param name="stockpilingStorageList">Indices into sinkSourcePairs</param>
        /// <param name="otherStorageList">Indices into sinkSourcePairs</param>
        /// <param name="maxAvailableResource"></param>
		private static void ComputeInitialDistributionData(
			ref MyDefinitionId typeId,
			MySinkGroupData[] sinkDataByPriority,
			MySourceGroupData[] sourceDataByPriority,
            ref MyTuple<MySinkGroupData, MySourceGroupData> sinkSourceData,
            HashSet<MyResourceSinkComponent>[] sinksByPriority,
			HashSet<MyResourceSourceComponent>[] sourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> sinkSourcePairs,
			List<int> stockpilingStorageList,
			List<int> otherStorageList,
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

            PrepareSinkSourceData(ref typeId, ref sinkSourceData, sinkSourcePairs, stockpilingStorageList, otherStorageList);
            maxAvailableResource += sinkSourceData.Item2.MaxAvailableResource;
		}

		private static void PrepareSinkSourceData(
			ref MyDefinitionId typeId,
			ref MyTuple<MySinkGroupData, MySourceGroupData> sinkSourceData,
			List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> sinkSourcePairs,
			List<int> stockpilingStorageList,
			List<int> otherStorageList)
		{
			stockpilingStorageList.Clear();
			otherStorageList.Clear();
			sinkSourceData.Item1.IsAdaptible = true;
			sinkSourceData.Item1.RequiredInput = 0f;
			sinkSourceData.Item1.RequiredInputCumulative = 0f;
			sinkSourceData.Item2.MaxAvailableResource = 0f;
            sinkSourceData.Item2.UsageRatio = 0f;
			for (int pairIndex = 0; pairIndex < sinkSourcePairs.Count; ++pairIndex)
			{
				var sinkSourcePair = sinkSourcePairs[pairIndex];
				bool productionEnabled = sinkSourcePair.Item2.ProductionEnabledByType(typeId);
				bool isStockpiling = sinkSourcePair.Item2.Enabled && !productionEnabled && sinkSourcePair.Item1.RequiredInputByType(typeId) > 0;
				sinkSourceData.Item1.IsAdaptible = sinkSourceData.Item1.IsAdaptible && IsAdaptible(sinkSourcePair.Item1);
				sinkSourceData.Item1.RequiredInput += sinkSourcePair.Item1.RequiredInputByType(typeId);
				if (isStockpiling)
					sinkSourceData.Item1.RequiredInputCumulative += sinkSourcePair.Item1.RequiredInputByType(typeId);

				sinkSourceData.Item2.InfiniteCapacity = float.IsInfinity(sinkSourcePair.Item2.RemainingCapacityByType(typeId));
				if (isStockpiling)
					stockpilingStorageList.Add(pairIndex);
				else
				{
					otherStorageList.Add(pairIndex);
					if (sinkSourcePair.Item2.Enabled && productionEnabled)
						sinkSourceData.Item2.MaxAvailableResource += sinkSourcePair.Item2.MaxOutputByType(typeId);
				}
			}
		}

	    /// <summary>
	    /// Recomputes power distribution in subset of all priority groups (in range
	    /// from startPriorityIdx until the end). Passing index 0 recomputes all priority groups.
	    /// </summary>
	    private static MyResourceStateEnum RecomputeResourceDistributionPartial(
			ref MyDefinitionId typeId,
			int startPriorityIdx, 
			MySinkGroupData[] sinkDataByPriority,
			MySourceGroupData[] sourceDataByPriority,
            ref MyTuple<MySinkGroupData, MySourceGroupData> sinkSourceData,
            HashSet<MyResourceSinkComponent>[] sinksByPriority,
			HashSet<MyResourceSourceComponent>[] sourcesByPriority,
            List<MyTuple<MyResourceSinkComponent, MyResourceSourceComponent>> sinkSourcePairs,
			List<int> stockpilingStorageList,
			List<int> otherStorageList,
			float availableResource)
	    {
            ProfilerShort.Begin("MyResourceDistributor.RecomputeResourceDistributionPartial");
		    ProfilerShort.Begin("Non-zero inputs");
		    float totalAvailableResource = availableResource;
		    int sinkPriorityIndex = startPriorityIdx;

            // Distribute power over the sinks by priority
			for (; sinkPriorityIndex < sinksByPriority.Length; ++sinkPriorityIndex)
		    {
				sinkDataByPriority[sinkPriorityIndex].RemainingAvailableResource = availableResource;

				if (sinkDataByPriority[sinkPriorityIndex].RequiredInput <= availableResource)
			    {
				    // Run everything in the group at max.
					availableResource -= sinkDataByPriority[sinkPriorityIndex].RequiredInput;
                    //ToArray = Hotfix for collection modified exception
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex].ToArray())
						sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), sinkDataByPriority[sinkPriorityIndex].IsAdaptible);
			    }
				else if (sinkDataByPriority[sinkPriorityIndex].IsAdaptible && availableResource > 0.0f)
			    {
				    // Distribute power in this group based on ratio of its requirement vs. group requirement.
                    //ToArray = Hotfix for collection modified exception
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex].ToArray())
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
                    //ToArray = Hotfix for collection modified exception
                    foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex].ToArray())
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
                //ToArray = Hotfix for collection modified exception
                foreach (MyResourceSinkComponent sink in sinksByPriority[sinkPriorityIndex].ToArray())
					sink.SetInputFromDistributor(typeId, 0.0f, sinkDataByPriority[sinkPriorityIndex].IsAdaptible);
		    }
            ProfilerShort.End();
            float consumptionForNonStorage = totalAvailableResource - availableResource + (startPriorityIdx != 0 ? sinkDataByPriority[0].RemainingAvailableResource - sinkDataByPriority[startPriorityIdx].RemainingAvailableResource : 0f);

            // Distribute remaining energy over stockpiling storage
	        float totalAvailableResourcesForStockpiles = Math.Max(totalAvailableResource - consumptionForNonStorage, 0);
	        float availableResourcesForStockpiles = totalAvailableResourcesForStockpiles;
            if (stockpilingStorageList.Count > 0)
            {
                ProfilerShort.Begin("Stockpiles");
				float stockpilesRequiredInput = sinkSourceData.Item1.RequiredInputCumulative;
				if (stockpilesRequiredInput <= availableResourcesForStockpiles)
                {
					availableResourcesForStockpiles -= stockpilesRequiredInput;
                    //ToArray = Hotfix for collection modified exception
                    foreach (int pairIndex in stockpilingStorageList.ToArray())
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), true);
                    }
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStockpiles;
                }
                else
                {
                    //ToArray = Hotfix for collection modified exception
                    foreach (int pairIndex in stockpilingStorageList.ToArray())
                    {
                        var sink = sinkSourcePairs[pairIndex].Item1;
						float ratio = sink.RequiredInputByType(typeId) / stockpilesRequiredInput;
                        sink.SetInputFromDistributor(typeId, ratio * totalAvailableResourcesForStockpiles, true);
                    }
                    availableResourcesForStockpiles = 0f;
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStockpiles;
                }
                ProfilerShort.End();
            }

            // Distribute remaining power over non-stockpiling storage
	        float consumptionForStockpiles = totalAvailableResourcesForStockpiles - availableResourcesForStockpiles;
            float totalAvailableResourcesForStorage = Math.Max(totalAvailableResource - (sinkSourceData.Item2.MaxAvailableResource - sinkSourceData.Item2.MaxAvailableResource * sinkSourceData.Item2.UsageRatio) - consumptionForNonStorage - consumptionForStockpiles, 0);
            float availableResourcesForStorage = totalAvailableResourcesForStorage;
            if (otherStorageList.Count > 0)
            {
                ProfilerShort.Begin("Non-stockpiling storage");
				float ordinaryStorageRequiredInput = sinkSourceData.Item1.RequiredInput - sinkSourceData.Item1.RequiredInputCumulative;
				if (ordinaryStorageRequiredInput <= availableResourcesForStorage)
                {
					availableResourcesForStorage -= ordinaryStorageRequiredInput;
                    for (int i = 0; i<otherStorageList.Count; i++)
                    {
                        int pairIndex = otherStorageList[i];
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        sink.SetInputFromDistributor(typeId, sink.RequiredInputByType(typeId), true);
                    }
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStorage;
                }
                else
                {
                    for (int i = 0; i < otherStorageList.Count; i++)
                    {
                        int pairIndex = otherStorageList[i];
                        var sink = sinkSourcePairs[pairIndex].Item1;
                        float ratio = sink.RequiredInputByType(typeId) / ordinaryStorageRequiredInput;
                        sink.SetInputFromDistributor(typeId, ratio * availableResourcesForStorage, true);
                    }
                    availableResourcesForStorage = 0f;
                    sinkSourceData.Item1.RemainingAvailableResource = availableResourcesForStorage;
                }
                ProfilerShort.End();
            }
            
            ProfilerShort.Begin("Sources");
            float consumptionForStorage = totalAvailableResourcesForStorage - availableResourcesForStorage;
	        float consumptionForNonStorageAndStockpiles = consumptionForStockpiles + consumptionForNonStorage;
	        if (sinkSourceData.Item2.MaxAvailableResource > 0)
	        {
                float amountToSupply = consumptionForNonStorageAndStockpiles;
	            sinkSourceData.Item2.UsageRatio = Math.Min(1f, amountToSupply/sinkSourceData.Item2.MaxAvailableResource);
                consumptionForNonStorageAndStockpiles -= Math.Min(amountToSupply, sinkSourceData.Item2.MaxAvailableResource);
	        }
	        else
	            sinkSourceData.Item2.UsageRatio = 0f;

            sinkSourceData.Item2.ActiveCount = 0;
            for (int i = 0; i < otherStorageList.Count; i++)
            {
                int pairIndex = otherStorageList[i];
                var source = sinkSourcePairs[pairIndex].Item2;
                if (!source.Enabled || !source.ProductionEnabledByType(typeId) || !source.HasCapacityRemainingByType(typeId))
                    continue;

                ++sinkSourceData.Item2.ActiveCount;
                ProfilerShort.Begin("Set CurrentOutput");
                source.SetOutputByType(typeId, sinkSourceData.Item2.UsageRatio * source.MaxOutputByType(typeId));
                ProfilerShort.End();
            }

	        int sourcePriorityIndex = 0;
            float totalRemainingConsumption = consumptionForNonStorageAndStockpiles + consumptionForStorage;
			for (; sourcePriorityIndex < sourcesByPriority.Length; ++sourcePriorityIndex)
		    {
			    if (sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource > 0f)
			    {
                    float amountToSupply = Math.Max(totalRemainingConsumption, 0f);
				    sourceDataByPriority[sourcePriorityIndex].UsageRatio = Math.Min(1f, amountToSupply/sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource);
                    totalRemainingConsumption -= Math.Min(amountToSupply, sourceDataByPriority[sourcePriorityIndex].MaxAvailableResource);
			    }
			    else
				    sourceDataByPriority[sourcePriorityIndex].UsageRatio = 0f;

			    sourceDataByPriority[sourcePriorityIndex].ActiveCount = 0;
                //ToArray = Hotfix for collection modified exception
			    foreach (MyResourceSourceComponent source in sourcesByPriority[sourcePriorityIndex].ToArray())
			    {
				    if (!source.Enabled || !source.HasCapacityRemainingByType(typeId))
					    continue;

				    ++sourceDataByPriority[sourcePriorityIndex].ActiveCount;
				    ProfilerShort.Begin("Set CurrentOutput");
                    source.SetOutputByType(typeId, sourceDataByPriority[sourcePriorityIndex].UsageRatio * source.MaxOutputByType(typeId));
				    ProfilerShort.End();
			    }
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
                for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
                {
                    MyPhysicalDistributionGroup group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];

                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ".MaxAvailableResources", group.MaxAvailableResources);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ".ResourceState", group.ResourceState);

                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item1.IsAdaptible", group.InputOutputData.Item1.IsAdaptible);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item1.RequiredInput", group.InputOutputData.Item1.RequiredInput);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item1.RequiredInputCumulative", group.InputOutputData.Item1.RequiredInputCumulative);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item1.RemainingAvailableResource", group.InputOutputData.Item1.RemainingAvailableResource);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item2.MaxAvailableResource", group.InputOutputData.Item2.MaxAvailableResource);
                    MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + "Item2.UsageRatio", group.InputOutputData.Item2.UsageRatio);

                    for (int ss = 0; ss < group.SinkSourcePairs.Count; ss++)
                    {
                        MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ss + "Sink(" + group.SinkSourcePairs[ss].Item1.Entity.ToString() + ").CurrentInput", group.SinkSourcePairs[ss].Item1.CurrentInput);
                        MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ss + "Source(" + group.SinkSourcePairs[ss].Item2.Entity.ToString() + ").CurrentOutput", group.SinkSourcePairs[ss].Item2.CurrentOutput);
                        MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ss + "Source(" + group.SinkSourcePairs[ss].Item2.Entity.ToString() + ").UsedCapacity", group.SinkSourcePairs[ss].Item2.RemainingCapacity);
                        MyTrace.Watch(DebugName + "." + m_dataPerType[typeIndex].TypeId.SubtypeName + groupIndex + ss + "Source(" + group.SinkSourcePairs[ss].Item2.Entity.ToString() + ").MaxOutput", group.SinkSourcePairs[ss].Item2.MaxOutput);
                    }
                    
                    

                    
                }
            }


            //for (int typeIndex1 = 2; typeIndex1 < m_typeGroupCount; ++typeIndex1)
            //{
            //    if (m_dataPerType[typeIndex1].SinkDataByPriority != null)
            //    {
            //        for (int i = 0; i < m_dataPerType[typeIndex1].SinkDataByPriority.Length; ++i)
            //        {
            //            var data = m_dataPerType[typeIndex1].SinkDataByPriority[i];
            //            MyTrace.Watch(String.Format("Data[{0}][{1}].RemainingAvailableResource", m_dataPerType[typeIndex1].TypeId.SubtypeName, i), data.RemainingAvailableResource);
            //        }
            //        for (int i = 0; i < m_dataPerType[typeIndex1].SinkDataByPriority.Length; ++i)
            //        {
            //            var data = m_dataPerType[typeIndex1].SinkDataByPriority[i];
            //            MyTrace.Watch(String.Format("Data[{0}][{1}].RequiredInput", m_dataPerType[typeIndex1].TypeId.SubtypeName, i), data.RequiredInput);
            //        }
            //        for (int i = 0; i < m_dataPerType[typeIndex1].SinkDataByPriority.Length; ++i)
            //        {
            //            var data = m_dataPerType[typeIndex1].SinkDataByPriority[i];
            //            MyTrace.Watch(String.Format("Data[{0}][{1}].IsAdaptible", m_dataPerType[typeIndex1].TypeId.SubtypeName, i), data.IsAdaptible);
            //        }
            //    }

            //    int j = 0;
            //    foreach (var group in m_dataPerType[typeIndex].SourcesByPriority)
            //        foreach (var producer in group)
            //        {
            //            ++j;
            //            MyTrace.Watch(String.Format("Producer[{0}][{1}].IsTurnedOn", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.Enabled);
            //            MyTrace.Watch(String.Format("Producer[{0}][{1}].HasRemainingCapacity", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.HasCapacityRemaining);
            //            MyTrace.Watch(String.Format("Producer[{0}][{1}].CurrentOutput", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.CurrentOutput);
            //        }
         //   }


            //for (int typeIndex = 0; typeIndex < m_typeGroupCount; ++typeIndex)
            //{
            //    if (m_dataPerType[typeIndex].SinksByPriority != null)
            //    {
            //        for (int i = 0; i < m_dataPerType[typeIndex].SinksByPriority.Length; ++i)
            //        {
            //            var sinkGroup = m_dataPerType[typeIndex].SinksByPriority[i];

            //            int sg = 0;
            //            foreach (var component in sinkGroup)
            //            {
            //                if (component.Entity != null && !string.IsNullOrEmpty(((VRage.Game.Entity.MyEntity)component.Entity).DisplayNameText) && ((VRage.Game.Entity.MyEntity)component.Entity).DisplayNameText.Contains("Tank"))
            //                {
            //                    MyTrace.Watch(String.Format("SinksByPriority[{0}][{1}].CurrentInput", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString + sg.ToString()), component.CurrentInputByType(m_dataPerType[typeIndex].TypeId));
            //                    MyTrace.Watch(String.Format("SinksByPriority[{0}][{1}].RequiredInput", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString + sg.ToString()), component.RequiredInputByType(m_dataPerType[typeIndex].TypeId));
            //                    MyTrace.Watch(String.Format("SinksByPriority[{0}][{1}].SuppliedRatio", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString + sg.ToString()), component.SuppliedRatioByType(m_dataPerType[typeIndex].TypeId));
            //                }
            //                sg++;
            //            }
            //        }
            //    }
            //    if (m_dataPerType[typeIndex].SourcesByPriority != null)
            //    {
            //        for (int i = 0; i < m_dataPerType[typeIndex].SourcesByPriority.Length; ++i)
            //        {
            //            var sourceGroup = m_dataPerType[typeIndex].SourcesByPriority[i];
            //            foreach (var component in sourceGroup)
            //            {
            //                if (component.Entity != null && !string.IsNullOrEmpty(((VRage.Game.Entity.MyEntity)component.Entity).DisplayNameText) && ((VRage.Game.Entity.MyEntity)component.Entity).DisplayNameText.Contains("Tank"))
            //                {
            //                    MyTrace.Watch(String.Format("SourcesByPriority[{0}][{1}].CurrentOutput", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString), component.CurrentOutputByType(m_dataPerType[typeIndex].TypeId));
            //                    MyTrace.Watch(String.Format("SourcesByPriority[{0}][{1}].MaxOutput", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString), component.MaxOutputByType(m_dataPerType[typeIndex].TypeId));
            //                    MyTrace.Watch(String.Format("SourcesByPriority[{0}][{1}].RemainingCapacity", m_dataPerType[typeIndex].TypeId.SubtypeName, component.ComponentTypeDebugString), component.RemainingCapacityByType(m_dataPerType[typeIndex].TypeId));
            //                }
            //            }
            //        }
            //    }

            //    if (m_dataPerType[typeIndex].InputOutputList != null)
            //    {
            //        for (int i = 0; i < m_dataPerType[typeIndex].InputOutputList.Count; ++i)
            //        {
            //            var tuple = m_dataPerType[typeIndex].InputOutputList[i];

            //            MyTrace.Watch(String.Format("InputOutputList[{0}][{1}].CurrentInput", m_dataPerType[typeIndex].TypeId.SubtypeName, tuple.Item1.ComponentTypeDebugString + i.ToString()), tuple.Item1.CurrentInputByType(m_dataPerType[typeIndex].TypeId));
            //            MyTrace.Watch(String.Format("InputOutputList[{0}][{1}].CurrentOutput", m_dataPerType[typeIndex].TypeId.SubtypeName, tuple.Item1.ComponentTypeDebugString + i.ToString()), tuple.Item2.CurrentOutputByType(m_dataPerType[typeIndex].TypeId));
                             
            //        }
            //    }


                //int j = 0;
                //foreach (var group in m_dataPerType[typeIndex].SourcesByPriority)
                //    foreach (var producer in group)
                //    {
                //        ++j;
                //        MyTrace.Watch(String.Format("Producer[{0}][{1}].Enabled", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.Enabled);
                //        MyTrace.Watch(String.Format("Producer[{0}][{1}].HasRemainingCapacity", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.HasCapacityRemaining);
                //        MyTrace.Watch(String.Format("Producer[{0}][{1}].CurrentOutput", m_dataPerType[typeIndex].TypeId.SubtypeName, j), producer.CurrentOutput);
                //    }
           // }
	    }

	    private HashSet<MyResourceSinkComponent> GetSinksOfType(ref MyDefinitionId typeId, MyStringHash groupType)
	    {
            int typeIndex;
            int priorityIndex;
            if (!TryGetTypeIndex(typeId, out typeIndex) || !m_sinkSubtypeToPriority.TryGetValue(groupType, out priorityIndex))
                return null;

			return m_dataPerType[typeIndex].SinksByPriority[priorityIndex];
	    }

	    private HashSet<MyResourceSourceComponent> GetSourcesOfType(ref MyDefinitionId typeId, MyStringHash groupType)
	    {
            int typeIndex, priorityIndex;
            if (!TryGetTypeIndex(typeId, out typeIndex) || !m_sourceSubtypeToPriority.TryGetValue(groupType, out priorityIndex))
                return null;

		    return m_dataPerType[typeIndex].SourcesByPriority[priorityIndex];
	    }

	    private MyResourceSourceComponent GetFirstSourceOfType(ref MyDefinitionId resourceTypeId)
	    {
		    var typeIndex = GetTypeIndex(ref resourceTypeId);
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
            if(!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
                return MyMultipleEnabledEnum.NoObjects;

			if (m_dataPerType[typeIndex].SourcesEnabledDirty)
				RefreshSourcesEnabled(resourceTypeId);

			return m_dataPerType[typeIndex].SourcesEnabled;
		}

		public float RemainingFuelTimeByType(MyDefinitionId resourceTypeId)
		{
			int typeIndex;
		    if (!TryGetTypeIndex(ref resourceTypeId, out typeIndex))
		        return 0f;

			if (!m_dataPerType[typeIndex].RemainingFuelTimeDirty)
				return m_dataPerType[typeIndex].RemainingFuelTime;

			m_dataPerType[typeIndex].RemainingFuelTime = ComputeRemainingFuelTime(resourceTypeId);
			return m_dataPerType[typeIndex].RemainingFuelTime;
		}

        private bool NeedsRecompute(ref MyDefinitionId typeId)
        {
            int typeIndex;
            int changeTypeCounter;
            return  (m_changedTypes.TryGetValue(typeId, out changeTypeCounter) && changeTypeCounter > 0) ||
                    (TryGetTypeIndex(ref typeId, out typeIndex) && m_dataPerType[typeIndex].NeedsRecompute);
        }

		public MyResourceStateEnum ResourceStateByType(MyDefinitionId typeId)
		{
			var typeIndex = GetTypeIndex(ref typeId);
			if (NeedsRecompute(ref typeId))
				RecomputeResourceDistribution(ref typeId);

			return m_dataPerType[typeIndex].ResourceState;
		}

        private bool TryGetTypeIndex(MyDefinitionId typeId, out int typeIndex)
        {
            return TryGetTypeIndex(ref typeId, out typeIndex);
        }

	    private bool TryGetTypeIndex(ref MyDefinitionId typeId, out int typeIndex)
	    {
	        typeIndex = 0;
	        if (m_typeGroupCount == 0)
	            return false;
            else if (m_typeGroupCount > 1)
                return m_typeIdToIndex.TryGetValue(typeId, out typeIndex);

	        return true;
	    }

		private int GetTypeIndex(ref MyDefinitionId typeId)
		{
			var typeIndex = 0;
			if (m_typeGroupCount > 1)
				typeIndex = m_typeIdToIndex[typeId];
			return typeIndex;
		}

		private static int GetTypeIndexTotal(ref MyDefinitionId typeId)
		{
			var typeIndex = 0;
			if (m_typeGroupCountTotal > 1)
				typeIndex = m_typeIdToIndexTotal[typeId];
			return typeIndex;
		}

        public static bool IsConveyorConnectionRequiredTotal(MyDefinitionId typeId)
        {
            return IsConveyorConnectionRequiredTotal(ref typeId);
        }

		public static bool IsConveyorConnectionRequiredTotal(ref MyDefinitionId typeId)
		{
			return m_typeIdToConveyorConnectionRequiredTotal[typeId];
		}

		private bool IsConveyorConnectionRequired(ref MyDefinitionId typeId)
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

		private static bool IsAdaptible(MyResourceSinkComponent sink)
		{
			return m_sinkSubtypeToAdaptability[sink.Group];
		}

        private void RemoveType(ref MyDefinitionId typeId)
        {
            int typeIndex;
            if (!TryGetTypeIndex(ref typeId, out typeIndex))
                return;


            m_dataPerType.RemoveAt(typeIndex);
            m_initializedTypes.Remove(typeId);
            --m_typeGroupCount;
            m_typeIdToIndex.Remove(typeId);
            m_typeIdToConveyorConnectionRequired.Remove(typeId);
            return;
        }

	    #endregion

	    #region Event handlers

        private void Sink_OnAddType(MyResourceSinkComponent sink, MyDefinitionId resourceType)
	    {
	        RemoveSinkLazy(sink, false);
            CheckDistributionSystemChanges();
            AddSinkLazy(sink);
	    }

        private void Sink_OnRemoveType(MyResourceSinkComponent sink, MyDefinitionId resourceType)
        {
            RemoveSinkLazy(sink, false);
            CheckDistributionSystemChanges();
            AddSinkLazy(sink);
        }

	    private void Sink_RequiredInputChanged(MyDefinitionId changedResourceTypeId, MyResourceSinkComponent changedSink, float oldRequirement, float newRequirement)
	    {
            if (m_typeIdToIndex.ContainsKey(changedResourceTypeId) == false || m_sinkSubtypeToPriority.ContainsKey(changedSink.Group) == false)
            {
                return;
            }

			var typeIndex = GetTypeIndex(ref changedResourceTypeId);
	        if (!TryGetTypeIndex(changedResourceTypeId, out typeIndex))
	        {
	            Debug.Fail("Sink required input changed but missing the resource id.");
	            return;
	        }

            m_dataPerType[typeIndex].NeedsRecompute = true;

            if (NeedsRecompute(ref changedResourceTypeId))  //because of jetpack
            {
                RecomputeResourceDistribution(ref changedResourceTypeId);
                return;
            }

            //int groupId = GetPriority(changedSink);

            //if (!IsConveyorConnectionRequired(ref changedResourceTypeId))
            //{
            //    // Go over all priorities, starting from the changedSink.
            //    MyDebug.AssertDebug(m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput >= 0.0f);
            //    m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput = 0.0f;
            //    foreach (MyResourceSinkComponent sink in m_dataPerType[typeIndex].SinksByPriority[groupId])
            //        m_dataPerType[typeIndex].SinkDataByPriority[groupId].RequiredInput += sink.RequiredInputByType(changedResourceTypeId);

            //    // Update cumulative requirements.
            //    float cumulative = (groupId != 0) ? m_dataPerType[typeIndex].SinkDataByPriority[groupId - 1].RequiredInputCumulative : 0.0f;
            //    for (int index = groupId; index < m_dataPerType[typeIndex].SinkDataByPriority.Length; ++index)
            //    {
            //        cumulative += m_dataPerType[typeIndex].SinkDataByPriority[index].RequiredInput;
            //        m_dataPerType[typeIndex].SinkDataByPriority[index].RequiredInputCumulative = cumulative;
            //    }

            //    PrepareSinkSourceData(
            //        ref changedResourceTypeId,
            //        ref m_dataPerType[typeIndex].InputOutputData,
            //        m_dataPerType[typeIndex].InputOutputList,
            //        m_dataPerType[typeIndex].StockpilingStorageIndices,
            //        m_dataPerType[typeIndex].OtherStorageIndices);

            //    m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
            //        ref changedResourceTypeId,
            //        groupId,
            //        m_dataPerType[typeIndex].SinkDataByPriority,
            //        m_dataPerType[typeIndex].SourceDataByPriority,
            //        ref m_dataPerType[typeIndex].InputOutputData,
            //        m_dataPerType[typeIndex].SinksByPriority,
            //        m_dataPerType[typeIndex].SourcesByPriority,
            //        m_dataPerType[typeIndex].InputOutputList,
            //        m_dataPerType[typeIndex].StockpilingStorageIndices,
            //        m_dataPerType[typeIndex].OtherStorageIndices,
            //        m_dataPerType[typeIndex].SinkDataByPriority[groupId].RemainingAvailableResource);
            //}
            //else
            //{
            //    for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
            //    {
            //        if (!m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinksByPriority[groupId].Contains(changedSink) && m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinkSourcePairs.TrueForAll((pair) => pair.Item1 != changedSink))
            //            continue;

            //        var group = m_dataPerType[typeIndex].DistributionGroups[groupIndex];

            //        MyDebug.AssertDebug(group.SinkDataByPriority[groupId].RequiredInput >= 0.0f);
            //        group.SinkDataByPriority[groupId].RequiredInput = 0.0f;
            //        foreach (MyResourceSinkComponent sink in group.SinksByPriority[groupId])
            //            group.SinkDataByPriority[groupId].RequiredInput += sink.RequiredInputByType(changedResourceTypeId);

            //        float cumulative = (groupId != 0) ? group.SinkDataByPriority[groupId - 1].RequiredInputCumulative : 0.0f;
            //        for (int index = groupId; index < group.SinkDataByPriority.Length; ++index)
            //        {
            //            cumulative += group.SinkDataByPriority[index].RequiredInput;
            //            group.SinkDataByPriority[index].RequiredInputCumulative = cumulative;
            //        }

            //        PrepareSinkSourceData(
            //        ref changedResourceTypeId,
            //        ref group.InputOutputData,
            //        group.SinkSourcePairs,
            //        group.StockpilingStorage,
            //        group.OtherStorage);

            //        group.ResourceState = RecomputeResourceDistributionPartial(
            //        ref changedResourceTypeId,
            //        groupId,
            //        group.SinkDataByPriority,
            //        group.SourceDataByPriority,
            //        ref group.InputOutputData,
            //        group.SinksByPriority,
            //        group.SourcesByPriority,
            //        group.SinkSourcePairs,
            //        group.StockpilingStorage,
            //        group.OtherStorage,
            //        group.MaxAvailableResources);

            //        m_dataPerType[typeIndex].DistributionGroups[groupIndex] = group;

            //        break;
            //    }
            //}
	    }

	    private float Sink_IsResourceAvailable(MyDefinitionId resourceTypeId, MyResourceSinkComponent receiver)
	    {
		    var typeIndex = GetTypeIndex(ref resourceTypeId);
            int priorityIndex = GetPriority(receiver);

            if (IsConveyorConnectionRequired(ref resourceTypeId))
            {
                var receiverEndpoint = receiver.Entity as IMyConveyorEndpointBlock;
                if(receiverEndpoint == null)
                    return 0f;
                var conveyorEndpoint = receiverEndpoint.ConveyorEndpoint;

                int groupIndex;
                for(groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
                {
                    if(m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinksByPriority[priorityIndex].Contains(receiver))
                        break;
                }
                if (groupIndex == m_dataPerType[typeIndex].DistributionGroupsInUse)
                    return 0f;

                return m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinkDataByPriority[priorityIndex].RemainingAvailableResource
                        //+ m_dataPerType[typeIndex].DistributionGroups[groupIndex].InputOutputData.Item1.RemainingAvailableResource
                        - m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinkDataByPriority[priorityIndex].RequiredInput;
                        //- m_dataPerType[typeIndex].DistributionGroups[groupIndex].InputOutputData.Item1.RequiredInput;
            }
            else
            {
                float availableForSink = m_dataPerType[typeIndex].SinkDataByPriority[priorityIndex].RemainingAvailableResource;
                //float availableForStorage = m_dataPerType[typeIndex].InputOutputData.Item1.RemainingAvailableResource;

                float requiredInputForSink = m_dataPerType[typeIndex].SinkDataByPriority[priorityIndex].RequiredInput;
                //float requiredInputForStorage = m_dataPerType[typeIndex].InputOutputData.Item1.RequiredInput;

                float totalAvailable = availableForSink /*+ availableForStorage*/ - requiredInputForSink;// -requiredInputForStorage;

                return totalAvailable;
            }
	    }

	    private void source_HasRemainingCapacityChanged(MyDefinitionId changedResourceTypeId, MyResourceSourceComponent source)
	    {
		    int typeIndex = GetTypeIndex(ref changedResourceTypeId);
			m_dataPerType[typeIndex].NeedsRecompute = true;
			m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
	    }

        public void ConveyorSystem_OnPoweredChanged()
        {
            
            for (int typeIndex = 0; typeIndex < m_dataPerType.Count; ++typeIndex)
            {
            //    m_dataPerType[typeIndex].DistributionGroupsInUse = 0;
            //    RecreatePhysicalDistributionGroups(typeId, m_dataPerType[typeIndex].SinksByPriority, m_dataPerType[typeIndex].SourcesByPriority, m_dataPerType[typeIndex].InputOutputList);
            //    m_dataPerType[typeIndex].GroupsDirty = false;
                m_dataPerType[typeIndex].GroupsDirty = true;
                m_dataPerType[typeIndex].NeedsRecompute = true;
                m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
                m_dataPerType[typeIndex].SourcesEnabledDirty = true;
            }
        }

	    private void source_MaxOutputChanged(MyDefinitionId changedResourceTypeId, float oldOutput, MyResourceSourceComponent obj)
	    {
			int typeIndex = GetTypeIndex(ref changedResourceTypeId);
		    m_dataPerType[typeIndex].NeedsRecompute = true;
		    m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
		    m_dataPerType[typeIndex].SourcesEnabledDirty = true;

		    if (m_dataPerType[typeIndex].SourceCount == 1)
			    RecomputeResourceDistribution(ref changedResourceTypeId);
	    }

        private void source_ProductionEnabledChanged(MyDefinitionId changedResourceTypeId, MyResourceSourceComponent obj)
        {
            int typeIndex = GetTypeIndex(ref changedResourceTypeId);
            m_dataPerType[typeIndex].NeedsRecompute = true;
            m_dataPerType[typeIndex].RemainingFuelTimeDirty = true;
            m_dataPerType[typeIndex].SourcesEnabledDirty = true;

            if (m_dataPerType[typeIndex].SourceCount == 1)
                RecomputeResourceDistribution(ref changedResourceTypeId);
        }

        public float MaxAvailableResourceByType(MyDefinitionId resourceTypeId)
        {
            int typeIndex;
            return TryGetTypeIndex(ref resourceTypeId, out typeIndex) ? m_dataPerType[typeIndex].MaxAvailableResource : 0f;
        }

        public float TotalRequiredInputByType(MyDefinitionId resourceTypeId)
        {
            int typeIndex;
            return TryGetTypeIndex(ref resourceTypeId, out typeIndex) ? m_dataPerType[typeIndex].SinkDataByPriority.Last().RequiredInputCumulative : 0f;
        }

	    #endregion

		public override string ComponentTypeDebugString { get { return "Resource Distributor"; } }
	}
}
