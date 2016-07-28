using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Network;
using VRageMath;
using VRageRender;
using VRage.ModAPI;
using VRage.Game;
using VRage.Game.Gui;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_TerminalBlock))]
    public partial class MyTerminalBlock : MySyncedBlock
    {
        private Sync<bool> m_showOnHUD;
        private Sync<bool> m_showInTerminal;
        private Sync<bool> m_showInToolbarConfig;

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
                    m_showOnHUD.Value = value;
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
                    m_showInTerminal.Value = value;
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
                    m_showInToolbarConfig.Value = value;
                    RaiseShowInToolbarConfigChanged();
                }
            }
        }

        public bool IsAccessibleForProgrammableBlock = true;

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
        
        public MyTerminalBlock()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_showOnHUD = SyncType.CreateAndAddProp<bool>();
            m_showInTerminal = SyncType.CreateAndAddProp<bool>();
            m_showInToolbarConfig = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            DetailedInfo = new StringBuilder();
            CustomInfo = new StringBuilder();
            CustomNameWithFaction = new StringBuilder();
            
            CustomName = new StringBuilder();

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
            UpdateCustomName(text);
            MyMultiplayer.RaiseEvent(this, x => x.SetCustomNameEvent, text);
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
            UpdateCustomName(text);
            MyMultiplayer.RaiseEvent(this, x => x.SetCustomNameEvent,text.ToString());
        }

        [Event, Reliable, Server, BroadcastExcept]
        public void SetCustomNameEvent(String name)
        {
            UpdateCustomName(name);
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
        public void RaisePropertiesChanged()
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
            return HasPlayerAccess(MySession.Static.LocalPlayerId);
        }

        public virtual bool HasPlayerAccess(long playerId)
        {
            if (!MyFakes.SHOW_FACTIONS_GUI)
                return true;

            VRage.Game.MyRelationsBetweenPlayerAndBlock relation = GetUserRelationToOwner(playerId);

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

        #region Fixing inventory

        protected void FixSingleInventory()
        {
            MyInventoryBase inventoryBase;
            if (!Components.TryGet<MyInventoryBase>(out inventoryBase))
                return;
            MyInventoryAggregate aggregate = inventoryBase as MyInventoryAggregate;
            MyInventory bestInventory = null;
            if (aggregate != null)
            {
                foreach (var inventory in aggregate.ChildList.Reader)
                {
                    var myInventory = inventory as MyInventory;
                    if (myInventory == null)
                        continue;
                    if (bestInventory == null)
                    {
                        bestInventory = myInventory;
                    }
                    else if (bestInventory.GetItemsCount() < myInventory.GetItemsCount())
                    {
                        bestInventory = myInventory;
                    }
                }
            }
            if (bestInventory != null)
            {
                Components.Remove<MyInventoryBase>();
                Components.Add<MyInventoryBase>(bestInventory);
            }
        }

        #endregion
        
        /// <summary>
        /// Control creation was moved from the static ctor into this static function.  Control creation should still be static, but static ctors
        /// only ever get called once, which means we can never modify these controls (remove), since they will be removed forever.  All classes
        /// that inherit MyTerminalBlock should put terminal control creation in a function called CreateTerminalControls, as MyTerminalControlFactory 
        /// will properly ensure their base classes' controls are added in.  I can't make this virtual because terminal controls don't deal with instances
        /// directly (this should probably change)
        /// </summary>
        static void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyTerminalBlock>())
                return;

            var show = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowInTerminal", MySpaceTexts.Terminal_ShowInTerminal, MySpaceTexts.Terminal_ShowInTerminalToolTip);
            show.Getter = (x) => x.m_showInTerminal;
            show.Setter = (x, v) => x.ShowInTerminal = v;
            MyTerminalControlFactory.AddControl(show);

            var showConfig = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowInToolbarConfig", MySpaceTexts.Terminal_ShowInToolbarConfig, MySpaceTexts.Terminal_ShowInToolbarConfigToolTip);
            showConfig.Getter = (x) => x.m_showInToolbarConfig;
            showConfig.Setter = (x, v) => x.ShowInToolbarConfig = v;
            MyTerminalControlFactory.AddControl(showConfig);

            var customName = new MyTerminalControlTextbox<MyTerminalBlock>("Name", MyCommonTexts.Name, MySpaceTexts.Blank);
            customName.Getter = (x) => x.CustomName;
            customName.Setter = (x, v) => x.SetCustomName(v);
            customName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customName);

            var onOffSwitch = new MyTerminalControlOnOffSwitch<MyTerminalBlock>("ShowOnHUD", MySpaceTexts.Terminal_ShowOnHUD, MySpaceTexts.Terminal_ShowOnHUDToolTip);
            onOffSwitch.Getter = (x) => x.ShowOnHUD;
            onOffSwitch.Setter = (x, v) => x.ShowOnHUD = v;
            MyTerminalControlFactory.AddControl(onOffSwitch);
        }
        public override string ToString()
        {
            return base.ToString() + " " + this.CustomName;
        }
    }
}
