using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using VRage.Collections;
using VRage.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRage.ModAPI;

namespace Sandbox.Game.EntityComponents
{
	public delegate void MyRequiredResourceChangeDelegate(MyDefinitionId changedResourceTypeId, MyResourceSinkComponent sink, float oldRequirement, float newRequirement);
	public delegate float MyResourceAvailableDelegate(MyDefinitionId resourceTypeId, MyResourceSinkComponent sink);
	public delegate void MyCurrentResourceInputChangedDelegate(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink);

	public class MyResourceSinkComponent : MyResourceSinkComponentBase
	{
		private readonly StringBuilder m_textCache = new StringBuilder();

        private MyEntity m_tmpConnectedEntity;
        public override IMyEntity TemporaryConnectedEntity 
        { 
            get { return m_tmpConnectedEntity; } 
            set { m_tmpConnectedEntity = (MyEntity)value; } 
        }

	    struct PerTypeData
	    {
	        public float CurrentInput;
	        public float RequiredInput;
	        public float MaxRequiredInput;
	        public float SuppliedRatio;
	        public Func<float> RequiredInputFunc;
	        public bool IsPowered;
	    }

	    private PerTypeData[] m_dataPerType;

	    private static readonly List<MyResourceSinkInfo> m_singleHelperList = new List<MyResourceSinkInfo>();

        #region Properties
        /// <summary>
        /// Higher priority groups get more resources than lower priority ones.
        /// If there are not enough resources for everything, lower priority groups
        /// are turned off first.
        /// </summary>
        internal MyStringHash Group;

        /// <summary>
        /// Theoretical maximum of required input. This can be different from RequiredInput, but
        /// it has to be >= RequiredInput. It is used to check whether current power supply can meet
        /// demand under stress.
        /// </summary>
        public float MaxRequiredInput { get { return m_dataPerType[0].MaxRequiredInput; } set { m_dataPerType[0].MaxRequiredInput = value; } }
        public float RequiredInput { get { return m_dataPerType[0].RequiredInput; } }
        public float SuppliedRatio { get { return m_dataPerType[0].SuppliedRatio; } }
        public float CurrentInput { get { return m_dataPerType[0].CurrentInput; } }
        public bool IsPowered { get { return m_dataPerType[0].IsPowered; } }

		private readonly Dictionary<MyDefinitionId, int> m_resourceTypeToIndex = new Dictionary<MyDefinitionId, int>(1); 
		private readonly List<MyDefinitionId> m_resourceIds = new List<MyDefinitionId>(1);
        public override ListReader<MyDefinitionId> AcceptedResources { get { return new ListReader<MyDefinitionId>(m_resourceIds); } }
        #endregion

        public event MyRequiredResourceChangeDelegate RequiredInputChanged;
        public event MyResourceAvailableDelegate ResourceAvailable;
        public MyCurrentResourceInputChangedDelegate CurrentInputChanged;
        public event Action IsPoweredChanged;
        public event Action<MyResourceSinkComponent> OnAddType;

		public MyResourceSinkComponent(int initialAllocationSize = 1)
		{
			AllocateData(initialAllocationSize);
		}

		public void Init(MyStringHash group, float maxRequiredInput, Func<float> requiredInputFunc)	// MK: TODO: Remove this
		{
            m_singleHelperList.Add(new MyResourceSinkInfo { ResourceTypeId = MyResourceDistributorComponent.ElectricityId, MaxRequiredInput = maxRequiredInput, RequiredInputFunc = requiredInputFunc });
            Init(group, m_singleHelperList);
            m_singleHelperList.Clear();
		}

	    public void Init(MyStringHash group, MyResourceSinkInfo sinkData)
	    {
	        m_singleHelperList.Add(sinkData);
	        Init(group, m_singleHelperList);
	        m_singleHelperList.Clear();
	    }

        public void Init(MyStringHash group, List<MyResourceSinkInfo> sinkData)
		{
			Debug.Assert(sinkData != null && sinkData.Count > 0);
			Group = group;

			if (m_dataPerType.Length != sinkData.Count)
				AllocateData(sinkData.Count);

            m_resourceTypeToIndex.Clear();
            m_resourceIds.Clear();

			int resourceIndexCounter = 0;
            for (int dataIndex = 0; dataIndex < sinkData.Count; ++dataIndex)
			{
                m_resourceTypeToIndex.Add(sinkData[dataIndex].ResourceTypeId, resourceIndexCounter++);
                m_resourceIds.Add(sinkData[dataIndex].ResourceTypeId);
                m_dataPerType[resourceIndexCounter - 1].MaxRequiredInput = sinkData[dataIndex].MaxRequiredInput;
                m_dataPerType[resourceIndexCounter - 1].RequiredInputFunc = sinkData[dataIndex].RequiredInputFunc;
			}
		}

	    public void AddType(ref MyResourceSinkInfo sinkData)
	    {
	        var newDataPerType = new PerTypeData[m_dataPerType.Length + 1];
	        for (int dataIndex = 0; dataIndex < m_dataPerType.Length; ++dataIndex)
	        {
	            newDataPerType[dataIndex] = m_dataPerType[dataIndex];
	        }
	        m_dataPerType = newDataPerType;
	        m_dataPerType[m_dataPerType.Length - 1] = new PerTypeData
	        {
	            MaxRequiredInput = sinkData.MaxRequiredInput,
	            RequiredInputFunc = sinkData.RequiredInputFunc
	        };
            m_resourceIds.Add(sinkData.ResourceTypeId);
            m_resourceTypeToIndex.Add(sinkData.ResourceTypeId, m_dataPerType.Length - 1);
	        OnAddType(this);
	    }

