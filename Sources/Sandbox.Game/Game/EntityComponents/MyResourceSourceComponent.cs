using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage.Collections;
using VRage.Utils;
using VRage.Game.Components;
using Sandbox.Common;
using System.Text;
using VRage.Game.Entity;
using VRage.Game;

namespace Sandbox.Game.EntityComponents
{
	public delegate void MyResourceCapacityRemainingChangedDelegate(MyDefinitionId changedResourceId, MyResourceSourceComponent source);
	public delegate void MyResourceOutputChangedDelegate(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source);

	public struct MyResourceSourceInfo
	{
		public MyDefinitionId ResourceTypeId;
		public float DefinedOutput;
	    public float ProductionToCapacityMultiplier;
		public bool IsInfiniteCapacity;
	}

	public class MyResourceSourceComponent : MyResourceSourceComponentBase
	{
		public event MyResourceCapacityRemainingChangedDelegate HasCapacityRemainingChanged;
        public event MyResourceCapacityRemainingChangedDelegate ProductionEnabledChanged;
		public event MyResourceOutputChangedDelegate OutputChanged;
		public event MyResourceOutputChangedDelegate MaxOutputChanged;

        public MyEntity TemporaryConnectedEntity { get; set; }

		private int m_allocatedTypeCount;

	    struct PerTypeData
	    {
	        public float CurrentOutput;
	        public float MaxOutput;
	        public float DefinedOutput;
	        public float RemainingCapacity;
            public float ProductionToCapacityMultiplier;
	        public bool HasRemainingCapacity;
	        public bool IsProducerEnabled;
	    }

	    private PerTypeData[] m_dataPerType;
	    private bool m_enabled;

        private readonly StringBuilder m_textCache = new StringBuilder();

        [ThreadStatic]
	    private static List<MyResourceSourceInfo> m_singleHelperList = new List<MyResourceSourceInfo>(); 

		public MyStringHash Group { get; private set; }

		public float CurrentOutput { get { return CurrentOutputByType(m_resourceTypeToIndex.Keys.First()); } }
		public float MaxOutput { get { return MaxOutputByType(m_resourceTypeToIndex.Keys.First()); } }
		public float DefinedOutput { get { return DefinedOutputByType(m_resourceTypeToIndex.Keys.First()); } }
	    public bool ProductionEnabled { get { return ProductionEnabledByType(m_resourceTypeToIndex.Keys.First()); } }

        //How much resource is filled actually (usedratio * Capacity)
		public float RemainingCapacity { get { return RemainingCapacityByType(m_resourceTypeToIndex.Keys.First()); } }

		public bool IsInfiniteCapacity { get { return float.IsInfinity(RemainingCapacity); } }
	    public float ProductionToCapacityMultiplier { get { return ProductionToCapacityMultiplierByType(m_resourceTypeToIndex.Keys.First()); } }
	    public bool Enabled { get { return m_enabled; } set { SetEnabled(value); } }

	    public bool HasCapacityRemaining
		{
			get { return HasCapacityRemainingByType(m_resourceTypeToIndex.Keys.First()); }
		}

		private readonly Dictionary<MyDefinitionId, int> m_resourceTypeToIndex = new Dictionary<MyDefinitionId, int>(1, MyDefinitionId.Comparer);
        private readonly List<MyDefinitionId> m_resourceIds = new List<MyDefinitionId>(1);
		public ListReader<MyDefinitionId> ResourceTypes { get { return new ListReader<MyDefinitionId>(m_resourceIds); } }

		public MyResourceSourceComponent(int initialAllocationSize = 1)
		{
			AllocateData(initialAllocationSize);
		}

	    public void Init(MyStringHash sourceGroup, MyResourceSourceInfo sourceResourceData)
	    {
            MyUtils.Init(ref m_singleHelperList);
	        m_singleHelperList.Add(sourceResourceData);
            Init(sourceGroup, m_singleHelperList);
            m_singleHelperList.Clear();
	    }

