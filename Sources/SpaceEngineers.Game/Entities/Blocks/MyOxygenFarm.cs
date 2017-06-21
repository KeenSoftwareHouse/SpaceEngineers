using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_OxygenFarm))]
    public class MyOxygenFarm : MyFunctionalBlock, IMyOxygenFarm, IMyGasBlock
    {
        static readonly string[] m_emissiveNames = { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

        private float m_maxGasOutputFactor;
        private bool firstUpdate = true;
        public new MyOxygenFarmDefinition BlockDefinition { get { return base.BlockDefinition as MyOxygenFarmDefinition; } }

        public MySolarGameLogicComponent SolarComponent { get; private set; }
	    readonly MyDefinitionId m_oxygenGasId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Oxygen");	// Required for oxygen MyFake checks

        public bool CanProduce { get { return (MySession.Static.Settings.EnableOxygen || BlockDefinition.ProducedGas != m_oxygenGasId) && Enabled && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && IsWorking && IsFunctional; } }

        private MyResourceSourceComponent m_sourceComp;
        public MyResourceSourceComponent SourceComp
        {
            get { return m_sourceComp; }
            set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
        }

        public MyOxygenFarm()
        {
            ResourceSink = new MyResourceSinkComponent();
            SourceComp = new MyResourceSourceComponent();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            IsWorkingChanged += OnIsWorkingChanged;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            InitializeConveyorEndpoint();

            SourceComp.Init(
                BlockDefinition.ResourceSourceGroup,
                new MyResourceSourceInfo
                {
                    ResourceTypeId = BlockDefinition.ProducedGas,
                    DefinedOutput = BlockDefinition.MaxGasOutput,
                    ProductionToCapacityMultiplier = 1,
                    IsInfiniteCapacity = true,
                });
            SourceComp.Enabled = IsWorking;

            ResourceSink.Init(
                BlockDefinition.ResourceSinkGroup,
                new MyResourceSinkInfo
                {
                    ResourceTypeId = MyResourceDistributorComponent.ElectricityId,
                    MaxRequiredInput = BlockDefinition.OperationalPowerConsumption,
                    RequiredInputFunc = ComputeRequiredPower,
                });
            ResourceSink.IsPoweredChanged += PowerReceiver_IsPoweredChanged;
            ResourceSink.Update();

            GameLogic = new MySolarGameLogicComponent();
            SolarComponent = GameLogic as MySolarGameLogicComponent;

            SolarComponent.Initialize(BlockDefinition.PanelOrientation, BlockDefinition.IsTwoSided, BlockDefinition.PanelOffset, this);

            AddDebugRenderComponent(new MyDebugRenderComponentSolarPanel(this));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_OnIsFunctionalChanged;

            UpdateVisual();
            UpdateDisplay();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var builder = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_OxygenFarm;

            return builder;
        }

        private float ComputeRequiredPower()
        {
            return Enabled && IsFunctional ? BlockDefinition.OperationalPowerConsumption : 0f;
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            UpdateEmissivity();
        }

        private void ComponentStack_OnIsFunctionalChanged()
        {
            SourceComp.Enabled = IsWorking;
            ResourceSink.Update();
            UpdateEmissivity();
        }

        private void PowerReceiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        void OnIsWorkingChanged(MyCubeBlock obj)
        {
            SourceComp.Enabled = IsWorking;
            UpdateEmissivity();
        }

        protected override bool CheckIsWorking()
        {
			return (MySession.Static.Settings.EnableOxygen || BlockDefinition.ProducedGas != m_oxygenGasId) && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        private void UpdateDisplay()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");

            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_OxygenOutput));
            DetailedInfo.Append((SourceComp.MaxOutputByType(BlockDefinition.ProducedGas)*60).ToString("F"));
            DetailedInfo.Append(" L/min");

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
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Red, 1);
                }
                return;
            }
            if (m_maxGasOutputFactor > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i < m_maxGasOutputFactor * 4)
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Green, 1);
                    else
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Black, 1); 
                }
            }
            else
            {
               UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[0], Color.Red, 0); 
                for (int i = 1; i < 4; i++)
                {
                    UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Red, 0); 
                }
            }
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

            ResourceSink.Update();
            float maxGasOutputFactor = IsWorking && SourceComp.ProductionEnabledByType(BlockDefinition.ProducedGas) ? SolarComponent.MaxOutput : 0f;

            if (maxGasOutputFactor != m_maxGasOutputFactor || firstUpdate)
            {
                m_maxGasOutputFactor = maxGasOutputFactor;
                SourceComp.SetMaxOutputByType(BlockDefinition.ProducedGas, SourceComp.DefinedOutputByType(BlockDefinition.ProducedGas)*m_maxGasOutputFactor);
                UpdateVisual();
                UpdateDisplay();
                firstUpdate = false;
            }

            ResourceSink.Update();

        }

        bool IMyGasBlock.IsWorking()
        {
            return MySession.Static.Settings.EnableOxygen && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && IsWorking && IsFunctional;
        }

        #region Conveyor
        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint { get { return m_conveyorEndpoint; } }
        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
        }
        #endregion

        float ModAPI.Ingame.IMyOxygenFarm.GetOutput()
        {
	        return !IsWorking ? 0f : SolarComponent.MaxOutput;
        }

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            return null;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            return null;
        }

        #endregion

    }
}