	    private void AllocateData(int allocationSize)
		{
            m_dataPerType = new PerTypeData[allocationSize];
		}

        /// <summary>
        /// This should be called only from MyResourceDistributor.
        /// </summary>
        public override void SetInputFromDistributor(MyDefinitionId resourceTypeId, float newResourceInput, bool isAdaptible)
        {
	        int typeIndex = GetTypeIndex(resourceTypeId);
            float newSuppliedRatio;
	        float oldInput = m_dataPerType[typeIndex].CurrentInput;
            bool newIsPowered;
            if (newResourceInput > 0f)
            {
                newIsPowered = isAdaptible || newResourceInput >= m_dataPerType[typeIndex].RequiredInput;
                newSuppliedRatio = newResourceInput / m_dataPerType[typeIndex].RequiredInput;
            }
            else
            {
                newIsPowered = m_dataPerType[typeIndex].RequiredInput == 0f;
                newSuppliedRatio = (m_dataPerType[typeIndex].RequiredInput == 0 ? 1f : 0f);
            }

            bool currentInputChanged = newResourceInput != m_dataPerType[typeIndex].CurrentInput;
			bool isPoweredChanged = (newIsPowered != IsPowered);

            m_dataPerType[typeIndex].IsPowered = newIsPowered;
            m_dataPerType[typeIndex].SuppliedRatio = newSuppliedRatio;
            m_dataPerType[typeIndex].CurrentInput = newResourceInput;

            if (currentInputChanged && CurrentInputChanged != null)
                CurrentInputChanged(resourceTypeId, oldInput, this);
            if (isPoweredChanged && IsPoweredChanged != null)
                IsPoweredChanged();
        }

        public override bool IsPowerAvailable(MyDefinitionId resourceTypeId, float power)
        {
	        if (ResourceAvailable == null)
				return false;

	        float available = ResourceAvailable(resourceTypeId, this);
	        return available >= power - CurrentInput;
        }

        public void Update()
        {
            // This will fire an event which will update IsPowered and CurrentInp values.
	        foreach (var typeId in m_resourceTypeToIndex.Keys)
	        {
				SetRequiredInputByType(typeId, m_dataPerType[GetTypeIndex(typeId)].RequiredInputFunc());
	        }
        }

        public override float MaxRequiredInputByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].MaxRequiredInput;
		}

        public override void SetMaxRequiredInputByType(MyDefinitionId resourceTypeId, float newMaxRequiredInput)
		{
			m_dataPerType[GetTypeIndex(resourceTypeId)].MaxRequiredInput = newMaxRequiredInput;
		}

        public override float CurrentInputByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].CurrentInput;
		}
        public override float RequiredInputByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].RequiredInput;
		}

        public override bool IsPoweredByType(MyDefinitionId resourceTypeId)
	    {
	        return m_dataPerType[GetTypeIndex(resourceTypeId)].IsPowered;
	    }

        public override void SetRequiredInputByType(MyDefinitionId resourceTypeId, float newRequiredInput)
		{
			int typeIndex = GetTypeIndex(resourceTypeId);
			if (m_dataPerType[typeIndex].RequiredInput == newRequiredInput)
				return;

			float oldValue = m_dataPerType[typeIndex].RequiredInput;
			m_dataPerType[typeIndex].RequiredInput = newRequiredInput;
			if (RequiredInputChanged != null)
				RequiredInputChanged(resourceTypeId, this, oldValue, newRequiredInput);
		}

        public override float SuppliedRatioByType(MyDefinitionId resourceTypeId)
		{
			return m_dataPerType[GetTypeIndex(resourceTypeId)].SuppliedRatio;
		}

		protected int GetTypeIndex(MyDefinitionId resourceTypeId)
		{
			var typeIndex = 0;
			if(m_resourceTypeToIndex.Count > 1)
				typeIndex = m_resourceTypeToIndex[resourceTypeId];
			return typeIndex;
		}

		public override string ToString()
		{
			const string separator = "; \n";
			m_textCache.Clear();
			m_textCache.AppendFormat("IsPowered: {0}", IsPowered).Append(separator);
			m_textCache.Append("Input: "); MyValueFormatter.AppendWorkInBestUnit(CurrentInput, m_textCache); m_textCache.Append(separator);
			m_textCache.Append("Required: "); MyValueFormatter.AppendWorkInBestUnit(RequiredInput, m_textCache); m_textCache.Append(separator);
			m_textCache.AppendFormat("Ratio: {0}%", SuppliedRatio * 100f);
			return m_textCache.ToString();
		}

        public void DebugDraw(Matrix worldMatrix)
        {
	        if (!MyDebugDrawSettings.DEBUG_DRAW_RESOURCE_RECEIVERS)
				return;

	        Vector3 position = worldMatrix.Translation + worldMatrix.Up;
	        MyRenderProxy.DebugDrawText3D(position, ToString(), Color.White, 0.5f, true, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
        }

		public override string ComponentTypeDebugString { get { return "Resource Sink"; } }
	}
}
