using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Sandbox.Common;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Multiplayer;
using System.Diagnostics;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Engine.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.Components;
using Sandbox.Game.Localization;
using VRage.Network;
using VRage.Library.Sync;
using VRage;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TerminalBlock))]
    public partial class MyTerminalBlock : MyCubeBlock, IMyEventProxy
    {
        static MyTerminalBlock()
        {
            var show = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowInTerminal", MySpaceTexts.Terminal_ShowInTerminal, MySpaceTexts.Terminal_ShowInTerminalToolTip);
            show.Getter = (x) => x.m_showInTerminal;
            show.Setter = (x, v) => x.RequestShowInTerminal(v);
            MyTerminalControlFactory.AddControl(show);

            var showConfig = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowInToolbarConfig", MySpaceTexts.Terminal_ShowInToolbarConfig, MySpaceTexts.Terminal_ShowInToolbarConfigToolTip);
            showConfig.Getter = (x) => x.m_showInToolbarConfig;
            showConfig.Setter = (x, v) => x.RequestShowInToolbarConfig(v);
            MyTerminalControlFactory.AddControl(showConfig);

            var customName = new MyTerminalControlTextbox<MyTerminalBlock>("Name", MySpaceTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => MySyncBlockHelpers.SendChangeNameRequest(x, v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);

            var onOffSwitch = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowOnHUD", MySpaceTexts.Terminal_ShowOnHUD, MySpaceTexts.Terminal_ShowOnHUDToolTip);
            onOffSwitch.Getter = (x) => x.ShowOnHUD;
            onOffSwitch.Setter = (x, v) => x.RequestShowOnHUD(v);
            MyTerminalControlFactory.AddControl(onOffSwitch);
        }

        private bool m_showOnHUD;
        private bool m_showInTerminal;
        private bool m_showInToolbarConfig;

        /// <summary>
        /// Name in terminal
        /// </summary>
        public StringBuilder CustomName { get; private set; }

        public StringBuilder CustomNameWithFaction { get; private set; }

        public bool ShowOnHUD
        {
            get { return m_showOnHUD; }
            set
            {
                if (m_showOnHUD != value)
                {
                    m_showOnHUD = value;
                    RaiseShowOnHUDChanged();
                }
            }
        }

        public bool ShowInTerminal
        {
            get { return m_showInTerminal; }
            set
            {
                if (m_showInTerminal != value)
                {
                    m_showInTerminal = value;
                    RaiseShowInTerminalChanged();
                }
            }
        }

        public bool ShowInToolbarConfig
        {
            get { return m_showInToolbarConfig; }
            set
            {
                if (m_showInToolbarConfig != value)
                {
                    m_showInToolbarConfig = value;
                    RaiseShowInToolbarConfigChanged();
                }
            }
        }

        public bool IsAccessibleForProgrammableBlock = true;

        public void RequestShowOnHUD(bool enable)
        {
            MySyncBlockHelpers.SendShowOnHUDRequest(this, enable);
        }

        public void RequestShowInTerminal(bool enable)
        {
            MySyncBlockHelpers.SendShowInTerminalRequest(this, enable);
        }

        public void RequestShowInToolbarConfig(bool enable)
        {
            MySyncBlockHelpers.SendShowInToolbarConfigRequest(this, enable);
        }

        /// <summary>
        /// Detailed text in terminal (on right side)
        /// </summary>
        public StringBuilder DetailedInfo { get; private set; }

        /// <summary>
        /// Moddable part of detailed text in terminal.
        /// </summary>
        public StringBuilder CustomInfo { get; private set; }

        public event Action<MyTerminalBlock> CustomNameChanged;
        public event Action<MyTerminalBlock> PropertiesChanged;
        public event Action<MyTerminalBlock> OwnershipChanged;
        public event Action<MyTerminalBlock> VisibilityChanged;
        public event Action<MyTerminalBlock> ShowOnHUDChanged;
        public event Action<MyTerminalBlock> ShowInTerminalChanged;
        public event Action<MyTerminalBlock> ShowInToolbarConfigChanged;
        public event Action<MyTerminalBlock, StringBuilder> AppendingCustomInfo;

        public event Action<SyncBase> SyncPropertyChanged
        {
            add { SyncType.PropertyChanged += value; }
            remove { SyncType.PropertyChanged -= value; }
        }

        public SyncType SyncType;

        public MyTerminalBlock()
        {
            CustomName = new StringBuilder();
            DetailedInfo = new StringBuilder();
            CustomInfo = new StringBuilder();
            CustomNameWithFaction = new StringBuilder();

            SyncType = SyncHelpers.Compose(this);
            SyncType.PropertyChanged += sync => RaisePropertiesChanged();
        }

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            var ob = (MyObjectBuilder_TerminalBlock)objectBuilder;

            if (ob.CustomName != null)
            {
                CustomName.Clear().Append(ob.CustomName);
                DisplayNameText = ob.CustomName;
            }
            else
            {
                CustomName.Clear();
                GetTerminalName(CustomName);
            }

            ShowOnHUD = ob.ShowOnHUD;
            ShowInTerminal = ob.ShowInTerminal;
            ShowInToolbarConfig = ob.ShowInToolbarConfig;
            AddDebugRenderComponent(new MyDebugRenderComponentTerminal(this));
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = (MyObjectBuilder_TerminalBlock)base.GetObjectBuilderCubeBlock(copy);
            ob.CustomName = (DisplayNameText.CompareTo(BlockDefinition.DisplayNameText) != 0) ? DisplayNameText.ToString() : null;
            ob.ShowOnHUD = ShowOnHUD;
            ob.ShowInTerminal = ShowInTerminal;
            ob.ShowInToolbarConfig = ShowInToolbarConfig;
            return ob;
        }

        public void NotifyTerminalValueChanged(ITerminalControl control)
        {
            // Value in terminal screen was change through GUI
        }

        public void RefreshCustomInfo()
        {
            CustomInfo.Clear();
            var handler = AppendingCustomInfo;
            if (handler != null)
            {
                handler(this, CustomInfo);
            }
        }

        public void SetCustomName(string text)
        {
            MySyncBlockHelpers.SendChangeNameRequest(this, text);
        }

        public void UpdateCustomName(string text)
        {
            if (CustomName.CompareUpdate(text))
            {
                RaiseCustomNameChanged();
                RaiseShowOnHUDChanged();
                DisplayNameText = text;
            }
        }
        public void SetCustomName(StringBuilder text)
        {
            MySyncBlockHelpers.SendChangeNameRequest(this, text);
        }

        public void UpdateCustomName(StringBuilder text)
        {
            if (CustomName.CompareUpdate(text))
            {
                RaiseCustomNameChanged();
                RaiseShowOnHUDChanged();
                DisplayNameText = text.ToString();
            }
        }

        /// <summary>
        /// Call this when you change the name
        /// </summary>
        private void RaiseCustomNameChanged()
        {
            var handler = CustomNameChanged;
            if (handler != null) handler(this);
        }



        /// <summary>
        /// Call this when you change detailed info or other terminal properties
        /// </summary>
        protected void RaisePropertiesChanged()
        {
            var handler = PropertiesChanged;
            if (handler != null) handler(this);
        }

        /// <summary>
        /// Call this when you change the properties that modify the visibility of this block's controls
        /// </summary>
        protected void RaiseVisibilityChanged()
        {
            var handler = VisibilityChanged;
            if (handler != null) handler(this);
        }

        protected void RaiseShowOnHUDChanged()
        {
            var handler = ShowOnHUDChanged;
            if (handler != null) handler(this);
        }

        protected void RaiseShowInTerminalChanged()
        {
            var handler = ShowInTerminalChanged;
            if (handler != null) handler(this);
        }

        protected void RaiseShowInToolbarConfigChanged()
        {
            var handler = ShowInToolbarConfigChanged;
            if (handler != null) handler(this);
        }

        public bool HasLocalPlayerAccess()
        {
            return HasPlayerAccess(MySession.LocalPlayerId);
        }

        public virtual bool HasPlayerAccess(long playerId)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return true;

            MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner(playerId);

            bool accessAllowed = relation.IsFriendly();
            return accessAllowed;
        }

        public override List<MyHudEntityParams> GetHudParams(bool allowBlink)
        {
            CustomNameWithFaction.Clear();

            if (!string.IsNullOrEmpty(GetOwnerFactionTag()))
            {
                CustomNameWithFaction.Append(GetOwnerFactionTag());
                CustomNameWithFaction.Append(".");
            }

            CustomNameWithFaction.AppendStringBuilder(CustomName);

            m_hudParams.Clear();
            m_hudParams.Add(new MyHudEntityParams()
            {
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_ALL,
                Text = CustomNameWithFaction,
                OffsetText = true,
                TargetMode = GetPlayerRelationToOwner(),
                Entity = this,
                Parent = CubeGrid,
                RelativePosition = Vector3.Transform(PositionComp.GetPosition(), CubeGrid.PositionComp.WorldMatrixNormalizedInv),
                BlinkingTime = allowBlink && IsBeingHacked ? MyGridConstants.HACKING_INDICATION_TIME_MS / 1000 : 0
            });

            return m_hudParams;
        }

        protected override void OnOwnershipChanged()
        {
            base.OnOwnershipChanged();

            RaiseOwnershipChanged();
            RaiseShowOnHUDChanged();
            RaisePropertiesChanged();
        }

        private void RaiseOwnershipChanged()
        {
            if (OwnershipChanged != null)
                OwnershipChanged(this);
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            CustomName.Clear();
            GetTerminalName(CustomName);
        }
    }
}
