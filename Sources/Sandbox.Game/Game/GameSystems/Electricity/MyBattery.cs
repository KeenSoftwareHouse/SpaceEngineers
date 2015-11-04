using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Components;

namespace Sandbox.Game.GameSystems.Electricity
{
    public class MyBattery
    {
        internal class Friend
        {
            protected static void OnSyncCapacitySuccess(MyBattery battery, float remainingCapacity)
            {
                battery.SyncCapacitySuccess(remainingCapacity);
            }
        }

		private int m_lastUpdateTime;

        private MyEntity m_lastParent = null;

        public const float EnergyCriticalThreshold = 0.10f;
        public const float EnergyLowThreshold = 0.25f;

        public bool IsEnergyCritical { get { return (ResourceSource.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < EnergyCriticalThreshold; } }
        public bool IsEnergyLow { get { return (ResourceSource.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < EnergyLowThreshold; } }

        private readonly MyCharacter m_owner;
		public MyCharacter Owner { get { return m_owner; } }

        internal readonly MySyncBattery SyncObject;

        public MyResourceSinkComponent ResourceSink { get; private set; }
	    public MyResourceSourceComponent ResourceSource { get; private set; }

		private readonly MyStringHash m_resourceSinkGroup = MyStringHash.GetOrCompute("Charging");
	    private readonly MyStringHash m_resourceSourceGroup = MyStringHash.GetOrCompute("Battery");	

        public bool OwnedByLocalPlayer { get; set; }

        public MyBattery(MyCharacter owner)
        {
            m_owner = owner;
            SyncObject = new MySyncBattery(this);
			ResourceSink = new MyResourceSinkComponent();
			ResourceSource = new MyResourceSourceComponent();
        }

        public void Init(MyObjectBuilder_Battery builder, List<MyResourceSinkInfo> additionalSinks = null, List<MyResourceSourceInfo> additionalSources = null)
        {
            var defaultSinkInfo = new MyResourceSinkInfo
            {
                MaxRequiredInput = MyEnergyConstants.BATTERY_MAX_POWER_INPUT,
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                RequiredInputFunc = () => (ResourceSource.RemainingCapacity < MyEnergyConstants.BATTERY_MAX_CAPACITY) ? MyEnergyConstants.BATTERY_MAX_POWER_INPUT : 0f,
            };

            if (additionalSinks != null)
            {
				additionalSinks.Insert(0, defaultSinkInfo);
                ResourceSink.Init(m_resourceSinkGroup, additionalSinks);
            }
            else
            {
                ResourceSink.Init(m_resourceSinkGroup, defaultSinkInfo);
            }

            ResourceSink.TemporaryConnectedEntity = m_owner;

            var defaultSourceInfo = new MyResourceSourceInfo
            {
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                DefinedOutput = MyEnergyConstants.BATTERY_MAX_POWER_OUTPUT, // TODO: Load max output from definitions
                ProductionToCapacityMultiplier = 60*60
            };

            if (additionalSources != null)
            {
                additionalSources.Insert(0, defaultSourceInfo);
                ResourceSource.Init(m_resourceSourceGroup, additionalSources);
            }
            else
                ResourceSource.Init(m_resourceSourceGroup, defaultSourceInfo);

            ResourceSource.TemporaryConnectedEntity = m_owner;
	        m_lastUpdateTime = MySession.Static.GameplayFrameCounter;
			
            if (builder == null)
            {
                ResourceSource.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, true);
                ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, MyEnergyConstants.BATTERY_MAX_CAPACITY);
			    ResourceSink.Update();
                return;
            }

            ResourceSource.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, builder.ProducerEnabled);
			if (MySession.Static.SurvivalMode)
				ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, MathHelper.Clamp(builder.CurrentCapacity, 0f, MyEnergyConstants.BATTERY_MAX_CAPACITY));
            else
                ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, MyEnergyConstants.BATTERY_MAX_CAPACITY);
                
            ResourceSink.Update();
        }

		public MyObjectBuilder_Battery GetObjectBuilder()
		{
			MyObjectBuilder_Battery builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Battery>();
			builder.ProducerEnabled = ResourceSource.Enabled;
			builder.CurrentCapacity = ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId);
			return builder;
		}

		public void UpdateOnServer()
		{
			if (!Sync.IsServer)
				return;

		    MyEntity newParent = m_owner.Parent;
		    if (m_lastParent != newParent) // Need to rethink batteries
		    {
		        ResourceSink.Update();

		        m_lastParent = newParent;
		    }

		    if (ResourceSource.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId) || ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) > 0.0f)
			{
                float secondsSinceLastUpdate = (MySession.Static.GameplayFrameCounter - m_lastUpdateTime)*MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
                m_lastUpdateTime = MySession.Static.GameplayFrameCounter;
                var productionToCapacity = ResourceSource.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId);
                float consumptionPerSecond = ResourceSource.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId) / productionToCapacity;
                float rechargePerSecond = (MyFakes.ENABLE_BATTERY_SELF_RECHARGE ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) / productionToCapacity);
                float consumedEnergy = (MySession.Static.CreativeMode) ? 0 : secondsSinceLastUpdate * consumptionPerSecond;
                float rechargedEnergy = secondsSinceLastUpdate * rechargePerSecond;
			    float energyTransfer = rechargedEnergy - consumedEnergy;
			    float newCapacity = ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId) + energyTransfer;
				ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, MathHelper.Clamp(newCapacity, 0f, MyEnergyConstants.BATTERY_MAX_CAPACITY));
			}

			if (!ResourceSource.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId))
                ResourceSink.Update();
           
            SyncObject.SendCapacitySync(Owner, ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId));
		}

		internal void SyncCapacitySuccess(float remainingCapacity)
		{
			ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, remainingCapacity);
		}

        public void DebugDepleteBattery()
        {
            ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, 0f);
        }
    }
}
