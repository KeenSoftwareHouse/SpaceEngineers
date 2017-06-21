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
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.Entities.Cube;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.ModAPI;
using VRage.Network;
using VRage.Serialization;
using VRage.Sync;
using VRageMath;

namespace SpaceEngineers.Game.Entities.Blocks
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ButtonPanel))]
    public class MyButtonPanel : MyFunctionalBlock, IMyButtonPanel
    {
        private const string DETECTOR_NAME = "panel";
        private List<string> m_emissiveNames; // new string[] { "Emissive1", "Emissive2", "Emissive3", "Emissive4", "Emissive5", "Emissive6", "Emissive7", "Emissive8" };
        private readonly Sync<bool> m_anyoneCanUse;
        int m_selectedButton = -1;
		

        public MyToolbar Toolbar { get; set; }

        public new MyButtonPanelDefinition BlockDefinition { get { return base.BlockDefinition as MyButtonPanelDefinition; } }

        public bool AnyoneCanUse
        {
            get { return m_anyoneCanUse; }
            set
            {
                m_anyoneCanUse.Value = value;
            }
        }

        private static List<MyToolbar> m_openedToolbars;
        private static bool m_shouldSetOtherToolbars;

        SerializableDictionary<int, string> m_customButtonNames = new SerializableDictionary<int, string>();
        List<MyUseObjectPanelButton> m_buttonsUseObjects = new List<MyUseObjectPanelButton>();
        StringBuilder m_emptyName = new StringBuilder("");

        bool m_syncing = false;

        public MyButtonPanel()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_anyoneCanUse = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();
            m_openedToolbars = new List<MyToolbar>();
        }

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyButtonPanel>())
                return;
            base.CreateTerminalControls();
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

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            SyncFlag = true; 

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                BlockDefinition.ResourceSinkGroup,
                0.0001f,
                () => IsFunctional ? 0.0001f : 0);
            sinkComp.IsPoweredChanged += Receiver_IsPoweredChanged;
            sinkComp.IsPoweredChanged += ComponentStack_IsFunctionalChanged;
            ResourceSink = sinkComp;

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
		
            ResourceSink.Update();

            if (ob.CustomButtonNames != null)
            {
                m_customButtonNames = ob.CustomButtonNames;
            }

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            UseObjectsComponent.GetInteractiveObjects<MyUseObjectPanelButton>(m_buttonsUseObjects);
        }

        private void Receiver_IsPoweredChanged()
        {
            UpdateIsWorking();
        }

        protected override bool CheckIsWorking()
        {
			return base.CheckIsWorking() && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId);
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateEmissivity();
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            UpdateEmissivity();
        }

        void Toolbar_ItemChanged(MyToolbar self, MyToolbar.IndexArgs index)
        {
            if(m_syncing)
            {
                return;
            }
            Debug.Assert(self == Toolbar);
            
            var tItem = ToolbarItem.FromItem(self.GetItemAtIndex(index.ItemIndex));
            UpdateButtonEmissivity(index.ItemIndex);
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

            var slot = Toolbar.GetItemAtIndex(index.ItemIndex);
            if (slot != null)
            {
                string name = slot.DisplayName.ToString();
                MyMultiplayer.RaiseEvent(this, x => x.SetButtonName, name, index.ItemIndex);
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
            UseObjectsComponent.GetInteractiveObjects<MyUseObjectPanelButton>(m_buttonsUseObjects);
        }

        public override void OnRegisteredToGridSystems()
        {
            base.OnRegisteredToGridSystems();
            UpdateEmissivity();
        }

        protected override void OnEnabledChanged()
        {
            base.OnEnabledChanged();
            UpdateEmissivity();
        }

        void UpdateButtonEmissivity(int index)
        {
            if (!InScene)
                return;
            var c = BlockDefinition.ButtonColors[index % BlockDefinition.ButtonColors.Length];
            if (Toolbar.GetItemAtIndex(index) == null)
                c = BlockDefinition.UnassignedButtonColor;
            float emissivity = c.W;
            if (!IsWorking)
            {
                c = Color.Red.ToVector4();
                emissivity = 0;
            }
            UpdateNamedEmissiveParts(Render.RenderObjectIDs[0], m_emissiveNames[index], new Color(c.X, c.Y, c.Z), emissivity);
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            var ob = base.GetObjectBuilderCubeBlock(copy) as MyObjectBuilder_ButtonPanel;
            ob.Toolbar = Toolbar.GetObjectBuilder();
            ob.AnyoneCanUse = AnyoneCanUse;
            ob.CustomButtonNames = m_customButtonNames;
            return ob;
        }

        public void PressButton(int i)
        {
            var handle = ButtonPressed;
            if (handle != null) ButtonPressed(i);
        }

        event Action<int> ButtonPressed;
        event Action<int> IMyButtonPanel.ButtonPressed
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

        [Event,Reliable,Server,Broadcast]
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
            MyMultiplayer.RaiseEvent(this, x => x.SetButtonName, name.ToString(), m_selectedButton);   
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

        protected override void WorldPositionChanged(object source)
        {
            base.WorldPositionChanged(source);

            foreach (var button in m_buttonsUseObjects)
            {
                button.UpdateMarkerPosition();
            }
        }
    }
}
