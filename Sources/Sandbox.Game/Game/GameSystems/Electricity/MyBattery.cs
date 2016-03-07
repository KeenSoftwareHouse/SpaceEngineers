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
using VRage.Game.Components;
using System;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Network;
using Sandbox.Engine.Multiplayer;

namespace Sandbox.Game.GameSystems.Electricity
{
    [StaticEventOwner]
    public class MyBattery
    {
		private int m_lastUpdateTime;

        private MyEntity m_lastParent = null;

        public const float EnergyCriticalThreshold = 0.10f;
        public const float EnergyLowThreshold = 0.25f;

        private const int m_productionUpdateInterval = 100;

        public bool IsEnergyCritical { get { return (ResourceSource.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < EnergyCriticalThreshold; } }
        public bool IsEnergyLow { get { return (ResourceSource.RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < EnergyLowThreshold; } }

        private readonly MyCharacter m_owner;
		public MyCharacter Owner { get { return m_owner; } }

        public MyResourceSinkComponent ResourceSink { get; private set; }
	    public MyResourceSourceComponent ResourceSource { get; private set; }

		private readonly MyStringHash m_resourceSinkGroup = MyStringHash.GetOrCompute("Charging");
	    private readonly MyStringHash m_resourceSourceGroup = MyStringHash.GetOrCompute("Battery");	

        public bool OwnedByLocalPlayer { get; set; }

        public MyBattery(MyCharacter owner)
        {
            m_owner = owner;
			ResourceSink = new MyResourceSinkComponent();
			ResourceSource = new MyResourceSourceComponent();
        }

        public void Init(MyObjectBuilder_Battery builder, List<MyResourceSinkInfo> additionalSinks = null, List<MyResourceSourceInfo> additionalSources = null)
        {
            var defaultSinkInfo = new MyResourceSinkInfo
            {
                MaxRequiredInput = MyEnergyConstants.BATTERY_MAX_POWER_INPUT,
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                RequiredInputFunc = Sink_ComputeRequiredPower,
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

        public float Sink_ComputeRequiredPower()
        {
            float inputRequiredToFillIn100Updates = (MyEnergyConstants.BATTERY_MAX_CAPACITY - ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId)) * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / m_productionUpdateInterval * ResourceSource.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId);
            float currentOutput = ResourceSource.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId);
            currentOutput *= MySession.Static.CreativeMode ? 0f : 1f;
            return Math.Min(inputRequiredToFillIn100Updates + currentOutput, MyEnergyConstants.BATTERY_MAX_POWER_INPUT);
        }

		public void UpdateOnServer100()
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
                float secondsSinceLastUpdate = (MySession.Static.GameplayFrameCounter - m_lastUpdateTime) * MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
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

            MyMultiplayer.RaiseStaticEvent(s => MyBattery.SyncCapacitySuccess, Owner.EntityId, ResourceSource.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId));
		}

        [Event, Reliable, Server, Broadcast]
        private static void SyncCapacitySuccess(long entityId, float remainingCapacity)
        {
            MyCharacter owner;
            MyEntities.TryGetEntityById(entityId, out owner);
            if (owner == null)
                return;

            owner.SuitBattery.ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, remainingCapacity);
        }

        public void DebugDepleteBattery()
        {
            ResourceSource.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, 0f);
        }
    }
}
