using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using VRage;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TimerBlock))]
    internal class MyTimerBlock : MyFunctionalBlock, IMyTimerBlock
    {
        [PreloadRequired]
        internal class MySyncTimerBlock : MySyncEntity
        {
            [MessageIdAttribute(2457, P2PMessageEnum.Reliable)]
            protected struct TriggerMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }
            }

            [MessageIdAttribute(2458, P2PMessageEnum.Reliable)]
            protected struct ToggleMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit Start;
            }

            [MessageIdAttribute(2459, P2PMessageEnum.Reliable)]
            protected struct SetTimerMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public int Time;
            }

            [ProtoContract]
            [MessageIdAttribute(2460, P2PMessageEnum.Reliable)]
            protected struct ChangeToolbarItemMsg : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public ToolbarItem Item;

                [ProtoMember]
                public int Index;
            }

            private bool m_syncing;
            public bool IsSyncing
            {
                get { return m_syncing; }
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index)
            {
                if (m_syncing)
                    return;
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_timer.EntityId;
                msg.Item = item;
                msg.Index = index;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            private static void OnToolbarItemChanged(MySyncTimerBlock sync, ref ChangeToolbarItemMsg msg, MyNetworkClient sender)
            {
                sync.m_syncing = true;
                MyToolbarItem item = null;
                if (msg.Item.EntityID != 0)
                    item = ToolbarItem.ToItem(msg.Item);
                sync.m_timer.Toolbar.SetItemAtIndex(msg.Index, item);
                sync.m_syncing = false;
                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAll(ref msg);
            }

            static MySyncTimerBlock()
            {
                MySyncLayer.RegisterEntityMessage<MySyncTimerBlock, TriggerMsg>(OnTriggered, MyMessagePermissions.ToServer| MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncTimerBlock, ToggleMsg>(OnToggle, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncTimerBlock, SetTimerMsg>(OnSetTimer, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
                MySyncLayer.RegisterEntityMessage<MySyncTimerBlock, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.ToServer | MyMessagePermissions.FromServer);
            }

            private MyTimerBlock m_timer;

            public MySyncTimerBlock(MyTimerBlock timer)
                : base(timer)
            {
                m_timer = timer;
            }

            public void Trigger()
            {
                Debug.Assert(!Sync.IsServer);

                TriggerMsg msg = new TriggerMsg();
                msg.EntityId = m_timer.EntityId;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            public void Toggle(bool start)
            {
                var msg = new ToggleMsg();
                msg.EntityId = m_timer.EntityId;
                msg.Start = start;

                Sync.Layer.SendMessageToServer(ref msg);
            }

            public void SetTimer(int p)
            {
                var msg = new SetTimerMsg();
                msg.EntityId = m_timer.EntityId;
                msg.Time = p;

                Sync.Layer.SendMessageToServer(ref msg);

                m_timer.SetTimer(p);
            }

            private static void OnTriggered(MySyncTimerBlock syncObject, ref TriggerMsg msg, MyNetworkClient sender)
            {
                MyTimerBlock.Trigger(syncObject.m_timer);
                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnToggle(MySyncTimerBlock sync, ref ToggleMsg msg, MyNetworkClient sender)
            {
                if (msg.Start)
                    sync.m_timer.Start();
                else
                    sync.m_timer.Stop();
                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAll(ref msg);
            }
           
            private static void OnSetTimer(MySyncTimerBlock sync, ref SetTimerMsg msg, MyNetworkClient sender)
            {
                sync.m_timer.SetTimer(msg.Time);
                if (Sync.IsServer)
                    Sync.Layer.SendMessageToAll(ref msg);
            }

        }

        public MyToolbar Toolbar { get; set; }
        protected MySyncTimerBlock TimerSyncObject { get { return SyncObject as MySyncTimerBlock; } }

        private int m_countdownMsCurrent;
        private int m_countdownMsStart;
        public bool IsCountingDown { get; private set; }

        private static readonly List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;

        static MyTimerBlock()
        {
            m_openedToolbars = new List<MyToolbar>();

            var slider = new MyTerminalControlSlider<MyTimerBlock>("TriggerDelay", MySpaceTexts.TerminalControlPanel_TimerDelay, MySpaceTexts.TerminalControlPanel_TimerDelay);
            slider.SetLogLimits(1, 60 * 60);
            slider.DefaultValue = 10;
            slider.Enabled = (x) => !x.IsCountingDown;
            slider.Getter = (x) => x.TriggerDelay;
            slider.Setter = (x, v) => x.TimerSyncObject.SetTimer((int)(Math.Round(v, 1) * 1000));
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

            var triggerButton = new MyTerminalControlButton<MyTimerBlock>("TriggerNow", MySpaceTexts.BlockPropertyTitle_TimerTrigger, MySpaceTexts.BlockPropertyTitle_TimerTrigger, OnTrigger);
            triggerButton.EnableAction();
            MyTerminalControlFactory.AddControl(triggerButton);
            
            var startButton = new MyTerminalControlButton<MyTimerBlock>("Start", MySpaceTexts.BlockPropertyTitle_TimerStart, MySpaceTexts.BlockPropertyTitle_TimerStart, StartBtn);
            startButton.EnableAction();
            MyTerminalControlFactory.AddControl(startButton);

            var stopButton = new MyTerminalControlButton<MyTimerBlock>("Stop", MySpaceTexts.BlockPropertyTitle_TimerStop, MySpaceTexts.BlockPropertyTitle_TimerStop, StopBtn);
            stopButton.EnableAction();
            MyTerminalControlFactory.AddControl(stopButton);
        }

        private static void StopBtn(MyTimerBlock obj)
        {
            if (!obj.IsWorking)
                return;
            obj.Stop();
            obj.TimerSyncObject.Toggle(false);
        }

        private static void StartBtn(MyTimerBlock obj)
        {
            if (!obj.IsWorking)
                return;
            obj.Start();
            obj.TimerSyncObject.Toggle(true);
        }

        public void Stop()
        {
            IsCountingDown = false;
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
            m_countdownMsCurrent = 0;
            UpdateEmissivity();
            DetailedInfo.Clear();
            RaisePropertiesChanged();
        }

        public void Start()
        {
            IsCountingDown = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            m_countdownMsCurrent = m_countdownMsStart;
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            Debug.Assert(self == Toolbar);

            var tItem = ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex));
            (SyncObject as MySyncTimerBlock).SendToolbarItemChanged(tItem, index.ItemIndex);
            
            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;
                if (!(SyncObject as MySyncTimerBlock).IsSyncing)
                {
                    foreach (var toolbar in m_openedToolbars)
                    {
                        if (toolbar != self)
                        {
                            toolbar.SetItemAtIndex(index.ItemIndex, self.GetItemAtIndex(index.ItemIndex));
                        }
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

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncTimerBlock(this);
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(objectBuilder, cubeGrid);

            var ob = objectBuilder as MyObjectBuilder_TimerBlock;

            Toolbar = new MyToolbar(MyToolbarType.ButtonPanel, 9, 10);
            Toolbar.Init(ob.Toolbar, this);
            Toolbar.ItemChanged += Toolbar_ItemChanged;

            if (ob.JustTriggered) NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            m_countdownMsStart = ob.Delay;
            m_countdownMsCurrent = ob.CurrentTime;
            if (m_countdownMsCurrent > 0)
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

	        var timerBlockDefinition = BlockDefinition as MyTimerBlockDefinition;

			var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                timerBlockDefinition.ResourceSinkGroup,
                0.0000001f,
				() => (Enabled && IsFunctional) ? ResourceSink.MaxRequiredInput : 0f);
	        ResourceSink = sinkComp;
			ResourceSink.Update();

			AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawPowerReciever(ResourceSink, this));

			ResourceSink.IsPoweredChanged += Receiver_IsPoweredChanged;
            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_TimerBlock;
            ob.Toolbar = Toolbar.GetObjectBuilder();
            ob.JustTriggered = NeedsUpdate.HasFlag(MyEntityUpdateEnum.BEFORE_NEXT_FRAME);
            ob.Delay = m_countdownMsStart;
            ob.CurrentTime = m_countdownMsCurrent;
            return ob;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            if (Sync.IsServer)
            {
                for (int i = 0; i < Toolbar.ItemCount; ++i)
                {
                    Toolbar.UpdateItem(i);
                    Toolbar.ActivateItemAtIndex(i);
                }
            }
            IsCountingDown = false;
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
            }
            DetailedInfo.Clear().AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyTitle_TimerToTrigger));
            MyValueFormatter.AppendTimeExact(m_countdownMsCurrent / 1000, DetailedInfo);
            RaisePropertiesChanged();
        }

        public void StopCountdown()
        {
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
            m_countdownMsCurrent = 0;
            IsCountingDown = false;
            DetailedInfo.Clear();
            RaisePropertiesChanged();
        }

        protected static void OnTrigger(MyTimerBlock obj)
        {
            if (!obj.IsWorking)
            {
                return;
            }

            obj.StopCountdown();
            if (Sync.IsServer)
            {
                obj.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            else
            {
                obj.TimerSyncObject.Trigger();
            }
        }

        protected static void Trigger(MyTimerBlock obj)
        {
            if (!obj.IsWorking)
            {
                return;
            }

            obj.StopCountdown();
            if (Sync.IsServer)
            {
                obj.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
            else
            {
                obj.UpdateEmissivity();
            }
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
                        MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, Color.SteelBlue, Color.White);
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
        }

        protected override bool CheckIsWorking()
        {
			return ResourceSink.IsPowered && base.CheckIsWorking();
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
        bool IMyTimerBlock.IsCountingDown { get { return IsCountingDown; } }
        float IMyTimerBlock.TriggerDelay { get { return TriggerDelay; } }
    }
}
