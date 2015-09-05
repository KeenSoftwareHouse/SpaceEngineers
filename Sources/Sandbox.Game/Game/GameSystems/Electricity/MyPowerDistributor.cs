using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;

using VRage;
using VRage.Trace;
using Sandbox.Graphics;
using VRage.Utils;
using Sandbox.Engine.Utils;
using VRage;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;

namespace Sandbox.Game.GameSystems.Electricity
{
    /// <summary>
    /// Used as indices to array, so starting from 0, which is highest priority.
    /// Use default numbering to ensure continuity and 0-based indexing.
    /// </summary>
    public enum MyConsumerGroupEnum
    {
        Defense,
        Conveyors,
        Factory,
        Doors,
        Utility,  //lights etc.
        Charging,
        Gyro,
        Thrust,
        BatteryBlock,
    }

    public enum MyProducerGroupEnum
    {
        SolarPanels,
        Battery,
        Reactors,
    }

    public enum MyPowerStateEnum
    {
        Ok,
        OverloadAdaptible, // some adaptible group does not have enough power, but everything else still works fine
        OverloadBlackout,  // some non-adaptible group does not have enough power, so it is without power
        NoPower
    }

    public class MyPowerDistributor
    {
        /// <summary>
        /// Some precomputed data for each priority group.
        /// </summary>
        struct ConsumerGroupData
        {
            public bool IsPowerAdaptible;
            public float RequiredInput;
            public float RequiredInputCumulative; // Sum of required input for this group and groups above it.
            public float AvailablePower; // Remaining power after distributing to higher priorities.

            public override string ToString()
            {
                return string.Format("IsPowerAdaptible: {0}, RequiredInput: {1}, AvailablePower: {2}", IsPowerAdaptible, RequiredInput, AvailablePower);
            }
        }

        struct ProducerGroupData
        {
            public float MaxAvailablePower;
            public float UsageRatio;
            public bool InfiniteCapacity;
            public int ActiveCount;

            public override string ToString()
            {
                return string.Format("MaxAvailablePower: {0}, UsageRatio: {1}", MaxAvailablePower, UsageRatio);
            }
        }

        private ConsumerGroupData[] m_consumerDataByPriority;
        private ProducerGroupData[] m_producerDataByPriority;
        private HashSet<IMyPowerConsumer>[] m_consumersByPriority;
        private HashSet<IMyPowerProducer>[] m_producersByPriority;
        private bool m_needsRecompute;
        private int m_producerCount;

        private float m_remainingFuelTime;
        private bool m_remainingFuelTimeDirty;
        private int m_lastFuelTimeCompute;
		private int m_allEnabledCounter = 0;
		public bool AllEnabledRecently { get { return m_allEnabledCounter <= 30; } }
        /// <summary>
        /// Remaining fuel time in hours.
        /// </summary>
        public float RemainingFuelTime
        {
            get
            {
                if (m_remainingFuelTimeDirty && m_lastFuelTimeCompute > 30)
                {
                    m_remainingFuelTime = ComputeRemainingFuelTime();
                    m_lastFuelTimeCompute = 0;
                }
                return m_remainingFuelTime;
            }
        }

        #region Properties
        public event Action<MyPowerStateEnum> PowerStateChaged;

        private float m_maxAvailablePower;
        public float MaxAvailablePower
        {
            get { return m_maxAvailablePower; }
        }
        
        public float TotalRequiredInput
        {
            get { return m_consumerDataByPriority.Last().RequiredInputCumulative; }
        }

        /// <summary>
        /// For debugging purposes. Enables trace messages and watches for this instance.
        /// </summary>
        public bool ShowTrace { get; set; }

        public MyMultipleEnabledEnum ProducersEnabled
        {
            get
            {
                if (m_producersEnabledDirty)
                    RefreshProducersEnabled();

                return m_producersEnabled;
            }         
        }