		public void Init(MyStringHash sourceGroup, List<MyResourceSourceInfo> sourceResourceData)
		{
			Group = sourceGroup;

			var resourceData = sourceResourceData;
			bool hasResourceData = resourceData != null && resourceData.Count != 0;
			int resourceCount = hasResourceData ? resourceData.Count : 1;
		    Enabled = true;

			if(resourceCount != m_allocatedTypeCount)
				AllocateData(resourceCount);

			int resourceIndexCounter = 0;
			if (!hasResourceData)
			{
				m_resourceTypeToIndex.Add(MyResourceDistributorComponent.ElectricityId, resourceIndexCounter++);
				m_resourceIds.Add(MyResourceDistributorComponent.ElectricityId);
			} 
			else
			{
				foreach (var resourceInfo in resourceData)
				{
					m_resourceTypeToIndex.Add(resourceInfo.ResourceTypeId, resourceIndexCounter++);
					m_resourceIds.Add(resourceInfo.ResourceTypeId);
				    m_dataPerType[resourceIndexCounter - 1].DefinedOutput = resourceInfo.DefinedOutput;
					SetOutputByType(resourceInfo.ResourceTypeId, 0f);
					SetMaxOutputByType(resourceInfo.ResourceTypeId, m_dataPerType[GetTypeIndex(resourceInfo.ResourceTypeId)].DefinedOutput);
                    SetProductionEnabledByType(resourceInfo.ResourceTypeId, true);
				    m_dataPerType[resourceIndexCounter - 1].ProductionToCapacityMultiplier = (resourceInfo.ProductionToCapacityMultiplier != 0f ? resourceInfo.ProductionToCapacityMultiplier : 1f);

					if(resourceInfo.IsInfiniteCapacity)
						SetRemainingCapacityByType(resourceInfo.ResourceTypeId, float.PositiveInfinity);
				}
			}
		}

		private void AllocateData(int allocationSize)
		{
            m_dataPerType = new PerTypeData[allocationSize];

			m_allocatedTypeCount = allocationSize;
		}

