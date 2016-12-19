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
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Engine.Utils;
using Sandbox.Engine;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using Sandbox.Engine.Multiplayer;
using VRage.Sync;
using Sandbox.ModAPI.Ingame;
using IMyConveyorSorter = Sandbox.ModAPI.IMyConveyorSorter;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_ConveyorSorter))]
    public class MyConveyorSorter : MyFunctionalBlock, IMyConveyorEndpointBlock, IMyConveyorSorter, IMyInventoryOwner
    {
        public bool IsWhitelist
        {
            get
            {
                return m_inventoryConstraint.IsWhitelist;
            }
            private set
            {
                if (m_inventoryConstraint.IsWhitelist != value)
                {
                    m_inventoryConstraint.IsWhitelist = value;

                    // Recompute because of new sorter settings
                    CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
                }
            }
        }

        private MyInventoryConstraint m_inventoryConstraint = new MyInventoryConstraint(String.Empty);

        public bool IsAllowed(MyDefinitionId itemId)
        {
			if (!Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
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

        readonly Sync<bool> m_drainAll;
        public bool DrainAll
        {
            get
            {
                return m_drainAll;
            }
            set
            {
                m_drainAll.Value = value;
            }
        }


        private MyConveyorSorterDefinition m_conveyorSorterDefinition;

        private int m_pushRequestFrameCounter;

        public MyConveyorSorter()
        {
#if XB1 // XB1_SYNC_NOREFLECTION
            m_drainAll = SyncType.CreateAndAddProp<bool>();
#endif // XB1
            CreateTerminalControls();

            m_drainAll.ValueChanged += x => DoChangeDrainAll();
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

        protected override void CreateTerminalControls()
        {
            if (MyTerminalControlFactory.AreControlsCreated<MyConveyorSorter>())
                return;
            base.CreateTerminalControls();
            drainAll = new MyTerminalControlOnOffSwitch<MyConveyorSorter>("DrainAll", MySpaceTexts.Terminal_DrainAll);
            drainAll.Getter = (block) => block.DrainAll;
            drainAll.Setter = (block, val) => block.DrainAll = val;
            drainAll.EnableToggleAction();
            MyTerminalControlFactory.AddControl(drainAll);

            MyTerminalControlFactory.AddControl(new MyTerminalControlSeparator<MyConveyorSorter>());

            blacklistWhitelist = new MyTerminalControlCombobox<MyConveyorSorter>("blacklistWhitelist", MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterMode, MySpaceTexts.Blank);
            blacklistWhitelist.ComboBoxContent = (block) => FillBlWlCombo(block);
            blacklistWhitelist.Getter = (block) => (long)(block.IsWhitelist ? 1 : 0);
            blacklistWhitelist.Setter = (block, val) => block.ChangeBlWl(val == 1);
            blacklistWhitelist.SetSerializerBit();
            blacklistWhitelist.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(blacklistWhitelist);

            currentList = new MyTerminalControlListbox<MyConveyorSorter>("CurrentList", MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterItemsList, MySpaceTexts.Blank, true);
            currentList.ListContent = (block, list1, list2) => block.FillCurrentList(list1, list2);
            currentList.ItemSelected = (block, val) => block.SelectFromCurrentList(val);
            currentList.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(currentList);

            removeFromSelectionButton = new MyTerminalControlButton<MyConveyorSorter>("removeFromSelectionButton",
                MySpaceTexts.BlockPropertyTitle_ConveyorSorterRemove,
                MySpaceTexts.Blank,
                (block) => block.RemoveFromCurrentList());
            removeFromSelectionButton.Enabled = (x) => x.m_selectedForDelete != null && x.m_selectedForDelete.Count > 0; ;
            removeFromSelectionButton.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(removeFromSelectionButton);

            candidates = new MyTerminalControlListbox<MyConveyorSorter>("candidatesList", MySpaceTexts.BlockPropertyTitle_ConveyorSorterCandidatesList, MySpaceTexts.Blank, true);
            candidates.ListContent = (block, list1, list2) => block.FillCandidatesList(list1, list2);
            candidates.ItemSelected = (block, val) => block.SelectCandidate(val);
            candidates.SupportsMultipleBlocks = false;
            MyTerminalControlFactory.AddControl(candidates);

            addToSelectionButton = new MyTerminalControlButton<MyConveyorSorter>("addToSelectionButton",
                MySpaceTexts.BlockPropertyTitle_ConveyorSorterAdd,
                MySpaceTexts.Blank,
                (x) => x.AddToCurrentList());
            addToSelectionButton.SupportsMultipleBlocks = false;
            addToSelectionButton.Enabled = (x) => x.m_selectedForAdd != null && x.m_selectedForAdd.Count > 0;
            MyTerminalControlFactory.AddControl(addToSelectionButton);
        }

        //candidates:
        static readonly Dictionary<byte, Tuple<MyObjectBuilderType, StringBuilder>> CandidateTypes = new Dictionary<byte, Tuple<MyObjectBuilderType, StringBuilder>>();
        static readonly Dictionary<MyObjectBuilderType, byte> CandidateTypesToId = new Dictionary<MyObjectBuilderType, byte>();
        bool m_allowCurrentListUpdate = true;

        //BL/WL:
        private static void FillBlWlCombo(List<MyTerminalControlComboBoxItem> list)
        {
            list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterModeBlacklist });
            list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MySpaceTexts.BlockPropertyTitle_ConveyorSorterFilterModeWhitelist });
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

        private void ModifyCurrentList(ref List<MyGuiControlListbox.Item> list, bool Add)
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
            ModifyCurrentList(ref m_selectedForDelete, false);
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
                if (physicalItemDef == null || definition.Public == false || physicalItemDef.CanSpawnFromScreen == false)
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
            ModifyCurrentList(ref m_selectedForAdd, true);
        }

        private void UpdateText()
        {
            DetailedInfo.Clear();
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MyCommonTexts.BlockPropertiesText_Type));
            DetailedInfo.Append(BlockDefinition.DisplayNameText);
            DetailedInfo.Append("\n");
            DetailedInfo.AppendStringBuilder(MyTexts.Get(MySpaceTexts.BlockPropertyProperties_CurrentInput));
            MyValueFormatter.AppendWorkInBestUnit(ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? ResourceSink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId) : 0, DetailedInfo);
            DetailedInfo.Append("\n");
            RaisePropertiesChanged();
        }

        #endregion

        #region sync

        internal void DoChangeDrainAll()
        {
            DrainAll = m_drainAll;
            drainAll.UpdateVisual();
        }

        public void ChangeBlWl(bool IsWl)
        {
            MyMultiplayer.RaiseEvent(this, x => x.DoChangeBlWl, IsWl);
        }

        [Event, Reliable, Server, Broadcast] 
        void DoChangeBlWl(bool IsWl)
        {
            IsWhitelist = IsWl;
            blacklistWhitelist.UpdateVisual();
        }

        void ChangeListId(SerializableDefinitionId id, bool wasAdded)
        {
            MyMultiplayer.RaiseEvent(this, x => x.DoChangeListId, id, wasAdded);
        }

        [Event,Reliable,Server,Broadcast] 
        void DoChangeListId(SerializableDefinitionId id, bool add)
        {
            if (add)
                m_inventoryConstraint.Add(id);
            else
                m_inventoryConstraint.Remove(id);

            // Recompute because of new sorter settings
            CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();

            if (m_allowCurrentListUpdate)
                currentList.UpdateVisual();
        }

        void ChangeListType(byte type, bool wasAdded)
        {
            MyMultiplayer.RaiseEvent(this, x => x.DoChangeListType, type, wasAdded);
        }

        [Event, Reliable, Server, Broadcast] 
        void DoChangeListType(byte type, bool add)
        {
            Tuple<MyObjectBuilderType, StringBuilder> tuple;
            if (!CandidateTypes.TryGetValue(type, out tuple))
            {
                Debug.Assert(false, "type not in dictionary");
                return;
            }
            if (add)
            {
                m_inventoryConstraint.AddObjectBuilderType(tuple.Item1);
            }
            else
                m_inventoryConstraint.RemoveObjectBuilderType(tuple.Item1);

            // Recompute because of new sorter settings
            CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();

            if (m_allowCurrentListUpdate)
                currentList.UpdateVisual();
        }

        #endregion

        #region init & builder

        public override void Init(MyObjectBuilder_CubeBlock objectBuilder, MyCubeGrid cubeGrid)
        {
            m_conveyorSorterDefinition = (MyConveyorSorterDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(objectBuilder.GetId());

            var sinkComp = new MyResourceSinkComponent();
            sinkComp.Init(
                m_conveyorSorterDefinition.ResourceSinkGroup,
                BlockDefinition.PowerInput,
                UpdatePowerInput);
            sinkComp.IsPoweredChanged += IsPoweredChanged;
            ResourceSink = sinkComp;

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

            if (MyFakes.ENABLE_INVENTORY_FIX)
            {
                FixSingleInventory();
            }

            
            if (this.GetInventory() == null)
            {
                MyInventory inventory = new MyInventory(m_conveyorSorterDefinition.InventorySize.Volume, m_conveyorSorterDefinition.InventorySize, MyInventoryFlags.CanSend);
                Components.Add<MyInventoryBase>(inventory);
                inventory.Init(ob.Inventory);
            }
            Debug.Assert(this.GetInventory().Owner == this, "Ownership was not set!");

            SlimBlock.ComponentStack.IsFunctionalChanged += ComponentStack_IsFunctionalChanged;

            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

			
			ResourceSink.Update();
            UpdateText();
        }

        public override MyObjectBuilder_CubeBlock GetObjectBuilderCubeBlock(bool copy = false)
        {
            MyObjectBuilder_ConveyorSorter objectBuilder = (MyObjectBuilder_ConveyorSorter)base.GetObjectBuilderCubeBlock(copy);
            objectBuilder.DrainAll = DrainAll;
            objectBuilder.IsWhiteList = IsWhitelist;
            objectBuilder.Inventory = this.GetInventory().GetObjectBuilder();
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
            //GR: this is taking a long time but is needed when sorter is enabled/disabled
            CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
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

        protected override void OnInventoryComponentAdded(MyInventoryBase inventory)
        {
            base.OnInventoryComponentAdded(inventory);
            Debug.Assert(this.GetInventory() != null, "Added inventory to collector, but different type than MyInventory?! Check this.");
            if (this.GetInventory() != null)
            {
                if (MyPerGameSettings.InventoryMass)
                {
                    this.GetInventory().ContentsChanged += Inventory_ContentsChanged;
                }
            }
        }

        protected override void OnInventoryComponentRemoved(MyInventoryBase inventory)
        {
            base.OnInventoryComponentRemoved(inventory);
            var removedInventory = inventory as MyInventory;
            Debug.Assert(removedInventory != null, "Removed inventory is not MyInventory type? Check this.");
            if (removedInventory != null)
            {
                if (MyPerGameSettings.InventoryMass)
                {
                    removedInventory.ContentsChanged -= Inventory_ContentsChanged;
                }
            }
        }

		void Inventory_ContentsChanged(MyInventoryBase obj)
		{
			CubeGrid.SetInventoryMassDirty();
		}

        bool UseConveyorSystem
        {
            get
            {
                return true;
            }
            set
            {                
            }
        }

        #endregion

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (!Sync.IsServer || !DrainAll || !Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                return;

            if (!this.GetInventory().IsFull)
            {
                MyGridConveyorSystem.PullAllRequest(this, this.GetInventory(), OwnerId, m_inventoryConstraint);
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            base.UpdateBeforeSimulation10();
            if (!Sync.IsServer || !DrainAll || !Enabled || !IsFunctional || !IsWorking || !ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId))
                return;

            m_pushRequestFrameCounter++;
            if (m_pushRequestFrameCounter >= 4)
            {
                m_pushRequestFrameCounter = 0;

                if (this.GetInventory().GetItems().Count > 0)
                {
                    MyGridConveyorSystem.PushAnyRequest(this, this.GetInventory(), OwnerId);
                }
            }
        }

        public override void OnRemovedByCubeBuilder()
        {
            ReleaseInventory(this.GetInventory());
            base.OnRemovedByCubeBuilder();
        }

        public override void OnDestroy()
        {
            ReleaseInventory(this.GetInventory(), true);
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

            Color newColor = Enabled && IsFunctional && IsWorking && ResourceSink.IsPoweredByType(MyResourceDistributorComponent.ElectricityId) ? Color.GreenYellow : Color.DarkRed;
            MyCubeBlock.UpdateEmissiveParts(Render.RenderObjectIDs[0], 1.0f, newColor, Color.White);
        }

        [Event, Reliable, Server, Broadcast] 
        void DoSetupFilter(ModAPI.Ingame.MyConveyorSorterMode mode, List<ModAPI.Ingame.MyInventoryItemFilter> items)
        {
            IsWhitelist = mode == ModAPI.Ingame.MyConveyorSorterMode.Whitelist;
            m_inventoryConstraint.Clear();
            if (items != null)
            {
                m_allowCurrentListUpdate = false;
                try
                {
                    foreach (var item in items)
                    {
                        if (item.AllSubTypes)
                            m_inventoryConstraint.AddObjectBuilderType(item.ItemId.TypeId);
                        else
                            m_inventoryConstraint.Add(item.ItemId);
                    }
                }
                finally
                {
                    m_allowCurrentListUpdate = true;
                }
            }

            // Recompute because of new sorter settings
            CubeGrid.GridSystems.ConveyorSystem.FlagForRecomputation();
            currentList.UpdateVisual();
        }

        #region IMyInventoryOwner

        int IMyInventoryOwner.InventoryCount
        {
            get { return InventoryCount; }
        }

        long IMyInventoryOwner.EntityId
        {
            get { return EntityId; }
        }

        bool IMyInventoryOwner.HasInventory
        {
            get { return HasInventory; }
        }

        bool IMyInventoryOwner.UseConveyorSystem
        {
            get
            {
                return UseConveyorSystem;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        IMyInventory IMyInventoryOwner.GetInventory(int index)
        {
            return this.GetInventory(index);
        }

        #endregion

        #region IMyConveyorEndpointBlock implementation

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPullInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pullInformation = new PullInformation();
            pullInformation.Inventory = this.GetInventory(0);
            pullInformation.OwnerID = OwnerId;
            pullInformation.Constraint = m_inventoryConstraint;
            return pullInformation;
        }

        public Sandbox.Game.GameSystems.Conveyors.PullInformation GetPushInformation()
        {
            Sandbox.Game.GameSystems.Conveyors.PullInformation pushInformation = new PullInformation();
            pushInformation.Inventory = this.GetInventory(0);
            pushInformation.OwnerID = OwnerId;
            pushInformation.Constraint = new MyInventoryConstraint("Empty constraint");
            return pushInformation;
        }

        #endregion

        ModAPI.Ingame.MyConveyorSorterMode ModAPI.Ingame.IMyConveyorSorter.Mode
        {
            get { return m_inventoryConstraint.IsWhitelist ? ModAPI.Ingame.MyConveyorSorterMode.Whitelist : ModAPI.Ingame.MyConveyorSorterMode.Blacklist; }
        }

        void ModAPI.Ingame.IMyConveyorSorter.GetFilterList(List<ModAPI.Ingame.MyInventoryItemFilter> items)
        {
            items.Clear();
            foreach (var item in m_inventoryConstraint.ConstrainedTypes)
                items.Add(new MyInventoryItemFilter(new MyDefinitionId(item), true));
            foreach (var item in m_inventoryConstraint.ConstrainedIds)
                items.Add(new MyInventoryItemFilter(item));
        }

        void ModAPI.Ingame.IMyConveyorSorter.SetFilter(ModAPI.Ingame.MyConveyorSorterMode mode, List<ModAPI.Ingame.MyInventoryItemFilter> items)
        {
            // Update everyone else - except self
            MyMultiplayer.RaiseEvent(this, x => x.DoSetupFilter, mode, items);
        }

        void ModAPI.Ingame.IMyConveyorSorter.AddItem(ModAPI.Ingame.MyInventoryItemFilter item)
        {
            if (item.AllSubTypes)
            {
                byte id;
                if (!CandidateTypesToId.TryGetValue(item.ItemId.TypeId, out id))
                {
                    Debug.Assert(false, "type not in dictionary");
                    return;
                }
                ChangeListType(id, true);
                return;
            }

            ChangeListId(item.ItemId, true);
        }

        void ModAPI.Ingame.IMyConveyorSorter.RemoveItem(ModAPI.Ingame.MyInventoryItemFilter item)
        {
            if (item.AllSubTypes)
            {
                byte id;
                if (!CandidateTypesToId.TryGetValue(item.ItemId.TypeId, out id))
                {
                    Debug.Assert(false, "type not in dictionary");
                    return;
                }
                ChangeListType(id, true);
                return;
            }

            ChangeListId(item.ItemId, true);
        }
    }
}
