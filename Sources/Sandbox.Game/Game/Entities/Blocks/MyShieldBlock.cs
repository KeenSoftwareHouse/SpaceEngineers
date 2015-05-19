using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Utils;

namespace Sandbox.Game.Entities.Blocks
{
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
                0,
                this.CalculateRequiredPowerInput);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.RequiredInputChanged += Receiver_RequiredInputChanged;
            PowerReceiver.Update();
        }

        protected void Receiver_RequiredInputChanged(MyPowerReceiver receiver, float oldRequirement, float newRequirement)
        {

        }

        protected void Receiver_IsPoweredChanged()
        {

        }

        protected float CalculateRequiredPowerInput()
        {
            if (Enabled && IsFunctional)
            {

                return m_MinRequiredPowerInput +m_PowerConsumption;
            }
            return 0.0f;
        }


        public override void UpdateAfterSimulation100()
        {
             base.UpdateAfterSimulation100();

             if (IsFunctional)
             {

             }
        }




        class MySyncShieldBlock
        {
            MyShieldBlock m_shieldBlock;

            public MySyncShieldBlock(MyShieldBlock shieldBlock)
            {
                m_shieldBlock = shieldBlock;
            }
        }
    }
}
