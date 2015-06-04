using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;

using VRage.Trace;
using VRageMath;
using VRage.Utils;
using VRage.ObjectBuilders;

namespace Sandbox.Game.GameSystems.Electricity
{
    public class MyBattery : IMyPowerProducer, IMyPowerConsumer
    {
        internal class Friend
        {
            protected static void OnSyncCapacitySuccess(MyBattery battery, float remainingCapacity)
            {
                battery.SyncCapacitySuccess(remainingCapacity);
            }
        }

        private int m_lastUpdateTime;
        private bool m_canProduce;
        private MyCharacter m_owner;
        private MySyncBattery SyncObject;

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        #region IMyPowerProducer
        MyProducerGroupEnum IMyPowerProducer.Group
        {
            get { return MyProducerGroupEnum.Battery; }
        }

        public float MaxPowerOutput
        {
            get { return MyEnergyConstants.BATTERY_MAX_POWER_OUTPUT; }
        }

        bool Sandbox.ModAPI.Ingame.IMyPowerProducer.ProductionEnabled
        {
            get { return HasCapacityRemaining; }
        }

        float ModAPI.Ingame.IMyPowerProducer.DefinedPowerOutput
        {
            get { return MyEnergyConstants.BATTERY_MAX_POWER_OUTPUT; }
        }

        private float m_currentPowerOutput;
        public float CurrentPowerOutput
        {
            get { return m_currentPowerOutput; }
            set
            {
                MyDebug.AssertDebug(value <= MaxPowerOutput && value >= 0.0f, "Battery power output out of bounds.");
                m_currentPowerOutput = value;
            }
        }

        /// <summary>
        /// Controls energy production from battery. Note that this property only
        /// changes power production and does not affect ability to recharge 
        /// battery (power consumption) as this is property from IMyPowerProducer.
        /// </summary>
        public bool Enabled
        {
            get { return m_producerEnabled; }
            set
            {
                if (m_producerEnabled != value)
                {
                    m_producerEnabled = value;
                    if (m_producerEnabled)
                        m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                    else
                        CurrentPowerOutput = 0.0f;

                    if (MaxPowerOutputChanged != null)
                        MaxPowerOutputChanged(this);
                }
            }
        }
        private bool m_producerEnabled;

        public event Action<IMyPowerProducer> MaxPowerOutputChanged;

        private bool m_hasRemainingCapacity;
        public bool HasCapacityRemaining
        {
            get { return m_hasRemainingCapacity; }
            private set
            {
                if (m_hasRemainingCapacity != value)
                {
                    m_hasRemainingCapacity = value;
                    if (HasCapacityRemainingChanged != null)
                        HasCapacityRemainingChanged(this);
                }
            }
        }

        public event Action<IMyPowerProducer> HasCapacityRemainingChanged;

        private float m_remainingCapacity;
        public float RemainingCapacity
        {
            get { return m_remainingCapacity; }
            private set
            {
                m_remainingCapacity = value;
                PowerReceiver.Update();
            }
        }
        #endregion

        public bool OwnedByLocalPlayer
        {
            get;
            set;
        }

        public bool IsEnergyCritical
        {
            get { return (RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < 0.05f; }
        }

        public bool IsEnergyLow
        {
            get { return (RemainingCapacity / MyEnergyConstants.BATTERY_MAX_CAPACITY) < 0.2f; }
        }

        public MyBattery(MyCharacter owner)
        {
            m_owner = owner;
            SyncObject = new MySyncBattery(this);
            m_remainingCapacity = MyEnergyConstants.BATTERY_MAX_CAPACITY;
            (this as IMyPowerProducer).Enabled = true;
            CurrentPowerOutput = 0.0f;
        }

        public void Init(MyObjectBuilder_Battery builder)
        {
            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Charging,
                true,
                MyEnergyConstants.BATTERY_MAX_POWER_INPUT,
                () => (RemainingCapacity < MyEnergyConstants.BATTERY_MAX_CAPACITY) ? MyEnergyConstants.BATTERY_MAX_POWER_INPUT : 0f);
            PowerReceiver.Update();

            if (builder != null)
            {
                (this as IMyPowerProducer).Enabled = builder.ProducerEnabled;
                if (MySession.Static.SurvivalMode)
                    RemainingCapacity = MathHelper.Clamp(builder.CurrentCapacity, 0f, MyEnergyConstants.BATTERY_MAX_CAPACITY);
            }
            RefreshHasRemainingCapacity();
        }

        public MyObjectBuilder_Battery GetObjectBuilder()
        {
            MyObjectBuilder_Battery builder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Battery>();
            builder.ProducerEnabled = (this as IMyPowerProducer).Enabled;
            builder.CurrentCapacity = RemainingCapacity;
            return builder;
        }

        public void UpdateOnServer()
        {
            if (!Sync.IsServer)
                return;

            RefreshHasRemainingCapacity();
            if (HasCapacityRemaining || PowerReceiver.RequiredInput > 0.0f)
            {
                int timePassed = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime;
                m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                float consumptionPerMillisecond = CurrentPowerOutput / (60 * 60 * 1000);
                float rechargePerMillisecond = (MyFakes.ENABLE_BATTERY_SELF_RECHARGE ? PowerReceiver.MaxRequiredInput : PowerReceiver.CurrentInput) / (60 * 60 * 1000);
                float consumedEnergy = (MySession.Static.CreativeMode) ? 0 : timePassed * consumptionPerMillisecond;
                float rechargedEnergy = timePassed * rechargePerMillisecond;
                float newCapacity = RemainingCapacity;
                newCapacity -= consumedEnergy;
                newCapacity += rechargedEnergy;
                RemainingCapacity = MathHelper.Clamp(newCapacity, 0f, MyEnergyConstants.BATTERY_MAX_CAPACITY);
            }

            RefreshHasRemainingCapacity();
            //Moved to HudWarnings
            SyncObject.SendCapacitySync(m_owner, RemainingCapacity);

            if (false)
            {
                MyTrace.Watch("MyBattery.RequiredPowerInput", PowerReceiver.RequiredInput);
                MyTrace.Watch("MyBattery.CurrentPowerOutput", this.CurrentPowerOutput);
                MyTrace.Watch("MyBattery.CurrentPowerInput", PowerReceiver.CurrentInput);
                MyTrace.Watch("MyBattery.CurrentCapacity", this.RemainingCapacity);
            }
        }

        private void RefreshHasRemainingCapacity()
        {
            HasCapacityRemaining = RemainingCapacity > 0.0f;
        }

        private void SyncCapacitySuccess(float remainingCapacity)
        {
            RemainingCapacity = remainingCapacity;
            RefreshHasRemainingCapacity();
        }
        public bool HasPlayerAccess(long playerId)
        {
            return true;
        }

        /// <summary>
        /// This should be only used for debug
        /// </summary>
        public void DebugDepleteBattery()
        {
            RemainingCapacity = 0f;
        }
    }
}