        public void ChangeProducersState(MyMultipleEnabledEnum state,long playerId)
        {
            MyDebug.AssertDebug(state != MyMultipleEnabledEnum.Mixed, "You must NOT use this property to set mixed state.");
            MyDebug.AssertDebug(state != MyMultipleEnabledEnum.NoObjects, "You must NOT use this property to set state without any objects.");
            // You cannot change the state when there are no objects.
            if (ProducersEnabled != state && ProducersEnabled != MyMultipleEnabledEnum.NoObjects)
            {
                m_producersEnabled = state;
                bool enabled = (state == MyMultipleEnabledEnum.AllEnabled);
                foreach (var group in m_producersByPriority)
                {
                    foreach (var producer in group)
                    {
                        if (producer.HasPlayerAccess(playerId))
                        {
                            producer.MaxPowerOutputChanged -= producer_MaxPowerOutputChanged;
                            producer.Enabled = enabled;
                            producer.MaxPowerOutputChanged += producer_MaxPowerOutputChanged;
                        }
                    }
                }

                // recomputing power distribution here caused problems with 
                // connecting to ship connector. The bug caused immediate disconnect
                // in only special cases.
                
                //RecomputePowerDistribution();
                m_producersEnabledDirty = false;
                m_needsRecompute = true;
				m_allEnabledCounter = 0;
            }
        }

        private MyMultipleEnabledEnum m_producersEnabled;
        private bool m_producersEnabledDirty;

        public MyPowerStateEnum PowerState
        {
            get
            {
                if (m_needsRecompute)
                    RecomputePowerDistribution();
                return m_powerState;
            }
            private set { m_powerState = value; }
        }
        private MyPowerStateEnum m_powerState;

        #endregion

        public MyPowerDistributor()
        {
            m_consumerDataByPriority = new ConsumerGroupData[typeof(MyConsumerGroupEnum).GetEnumValues().Length];
            m_producerDataByPriority = new ProducerGroupData[typeof(MyProducerGroupEnum).GetEnumValues().Length];
            m_consumersByPriority = new HashSet<IMyPowerConsumer>[m_consumerDataByPriority.Length];
            for (int i = 0; i < m_consumersByPriority.Length; ++i)
                m_consumersByPriority[i] = new HashSet<IMyPowerConsumer>();

            m_producersByPriority = new HashSet<IMyPowerProducer>[m_producerDataByPriority.Length];
            for (int i = 0; i < m_producerDataByPriority.Length; ++i)
                m_producersByPriority[i] = new HashSet<IMyPowerProducer>();

            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            m_producersEnabled = MyMultipleEnabledEnum.NoObjects;
        }

        #region Add and remove

        public void AddConsumer(IMyPowerConsumer consumer)
        {
            Debug.Assert(consumer != null);
            Debug.Assert(consumer.PowerReceiver != null);
            Debug.Assert(MatchesPowerAdaptability(GetConsumers(consumer.PowerReceiver.Group), consumer),
                         "All consumers in the same group must have same power-adaptability.");
            Debug.Assert(!GetConsumers(consumer.PowerReceiver.Group).Contains(consumer));
            if (GetConsumers(consumer.PowerReceiver.Group).Contains(consumer))
                return;

            m_consumersByPriority[(int)consumer.PowerReceiver.Group].Add(consumer);
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            consumer.PowerReceiver.RequiredInputChanged += Receiver_RequiredInputChanged;
        }

        public void RemoveConsumer(IMyPowerConsumer consumer, bool resetConsumerInput = true, bool markedForClose = false)
        {
            if (markedForClose)
                return;
            Debug.Assert(consumer != null);
            Debug.Assert(m_consumersByPriority[(int)consumer.PowerReceiver.Group].Contains(consumer));

            m_consumersByPriority[(int)consumer.PowerReceiver.Group].Remove(consumer);
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            consumer.PowerReceiver.RequiredInputChanged -= Receiver_RequiredInputChanged;
            if (resetConsumerInput)
                consumer.PowerReceiver.SetInputFromDistributor(0.0f);
        }

