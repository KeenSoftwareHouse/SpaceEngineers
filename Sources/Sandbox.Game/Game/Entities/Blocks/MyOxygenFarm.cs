using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Text;
using VRageMath;
using Sandbox.Common;
using Sandbox.ModAPI.Ingame;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using VRage.ModAPI;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenFarm))]
    class MyOxygenFarm : MyFunctionalBlock, IMyOxygenProducer, IMyConveyorEndpointBlock, IMyPowerConsumer, IMyOxygenFarm
    {
        static string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        private float m_maxOxygenOutput;
        private MyOxygenFarmDefinition m_oxygenFarmDefinition;

        private MySolarGameLogicComponent m_solarComponent;

        public MySolarGameLogicComponent SolarComponent
        {
            get
            {
                return m_solarComponent;
            }
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            protected set;
        }

        public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_oxygenFarmDefinition = BlockDefinition as MyOxygenFarmDefinition;
            IsWorkingChanged += OnIsWorkingChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            InitializeConveyorEndpoint();

            PowerReceiver = new MyPowerReceiver(
                MyConsumerGroupEnum.Factory,
                false,
                m_oxygenFarmDefinition.OperationalPowerConsumption,
                ComputeRequiredPower);
            PowerReceiver.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            PowerReceiver.Update();

            GameLogic = new MySolarGameLogicComponent();
            m_solarComponent = GameLogic as MySolarGameLogicComponent;

            m_solarComponent.Initialize(m_oxygenFarmDefinition.PanelOrientation, m_oxygenFarmDefinition.IsTwoSided, m_oxygenFarmDefinition.PanelOffset, this);

            AddDebugRenderComponent(new Components.MyDebugRenderComponentSolarPanel(this));
        }

        private float ComputeRequiredPower()
        {
            return (Enabled && IsFunctional) ? m_oxygenFarmDefinition.OperationalPowerConsumption : 0f;
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected override bool CheckIsWorking()
        {
            return MySession.Static.Settings.EnableOxygen && PowerReceiver.IsPowered && base.CheckIsWorking();
        }

        private void UpdateDisplay()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(PowerReceiver.MaxRequiredInput, DetailedInfo);
            DetailedInfo.Append("\n");

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_OxygenOutput));
            DetailedInfo.Append((m_maxOxygenOutput * 100f).ToString("F"));
            DetailedInfo.Append("%");
            
            RaisePropertiesChanged();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;
            if (!IsWorking)
            {
                for (int i = 0; i < 4; i++)
                {
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Red, null, null, 1);
                }
                return;
            }
            if (m_maxOxygenOutput > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i < m_maxOxygenOutput * 4)
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Green, null, null, 1);
                    else
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 1);
                }
            }
            else
            {
                VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[0], null, Color.Black, null, null, 0);
                for (int i = 1; i < 4; i++)
                {
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 0);
                }
            }
        }

        void OnIsWorkingChanged(MyCubeBlock obj)
        {
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();

            UpdateIsWorking();
            UpdateEmissivity();
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (CubeGrid.Physics == null)
                return;

            float maxOxygenOutput = m_solarComponent.MaxOutput;

            if (maxOxygenOutput != m_maxOxygenOutput)
            {
                m_maxOxygenOutput = maxOxygenOutput;
                UpdateDisplay();
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();

            PowerReceiver.Update();
        }

        #region Oxygen
        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.RegisterOxygenBlock(this);
            }
        }

        public override void OnUnregisteredFromGridSystems()
        {
            base.OnUnregisteredFromGridSystems();

            if (CubeGrid.GridSystems.OxygenSystem != null)
            {
                CubeGrid.GridSystems.OxygenSystem.UnregisterOxygenBlock(this);
            }
        }

        float IMyOxygenProducer.ProductionCapacity(float deltaTime)
        {
            return m_maxOxygenOutput * m_oxygenFarmDefinition.MaxOxygenOutput;
        }
        void IMyOxygenProducer.Produce(float amount)
        {
            // Nothing to do
        }
        int IMyOxygenProducer.GetPriority()
        {
            return 1;
        }

        bool IMyOxygenBlock.IsWorking()
        {
            return MySession.Static.Settings.EnableOxygen && PowerReceiver.IsPowered && IsWorking && IsFunctional;
        }
        #endregion

        #region Conveyor
        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }
        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }
        #endregion

        float IMyOxygenFarm.GetOutput()
        {
            if (!IsWorking)
            {
                return 0f;
            }

            return m_solarComponent.MaxOutput;
        }
    }
}