		public override float CurrentOutputByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].CurrentOutput;
		}

		public void SetOutput(float newOutput)
		{
			SetOutputByType(m_resourceTypeToIndex.Keys.First(), newOutput);
		}

		public void SetOutputByType(MyDefinitionId resourceTypeId, float newOutput)
		{
			var typeIndex = GetTypeIndex(resourceTypeId);
			Debug.Assert(newOutput <= m_dataPerType[typeIndex].MaxOutput && newOutput >= 0 && !float.IsNaN(newOutput), "Invalid resource source current output");
		    float oldOutput = m_dataPerType[typeIndex].CurrentOutput;
			m_dataPerType[typeIndex].CurrentOutput = newOutput;
			if (oldOutput != newOutput && OutputChanged != null)
				OutputChanged(resourceTypeId, oldOutput, this);
		}

		public float RemainingCapacityByType(MyDefinitionId resourceTypeId)
		{
		    return m_dataPerType[GetTypeIndex(resourceTypeId)].RemainingCapacity;
		}

		public void SetRemainingCapacityByType(MyDefinitionId resourceTypeId, float newRemainingCapacity)
		{
			var typeIndex = GetTypeIndex(resourceTypeId);
			float oldRemainingCapacity = m_dataPerType[typeIndex].RemainingCapacity;
            float oldOutput = MaxOutputLimitedByCapacity(typeIndex);
			m_dataPerType[typeIndex].RemainingCapacity = newRemainingCapacity;
		    if (oldRemainingCapacity != newRemainingCapacity)
		        SetHasCapacityRemainingByType(resourceTypeId, newRemainingCapacity > 0);

		    if (MaxOutputChanged == null)
		        return;

		    float newOutput = MaxOutputLimitedByCapacity(typeIndex);
		    if (newOutput != oldOutput)
		        MaxOutputChanged(resourceTypeId, oldOutput, this);
		}

		public override float MaxOutputByType(MyDefinitionId resourceTypeId)
		{
		    int typeIndex = GetTypeIndex(resourceTypeId);
            return MaxOutputLimitedByCapacity(typeIndex);
		}

	    private float MaxOutputLimitedByCapacity(int typeIndex)
	    {
	        return Math.Min(m_dataPerType[typeIndex].MaxOutput, m_dataPerType[typeIndex].RemainingCapacity*m_dataPerType[typeIndex].ProductionToCapacityMultiplier*MyEngineConstants.UPDATE_STEPS_PER_SECOND);
	    }

		public void SetMaxOutput(float newMaxOutput) { SetMaxOutputByType(m_resourceTypeToIndex.Keys.First(), newMaxOutput); }
		public void SetMaxOutputByType(MyDefinitionId resourceTypeId, float newMaxOutput)
		{
			var typeIndex = GetTypeIndex(resourceTypeId);
			if (m_dataPerType[typeIndex].MaxOutput != newMaxOutput)
			{
				var oldOutput = m_dataPerType[typeIndex].MaxOutput;
				m_dataPerType[typeIndex].MaxOutput = newMaxOutput;
				if (MaxOutputChanged != null)
					MaxOutputChanged(resourceTypeId, oldOutput, this);
			}
		}

		public override float DefinedOutputByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].DefinedOutput;
		}

	    public float ProductionToCapacityMultiplierByType(MyDefinitionId resourceTypeId)
	    {
	        return m_dataPerType[GetTypeIndex(resourceTypeId)].ProductionToCapacityMultiplier;
	    }

		public bool HasCapacityRemainingByType(MyDefinitionId resourceTypeId)
		{
			return IsInfiniteCapacity || MySession.Static.CreativeMode || m_dataPerType[GetTypeIndex(resourceTypeId)].HasRemainingCapacity;
		}

		private void SetHasCapacityRemainingByType(MyDefinitionId resourceTypeId, bool newHasCapacity)
		{
			if (IsInfiniteCapacity)
				return;

			int typeIndex = GetTypeIndex(resourceTypeId);
            if (m_dataPerType[typeIndex].HasRemainingCapacity != newHasCapacity)
			{
                m_dataPerType[typeIndex].HasRemainingCapacity = newHasCapacity;
				if (HasCapacityRemainingChanged != null)
					HasCapacityRemainingChanged(resourceTypeId, this);

                if (!newHasCapacity)
                    m_dataPerType[typeIndex].CurrentOutput = 0f;
			}
		}

		public override bool ProductionEnabledByType(MyDefinitionId resourceTypeId)
		{
            return m_dataPerType[GetTypeIndex(resourceTypeId)].IsProducerEnabled;
		}

		public void SetProductionEnabledByType(MyDefinitionId resourceTypeId, bool newProducerEnabled)
		{
		    int typeIndex = GetTypeIndex(resourceTypeId);
            bool valueChanged = m_dataPerType[typeIndex].IsProducerEnabled != newProducerEnabled;
            m_dataPerType[typeIndex].IsProducerEnabled = newProducerEnabled;

		    if (valueChanged && ProductionEnabledChanged != null)
		        ProductionEnabledChanged(resourceTypeId, this);

			if(!newProducerEnabled)
				SetOutputByType(resourceTypeId, 0f);
		}

	    private void SetEnabled(bool newValue)
	    {
            bool oldValue = m_enabled;

	        m_enabled = newValue;

            if (oldValue != m_enabled)
            {
                foreach (var resourceId in m_resourceIds)
                    if (ProductionEnabledChanged != null)
                        ProductionEnabledChanged(resourceId, this);

                if (!m_enabled)
                {
                    foreach (var resourceId in m_resourceIds)
                        SetOutputByType(resourceId, 0f);
                }
            }
	    }

		protected int GetTypeIndex(MyDefinitionId resourceTypeId)
		{
			int typeIndex = 0;
			if(m_resourceTypeToIndex.Count > 1)
				typeIndex = m_resourceTypeToIndex[resourceTypeId];
			return typeIndex;
		}

        public override string ToString()
        {
            const string separator = "; \n";
            m_textCache.Clear();
            m_textCache.AppendFormat("Enabled: {0}", Enabled).Append(separator);
            m_textCache.Append("Output: "); MyValueFormatter.AppendWorkInBestUnit(CurrentOutput, m_textCache); m_textCache.Append(separator);
            m_textCache.Append("Max Output: "); MyValueFormatter.AppendWorkInBestUnit(MaxOutput, m_textCache); m_textCache.Append(separator);
            m_textCache.AppendFormat("ProductionEnabled: {0}", ProductionEnabled);
            return m_textCache.ToString();
        }

		public override string ComponentTypeDebugString { get { return "Resource Source"; } }
	}
}
