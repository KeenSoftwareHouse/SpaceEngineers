using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Utils;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ShieldBlock))]
    class MyShieldBlock : MyFunctionalBlock, IMyPowerConsumer, Sandbox.ModAPI.Ingame.IMyShieldBlock
    {

        private float m_MinRequiredPowerInput;
        private float m_PowerConsumption;
        private float m_MaxShieldCapacity;
        private float m_ShieldUpRate;

        private float m_currentShieldCapacity;

        private new MySyncShieldBlock SyncObject;

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            SyncObject = new MySyncShieldBlock(this);

            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_ShieldBlock));
            var shieldBlockDefinition = BlockDefinition as MyShieldBlockDefinition;
            MyDebug.AssertDebug(shieldBlockDefinition != null);

            m_MinRequiredPowerInput = shieldBlockDefinition.MinRequiredPowerInput;
            m_PowerConsumption = shieldBlockDefinition.PowerConsumption;
            m_MaxShieldCapacity = shieldBlockDefinition.MaxShieldCapacity;
            m_ShieldUpRate = shieldBlockDefinition.ShieldUpRate;

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Utility,
                false,
                m_MinRequiredPowerInput,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.RequiredInputChanged += Receiver_RequiredInputChanged;
            PowerReceiver.Update();

            UpdateText();
        }

        protected void Receiver_RequiredInputChanged(MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {
            UpdateIsWorking();
        }

        protected void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
            {
                if( m_currentShieldCapacity ==m_MaxShieldCapacity)
                {
                    return m_MinRequiredPowerInput;
                }
                return m_MinRequiredPowerInput + m_PowerConsumption;
            }
            return 0.0f;
        }
        private void StoreCapacity(int timeDelta, float input)
        {
            float inputPowerPerMillisecond = input / (60 * 60 );
            float increment = (timeDelta * inputPowerPerMillisecond) * m_ShieldUpRate ;

            if ((m_currentShieldCapacity + increment) < m_MaxShieldCapacity)
            {
                m_currentShieldCapacity += increment;
            }
            else
            {
                m_currentShieldCapacity = m_MaxShieldCapacity;
            }

            SyncObject.CapacityChange(m_currentShieldCapacity);
        }

        private float ConsumeCapacity(float consumedCapacity)
        {
            UpdateIsWorking();
            if (!IsWorking)
                return consumedCapacity;

            if (consumedCapacity == 0)
            {
                return 0;
            }
            float Remaining = 0;

            if ((m_currentShieldCapacity - consumedCapacity) <= 0)
            {
                Remaining = consumedCapacity - m_currentShieldCapacity;
                m_currentShieldCapacity = 0;
            }
            else
            {
                m_currentShieldCapacity -= consumedCapacity;
            }
            SyncObject.CapacityChange(m_currentShieldCapacity);
            return Remaining;
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && PowerReceiver.CurrentInput >= m_MinRequiredPowerInput;
        }
        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();
            PowerReceiver.Update();
            UpdateIsWorking();
            if (IsWorking)
            {
                int timeDelta = 100 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
                StoreCapacity(timeDelta, PowerReceiver.CurrentInput);
            }
        }

        private float DoDamage(float damage, Common.ObjectBuilders.Definitions.MyDamageType damageType)
        {
            return ConsumeCapacity(damage);
        }

        internal static float DoDamage(float damage, Common.ObjectBuilders.Definitions.MyDamageType damageType, MySlimBlock mySlimBlock)
        {
            var shields = mySlimBlock.CubeGrid.GetFatBlocks<MyShieldBlock>();


            foreach (var item in shields)
            {
                damage = item.DoDamage(damage, damageType);
                if(damage<=0)
                {
                    break;
                }
            }
            return damage;
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BatteryBlock));
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.RequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxShieldCapacity));
            MyValueFormatter.AppendWorkHoursInBestUnit(m_MaxShieldCapacity, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.CurrentInput, DetailedInfo);
            DetailedInfo.Append("\n");

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_ShieldCapacity));
            MyValueFormatter.AppendWorkHoursInBestUnit(m_currentShieldCapacity, DetailedInfo);
            DetailedInfo.Append("\n");
            RaisePropertiesChanged();
        }

        [PreloadRequired]
        class MySyncShieldBlock
        {
            MyShieldBlock m_shieldBlock;

            public MySyncShieldBlock(MyShieldBlock shieldBlock)
            {
                m_shieldBlock = shieldBlock;
            }
            static MySyncShieldBlock()
            {

                MySyncLayer.RegisterMessage<CapacityMsg>(CapacityChange, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            }
            [MessageIdAttribute(11587, P2PMessageEnum.Reliable)]
            protected struct CapacityMsg : IEntityMessage
            {
                public long EntityId;

                public long GetEntityId() { return EntityId; }

                public float capacity;

                public override string ToString()
                {
                    return String.Format("{0}, {1}", this.GetType().Name, this.GetEntityText());
                }
            }

            static void CapacityChange(ref CapacityMsg msg, MyNetworkClient sender)
            {
                MyShieldBlock shieldBlock;
                if (MyEntities.TryGetEntityById<MyShieldBlock>(msg.EntityId, out shieldBlock))
                {
                    shieldBlock.m_currentShieldCapacity = msg.capacity;
                    shieldBlock.UpdateText();
                    if (Sync.IsServer)
                        Sync.Layer.SendMessageToAll(ref msg);
                }
            }

            public void CapacityChange(float capacity)
            {
                var msg = new CapacityMsg();
                msg.EntityId = m_shieldBlock.EntityId;
                msg.capacity = capacity;

                Sync.Layer.SendMessageToServer(ref msg);
            }
        }
    }

}
