using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Havok;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Physics;
using Sandbox.Graphics.TransparentGeometry.Particles;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.World;

using VRage.Trace;
using VRageMath;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.ModAPI;
using Sandbox.Game.GameSystems.Conveyors;
using System.Reflection;
using Sandbox.Common;
using Sandbox.Game.GameSystems;
using VRage;
using Sandbox.Game.Localization;
using VRage.Audio;
using VRage.Utils;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_Reactor))]
    class MyReactor : MyFunctionalBlock, IMyPowerProducer, IMyInventoryOwner, IMyConveyorEndpointBlock, IMyReactor
    {
        static MyReactor()
        {
            var useConveyorSystem = new MyTerminalControlOnOffSwitch<MyReactor>("UseConveyor", MySpaceTexts.Terminal_UseConveyorSystem);
            useConveyorSystem.Getter = (x) => (x as IMyInventoryOwner).UseConveyorSystem;
            useConveyorSystem.Setter = (x, v) => MySyncConveyors.SendChangeUseConveyorSystemRequest(x.EntityId, v);
            useConveyorSystem.EnableToggleAction();
            MyTerminalControlFactory.AddControl(useConveyorSystem);
        }

        private MyReactorDefinition m_reactorDefinition;
        private MyInventory m_inventory;
        private bool m_hasRemainingCapacity;
        private float m_maxPowerOutput;
        private float m_currentPowerOutput;
        //private MyParticleEffect m_damageEffect;// = new MyParticleEffect();

        protected override bool CheckIsWorking()
        {
            return HasCapacityRemaining && base.CheckIsWorking();
        }

        public MyReactor()
        {
            m_soundEmitter = new MyEntity3DSoundEmitter(this);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {             
            base.Init(objectBuilder, cubeGrid);

            MyDebug.AssertDebug(BlockDefinition.Id.TypeId == typeof(MyObjectBuilder_Reactor));
            m_reactorDefinition = BlockDefinition as MyReactorDefinition;
            MyDebug.AssertDebug(m_reactorDefinition != null);

            CurrentPowerOutput = 0;
            m_inventory = new MyInventory(m_reactorDefinition.InventoryMaxVolume, m_reactorDefinition.InventorySize, MyInventoryFlags.CanReceive, this);

            var obGenerator = (MyObjectBuilder_Reactor)objectBuilder;
            m_inventory.Init(obGenerator.Inventory);
            m_inventory.ContentsChanged += inventory_ContentsChanged;
            m_inventory.Constraint = m_reactorDefinition.InventoryConstraint;
            RefreshRemainingCapacity();

            UpdateText();

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            //if (MyFakes.SHOW_DAMAGE_EFFECTS && IsWorking)
            //    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;

            m_baseIdleSound = BlockDefinition.PrimarySound;

            m_useConveyorSystem = obGenerator.UseConveyorSystem;

            if (IsWorking)
                OnStartWorking();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_Reactor)base.GetObjectBuilderCubeBlock(copy);
            ob.Inventory = m_inventory.GetObjectBuilder();
            ob.UseConveyorSystem = m_useConveyorSystem;
            return ob;
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        protected override void Closing()
        {
            m_soundEmitter.StopSound(true);
            base.Closing();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            /*if (m_damageEffect != null)
            {
                m_damageEffect.WorldMatrix = WorldMatrix;
                Vector3 col = new Vector3(106, 153, 77) / 255.0f;
                m_damageEffect.UserColorMultiplier = new Vector4(col * 4, 6);
            }*///TODO
            }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            /*if (MyFakes.SHOW_DAMAGE_EFFECTS)
            {
                if (SlimBlock.ComponentStack.IsFunctional && m_damageEffect != null)
                {
                    m_damageEffect.Stop();
                    m_damageEffect = null;
                    NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
                }

                if (!SlimBlock.ComponentStack.IsFunctional && m_damageEffect == null)
                {
                    if (MyParticlesManager.TryCreateParticleEffect((int)MyParticleEffectsIDEnum.Prefab_LeakingSteamWhite, out m_damageEffect))
                    {
                        m_damageEffect.UserScale = 0.2f;
                        m_damageEffect.WorldMatrix = WorldMatrix;
                        m_damageEffect.OnDelete += damageEffect_OnDelete;
                    }
                    NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
                }
            }*/

            // Will not consume anything when disabled.
            int timeDelta = 100 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS;
            if (Enabled && !MySession.Static.CreativeMode)
            {
                ProfilerShort.Begin("ConsumeCondition");
                if ((Sync.IsServer && IsWorking && !CubeGrid.GridSystems.ControlSystem.IsControlled) ||
                    CubeGrid.GridSystems.ControlSystem.IsLocallyControlled)
                {
                    ProfilerShort.Begin("ConsumeFuel");
                    ConsumeFuel(timeDelta);
                    ProfilerShort.End();
                }
                ProfilerShort.End();
            }

            if (Sync.IsServer && IsFunctional && m_useConveyorSystem && m_inventory.VolumeFillFactor < 0.6f)
            {
                float consumptionPerSecond = m_reactorDefinition.MaxPowerOutput / (60 * 60);
                var consumedUranium = (60 * consumptionPerSecond); // Take enough uranium for one minute of operation
                consumedUranium /= m_reactorDefinition.FuelDefinition.Mass; // Convert weight to number of items
                ProfilerShort.Begin("PullRequest");
                MyGridConveyorSystem.ItemPullRequest(this, m_inventory, OwnerId, m_reactorDefinition.FuelId, (MyFixedPoint)consumedUranium);
                ProfilerShort.End();
            }
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        protected override void OnEnabledChanged()
        {
            UpdateMaxOutputAndEmissivity();

            base.OnEnabledChanged();
        }

        private void UpdateMaxOutputAndEmissivity()
        {
            MaxPowerOutput = ComputeMaxPowerOutput();
            UpdateEmissivity();
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_MaxOutput));
            MyValueFormatter.AppendWorkInBestUnit(m_reactorDefinition.MaxPowerOutput * m_powerOutputMultiplier, DetailedInfo);
            DetailedInfo.Append("\n");
            if (IsFunctional) DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentOutput));
            MyValueFormatter.AppendWorkInBestUnit(CurrentPowerOutput, DetailedInfo);
            RaisePropertiesChanged();
        }

        private void ConsumeFuel(int timeDelta)
        {
            RefreshRemainingCapacity();
            if (!HasCapacityRemaining)
                return;
            if (CurrentPowerOutput == 0.0f)
                return;

            float consumptionPerMillisecond = CurrentPowerOutput / (60 * 60 * 1000);
            consumptionPerMillisecond /= m_reactorDefinition.FuelDefinition.Mass; // Convert weight to number of items

            MyFixedPoint consumedFuel = (MyFixedPoint)(timeDelta * consumptionPerMillisecond);
            if (consumedFuel == 0)
            {
                consumedFuel = MyFixedPoint.SmallestPossibleValue;
            }

            if (m_inventory.ContainItems(consumedFuel, m_reactorDefinition.FuelId))
            {
                m_inventory.RemoveItemsOfType(consumedFuel, m_reactorDefinition.FuelId);
            }
            else if (MyFakes.ENABLE_INFINITE_REACTOR_FUEL)
            {
                m_inventory.AddItems((MyFixedPoint)(200 / m_reactorDefinition.FuelDefinition.Mass), m_reactorDefinition.FuelItem);
            }
            else
            {
                var amountAvailable = m_inventory.GetItemAmount(m_reactorDefinition.FuelId);
                m_inventory.RemoveItemsOfType(amountAvailable, m_reactorDefinition.FuelId);
            }

            //RefreshRemainingCapacity();
        }

        private void RefreshRemainingCapacity()
        {
            var amount = m_inventory.GetItemAmount(m_reactorDefinition.FuelId);
            RemainingCapacity = (float)amount * m_reactorDefinition.FuelDefinition.Mass;
            HasCapacityRemaining = (amount > 0) || MyFakes.ENABLE_INFINITE_REACTOR_FUEL || MySession.Static.CreativeMode;
            UpdateMaxOutputAndEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsWorking)
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Green, Color.White);
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        private float ComputeMaxPowerOutput()
        {
            return IsWorking ? m_reactorDefinition.MaxPowerOutput * m_powerOutputMultiplier : 0f;
        }

        #region IMyPowerProducer

        public event Action<IMyPowerProducer> MaxPowerOutputChanged;
        public event Action<IMyPowerProducer> HasCapacityRemainingChanged;
        private bool m_useConveyorSystem;
        private IMyConveyorEndpoint m_multilineConveyorEndpoint;

        bool IMyPowerProducer.Enabled
        {
            get { return base.Enabled; }
            set { base.Enabled = value; }
        }

        public bool HasCapacityRemaining
        {
            get { return m_hasRemainingCapacity; }
            private set
            {
                if (m_hasRemainingCapacity != value)
                {
                    m_hasRemainingCapacity = value;
                    UpdateIsWorking();
                    if (HasCapacityRemainingChanged != null)
                        HasCapacityRemainingChanged(this);
                }
            }
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

        public float CurrentPowerOutput
        {
            get { return m_currentPowerOutput; }
            set
            {
                MyDebug.AssertRelease(!float.IsNaN(value), "Reactor Power Output is NaN.");
                MyDebug.AssertDebug(value <= MaxPowerOutput && value >= 0.0f);

                MathHelper.Clamp(value, 0.0f, MaxPowerOutput);
                m_currentPowerOutput = value;
                UpdateText();
                if ((m_soundEmitter.Sound != null) && (m_soundEmitter.Sound.IsPlaying))
                {
                    if (MaxPowerOutput != 0f)
                    {
                        float semitones = 4f * (m_currentPowerOutput - 0.5f * MaxPowerOutput) / MaxPowerOutput;
                        m_soundEmitter.Sound.FrequencyRatio = MyAudio.Static.SemitonesToFrequencyRatio(semitones);
                    }
                    else
                        m_soundEmitter.Sound.FrequencyRatio = 1f;
                }
            }
        }

        public float RemainingCapacity { get; private set; }

        MyProducerGroupEnum IMyPowerProducer.Group
        {
            get { return MyProducerGroupEnum.Reactors; }
        }

        #endregion

        #region IMyInventoryOwner

        public int InventoryCount { get { return 1; } }

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Energy; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return m_useConveyorSystem;
            }
            set
            {
                m_useConveyorSystem = value;
            }
        }

        bool ModAPI.Interfaces.IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return (this as IMyInventoryOwner).UseConveyorSystem;
            }
            set
            {
                (this as IMyInventoryOwner).UseConveyorSystem = value;
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        #endregion

        /*void damageEffect_OnDelete(object sender, EventArgs e)
        {
            m_damageEffect = null;
        }*/

        void inventory_ContentsChanged(MyInventory obj)
        {
            var before = IsWorking;
            RefreshRemainingCapacity();
            if (!before && IsWorking)
                OnStartWorking();
            else if (before && !IsWorking)
                OnStopWorking();
        }

        void ComponentStack_IsFunctionalChanged()
        {
            UpdateMaxOutputAndEmissivity();
            if (IsWorking)
                OnStartWorking();
            else
                OnStopWorking();
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_multilineConveyorEndpoint; }
        }

        public void InitializeConveyorEndpoint()
        {
            m_multilineConveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_multilineConveyorEndpoint));
        }
        bool Sandbox.ModAPI.Ingame.IMyReactor.UseConveyorSystem { get { return (this as IMyInventoryOwner).UseConveyorSystem; } }

        private float m_powerOutputMultiplier = 1f;
        float Sandbox.ModAPI.IMyReactor.PowerOutputMultiplier
        {
            get
            {
                return m_powerOutputMultiplier;
            }
            set
            {
                m_powerOutputMultiplier = value;
                if (m_powerOutputMultiplier < 0.01f)
                {
                    m_powerOutputMultiplier = 0.01f;
                }

                MaxPowerOutput = ComputeMaxPowerOutput();

                UpdateText();
            }
        }
    }
}
