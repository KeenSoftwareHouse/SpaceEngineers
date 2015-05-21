using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender;


using Sandbox.Common;
using VRage;
using Sandbox.Game.Localization;
using VRage.Utils;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_SolarPanel))]
    class MySolarPanel : MyTerminalBlock, IMyPowerProducer, Sandbox.ModAPI.Ingame.IMySolarPanel
    {
        static string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        private float m_currentPowerOutput;
        private float m_maxPowerOutput;
        private MySolarPanelDefinition m_solarPanelDefinition;
        public MySolarPanelDefinition SolarPanelDefinition { get { return m_solarPanelDefinition; } }
        private MySolarGameLogicComponent m_solarComponent;

        public MySolarGameLogicComponent SolarComponent
        {
            get
            {
                return m_solarComponent;
            }
        }

        public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_CubeBlock objectBuilder, Sandbox.Game.Entities.MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            m_solarPanelDefinition = BlockDefinition as MySolarPanelDefinition;
            IsWorkingChanged += OnIsWorkingChanged;
            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;

            GameLogic = new MySolarGameLogicComponent();
            m_solarComponent = GameLogic as MySolarGameLogicComponent;

            m_solarComponent.Initialize(m_solarPanelDefinition.PanelOrientation, m_solarPanelDefinition.IsTwoSided, m_solarPanelDefinition.PanelOffset, this);

            AddDebugRenderComponent(new Components.MyDebugRenderComponentSolarPanel(this));
        }

        bool IMyPowerProducer.Enabled
        {
            get { return true; }
            set { }
        }

        public float MaxPowerOutput
        {
            get { return m_maxPowerOutput; }
            private set
            {
                if (m_maxPowerOutput != value)
                {
                    m_maxPowerOutput = value;
                    if (MaxPowerOutputChanged != null)
                        MaxPowerOutputChanged(this);
                }
            }
        }

        private void UpdateDisplay()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(MaxPowerOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
            MyValueFormatter.AppendWorkInBestUnit(CurrentPowerOutput, DetailedInfo);
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
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 0);
                }
                return;
            }
            if (MaxPowerOutput > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i < (MaxPowerOutput / m_solarPanelDefinition.MaxPowerOutput) * 4)
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Green, null, null, 1);
                    else
                        VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Black, null, null, 1);
                }
            }
            else
            {
                VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[0], null, Color.Red, null, null, 0);
                for (int i = 1; i < 4; i++)
                {
                    VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[i], null, Color.Red, null, null, 0);
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

            UpdateEmissivity();
        }

        public float CurrentPowerOutput
        {
            get
            {
                return m_currentPowerOutput;
            }
            set
            {
                m_currentPowerOutput = value;
                UpdateDisplay();
            }
        }

       

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (CubeGrid.Physics == null)
                return;

            float maxPowerOutput = m_solarComponent.MaxOutput * SolarPanelDefinition.MaxPowerOutput;

            if (maxPowerOutput != m_maxPowerOutput)
            {
                MaxPowerOutput = maxPowerOutput;
                UpdateDisplay();
            }
        }

        public bool HasCapacityRemaining
        {
            get { return true; }
        }

        public event Action<IMyPowerProducer> HasCapacityRemainingChanged
        {
            add { }
            remove { }
        }

        public event Action<IMyPowerProducer> MaxPowerOutputChanged;

        public float RemainingCapacity
        {
            get { return float.PositiveInfinity; }
        }

        MyProducerGroupEnum IMyPowerProducer.Group
        {
            get { return MyProducerGroupEnum.SolarPanels; }
        }
    }
}
