using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Network;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TimerBlock))]
    public class MyTimerBlock : MyFunctionalBlock, IMyTimerBlock, IMyTriggerableBlock
    {
        public MyToolbar Toolbar { get; set; }

        private int m_countdownMsCurrent;
        private int m_countdownMsStart;

        private MySoundPair m_beepStart = MySoundPair.Empty;
        private MySoundPair m_beepMid = MySoundPair.Empty;
        private MySoundPair m_beepEnd = MySoundPair.Empty;
        private MyEntity3DSoundEmitter m_beepEmitter = null;

        readonly Sync<bool> m_isCountingDown;
        public bool IsCountingDown 
        { 
            get
            {
                return m_isCountingDown;
            }
            private set
            {
                m_isCountingDown.Value = value;
            }
        }
        readonly Sync<bool> m_silent;
        public bool Silent
        {
            get
            {
                return m_silent;
            }
            private set
            {
                m_silent.Value = value;
            }
        }

        private static List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;
        bool m_syncing = false;

        readonly Sync<int> m_timerSync;
        public MyTimerBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_isCountingDown = SyncType.CreateAndAddProp<bool>();
            m_silent = SyncType.CreateAndAddProp<bool>();
            m_timerSync = SyncType.CreateAndAddProp<int>();
#endif // XB1
            CreateTerminalControls();

            m_openedToolbars = new List<MyToolbar>();
            m_timerSync.ValueChanged += (x) => TimerChanged();
            m_isCountingDown.ValueChanged += (x) => CountDownChanged();
            m_isCountingDown.ValidateNever();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyTimerBlock>())
                return;
            base.CreateTerminalControls();
            var silent = new MyTerminalControlCheckbox<MyTimerBlock>("Silent", MySpaceTexts.BlockPropertyTitle_Silent, MySpaceTexts.ToolTipTimerBlock_Silent);
            silent.Getter = (x) => x.Silent;
            silent.Setter = (x, v) => x.Silent = v;
            silent.EnableAction();
            MyTerminalControlFactory.AddControl(silent);

            var slider = new MyTerminalControlSlider<MyTimerBlock>("TriggerDelay", MySpaceTexts.TerminalControlPanel_TimerDelay, MySpaceTexts.TerminalControlPanel_TimerDelay);
            slider.SetLogLimits(1, 60 * 60);
            slider.DefaultValue = 10;
            slider.Enabled = (x) => !x.IsCountingDown;
            slider.Getter = (x) => x.TriggerDelay;
            slider.Setter = (x, v) => x.m_timerSync.Value = ((int)(Math.Round(v, 1) * 1000));
            slider.Writer = (x, sb) => MyValueFormatter.AppendTimeExact(Math.Max(x.m_countdownMsStart, 1000) / 1000, sb);
            slider.EnableActions();
            MyTerminalControlFactory.AddControl(slider);

            var toolbarButton = new MyTerminalControlButton<MyTimerBlock>("OpenToolbar", MySpaceTexts.BlockPropertyTitle_TimerToolbarOpen, MySpaceTexts.BlockPropertyTitle_TimerToolbarOpen,
            delegate(MyTimerBlock self)
            {
                m_openedToolbars.Add(self.Toolbar);
                if (MyGuiScreenCubeBuilder.Static == null)
                {
                    m_shouldSetOtherToolbars = true;
                    MyToolbarComponent.CurrentToolbar = self.Toolbar;
                    MyGuiScreenBase screen = MyGuiSandbox.CreateScreen(MyPerGameSettings.GUI.ToolbarConfigScreen, 0, self);
                    MyToolbarComponent.AutoUpdate = false;
                    screen.Closed += (source) =>
                    {
                        MyToolbarComponent.AutoUpdate = true;
                        m_openedToolbars.Clear();
                    };
                    MyGuiSandbox.AddScreen(screen);
                }
            });

            MyTerminalControlFactory.AddControl(toolbarButton);

            var triggerButton = new MyTerminalControlButton<MyTimerBlock>("TriggerNow", MySpaceTexts.BlockPropertyTitle_TimerTrigger, MySpaceTexts.BlockPropertyTitle_TimerTrigger, (x) => x.OnTrigger());
            triggerButton.EnableAction();
            MyTerminalControlFactory.AddControl(triggerButton);

            var startButton = new MyTerminalControlButton<MyTimerBlock>("Start", MySpaceTexts.BlockPropertyTitle_TimerStart, MySpaceTexts.BlockPropertyTitle_TimerStart, (x) => x.StartBtn());
            startButton.EnableAction();
            MyTerminalControlFactory.AddControl(startButton);

            var stopButton = new MyTerminalControlButton<MyTimerBlock>("Stop", MySpaceTexts.BlockPropertyTitle_TimerStop, MySpaceTexts.BlockPropertyTitle_TimerStop, (x) => x.StopBtn());
            stopButton.EnableAction();
            MyTerminalControlFactory.AddControl(stopButton);
        }

        void TimerChanged()
        {
            SetTimer(m_timerSync.Value);
        }

        void CountDownChanged()
        {
            if (m_isCountingDown.Value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        private void StopBtn()
        {
            MyMultiplayer.RaiseEvent(this, x => x.Stop);
        }

        private void StartBtn()
        {
            MyMultiplayer.RaiseEvent(this, x => x.Start);
        }

        [Event, Reliable, Server]
        public void Stop()
        {
            IsCountingDown = false;
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
            this.ClearMemory();
        }

        private void ClearMemory()
        {
            m_countdownMsCurrent = 0;
            UpdateEmissivity();
            DetailedInfo.Clear();
            RaisePropertiesChanged();
        }

        [Event, Reliable, Server]
        public void Start()
        {
            IsCountingDown = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            m_countdownMsCurrent = m_countdownMsStart;
            if (m_beepEmitter != null && Silent == false)
                m_beepEmitter.PlaySound(m_beepStart);
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            if (m_syncing)
            {
                return;
            }

            Debug.Assert(self == Toolbar);

            var tItem = ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex));
            MyMultiplayer.RaiseEvent(this, x => x.SendToolbarItemChanged, tItem, index.ItemIndex);

            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;

                foreach (var toolbar in m_openedToolbars)
                {
                    if (toolbar != self)
                    {
                        toolbar.SetItemAtIndex(index.ItemIndex, self.GetItemAtIndex(index.ItemIndex));
                    }
                }
                m_shouldSetOtherToolbars = true;
            }
        }

        protected override void OnStartWorking()
        {
            base.OnStartWorking();
            if(m_countdownMsCurrent != 0)
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            UpdateEmissivity();
        }

        protected override void OnStopWorking()
        {
            base.OnStopWorking();
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
            UpdateEmissivity();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;

            var timerBlockDefinition = BlockDefinition as MyTimerBlockDefinition;

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                timerBlockDefinition.ResourceSinkGroup,
                0.0000001f,
                () => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0f);
            ResourceSink = sinkComp;

            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_TimerBlock;

            Toolbar = new MyToolbar(MyToolbarType.ButtonPanel, 9, 10);
            Toolbar.Init(ob.Toolbar, this);
            Toolbar.ItemChanged += Toolbar_ItemChanged;

            if (ob.JustTriggered) NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            IsCountingDown = ob.IsCountingDown;
            m_countdownMsStart = ob.Delay;
            m_countdownMsCurrent = ob.CurrentTime;
            Silent = ob.Silent;
    
            if (m_countdownMsCurrent > 0)
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
	       
            ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
			ResourceSink.Update();

			AddDebugRenderComponent(new Sandbox.Game.Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            m_beepStart = new MySoundPair(timerBlockDefinition.TimerSoundStart);
            m_beepMid = new MySoundPair(timerBlockDefinition.TimerSoundMid);
            m_beepEnd = new MySoundPair(timerBlockDefinition.TimerSoundEnd);
            m_beepEmitter = new MyEntity3DSoundEmitter(this);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_TimerBlock;
            ob.Toolbar = Toolbar.GetObjectBuilder();
            ob.JustTriggered = NeedsUpdate.HasFlag(MyEntityUpdateEnum.BEFORE_NEXT_FRAME);
            ob.Delay = m_countdownMsStart;
            ob.CurrentTime = m_countdownMsCurrent;
            ob.IsCountingDown = IsCountingDown;
            ob.Silent = Silent;
            return ob;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            IsCountingDown = false;
            if (Sync.IsServer)
            {
                for (int i = 0; i < Toolbar.ItemCount; ++i)
                {
                    Toolbar.UpdateItem(i);
                    Toolbar.ActivateItemAtIndex(i);
                }

                //Visual scripting action
                if (CubeGrid.Physics != null && MyVisualScriptLogicProvider.TimerBlockTriggered != null)
                    MyVisualScriptLogicProvider.TimerBlockTriggered(CustomName.ToString());
                if (CubeGrid.Physics != null && !string.IsNullOrEmpty(Name) && MyVisualScriptLogicProvider.TimerBlockTriggeredEntityName != null)
                    MyVisualScriptLogicProvider.TimerBlockTriggeredEntityName(Name);
            }
            UpdateEmissivity();
            DetailedInfo.Clear();
            RaisePropertiesChanged();
        }

        public void SetTimer(int p)
        {
            m_countdownMsStart = p;
            RaisePropertiesChanged();
        }

        public override void UpdateAfterSimulation10()
        {
            base.UpdateAfterSimulation10();

            // If it is not working, than it cannot operate
            if (!this.IsWorking)
                return;

            var before = m_countdownMsCurrent % 1000;

            if (m_countdownMsCurrent > 0)
            {
                m_countdownMsCurrent -= (int)(1000 * 1f / 6f);
            }

            var after = m_countdownMsCurrent % 1000;
            if (before > 800 && after <= 800 || before <= 800 && after > 800)
            {
                UpdateEmissivity();
            }

            if (m_countdownMsCurrent <= 0)
            {
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
                m_countdownMsCurrent = 0;
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                if (m_beepEmitter != null && Silent == false)
                    m_beepEmitter.PlaySound(m_beepEnd, true);
            }
            DetailedInfo.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyTitle_TimerToTrigger));
            MyValueFormatter.AppendTimeExact(m_countdownMsCurrent / 1000, DetailedInfo);
            RaisePropertiesChanged();
        }

        public override void UpdateSoundEmitters()
        {
            base.UpdateSoundEmitters();
            if (m_beepEmitter != null)
                m_beepEmitter.Update();
        }

        public void StopCountdown()
        {
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
            m_countdownMsCurrent = 0;
            IsCountingDown = false;
            DetailedInfo.Clear();
            RaisePropertiesChanged();
        }

        protected void OnTrigger()
        {
            if (!IsWorking)
            {
                return;
            }

            StopCountdown();
            if (Sync.IsServer)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            else
            {
                MyMultiplayer.RaiseEvent(this, x => x.Trigger);
            }
        }

        [Event,Reliable,Server]
        protected void Trigger()
        {
            if (!IsWorking)
            {
                return;
            }

            StopCountdown();
            if (Sync.IsServer)
            {
                NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            else
            {
                UpdateEmissivity();
            }
        }

        void IMyTriggerableBlock.Trigger()
        {
            OnTrigger();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

            if (IsWorking)
            {
                if (IsCountingDown)
                {
                    if (m_countdownMsCurrent % 1000 > 800)
                    {
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.SteelBlue, Color.White);
                        if (m_beepEmitter != null && Silent == false)
                            m_beepEmitter.PlaySound(m_beepMid);
                    }
                    else
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.Black, Color.White);
                }
                else
                    MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.ForestGreen, Color.White);
            }
            else
                MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 0.0f, Color.Red, Color.White);
        }

        private void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
            // If no power, memory of the device is wiped.
            if (!ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
            {
                this.ClearMemory();
            }
        }

        protected override bool CheckIsWorking()
        {
            return ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) && base.CheckIsWorking();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            base.OnEnabledChanged();
        }

        public float TriggerDelay
        {
            get { return Math.Max(m_countdownMsStart, 1000) / 1000; }
        }
        bool ModAPI.Ingame.IMyTimerBlock.IsCountingDown { get { return IsCountingDown; } }
        float ModAPI.Ingame.IMyTimerBlock.TriggerDelay { get { return TriggerDelay; } }

        [Event, Reliable, Server, Broadcast]
        void SendToolbarItemChanged(ToolbarItem sentItem, int index)
        {
            m_syncing = true;
            MyToolbarItem item = null;
            if (sentItem.EntityID != 0)
            {
                item = ToolbarItem.ToItem(sentItem);
            }
            Toolbar.SetItemAtIndex(index, item);
            m_syncing = false;
        }

    }
}
