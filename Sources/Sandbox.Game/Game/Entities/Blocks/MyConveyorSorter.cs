using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.Conveyors;
using VRageMath;
using System.Diagnostics;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems.Electricity;
using VRage;
using Sandbox.Game.GameSystems;
using VRage.Utils;
using Sandbox.ModAPI.Ingame;
using VRage.ObjectBuilders;
using VRage.ModAPI;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ConveyorSorter))]
    class MyConveyorSorter : MyFunctionalBlock, IMyConveyorEndpointBlock, IMyInventoryOwner, IMyConveyorSorter
    {
        public bool IsWhitelist
        {
            get
            {
                return m_inventoryConstraint.IsWhitelist;
            }
            private set
            {
                m_inventoryConstraint.IsWhitelist = value;
            }
        }

        private MyInventoryConstraint m_inventoryConstraint = new MyInventoryConstraint(String.Empty);

        public bool IsAllowed(MyDefinitionId itemId)
        {
			if (!Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPowered)
                return false;

            return m_inventoryConstraint.Check(itemId);
        }

        private MyMultilineConveyorEndpoint m_conveyorEndpoint;
        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get
            {
                return m_conveyorEndpoint;
            }
        }

        public bool DrainAll
        {
            get;
            private set;
        }

        MySyncConveyorSorter m_sync;

        private MyConveyorSorterDefinition m_conveyorSorterDefinition;
        private MyInventory m_inventory;

        private int m_pushRequestFrameCounter;

        public MyConveyorSorter()
        {
            m_sync = new MySyncConveyorSorter(this);
        }

        public new MyConveyorSorterDefinition BlockDefinition
        {
            get { return (MyConveyorSorterDefinition)base.BlockDefinition; }
        }

        #region GUI

        private static StringBuilder m_helperSB = new StringBuilder();
        static MyTerminalControlOnOffSwitch<MyConveyorSorter> drainAll;
        static MyTerminalControlCombobox<MyConveyorSorter> blacklistWhitelist;
        static MyTerminalControlListbox<MyConveyorSorter> currentList;
        static MyTerminalControlButton<MyConveyorSorter> removeFromSelectionButton;
        static MyTerminalControlListbox<MyConveyorSorter> candidates;
        static MyTerminalControlButton<MyConveyorSorter> addToSelectionButton;

        static MyConveyorSorter()
        {
            drainAll = new MyTerminalControlOnOffSwitch<MyConveyorSorter>("DrainAll", MySpaceTexts.Terminal_DrainAll);
            drainAll.Getter = (block) => block.DrainAll;
            drainAll.Setter = (block, val) => block.ChangeDrainAll(val);
            drainAll.EnableToggleAction();
            MyTerminalControlFactory.AddControl(drainAll);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyConveyorSorter>());
            
            blacklistWhitelist = new MyTerminalControlCombobox<MyConveyorSorter>("blacklistWhitelist", MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterMode, MySpaceTexts.Blank);
            blacklistWhitelist.ComboBoxContent = (block) => FillBlWlCombo(block);
            blacklistWhitelist.Getter = (block) => (long)(block.IsWhitelist ? 1 : 0);
            blacklistWhitelist.Setter = (block, val) => block.ChangeBlWl(val == 1);
            blacklistWhitelist.SetSerializerBit();
            MyTerminalControlFactory.AddControl(blacklistWhitelist);

            currentList = new MyTerminalControlListbox<MyConveyorSorter>("CurrentList", MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterItemsList, MySpaceTexts.Blank, true);
            currentList.ListContent = (block, list1, list2) => block.FillCurrentList(list1, list2);
            currentList.ItemSelected = (block, val) => block.SelectFromCurrentList(val);
            MyTerminalControlFactory.AddControl(currentList);

            removeFromSelectionButton = new MyTerminalControlButton<MyConveyorSorter>("removeFromSelectionButton",
                MySpaceTexts.BlockPropertyTitle_ConveyorSorterRemove,
                MySpaceTexts.Blank,
                (block) => block.RemoveFromCurrentList());
            removeFromSelectionButton.Enabled = (x) => x.m_selectedForDelete != null && x.m_selectedForDelete.Count > 0; ;
            MyTerminalControlFactory.AddControl(removeFromSelectionButton);

            candidates = new MyTerminalControlListbox<MyConveyorSorter>("candidatesList", MySpaceTexts.BlockPropertyTitle_ConveyorSorterCandidatesList, MySpaceTexts.Blank, true);
            candidates.ListContent = (block, list1, list2) => block.FillCandidatesList(list1, list2);
            candidates.ItemSelected = (block, val) => block.SelectCandidate(val);
            MyTerminalControlFactory.AddControl(candidates);

            addToSelectionButton = new MyTerminalControlButton<MyConveyorSorter>("addToSelectionButton",
                MySpaceTexts.BlockPropertyTitle_ConveyorSorterAdd,
                MySpaceTexts.Blank,
                (x) => x.AddToCurrentList());
            addToSelectionButton.Enabled = (x) => x.m_selectedForAdd != null && x.m_selectedForAdd.Count > 0;
            MyTerminalControlFactory.AddControl(addToSelectionButton);

            byte index = 0;//warning: if you shuffle indexes, you will shuffle data in saved games
            CandidateTypes.Add(++index, new Tuple<MyObjectBuilderType, StringBuilder>(typeof(MyObjectBuilder_AmmoMagazine), MyTexts.Get(MySpaceTexts.DisplayName_ConvSorterTypes_Ammo)));
            CandidateTypes.Add(++index, new Tuple<MyObjectBuilderType, StringBuilder>(typeof(MyObjectBuilder_Component), MyTexts.Get(MySpaceTexts.DisplayName_ConvSorterTypes_Component)));
            CandidateTypes.Add(++index, new Tuple<MyObjectBuilderType, StringBuilder>(typeof(MyObjectBuilder_PhysicalGunObject), MyTexts.Get(MySpaceTexts.DisplayName_ConvSorterTypes_HandTool)));
            CandidateTypes.Add(++index, new Tuple<MyObjectBuilderType, StringBuilder>(typeof(MyObjectBuilder_Ingot), MyTexts.Get(MySpaceTexts.DisplayName_ConvSorterTypes_Ingot)));
            CandidateTypes.Add(++index, new Tuple<MyObjectBuilderType, StringBuilder>(typeof(MyObjectBuilder_Ore), MyTexts.Get(MySpaceTexts.DisplayName_ConvSorterTypes_Ore)));
            foreach (var val in CandidateTypes)
            {
                CandidateTypesToId.Add(val.Value.Item1, val.Key);
            }
        }

        //candidates:
        static readonly Dictionary<byte, Tuple<MyObjectBuilderType, StringBuilder>> CandidateTypes = new Dictionary<byte, Tuple<MyObjectBuilderType, StringBuilder>>();
        static readonly Dictionary<MyObjectBuilderType, byte> CandidateTypesToId = new Dictionary<MyObjectBuilderType, byte>();
        bool m_allowCurrentListUpdate = true;

        //BL/WL:
        private static void FillBlWlCombo(List<TerminalComboBoxItem> list)
        {
            list.Add(new TerminalComboBoxItem() { Key = 0, Value = MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterModeBlacklist });
            list.Add(new TerminalComboBoxItem() { Key = 1, Value = MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterModeWhitelist });
        }

        //current list:
        List<MyGuiControlListbox.Item> m_selectedForDelete;
        private void FillCurrentList(ICollection<MyGuiControlListbox.Item> content, ICollection<MyGuiControlListbox.Item> selectedItems)
        {
            foreach (var type in m_inventoryConstraint.ConstrainedTypes)
            {
                byte b;
                if (!CandidateTypesToId.TryGetValue(type, out b))
                {
                    Debug.Assert(false, "type not in reverse dictionary");
                    continue;
                }
                Tuple<MyObjectBuilderType, StringBuilder> tuple;
                if (!CandidateTypes.TryGetValue(b, out tuple))
                {
                    Debug.Assert(false, "type not in dictionary");
                    continue;
                }
                var item = new MyGuiControlListbox.Item(text: tuple.Item2, userData: b);
                content.Add(item);
            }
            foreach (var id in m_inventoryConstraint.ConstrainedIds)
            {
                MyPhysicalItemDefinition physDef;
                if (MyDefinitionManager.Static.TryGetPhysicalItemDefinition(id, out physDef))
                    m_helperSB.Clear().Append(physDef.DisplayNameText);
                else
                {
                    Debug.Assert(false, "no physical definition for item in list");
                    m_helperSB.Clear().Append(id.ToString());
                }
                var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: id);
                content.Add(item);
            }
        }

        private void SelectFromCurrentList(List<MyGuiControlListbox.Item> val)
        {
            m_selectedForDelete = val;
            removeFromSelectionButton.UpdateVisual();
        }

        private void modifyCurrentList(ref List<MyGuiControlListbox.Item> list, bool Add)
        {
            Debug.Assert(list != null, "Adding NULL from list");
            m_allowCurrentListUpdate = false;
            if (list != null)
            {
                foreach (var val in list)
                {
                    Debug.Assert(val.UserData != null, "User data is null");
                    MyDefinitionId? id = val.UserData as MyDefinitionId?;
                    if (id != null)
                    {
                        ChangeListId((MyDefinitionId)id, Add);
                        continue;
                    }
                    byte? b = val.UserData as byte?;
                    if (b == null)
                    {
                        Debug.Assert(false, "Should not be here");
                        continue;
                    }
                    ChangeListType((byte)b, Add);
                }
            }
            m_allowCurrentListUpdate = true;
            currentList.UpdateVisual();
            addToSelectionButton.UpdateVisual();
            removeFromSelectionButton.UpdateVisual();
        }

        //remove button:
        private void RemoveFromCurrentList()
        {
            modifyCurrentList(ref m_selectedForDelete, false);
        }

        List<MyGuiControlListbox.Item> m_selectedForAdd;
        private void FillCandidatesList(ICollection<MyGuiControlListbox.Item> content, ICollection<MyGuiControlListbox.Item> selectedItems)
        {
            //MyObjectBuilderType:
            foreach (var type in CandidateTypes)
            {
                var item = new MyGuiControlListbox.Item(text: (StringBuilder)type.Value.Item2, userData: type.Key);
                content.Add(item);
            }
            //MyDefinitionId
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions().OrderBy(x => sorter(x)))
            {
                if (!definition.Public)
                    continue;
                var physicalItemDef = definition as MyPhysicalItemDefinition;
                if (physicalItemDef == null)
                    continue;
                m_helperSB.Clear().Append(definition.DisplayNameText);
                var item = new MyGuiControlListbox.Item(text: m_helperSB, userData: physicalItemDef.Id);
                content.Add(item);
            }
        }

        private string sorter(MyDefinitionBase def)
        {
            var physDef = def as MyPhysicalItemDefinition;
            if (physDef != null)
                return physDef.DisplayNameText;
            return null;
        }

        private void SelectCandidate(List<MyGuiControlListbox.Item> val)
        {

            m_selectedForAdd = val;
            addToSelectionButton.UpdateVisual();
        }

        //add button:
        private void AddToCurrentList()
        {
            modifyCurrentList(ref m_selectedForAdd, true);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
			MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPowered ? ResourceSink.RequiredInput : 0, DetailedInfo);
            DetailedInfo.Append("\n");
            RaisePropertiesChanged();
        }

        #endregion

        #region sync

        public void ChangeDrainAll(bool cAll)
        {
            if (cAll == DrainAll)
                return;
            if (!Sync.MultiplayerActive)
                DoChangeDrainAll(cAll);
            else
                m_sync.ChangeDrainAll(cAll);
        }

        internal bool DoChangeDrainAll(bool cAll)
        {
            DrainAll = cAll;
            drainAll.UpdateVisual();
            return true;
        }

        public void ChangeBlWl(bool IsWl)
        {
            if (IsWl == IsWhitelist)
                return;
            if (!Sync.MultiplayerActive)
                DoChangeBlWl(IsWl);
            else
                m_sync.ChangeBlWl(IsWl);
        }

        internal bool DoChangeBlWl(bool isWl)
        {
            IsWhitelist = isWl;
            blacklistWhitelist.UpdateVisual();
            return true;
        }

        void ChangeListId(SerializableDefinitionId id, bool wasAdded)
        {
            if (!Sync.MultiplayerActive)
                DoChangeListId(id, wasAdded);
            else
                m_sync.ChangeListId(id, wasAdded);
        }

        internal bool DoChangeListId(SerializableDefinitionId id, bool add)
        {
            if (add)
                m_inventoryConstraint.Add(id);
            else
                m_inventoryConstraint.Remove(id);
            if (m_allowCurrentListUpdate)
                currentList.UpdateVisual();
            return true;
        }

        void ChangeListType(byte type, bool wasAdded)
        {
            if (!Sync.MultiplayerActive)
                DoChangeListType(type, wasAdded);
            else
                m_sync.ChangeListType(type, wasAdded);
        }

        internal bool DoChangeListType(byte type, bool add)
        {
            Tuple<MyObjectBuilderType, StringBuilder> tuple;
            if (!CandidateTypes.TryGetValue(type, out tuple))
            {
                Debug.Assert(false, "type not in dictionary");
                return false;
            }
            if (add)
            {
                m_inventoryConstraint.AddObjectBuilderType(tuple.Item1);
            }
            else
                m_inventoryConstraint.RemoveObjectBuilderType(tuple.Item1);
            if (m_allowCurrentListUpdate)
                currentList.UpdateVisual();
            return true;
        }

        #endregion

        #region init & builder

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            base.Init(objectBuilder, cubeGrid);

            MyObjectBuilder_ConveyorSorter ob = (MyObjectBuilder_ConveyorSorter)objectBuilder;
            DrainAll = ob.DrainAll;
            IsWhitelist = ob.IsWhiteList;
            foreach (var id in ob.DefinitionIds)
                m_inventoryConstraint.Add(id);
            foreach (byte b in ob.DefinitionTypes)
            {
                Tuple<MyObjectBuilderType, StringBuilder> tuple;
                if (!CandidateTypes.TryGetValue(b, out tuple))
                {
                    Debug.Assert(false, "type not in dictionary");
                    continue;
                }
                m_inventoryConstraint.AddObjectBuilderType(tuple.Item1);
            }

            m_conveyorSorterDefinition = (MyConveyorSorterDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());
            m_inventory = new MyInventory(m_conveyorSorterDefinition.InventorySize.Volume, m_conveyorSorterDefinition.InventorySize, MyInventoryFlags.CanSend, this);
            m_inventory.Init(ob.Inventory);

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

			if (MyPerGameSettings.InventoryMass)
				m_inventory.ContentsChanged += Inventory_ContentsChanged;

			var sinkComp = new MyResourceSinkComponent();
			sinkComp.Init(
                m_conveyorSorterDefinition.ResourceSinkGroup,
                BlockDefinition.PowerInput,
                UpdatePowerInput);
			sinkComp.IsPoweredChanged += IsPoweredChanged;
	        ResourceSink = sinkComp;
			ResourceSink.Update();
            UpdateText();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_ConveyorSorter objectBuilder = (MyObjectBuilder_ConveyorSorter)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.DrainAll = DrainAll;
            objectBuilder.IsWhiteList = IsWhitelist;
            objectBuilder.Inventory = m_inventory.GetObjectBuilder();
            foreach (var id in m_inventoryConstraint.ConstrainedIds)
                objectBuilder.DefinitionIds.Add(id);
            foreach (var type in m_inventoryConstraint.ConstrainedTypes)
            {
                byte b;
                if (!CandidateTypesToId.TryGetValue(type, out b))
                {
                    Debug.Assert(false, "type not in reverse dictionary");
                    continue;
                }
                objectBuilder.DefinitionTypes.Add(b);
            }
            return objectBuilder;
        }

        #endregion

        #region power

        float UpdatePowerInput()
        {
            return (Enabled && IsFunctional) ? BlockDefinition.PowerInput : 0.0f;
        }
        protected override void OnEnabledChanged()
        {
			ResourceSink.Update();
            UpdateText();
            UpdateEmissivity();
            base.OnEnabledChanged();
        }
        void IsPoweredChanged()
        {
			ResourceSink.Update();
            UpdateText();
            UpdateEmissivity();
        }

        #endregion

        #region Inventory

        public int InventoryCount { get { return 1; } }

        public MyInventory GetInventory(int index = 0)
        {
            Debug.Assert(index == 0);
            return m_inventory;
        }

        public void SetInventory(MyInventory inventory, int index)
        {
            if (m_inventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    m_inventory.ContentsChanged -= Inventory_ContentsChanged;
            }
            m_inventory = inventory;

            if (m_inventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                    m_inventory.ContentsChanged += Inventory_ContentsChanged;
            }
        }

		void Inventory_ContentsChanged(MyInventoryBase obj)
		{
			CubeGrid.SetInventoryMassDirty();
		}

        String IMyInventoryOwner.DisplayNameText
        {
            get { return CustomName.ToString(); }
        }

        public MyInventoryOwnerTypeEnum InventoryOwnerType
        {
            get { return MyInventoryOwnerTypeEnum.Storage; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        Sandbox.ModAPI.Interfaces.IMyInventory Sandbox.ModAPI.Interfaces.IMyInventoryOwner.GetInventory(int index)
        {
            return GetInventory(index);
        }

        #endregion

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
			if (!Sync.IsServer || !DrainAll || !Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPowered)
                return;

            if (!m_inventory.IsFull)
            {
                MyGridConveyorSystem.PullAllRequest(this, m_inventory, OwnerId, m_inventoryConstraint);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
			if (!Sync.IsServer || !DrainAll || !Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPowered)
                return;

            m_pushRequestFrameCounter++;
            if (m_pushRequestFrameCounter >= 4)
            {
                m_pushRequestFrameCounter = 0;

                if (m_inventory.GetItems().Count > 0)
                {
                    MyGridConveyorSystem.PushAnyRequest(this, m_inventory, OwnerId);
                }
            }
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(m_inventory);
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(m_inventory, true);
            base.OnDestroy();
        }

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyMultilineConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        void ComponentStack_IsFunctionalChanged()
        {
			ResourceSink.Update();
            UpdateText();
            UpdateEmissivity();
        }

        public override void OnAddedToScene(object source)
        {
            base.OnAddedToScene(source);

            UpdateEmissivity();
        }

        public override void OnModelChange()
        {
            base.OnModelChange();
            UpdateEmissivity();
        }

        public override void UpdateVisual()
        {
            base.UpdateVisual();
            UpdateEmissivity();
        }

        private void UpdateEmissivity()
        {
            if (!InScene)
                return;

			Color newColor = Enabled && IsFunctional && IsWorking && ResourceSink.IsPowered ? Color.GreenYellow : Color.DarkRed;
            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, newColor, Color.White);
        }
    }
}
