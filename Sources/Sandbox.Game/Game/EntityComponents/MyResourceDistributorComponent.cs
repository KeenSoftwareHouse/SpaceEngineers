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

				StockpilingStorage.Clear();
				OtherStorage.Clear();
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
			public int LastFuelTimeCompute;
			public float MaxAvailableResource;
			public MyMultipleEnabledEnum SourcesEnabled;
			public bool SourcesEnabledDirty;
			public MyResourceStateEnum ResourceState;
		}

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

			m_sinkSubtypeToPriority.Add(MyStringHash.NullOrEmpty, m_sinkGroupPrioritiesTotal-1);
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

		private void InitializeNewType(MyDefinitionId typeId)
		{
			m_typeIdToIndex.Add(typeId, m_typeGroupCount++);
			m_typeIdToConveyorConnectionRequired.Add(typeId, IsConveyorConnectionRequiredTotal(typeId));

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

		    if (IsConveyorConnectionRequired(typeId))
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
				if(!m_initializedTypes.Contains(typeId))
					InitializeNewType(typeId);

				var sinksOfType = GetSinksOfType(typeId, sink.Group);
				Debug.Assert(MatchesAdaptability(sinksOfType, sink), "All sinks in the same group must have same adaptability.");
				Debug.Assert(!sinksOfType.Contains(sink));
			    int typeIndex = GetTypeIndex(typeId);

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
				if(!m_initializedTypes.Contains(typeId))
					InitializeNewType(typeId);

			    var sourcesOfType = GetSourcesOfType(typeId, source.Group);
			    int typeIndex = GetTypeIndex(typeId);
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

			    --m_dataPerType[typeIndex].SourceCount;
				if (m_dataPerType[typeIndex].SourceCount == 0)
				{
					m_dataPerType[typeIndex].SourcesEnabled= MyMultipleEnabledEnum.NoObjects;
				}
				else if (m_dataPerType[typeIndex].SourceCount== 1)
				{
				    var firstSourceOfType = GetFirstSourceOfType(typeId);
                    if(firstSourceOfType != null)
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
            foreach(var pair in m_dataPerType[typeIndex].InputOutputList)
                if (pair.Item2.Group == sourceGroupType && pair.Item2.CurrentOutputByType(resourceTypeId) > 0) ++additionalCount;

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
			if (m_dataPerType[typeIndex].SourcesEnabled == state || m_dataPerType[typeIndex] .SourcesEnabled == MyMultipleEnabledEnum.NoObjects)
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
                    ref m_dataPerType[typeIndex].InputOutputData,
					m_dataPerType[typeIndex].SinksByPriority,
					m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].InputOutputList,
					m_dataPerType[typeIndex].StockpilingStorageIndices,
					m_dataPerType[typeIndex].OtherStorageIndices,
					out m_dataPerType[typeIndex].MaxAvailableResource);
                ProfilerShort.BeginNextBlock("RecomputeDistribution");
				m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
					typeId, 
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
                ProfilerShort.Begin("RecreatePhysical" + typeId);
		        if (m_dataPerType[typeIndex].GroupsDirty)
		        {
		            m_dataPerType[typeIndex].DistributionGroupsInUse = 0;
		            RecreatePhysicalDistributionGroups(typeId, m_dataPerType[typeIndex].SinksByPriority, m_dataPerType[typeIndex].SourcesByPriority, m_dataPerType[typeIndex].InputOutputList);
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
                    ref group.InputOutputData,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairs,
                    group.StockpilingStorage,
                    group.OtherStorage,
                    out group.MaxAvailableResources);

                    ProfilerShort.BeginNextBlock("RecomputeDistribution");
                    group.ResourceState = RecomputeResourceDistributionPartial(
					typeId,
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

            foreach (var sinkSourcePair in allSinkSources)
            {
                if (sinkSourcePair.Item1.Entity != null)
                    SetEntityGroup(typeId, sinkSourcePair.Item1.Entity);
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

            PrepareSinkSourceData(typeId, ref sinkSourceData, sinkSourcePairs, stockpilingStorageList, otherStorageList);
            maxAvailableResource += sinkSourceData.Item2.MaxAvailableResource;
		}

		private void PrepareSinkSourceData(
			MyDefinitionId typeId,
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
	    private MyResourceStateEnum RecomputeResourceDistributionPartial(
			MyDefinitionId typeId,
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
            if (stockpilingStorageList.Count > 0)
            {
                ProfilerShort.Begin("Stockpiles");
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
                ProfilerShort.End();
            }

	        float consumptionForStockpiles = totalAvailableResourcesForStockpiles - availableResourcesForStockpiles;
            float totalAvailableResourcesForStorage = Math.Max(totalAvailableResource - sinkSourceData.Item2.MaxAvailableResource - consumptionForNonStorage - consumptionForStockpiles, 0);
            float availableResourcesForStorage = totalAvailableResourcesForStorage;
            if (otherStorageList.Count > 0)
            {
                ProfilerShort.Begin("Non-stockpiling storage");
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
            foreach (int pairIndex in otherStorageList)
            {
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
			    foreach (MyResourceSourceComponent source in sourcesByPriority[sourcePriorityIndex])
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
            if(!TryGetTypeIndex(resourceTypeId, out typeIndex))
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

				PrepareSinkSourceData(
					changedResourceTypeId,
					ref m_dataPerType[typeIndex].InputOutputData,
					m_dataPerType[typeIndex].InputOutputList,
					m_dataPerType[typeIndex].StockpilingStorageIndices,
					m_dataPerType[typeIndex].OtherStorageIndices);

			    m_dataPerType[typeIndex].ResourceState = RecomputeResourceDistributionPartial(
					changedResourceTypeId,
					groupId,
					m_dataPerType[typeIndex].SinkDataByPriority,
					m_dataPerType[typeIndex].SourceDataByPriority,
                    ref m_dataPerType[typeIndex].InputOutputData,
					m_dataPerType[typeIndex].SinksByPriority,
					m_dataPerType[typeIndex].SourcesByPriority,
                    m_dataPerType[typeIndex].InputOutputList,
					m_dataPerType[typeIndex].StockpilingStorageIndices,
					m_dataPerType[typeIndex].OtherStorageIndices,
					m_dataPerType[typeIndex].SinkDataByPriority[groupId].RemainingAvailableResource);
		    }
		    else
		    {
                for (int groupIndex = 0; groupIndex < m_dataPerType[typeIndex].DistributionGroupsInUse; ++groupIndex)
				{
                    if (!m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinksByPriority[groupId].Contains(changedSink) && m_dataPerType[typeIndex].DistributionGroups[groupIndex].SinkSourcePairs.TrueForAll((pair) => pair.Item1 != changedSink))
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

					PrepareSinkSourceData(
					changedResourceTypeId,
                    ref group.InputOutputData,
                    group.SinkSourcePairs,
                    group.StockpilingStorage,
                    group.OtherStorage);

                    group.ResourceState = RecomputeResourceDistributionPartial(
					changedResourceTypeId,
					groupId,
                    group.SinkDataByPriority,
                    group.SourceDataByPriority,
                    ref group.InputOutputData,
                    group.SinksByPriority,
                    group.SourcesByPriority,
                    group.SinkSourcePairs,
                    group.StockpilingStorage,
                    group.OtherStorage,
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