        public void AddProducer(IMyPowerProducer producer)
        {
            Debug.Assert(producer != null);
            Debug.Assert(!GetProducers(producer.Group).Contains(producer));
            Debug.Assert(MatchesInfiniteCapacity(GetProducers(producer.Group), producer),
                         "All producers in the same group must have same 'infinite capacity' state.");
            if (GetProducers(producer.Group).Contains(producer))
                return;

            GetProducers(producer.Group).Add(producer);
            ++m_producerCount;
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            producer.HasCapacityRemainingChanged += producer_HasRemainingCapacityChanged;
            producer.MaxPowerOutputChanged += producer_MaxPowerOutputChanged;
            if (m_producerCount == 1)
            {
                // This is the only producer we have, so the state of all is the same as of this one.
                m_producersEnabled = (producer.Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            }
            else if ((ProducersEnabled == MyMultipleEnabledEnum.AllEnabled && !producer.Enabled) ||
                     (ProducersEnabled == MyMultipleEnabledEnum.AllDisabled && producer.Enabled))
            {
                m_producersEnabled = MyMultipleEnabledEnum.Mixed;
            }
        }

        public int GetProducerCount(MyProducerGroupEnum group)
        {
            return m_producerDataByPriority[(int)group].ActiveCount;
        }

        public void RemoveProducer(IMyPowerProducer producer)
        {
            Debug.Assert(producer != null);
            Debug.Assert(GetProducers(producer.Group).Contains(producer));

            GetProducers(producer.Group).Remove(producer);
            --m_producerCount;
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            producer.MaxPowerOutputChanged -= producer_MaxPowerOutputChanged;
            producer.HasCapacityRemainingChanged -= producer_HasRemainingCapacityChanged;
            if (m_producerCount == 0)
            {
                m_producersEnabled = MyMultipleEnabledEnum.NoObjects;
            }
            else if (m_producerCount == 1)
            {
                ChangeProducersState((GetFirstProducer().Enabled) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled,MySession.LocalPlayerId);
            }
            else if (ProducersEnabled == MyMultipleEnabledEnum.Mixed)
            {
                // We were in mixed state and need to check whether we still are.
                m_producersEnabledDirty = true;
            }
        }

        public void GetPowerProducers(List<IMyPowerProducer> producers)
        {
            for (int i = 0; i < m_producersByPriority.Length; ++i)
            {
                foreach (var producer in m_producersByPriority[i])
                {
                    producers.Add(producer);
                }
            }

            return;
        }

        #endregion

        public void UpdateBeforeSimulation10()
        {
            if (m_needsRecompute)
                RecomputePowerDistribution();

            if (ShowTrace)
                UpdateTrace();
            m_lastFuelTimeCompute += 10;
			m_allEnabledCounter += 10;
        }

        /// <summary>
        /// Computes number of groups that have enough energy to work.
        /// </summary>
        public void UpdateHud(MyHudConsumerGroupInfo info)
        {
            bool isWorking = true;
            int workingGroupCount = 0;
            int i = 0;
            for (; i < m_consumerDataByPriority.Length; ++i)
            {
                if (isWorking && m_consumerDataByPriority[i].AvailablePower < m_consumerDataByPriority[i].RequiredInput &&
                    !m_consumerDataByPriority[i].IsPowerAdaptible)
                    isWorking = false;

                if (isWorking)
                    ++workingGroupCount;

                info.SetGroupDeficit((MyConsumerGroupEnum)i, Math.Max(m_consumerDataByPriority[i].RequiredInput - m_consumerDataByPriority[i].AvailablePower, 0.0f));
            }

            info.WorkingGroupCount = workingGroupCount;
        }

        #region Private methods

        private float ComputeRemainingFuelTime()
        {
            ProfilerShort.Begin("MyPowerDistributor.ComputeRemainingFuelTime()");
            try
            {
                if (MaxAvailablePower == 0.0f)
                {
                    return 0.0f;
                }

                float powerInUse = 0.0f;
                for (int i = 0; i < m_consumerDataByPriority.Length; ++i)
                {
                    var data = m_consumerDataByPriority[i];
                    if (data.AvailablePower >= data.RequiredInput)
                        powerInUse += data.RequiredInput;
                    else if (data.IsPowerAdaptible)
                        powerInUse += data.AvailablePower;
                    else
                        break;
                }

                bool hasInfiniteProducer = false;
                bool hasAnyOtherProducer = false;
                float remainingCapacity = 0.0f;
                for (int i = 0; i < m_producersByPriority.Length; ++i)
                {
                    var groupData = m_producerDataByPriority[i];
                    if (groupData.UsageRatio <= 0f)
                        continue;

                    if (groupData.InfiniteCapacity)
                    {
                        hasInfiniteProducer = true;
                        // ignore power from infinite capacity group
                        powerInUse -= groupData.UsageRatio * groupData.MaxAvailablePower;
                        continue;
                    }

                    var group = m_producersByPriority[i];
                    foreach (var producer in group)
                    {
                        if (producer.Enabled)
                        {
                            hasAnyOtherProducer = true;
                            remainingCapacity += producer.RemainingCapacity;
                        }
                    }
                }

                if (hasInfiniteProducer && !hasAnyOtherProducer)
                    return float.PositiveInfinity;

                float res = 0f;
                if (powerInUse > 0f)
                    res = remainingCapacity / powerInUse;

                return res;
            }
            finally
            {
                ProfilerShort.End();
            }
        }

        private void RefreshProducersEnabled()
        {
            ProfilerShort.Begin("MyPowerDistributor.RefreshProducersEnabled");
            m_producersEnabledDirty = false;
            // Simplest method for now. If it takes too long at some point, we can change it.

            if (m_producerCount == 0)
            {
                m_producersEnabled = MyMultipleEnabledEnum.NoObjects;
                ProfilerShort.End();
                return;
            }

            bool allOn = true;
            bool allOff = true;
            foreach (var group in m_producersByPriority)
            foreach (var producer in group)
            {
                allOn = allOn && producer.Enabled;
                allOff = allOff && !producer.Enabled;
                if (!allOn && !allOff)
                {
                    m_producersEnabled = MyMultipleEnabledEnum.Mixed;
                    ProfilerShort.End();
                    return;
                }
            }
            m_producersEnabled = (allOn) ? MyMultipleEnabledEnum.AllEnabled : MyMultipleEnabledEnum.AllDisabled;
            ProfilerShort.End();
        }

        private void RecomputePowerDistribution()
        {
            ProfilerShort.Begin("MyPowerDistributor.RecomputePowerDistribution");

            // Clear state of all producers and consumers. Also find out how 
            // much power is available for distribution.
            m_maxAvailablePower = 0.0f;

            Debug.Assert(m_producerDataByPriority.Length == m_producersByPriority.Length);
            for (int i = 0; i < m_producerDataByPriority.Length; ++i)
            {
                var group = m_producersByPriority[i];
                var groupData = m_producerDataByPriority[i];
                groupData.MaxAvailablePower = 0f;
                foreach (var producer in group)
                {
                    producer.CurrentPowerOutput = 0;
                    if (producer.Enabled && producer.HasCapacityRemaining)
                    {
                        MyDebug.AssertDebug(producer.CurrentPowerOutput == 0.0f, "ClearPowerOutput must reduce current output to 0.");
                        groupData.MaxAvailablePower += producer.MaxPowerOutput;
                        groupData.InfiniteCapacity = producer.IsInfiniteCapacity();
                    }
                }
                m_maxAvailablePower += groupData.MaxAvailablePower;
                m_producerDataByPriority[i] = groupData;
            }

            float requiredInputCumulative = 0.0f;
            for (int i = 0; i < m_consumersByPriority.Length; ++i)
            {
                float requiredInput = 0.0f;
                bool isAdaptible = true;
                foreach (var consumer in m_consumersByPriority[i])
                {
                    requiredInput += consumer.PowerReceiver.RequiredInput;
                    isAdaptible = isAdaptible && consumer.PowerReceiver.IsAdaptible;
                }
                m_consumerDataByPriority[i].RequiredInput = requiredInput;
                m_consumerDataByPriority[i].IsPowerAdaptible = isAdaptible;

                requiredInputCumulative += requiredInput;
                m_consumerDataByPriority[i].RequiredInputCumulative = requiredInputCumulative;
            }

            RecomputePowerDistributionPartial(0, m_maxAvailablePower);

            m_needsRecompute = false;

            //MyTrace.Send(TraceWindow.Default, "MyPowerDitributor: Recomputed power distribution.");

            ProfilerShort.End();
        }

        /// <summary>
        /// Recomputes power distribution in subset of all priority groups (in range
        /// from startPriorityIdx until the end). Passing index 0 recomputes all priority groups.
        /// </summary>
        private void RecomputePowerDistributionPartial(int startPriorityIdx, float availablePower)
        {
            int i = startPriorityIdx;
            for (; i < m_consumersByPriority.Length; ++i)
            {
                var priorityGroup = m_consumersByPriority[i];

                m_consumerDataByPriority[i].AvailablePower = availablePower;

                if (m_consumerDataByPriority[i].RequiredInput <= availablePower)
                {
                    // Run everything in the group at max.
                    availablePower -= m_consumerDataByPriority[i].RequiredInput;
                    foreach (var consumer in priorityGroup)
                        consumer.PowerReceiver.SetInputFromDistributor(consumer.PowerReceiver.RequiredInput);
                }
                else if (m_consumerDataByPriority[i].IsPowerAdaptible && availablePower > 0.0f)
                {
                    // Distribute power in this group based on ratio of its requirement vs. group requirement.
                    foreach (var consumer in priorityGroup)
                    {
                        float ratio = consumer.PowerReceiver.RequiredInput / m_consumerDataByPriority[i].RequiredInput;
                        consumer.PowerReceiver.SetInputFromDistributor(ratio * availablePower);
                    }
                    availablePower = 0.0f;
                }
                else
                {
                    // Not enough power for this group and members can't adapt.
                    // None of the lower priority groups will get any power either.
                    foreach (var consumer in priorityGroup)
                        consumer.PowerReceiver.SetInputFromDistributor(0.0f);
                    m_consumerDataByPriority[i].AvailablePower = availablePower;
                    ++i; // move on to next group
                    break;
                }
            }

            // Set remaining data.
            for (; i < m_consumerDataByPriority.Length; ++i)
            {
                m_consumerDataByPriority[i].AvailablePower = 0.0f;
                foreach (var consumer in m_consumersByPriority[i])
                    consumer.PowerReceiver.SetInputFromDistributor(0.0f);
            }

            for (int j = 0; j < m_producersByPriority.Length; ++j)
            {
                Debug.Assert(availablePower < m_maxAvailablePower || m_maxAvailablePower == 0.0f || MyUtils.IsZero(m_maxAvailablePower - availablePower, MyMathConstants.EPSILON * m_maxAvailablePower));
                var group = m_producersByPriority[j];
                var groupData = m_producerDataByPriority[j];
                if (groupData.MaxAvailablePower > 0f)
                {
                    var inUse = m_maxAvailablePower - availablePower;
                    groupData.UsageRatio = Math.Min(1f, inUse / groupData.MaxAvailablePower);
                    availablePower += groupData.UsageRatio * groupData.MaxAvailablePower;
                }
                else
                    groupData.UsageRatio = 0f;

                groupData.ActiveCount = 0;
                foreach (var producer in group)
                {
                    if (producer.Enabled && producer.HasCapacityRemaining)
                    {
                        ++groupData.ActiveCount;
                        producer.CurrentPowerOutput = groupData.UsageRatio * producer.MaxPowerOutput;
                    }
                }
                m_producerDataByPriority[j] = groupData;
            }

            if (MaxAvailablePower == 0.0f)
                PowerState = MyPowerStateEnum.NoPower;
            else if (TotalRequiredInput > MaxAvailablePower)
            {
                var lastGroup = m_consumerDataByPriority.Last();
                if (lastGroup.IsPowerAdaptible && lastGroup.AvailablePower != 0.0f)
                    PowerState = MyPowerStateEnum.OverloadAdaptible;
                else
                    PowerState = MyPowerStateEnum.OverloadBlackout;
            }
            else
                PowerState = MyPowerStateEnum.Ok;
        }

        /// <summary>
        /// Mostly debug method to verify that all members of the group have 
        /// same ability to adapt as given consumer.
        /// </summary>
        private bool MatchesPowerAdaptability(HashSet<IMyPowerConsumer> group, IMyPowerConsumer consumer)
        {
            foreach (var member in group)
            {
                if (member.PowerReceiver.IsAdaptible != consumer.PowerReceiver.IsAdaptible)
                    return false;
            }
            return true;
        }

        private bool MatchesInfiniteCapacity(HashSet<IMyPowerProducer> group, IMyPowerProducer producer)
        {
            foreach (var member in group)
            {
                if (producer.IsInfiniteCapacity() != member.IsInfiniteCapacity())
                    return false;
            }
            return true;
        }

        [Conditional("DEBUG")]
        private void UpdateTrace()
        {
            for (int i = 0; i < m_consumerDataByPriority.Length; ++i)
            {
                var data = m_consumerDataByPriority[i];
                var group = (MyConsumerGroupEnum)i;
                MyTrace.Watch(String.Format("Data[{0}].AvailablePower", group), data.AvailablePower);
            }
            for (int i = 0; i < m_consumerDataByPriority.Length; ++i)
            {
                var data = m_consumerDataByPriority[i];
                var group = (MyConsumerGroupEnum)i;
                MyTrace.Watch(String.Format("Data[{0}].RequiredInput", group), data.RequiredInput);
            }
            for (int i = 0; i < m_consumerDataByPriority.Length; ++i)
            {
                var data = m_consumerDataByPriority[i];
                var group = (MyConsumerGroupEnum)i;
                MyTrace.Watch(String.Format("Data[{0}].IsPowerAdaptible", group), data.IsPowerAdaptible);
            }

            int j = 0;
            foreach (var group in m_producersByPriority)
            foreach (var producer in group)
            {
                ++j;
                MyTrace.Watch(String.Format("Producer[{0}].IsTurnedOn", j), producer.Enabled);
                MyTrace.Watch(String.Format("Producer[{0}].HasRemainingCapacity", j), producer.HasCapacityRemaining);
                MyTrace.Watch(String.Format("Producer[{0}].CurrentPowerOutput", j), producer.CurrentPowerOutput);
            }
        }

        private HashSet<IMyPowerConsumer> GetConsumers(MyConsumerGroupEnum group)
        {
            return m_consumersByPriority[(int)group];
        }

        private HashSet<IMyPowerProducer> GetProducers(MyProducerGroupEnum group)
        {
            return m_producersByPriority[(int)group];
        }

        private IMyPowerProducer GetFirstProducer()
        {
            for (int i = 0 ; i < m_producersByPriority.Length; ++i)
            {
                if (m_producersByPriority[i].Count > 0)
                    return m_producersByPriority[i].First();
            }
            return null;
        }
        #endregion

        #region Event handlers
        private void Receiver_RequiredInputChanged(MyPowerReceiver changedConsumer, float oldRequirement, float newRequirement)
        {
            if (m_needsRecompute)
                RecomputePowerDistribution();

            // Go over all priorities, starting from the changedConsumer.
            int idx = (int)changedConsumer.Group;
            MyDebug.AssertDebug(m_consumerDataByPriority[idx].RequiredInput >= 0.0f);
            m_consumerDataByPriority[idx].RequiredInput = 0.0f;
            foreach (var consumer in m_consumersByPriority[idx])
                m_consumerDataByPriority[idx].RequiredInput += consumer.PowerReceiver.RequiredInput;

            // Update cumulative requirements.
            float cumulative = (idx != 0) ? m_consumerDataByPriority[idx - 1].RequiredInputCumulative
                                          : 0.0f;
            for (int i = idx; i < m_consumerDataByPriority.Length; ++i)
            {
                cumulative += m_consumerDataByPriority[i].RequiredInput;
                m_consumerDataByPriority[i].RequiredInputCumulative = cumulative;
            }

            RecomputePowerDistributionPartial(idx, m_consumerDataByPriority[idx].AvailablePower);
            MyDebug.AssertDebug(m_consumerDataByPriority[idx].RequiredInput >= 0.0f);
        }

        private void producer_HasRemainingCapacityChanged(IMyPowerProducer producer)
        {
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
        }

        private void producer_MaxPowerOutputChanged(IMyPowerProducer obj)
        {
            m_needsRecompute = true;
            m_remainingFuelTimeDirty = true;
            m_producersEnabledDirty = true;

            // Don't wait for next update with few producers.
            // Also ensures that when character enters cockpit, his battery is
            // turned off right away without waiting for update.
            if (m_producerCount == 1)
                RecomputePowerDistribution();
        }
        #endregion
    }
}
