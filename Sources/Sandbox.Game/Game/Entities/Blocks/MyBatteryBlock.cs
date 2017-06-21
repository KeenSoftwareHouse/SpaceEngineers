using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Engine.Multiplayer;
using SteamSDK;
using Sandbox.Common;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRage.ModAPI;
using VRage.Sync;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_BatteryBlock))]
    public class MyBatteryBlock : MyFunctionalBlock, ModAPI.IMyBatteryBlock
    {
        static readonly string[] m_emissiveNames = new string[] { "Emissive0", "Emissive1", "Emissive2", "Emissive3" };

		public new MyBatteryBlockDefinition BlockDefinition { get { return base.BlockDefinition as MyBatteryBlockDefinition; } }
        private bool m_hasRemainingCapacity;
        private float m_maxOutput;
        private float m_currentOutput;
        private float m_currentStoredPower;
        private float m_maxStoredPower;
        private int m_lastUpdateTime;
        private float m_timeRemaining = 0;

        private const int m_productionUpdateInterval = 100;

        private readonly Sync<bool> m_isFull;
        private readonly Sync<bool> m_onlyRecharge;
        private readonly Sync<bool> m_onlyDischarge;
        private readonly Sync<bool> m_semiautoEnabled;
        private readonly Sync<bool> m_producerEnabled;
        private readonly Sync<float> m_storedPower;

        private Color m_prevEmissiveColor = Color.Black;
        private int m_prevFillCount = -1;

		private MyResourceSourceComponent m_sourceComp;
		public MyResourceSourceComponent SourceComp
		{
			get { return m_sourceComp; }
			set { if (Components.Contains(typeof(MyResourceSourceComponent))) Components.Remove<MyResourceSourceComponent>(); Components.Add<MyResourceSourceComponent>(value); m_sourceComp = value; }
		}

        public float TimeRemaining { get { return m_timeRemaining; } set { m_timeRemaining = value; UpdateText(); } }
        public bool HasCapacityRemaining { get { return SourceComp.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId); } }
        public float MaxStoredPower { get { return m_maxStoredPower; } private set { if (m_maxStoredPower != value) m_maxStoredPower = value; } }

        public float CurrentStoredPower
        {
            get { return SourceComp.RemainingCapacityByType(MyResourceDistributorComponent.ElectricityId); }
            set
            {
                SourceComp.SetRemainingCapacityByType(MyResourceDistributorComponent.ElectricityId, MathHelper.Clamp(value, 0f, MaxStoredPower));
                UpdateMaxOutputAndEmissivity();
            }
        }

        public float CurrentOutput
        {
            get { if (SourceComp != null) return SourceComp.CurrentOutput; return 0; }
        }

        public float CurrentInput
        {
            get { if (ResourceSink != null) return ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId); return 0; }
        }

        public bool IsCharging
        {
            get { return (CurrentInput > CurrentOutput && CurrentInput > 0); }
        }

        public bool SemiautoEnabled
        {
            get { return m_semiautoEnabled; }
            set
            {
                m_semiautoEnabled.Value = value;

                if(!OnlyRecharge && !OnlyDischarge)
                {
                    if (CurrentStoredPower == 0)
                        OnlyRecharge = true;
                    else
                        OnlyDischarge = true;
                }
            }
        }

        public bool OnlyRecharge
        {
            get { return m_onlyRecharge.Value; }
            set
            {
                if (value)
                    OnlyDischarge = false;
                m_onlyRecharge.Value = value;
                m_producerEnabled.Value = !value;
            }
        }

        public bool OnlyDischarge
        {
            get { return m_onlyDischarge.Value; }
            set
            {
                if (value)
                    OnlyRecharge = false;
                m_onlyDischarge.Value = value;
            }
        }

        protected override bool CheckIsWorking()
        {
			return Enabled && SourceComp.Enabled && SourceComp.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        public MyBatteryBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_isFull = SyncType.CreateAndAddProp<bool>();
            m_onlyRecharge = SyncType.CreateAndAddProp<bool>();
            m_onlyDischarge = SyncType.CreateAndAddProp<bool>();
            m_semiautoEnabled = SyncType.CreateAndAddProp<bool>();
            m_producerEnabled = SyncType.CreateAndAddProp<bool>();
            m_storedPower = SyncType.CreateAndAddProp<float>();
#endif // XB1
            CreateTerminalControls();

            SourceComp = new MyResourceSourceComponent();
            ResourceSink = new MyResourceSinkComponent();
            m_semiautoEnabled.ValueChanged += (x) => UpdateMaxOutputAndEmissivity();
            m_onlyRecharge.ValueChanged += (x) => { if (m_onlyRecharge.Value) m_onlyDischarge.Value = false; UpdateMaxOutputAndEmissivity(); };
            m_onlyDischarge.ValueChanged += (x) => { if (m_onlyDischarge.Value) m_onlyRecharge.Value = false; UpdateMaxOutputAndEmissivity(); };

            m_producerEnabled.ValueChanged += (x) => ProducerEnadChanged();
            m_storedPower.ValueChanged += (x) => CapacityChanged();
	    }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyBatteryBlock>())
                return;
            base.CreateTerminalControls();
            var recharge = new MyTerminalControlCheckbox<MyBatteryBlock>("Recharge", MySpaceTexts.BlockPropertyTitle_Recharge, MySpaceTexts.ToolTipBatteryBlock);
            recharge.Getter = (x) => x.OnlyRecharge;
            recharge.Setter = (x, v) => x.OnlyRecharge = v;
            recharge.Enabled = (x) => !x.SemiautoEnabled && !x.OnlyDischarge;
            recharge.EnableAction();

            var discharge = new MyTerminalControlCheckbox<MyBatteryBlock>("Discharge", MySpaceTexts.BlockPropertyTitle_Discharge, MySpaceTexts.ToolTipBatteryBlock_Discharge);
            discharge.Getter = (x) => x.OnlyDischarge;
            discharge.Setter = (x, v) => x.OnlyDischarge = v;
            discharge.Enabled = (x) => !x.SemiautoEnabled && !x.OnlyRecharge;
            discharge.EnableAction();

            var semiAuto = new MyTerminalControlCheckbox<MyBatteryBlock>("SemiAuto", MySpaceTexts.BlockPropertyTitle_Semiauto, MySpaceTexts.ToolTipBatteryBlock_Semiauto);
            semiAuto.Getter = (x) => x.SemiautoEnabled;
            semiAuto.Setter = (x, v) => x.SemiautoEnabled = v;
            semiAuto.EnableAction();

            MyTerminalControlFactory.AddControl(recharge);
            MyTerminalControlFactory.AddControl(discharge);
            MyTerminalControlFactory.AddControl(semiAuto);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            var sourceDataList = new List<MyResourceSourceInfo>
	        {
		        new MyResourceSourceInfo { ResourceTypeId = MyResourceDistributorComponent.ElectricityId, DefinedOutput = BlockDefinition.MaxPowerOutput, ProductionToCapacityMultiplier = 60*60}
	        };

            SourceComp.Init(BlockDefinition.ResourceSourceGroup, sourceDataList);
            SourceComp.HasCapacityRemainingChanged += (id, source) => UpdateIsWorking();
            SourceComp.ProductionEnabledChanged += Source_ProductionEnabledChanged;

            var batteryBuilder = (MyObjectBuilder_BatteryBlock)objectBuilder;

            SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, batteryBuilder.ProducerEnabled);

            MaxStoredPower = BlockDefinition.MaxStoredPower;

            ResourceSink.Init(
            BlockDefinition.ResourceSinkGroup,
            BlockDefinition.RequiredPowerInput,
            Sink_ComputeRequiredPower);

            base.Init(objectBuilder, cubeGrid);

            ResourceSink.Update();

            MyDebug.AssertDebug(BlockDefinition != null);
            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_BatteryBlock));
	                
            if (batteryBuilder.CurrentStoredPower >= 0)
                CurrentStoredPower = batteryBuilder.CurrentStoredPower;
            else
                CurrentStoredPower = BlockDefinition.InitialStoredPowerRatio * BlockDefinition.MaxStoredPower;

            m_storedPower.Value = CurrentStoredPower;

			
            SemiautoEnabled = batteryBuilder.SemiautoEnabled;
            OnlyRecharge = !batteryBuilder.ProducerEnabled;
            OnlyDischarge = batteryBuilder.OnlyDischargeEnabled;
            UpdateMaxOutputAndEmissivity();

            UpdateText();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            IsWorkingChanged += MyBatteryBlock_IsWorkingChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;

            m_lastUpdateTime = MySession.Static.GameplayFrameCounter;

            if (IsWorking)
                OnStartWorking();
        }

        void MyBatteryBlock_IsWorkingChanged(MyCubeBlock obj)
        {
            UpdateMaxOutputAndEmissivity();
			ResourceSink.Update();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_BatteryBlock)base.GetObjectBuilderCubeBlock(copy);
            ob.CurrentStoredPower = CurrentStoredPower;
			ob.ProducerEnabled = SourceComp.ProductionEnabled;
            ob.SemiautoEnabled = SemiautoEnabled;
            ob.OnlyDischargeEnabled = OnlyDischarge;
            return ob;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        protected override void Closing()
        {
            if (m_soundEmitter != null)
                m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private float Sink_ComputeRequiredPower()
        {
            bool canRecharge = Enabled && IsFunctional && !m_isFull;
            bool shouldRecharge = OnlyRecharge || !OnlyDischarge;
            float inputToFillInUpdateInterval = (MaxStoredPower - CurrentStoredPower) * VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND / m_productionUpdateInterval * SourceComp.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId);
            float currentOutput = SourceComp.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId);

            float requiredInput = 0;
            if (canRecharge && shouldRecharge)
            {
                float maxRequiredInput = ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId);
                requiredInput = Math.Min(inputToFillInUpdateInterval + currentOutput, maxRequiredInput);
            }
            return requiredInput;
        }

        private float ComputeMaxPowerOutput()
        {
            return CheckIsWorking() && SourceComp.ProductionEnabledByType(MyResourceDistributorComponent.ElectricityId) ? BlockDefinition.MaxPowerOutput : 0f;
        }   
        
        private void CalculateOutputTimeRemaining()
        {
			if (CurrentStoredPower != 0 && SourceComp.CurrentOutput != 0)
				TimeRemaining = CurrentStoredPower / (SourceComp.CurrentOutput / SourceComp.ProductionToCapacityMultiplier);
            else
                TimeRemaining = 0;
        }

        private void CalculateInputTimeRemaining()
        {
            if (ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) != 0)
				TimeRemaining = (MaxStoredPower - CurrentStoredPower) / (ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) / SourceComp.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId));
            else
                TimeRemaining = 0;
        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            if (IsFunctional)
            {
                if (SemiautoEnabled)
                {
                    if (CurrentStoredPower == 0)
                    {
                        OnlyRecharge = true;
                        OnlyDischarge = false;
                    }
                    else if (CurrentStoredPower == MaxStoredPower)
                    {
                        OnlyRecharge = false;
                        OnlyDischarge = true;
                    }
                }

                UpdateMaxOutputAndEmissivity();

                float timeDeltaMs = (MySession.Static.GameplayFrameCounter - m_lastUpdateTime) * VRage.Game.MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS * 1000f;
                m_lastUpdateTime = MySession.Static.GameplayFrameCounter;
                if (!MySession.Static.CreativeMode)
                {
                    if ((Sync.IsServer && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
						if (OnlyRecharge)
							StorePower(timeDeltaMs, ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId));
                        else if(OnlyDischarge)
                            ConsumePower(timeDeltaMs, SourceComp.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId));
                        else
                            TransferPower(timeDeltaMs, ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId), SourceComp.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId));
                    }
                }
                else
                {
                    if ((Sync.IsServer && IsFunctional && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                        CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                    {
                        if (OnlyRecharge || !OnlyDischarge)
                        {
                            float powerToStore = (SourceComp.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId) * MaxStoredPower) / 8f;
                            powerToStore *= Enabled && IsFunctional ? 1f : 0f;
                            StorePower(timeDeltaMs, powerToStore);
                        }
                        else
                        {
                            UpdateIsWorking();
                            if (!SourceComp.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId))
                                return;
                            CalculateOutputTimeRemaining();
                        }
                    }
                }
                ResourceSink.Update();

				if (OnlyRecharge)
                    CalculateInputTimeRemaining();
                else if (OnlyDischarge)
                    CalculateOutputTimeRemaining();
                else
                {
                    if(ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId) > SourceComp.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId))
                        CalculateInputTimeRemaining();
                    else
                        CalculateOutputTimeRemaining();
                }
            }
        }

        protected override void OnEnabledChanged()
        {
            UpdateMaxOutputAndEmissivity();
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        private void UpdateMaxOutputAndEmissivity()
        {
            ResourceSink.Update();
			SourceComp.SetMaxOutputByType(MyResourceDistributorComponent.ElectricityId, (ComputeMaxPowerOutput()));
            UpdateEmissivity();
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BatteryBlock));
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.MaxPowerOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxRequiredInput));
            MyValueFormatter.AppendWorkInBestUnit(BlockDefinition.RequiredPowerInput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxStoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(MaxStoredPower, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
			MyValueFormatter.AppendWorkInBestUnit(SourceComp.CurrentOutput, DetailedInfo);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_StoredPower));
            MyValueFormatter.AppendWorkHoursInBestUnit(CurrentStoredPower, DetailedInfo);
            DetailedInfo.Append("\n");
            float currentInput = ResourceSink.CurrentInputByType(MyResourceDistributorComponent.ElectricityId);
            float currentOutput = SourceComp.CurrentOutputByType(MyResourceDistributorComponent.ElectricityId);
			if (currentInput > currentOutput)
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_RechargedIn));
                MyValueFormatter.AppendTimeInBestUnit(m_timeRemaining, DetailedInfo);
            }
            else if(currentInput == currentOutput)
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_DepletedIn));
                MyValueFormatter.AppendTimeInBestUnit(float.PositiveInfinity, DetailedInfo);
            }
            else
            {
                DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_DepletedIn));
                MyValueFormatter.AppendTimeInBestUnit(m_timeRemaining, DetailedInfo);
            }
            RaisePropertiesChanged();
        }

        private void TransferPower(float timeDeltaMs, float input, float output)
        {
            float powerTransfer = input - output;
            if(powerTransfer < 0)
                ConsumePower(timeDeltaMs, -powerTransfer);
            else if(powerTransfer > 0)
                StorePower(timeDeltaMs, powerTransfer);
        }

        private void StorePower(float timeDeltaMs, float input)
        {
            float inputPowerPerMillisecond = input / (SourceComp.ProductionToCapacityMultiplierByType(MyResourceDistributorComponent.ElectricityId) * 1000);
            float increment = (timeDeltaMs * inputPowerPerMillisecond) * 0.80f;

            if ((CurrentStoredPower + increment) < MaxStoredPower)
            {
                CurrentStoredPower += increment;
            }
            else
            {
                CurrentStoredPower = MaxStoredPower;
                TimeRemaining = 0;
                if (!m_isFull)
                {
                    m_isFull.Value = true;
                }
            }

            m_storedPower.Value = CurrentStoredPower;
        }

        private void ConsumePower(float timeDeltaMs, float output)
        {
            if (!SourceComp.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId))
                return;

            float consumptionPerMillisecond = output / (SourceComp.ProductionToCapacityMultiplier * 1000f);
            float consumedPower = timeDeltaMs * consumptionPerMillisecond;

            if (consumedPower == 0)
                return;

            if ((CurrentStoredPower - consumedPower) <= 0)
            {
				SourceComp.SetOutput(0);
                CurrentStoredPower = 0;
                TimeRemaining = 0;
            }
            else
            {
                CurrentStoredPower -= consumedPower;
                if (m_isFull)
                {
                    m_isFull.Value = false;
                }
            }
            m_storedPower.Value = CurrentStoredPower;
        }

        internal void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsFunctional && Enabled)
            {
                if (IsWorking)
                {
                    float percentage = (CurrentStoredPower / MaxStoredPower);

                    if (!OnlyDischarge)
                        SetEmissive(Color.Green, percentage);
                    else
                        SetEmissive(Color.SteelBlue, percentage);
                }
                else
                {
                    SetEmissive(Color.Red, 0.25f);
                }
            }
            else
            {
                SetEmissive(Color.Red, 1f);
            }
        }

        private void SetEmissive(Color color, float fill)
        {
            int fillCount = (int)(fill * m_emissiveNames.Length);

            if (Render.RenderObjectIDs[0] != VRageRender.MyRenderProxy.RENDER_ID_UNASSIGNED && (color != m_prevEmissiveColor || fillCount != m_prevFillCount))
            {
                for (int i = 0; i < m_emissiveNames.Length; i++)
                {
                    if (i < fillCount)
                    {
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], color, 1f);
                    }
                    else
                    {
                        UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[i], Color.Black, 0f);
                    }
                }
                m_prevEmissiveColor = color;
                m_prevFillCount = fillCount;
            }
        }

        public override void OnModelChange()
        {
            base.OnModelChange();

            m_prevFillCount = -1;
        }

        void ComponentStack_IsFunctionalChanged()
        {
            CurrentStoredPower = IsFunctional ? BlockDefinition.InitialStoredPowerRatio * BlockDefinition.MaxStoredPower : 0;
            UpdateMaxOutputAndEmissivity();
        }

        void ProducerEnadChanged()
        {
            SourceComp.SetProductionEnabledByType(MyResourceDistributorComponent.ElectricityId, m_producerEnabled.Value);
        }

        private void Source_ProductionEnabledChanged(MyDefinitionId changedResourceId, MyResourceSourceComponent source)
        {
            Enabled = source.Enabled;
            UpdateIsWorking();
        }

        void CapacityChanged()
        {
            CurrentStoredPower = m_storedPower.Value;
        }
    }
}
