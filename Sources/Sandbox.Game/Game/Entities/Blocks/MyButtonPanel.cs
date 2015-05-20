using ProtoBuf;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Serialization;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ButtonPanel))]
    internal class MyButtonPanel : MyTerminalBlock, IMyPowerConsumer, Sandbox.ModAPI.IMyButtonPanel
    {
        [PreloadRequired]
        class MySyncButtonPanel : MySyncEntity
        {
            [MessageIdAttribute(3316, P2PMessageEnum.Reliable)]
            protected struct CheckAccessMsg : IEntityMessage
            {
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                public BoolBlit CheckAccess;
            }

            [ProtoContract]
            [MessageIdAttribute(3317, P2PMessageEnum.Reliable)]
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

            [ProtoContract]
            [MessageIdAttribute(3318, P2PMessageEnum.Reliable)]
            protected struct SetCustomButtonName : IEntityMessage
            {
                [ProtoMember]
                public long EntityId;
                public long GetEntityId() { return EntityId; }

                [ProtoMember]
                public String CustomName;

                [ProtoMember]
                public int Index;
            }

            static MySyncButtonPanel()
            {
                MySyncLayer.RegisterEntityMessage<MySyncButtonPanel, CheckAccessMsg>(OnCheckAccessChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncButtonPanel, ChangeToolbarItemMsg>(OnToolbarItemChanged, MyMessagePermissions.Any);
                MySyncLayer.RegisterEntityMessage<MySyncButtonPanel, SetCustomButtonName>(OnButtonCustomNameChanged, MyMessagePermissions.Any);
            }

            private MyButtonPanel m_panel;
            private bool m_syncing;
            public bool IsSyncing
            {
                get { return m_syncing; }
            }

            public MySyncButtonPanel(MyButtonPanel panel)
                :base(panel)
            {
                m_panel = panel;
            }

            public void SendCheckAccessChanged(bool value)
            {
                var msg = new CheckAccessMsg();
                msg.EntityId = m_panel.EntityId;
                msg.CheckAccess = value;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            public void SendToolbarItemChanged(ToolbarItem item, int index)
            {
                if (m_syncing)
                    return;
                var msg = new ChangeToolbarItemMsg();
                msg.EntityId = m_panel.EntityId;
                msg.Item = item;
                msg.Index = index;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            public void SendCustonNameChanged(string customName, int index)
            {
                if (m_syncing)
                    return;
                var msg = new SetCustomButtonName();
                msg.EntityId = m_panel.EntityId;
                msg.CustomName = customName;
                msg.Index = index;

                Sync.Layer.SendMessageToAll(ref msg);
            }

            private static void OnToolbarItemChanged(MySyncButtonPanel sync, ref ChangeToolbarItemMsg msg, MyNetworkClient sender)
            {
                sync.m_syncing = true;
                MyToolbarItem item = null;
                if(msg.Item.EntityID != 0)
                    if (string.IsNullOrEmpty(msg.Item.GroupName))
                    {
                        MyTerminalBlock block;
                        if(MyEntities.TryGetEntityById<MyTerminalBlock>(msg.Item.EntityID, out block))
                        {
                            var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                            builder.Action = msg.Item.Action;
                            item = MyToolbarItemFactory.CreateToolbarItem(builder);
                        }
                    }
                    else
                    {
                        MyButtonPanel parent;
                        if (MyEntities.TryGetEntityById<MyButtonPanel>(msg.Item.EntityID, out parent))
                        {
                            var grid = parent.CubeGrid;
                            var groupName = msg.Item.GroupName;
                            var group = grid.GridSystems.TerminalSystem.BlockGroups.Find((x) => x.Name.ToString() == groupName);
                            if (group != null)
                            {
                                var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(group);
                                builder.Action = msg.Item.Action;
                                builder.BlockEntityId = msg.Item.EntityID;
                                item = MyToolbarItemFactory.CreateToolbarItem(builder);
                            }
                        }
                    }
                sync.m_panel.Toolbar.SetItemAtIndex(msg.Index, item);
                sync.m_syncing = false;
            }

            private static void OnCheckAccessChanged(MySyncButtonPanel syncObject, ref CheckAccessMsg msg, MyNetworkClient sender)
            {
                syncObject.m_panel.m_anyoneCanUse = msg.CheckAccess;
            }

            private static void OnButtonCustomNameChanged(MySyncButtonPanel syncObject, ref SetCustomButtonName msg, MyNetworkClient sender)
            {
                syncObject.m_panel.SetButtonName(msg.CustomName,msg.Index);
            }

        }

        [ProtoContract]
        struct ToolbarItem : IEqualityComparer<ToolbarItem>
        {
            [ProtoMember]
            public long EntityID;
            [ProtoMember]
            public string GroupName;
            [ProtoMember]
            public string Action;

            public bool Equals(ToolbarItem x, ToolbarItem y)
            {
                if (x.EntityID != y.EntityID || x.GroupName != y.GroupName || x.Action != y.Action)
                    return false;
                return true;
            }

            public int GetHashCode(ToolbarItem obj)
            {
                unchecked
                {
                    int result = obj.EntityID.GetHashCode();
                    result = (result * 397) ^ obj.GroupName.GetHashCode();
                    result = (result * 397) ^ obj.Action.GetHashCode();
                    return result;
                }
            }
        }

        private const string DETECTOR_NAME = "panel";
        private List<string> m_emissiveNames; // new string[] { "Emissive1", "Emissive2", "Emissive3", "Emissive4", "Emissive5", "Emissive6", "Emissive7", "Emissive8" };
        private bool m_anyoneCanUse;
        private MyPowerReceiver m_powerReciever;
        int m_selectedButton = -1;

        public MyToolbar Toolbar { get; set; }

        public new MyButtonPanelDefinition BlockDefinition { get { return base.BlockDefinition as MyButtonPanelDefinition; } }

        public bool AnyoneCanUse
        {
            get { return m_anyoneCanUse; }
            set
            {
                if (m_anyoneCanUse != value)
                {
                    m_anyoneCanUse = value;
                    (SyncObject as MySyncButtonPanel).SendCheckAccessChanged(value);
                }
            }
        }

        private static List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;

        SerializableDictionary<int, String> m_customButtonNames = new SerializableDictionary<int, String>();
        List<MyUseObjectPanelButton> m_buttonsUseObjects = new List<MyUseObjectPanelButton>();
        StringBuilder m_emptyName = new StringBuilder("");

        Vector3D m_previusPosition = Vector3D.Zero;

        static MyButtonPanel()
        {
            m_openedToolbars = new List<MyToolbar>();

            var checkAccess = new MyTerminalControlCheckbox<MyButtonPanel>("AnyoneCanUse", MySpaceTexts.BlockPropertyText_AnyoneCanUse, MySpaceTexts.BlockPropertyDescription_AnyoneCanUse);
            checkAccess.Getter = (x) => x.AnyoneCanUse;
            checkAccess.Setter = (x, v) => x.AnyoneCanUse = v;
            checkAccess.EnableAction();
            MyTerminalControlFactory.AddControl(checkAccess);

            var toolbarButton = new MyTerminalControlButton<MyButtonPanel>("Open Toolbar", MySpaceTexts.BlockPropertyTitle_SensorToolbarOpen, MySpaceTexts.BlockPropertyDescription_SensorToolbarOpen,
                delegate(MyButtonPanel self)
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

            var buttonsList = new MyTerminalControlListbox<MyButtonPanel>("ButtonText", MySpaceTexts.BlockPropertyText_ButtonList, MySpaceTexts.Blank);
            buttonsList.ListContent = (x, list1, list2) => x.FillListContent(list1, list2);
            buttonsList.ItemSelected = (x, y) => x.SelectButtonToName(y);
            MyTerminalControlFactory.AddControl(buttonsList);

            var customButtonName = new MyTerminalControlTextbox<MyButtonPanel>("ButtonName", MySpaceTexts.BlockPropertyText_ButtonName, MySpaceTexts.Blank);
            customButtonName.Getter = (x) => x.GetButtonName();
            customButtonName.Setter = (x, v) => x.SetCustomButtonName(v);
            customButtonName.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(customButtonName);
        }

        protected override MySyncEntity OnCreateSync()
        {
            return new MySyncButtonPanel(this);
        }

        public override void Init(Common.ObjectBuilders.MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true;
            base.Init(builder, cubeGrid);
            m_emissiveNames = new List<string>(BlockDefinition.ButtonCount);
            for (int i = 1; i <= BlockDefinition.ButtonCount; i++) //button dummies have 1-based index
            {
                m_emissiveNames.Add(string.Format("Emissive{0}", i)); // because of modding
            }
            var ob = builder as MyObjectBuilder_ButtonPanel;
            Toolbar = new MyToolbar(MyToolbarType.ButtonPanel, Math.Min(BlockDefinition.ButtonCount, MyToolbar.DEF_SLOT_COUNT), (BlockDefinition.ButtonCount / MyToolbar.DEF_SLOT_COUNT) + 1);
            Toolbar.DrawNumbers = false;
            Toolbar.GetSymbol = (slot) =>
                {
                    var ret = new Sandbox.Graphics.GUI.MyGuiControlGrid.ColoredIcon();
                    if(Toolbar.SlotToIndex(slot) < BlockDefinition.ButtonCount) 
                    {
                        ret.Icon = BlockDefinition.ButtonSymbols[Toolbar.SlotToIndex(slot) % BlockDefinition.ButtonSymbols.Length];
                        var color = BlockDefinition.ButtonColors[Toolbar.SlotToIndex(slot) % BlockDefinition.ButtonColors.Length];
                        color.W = 1;
                        ret.Color = color;
                    }
                    return ret;
                };

            Toolbar.Init(ob.Toolbar, this);
            Toolbar.ItemChanged += Toolbar_ItemChanged;
            AnyoneCanUse = ob.AnyoneCanUse;

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;
            PowerReceiver = new MyPowerReceiver(MyConsumerGroupEnum.Utility, false, 0.0001f, () => IsFunctional ? 0.0001f : 0);
            PowerReceiver.IsPoweredChanged += Receiver_IsPoweredChanged;
            PowerReceiver.IsPoweredChanged += ComponentStack_IsFunctionalChanged;
            PowerReceiver.Update();

            if (ob.CustomButtonNames != null)
            {
                m_customButtonNames = ob.CustomButtonNames;
            }

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;

            GetInteractiveObjects<MyUseObjectPanelButton>(m_buttonsUseObjects);
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected override bool CheckIsWorking()
        {
            return base.CheckIsWorking() && PowerReceiver.IsPowered;
        }

        void ComponentStack_IsFunctionalChanged()
        {
            PowerReceiver.Update();
            UpdateEmissivity();
        }

        private ToolbarItem GetToolbarItem(MyToolbarItem item)
        {
            var tItem = new ToolbarItem();
            tItem.EntityID = 0;
            if (item is MyToolbarItemTerminalBlock)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalBlock;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
            }
            else if (item is MyToolbarItemTerminalGroup)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalGroup;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block.Action;
                tItem.GroupName = block.GroupName;
            }
            return tItem;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            UpdateEmissivity();
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            Debug.Assert(self == Toolbar);
            
            var tItem = GetToolbarItem(self.GetItemAtIndex(index.ItemIndex));
            UpdateButtonEmissivity(index.ItemIndex);
            (SyncObject as MySyncButtonPanel).SendToolbarItemChanged(tItem, index.ItemIndex);

            if (m_shouldSetOtherToolbars)
            {
                m_shouldSetOtherToolbars = false;
                if (!(SyncObject as MySyncButtonPanel).IsSyncing)
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

            var slot = Toolbar.GetItemAtIndex(index.ItemIndex);
            if (slot != null)
            {
                string name = slot.DisplayName.ToString();
                SetButtonName(name, index.ItemIndex);
                (SyncObject as MySyncButtonPanel).SendCustonNameChanged(name, index.ItemIndex);
            }
        }

        void UpdateEmissivity()
        {
            for (int i = 0; i < BlockDefinition.ButtonCount; i++)
                UpdateButtonEmissivity(i);
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            if (InScene)
            {
                UpdateEmissivity();
            }
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
            m_buttonsUseObjects.Clear();
            GetInteractiveObjects<MyUseObjectPanelButton>(m_buttonsUseObjects);
        }

        void UpdateButtonEmissivity(int index)
        {
            if (!InScene)
                return;
            var c = BlockDefinition.ButtonColors[index % BlockDefinition.ButtonColors.Length];
            if (Toolbar.GetItemAtIndex(index) == null)
                c = BlockDefinition.UnassignedButtonColor;
            float emissivity = c.W;
            if (!IsFunctional || CubeGrid.GridSystems.PowerDistributor.PowerState == GameSystems.Electricity.MyPowerStateEnum.NoPower)
            {
                c = Color.Red.ToVector4();
                emissivity = 0;
            }
            VRageRender.MyRenderProxy.UpdateModelProperties(Render.RenderObjectIDs[0], 0, null, -1, m_emissiveNames[index], null, new Color(c.X, c.Y, c.Z), null, null, emissivity);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_ButtonPanel;
            ob.Toolbar = Toolbar.GetObjectBuilder();
            ob.AnyoneCanUse = AnyoneCanUse;
            ob.CustomButtonNames = m_customButtonNames;
            return ob;
        }

        public MyPowerReceiver PowerReceiver
        {
            get;
            private set;
        }

        public void PressButton(int i)
        {
            var handle = ButtonPressed;
            if (handle != null) ButtonPressed(i);
        }

        event Action<int> ButtonPressed;
        event Action<int> Sandbox.ModAPI.IMyButtonPanel.ButtonPressed
        {
            add { ButtonPressed += value; }
            remove { ButtonPressed -= value; }
        }

        protected override void Closing()
        {
            base.Closing();
            foreach (var button in m_buttonsUseObjects)
            {
                button.RemoveButtonMarker();
            }
        }

        private static StringBuilder m_helperSB = new StringBuilder();

        public void FillListContent(ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems)
        {
            string button = MyTexts.GetString(MySpaceTexts.BlockPropertyText_Button);
            for (int i=0; i < m_buttonsUseObjects.Count;++i)
            {
                m_helperSB.Clear().Append(button + " " + (i+1).ToString());
                var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: i);
                listBoxContent.Add(item);

                if (i == m_selectedButton)
                {
                    listBoxSelectedItems.Add(item);
                }
            }
        }

        public void SelectButtonToName(List<MyGuiControlListbox.Item> imageIds)
        {
            m_selectedButton = (int)imageIds[0].UserData;
            RaisePropertiesChanged();
        }

        public StringBuilder GetButtonName()
        { 
            if(m_selectedButton == -1)
            {
                return m_emptyName;
            }

            string item = null;
            if (false == m_customButtonNames.Dictionary.TryGetValue(m_selectedButton, out item))          
            {
                var actionInSlot = Toolbar.GetItemAtIndex(m_selectedButton);
                if (actionInSlot != null)
                {
                    return actionInSlot.DisplayName;
                }

                return m_emptyName;
            }
            return  new StringBuilder(item);
        }

        public void SetButtonName(string name, int position)
        {
            string item = null;
            if (m_customButtonNames.Dictionary.TryGetValue(position, out item))
            {
                m_customButtonNames.Dictionary[position] = name.ToString();
            }
            else
            {
                m_customButtonNames.Dictionary.Add(position, name.ToString());
            }
        }

        public void SetCustomButtonName(StringBuilder name)
        {
            if (m_selectedButton == -1)
            {
                return;
            }
            SetButtonName(name.ToString(), m_selectedButton);

            (SyncObject as MySyncButtonPanel).SendCustonNameChanged(name.ToString(), m_selectedButton);
        }

        public string GetCustomButtonName(int pos)
        {
            string item = null;
            if (false == m_customButtonNames.Dictionary.TryGetValue(pos, out item))
            {
                var actionInSlot = Toolbar.GetItemAtIndex(pos);
                if (actionInSlot != null)
                {
                    return actionInSlot.DisplayName.ToString();
                }
                return MyTexts.GetString(MySpaceTexts.NotificationHintNoAction);
            }
            return item;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            if (m_previusPosition != PositionComp.GetPosition())
            {
                m_previusPosition = PositionComp.GetPosition();
                foreach (var button in m_buttonsUseObjects)
                {
                    button.UpdateMarkerPosition();
                }
            }
            
        }
    }
}
