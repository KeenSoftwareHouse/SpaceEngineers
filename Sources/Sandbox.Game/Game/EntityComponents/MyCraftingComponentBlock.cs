using Sandbox.Common.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using VRage.Game.Entity;

namespace Sandbox.Game.Components
{
    [MyComponentBuilder(typeof(MyObjectBuilder_CraftingComponentBlock))]
    public class MyCraftingComponentBlock : MyCraftingComponentBase, IMyEventProxy
    {
        // TODO: Move these to definition..                
        private string m_operatingItemsDisplayNameText = "Flammables";
        private bool m_requiresItemsToOperate = true;
        private float m_maxInsertedItems = 20f;        
        private int m_operatingItemLastTimeMs = 20 * 1000;
        private List<MyDefinitionId> m_acceptedOperatingItems = new List<MyDefinitionId>();

        #region Fields

        private List<MyPhysicalInventoryItem> m_insertedItems = new List<MyPhysicalInventoryItem>();

        private float m_operatingItemsLevel;

        private float m_lastOperatingLevel;

        private int m_operatingItemTimerMs;

        private int m_lastUpdateTime;

        private float m_insertedItemUseLevel;

        private float m_currentInsertedItemsCount;

        public event Action<MyCraftingComponentBlock, MyCubeBlock> OnBlockTurnedOn;

        public event Action<MyCraftingComponentBlock, MyCubeBlock> OnBlockTurnedOff;

        private bool m_updatingOperatingLevel;

        #endregion



        #region Properties

        public override string ComponentTypeDebugString
        {
            get { return "Block crafting component"; }
        }

        public override string DisplayNameText
        {
            get
            {
                if (Entity is MyCubeBlock)
                {
                    return (Entity as MyCubeBlock).DisplayNameText;
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public override bool RequiresItemsToOperate
        {
            get { return m_requiresItemsToOperate; }
        }

        public override bool CanOperate
        {
            get
            {
                if (MySession.Static.CreativeMode)
                    return true;
                return m_operatingItemsLevel > 0;
            }
        }

        public override string OperatingItemsDisplayNameText
        {
            get
            {
                return m_operatingItemsDisplayNameText;
            }
        }

        public override float OperatingItemsLevel
        {
            get
            {
                return m_operatingItemsLevel;
            }
        }

        public MyCubeBlock Block
        {
            get
            {
                return Entity as MyCubeBlock;
            }
        }

        private bool m_blockEnabled;
        private bool m_paused;
        public bool IsBlockEnabled
        {
            get
            {
                return m_blockEnabled;
            }
            set
            {
                m_blockEnabled = value;
            }
        }

        public override bool AcceptsOperatingItems
        {
            get             
            {
                return m_currentInsertedItemsCount < m_maxInsertedItems; 
            }
        }

        public override float AvailableOperatingSpace
        {
            get { return m_maxInsertedItems - m_currentInsertedItemsCount; }
        }
        
        #endregion



        #region Init

        public MyCraftingComponentBlock()
        {
            m_operatingItemsDisplayNameText = MyTexts.GetString(MyCommonTexts.DisplayName_Flammables);
        }        

        private void InitBlock()
        {
            var owner = Entity as MyCubeBlock;
            MyInventory blockInventory = null;

            owner.Components.ComponentAdded -= OnNewComponentAdded;

            if (owner != null)
            {
                if (owner.InventoryCount == 0)
                {
                    // Inventory wasn't created yet, let's wait for it to be added..
                    owner.Components.ComponentAdded += OnNewComponentAdded;
                    return;
                }
                else
                {
                    blockInventory = owner.GetInventory();
                }
                System.Diagnostics.Debug.Assert(blockInventory != null, "Block inventory was not initialized!");

                blockInventory.SetFlags(MyInventoryFlags.CanReceive | MyInventoryFlags.CanSend);

                System.Diagnostics.Debug.Assert(blockInventory.MaxVolume > 0, "Max volume of the inventory is not positive, items won't be added!");
                System.Diagnostics.Debug.Assert(blockInventory.MaxMass > 0, "Max volume of the inventory is not positive, items won't be added!");

                var inventoryConstraint = new MyInventoryConstraint("Crafting constraints");

                foreach (var blueprintClass in m_blueprintClasses)
                {
                    foreach (var blueprint in blueprintClass)
                    {
                        foreach (var item in blueprint.Results)
                        {
                            inventoryConstraint.Add(item.Id);
                        }
                        foreach (var item in blueprint.Prerequisites)
                        {
                            inventoryConstraint.Add(item.Id);
                        }
                    }
                }

                blockInventory.Constraint = inventoryConstraint;
            }
            else
            {
                System.Diagnostics.Debug.Fail("InitBlockInventory can be called only when Entity owning this component is not null and is MyEntity type ");
            }
            
            UpdateBlock();
        }

        void OnNewComponentAdded(Type type, MyEntityComponentBase component)
        {
            if (component is MyInventory)
            {
                InitBlock();
            }
        }

        public override void Init(MyComponentDefinitionBase definition)
        {
            base.Init(definition);

            var craftDefinition = definition as MyCraftingComponentBlockDefinition;

            System.Diagnostics.Debug.Assert(craftDefinition != null, "Trying to initialize crafting component from wrong definition type?");


            if (craftDefinition != null)
            {
                m_craftingSpeedMultiplier = craftDefinition.CraftingSpeedMultiplier;

                foreach (var blueprintClass in craftDefinition.AvailableBlueprintClasses)
                {
                    var classDefinition = MyDefinitionManager.Static.GetBlueprintClass(blueprintClass);
                    System.Diagnostics.Debug.Assert(classDefinition != null, blueprintClass + " blueprint class definition was not found.");
                    if (classDefinition != null)
                    {
                        m_blueprintClasses.Add(classDefinition);
                    }
                }

                foreach (var operatingItem in craftDefinition.AcceptedOperatingItems)
                {
                    m_acceptedOperatingItems.Add(operatingItem);
                }
            }
        }
        
        #endregion



        #region Component base methods

        protected override void StartProduction_Implementation()
        {
            m_paused = false;

            TurnBlockOn();
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();

            InitBlock();

            System.Diagnostics.Debug.Assert(Block != null, "Entity is null or not MyCubeBlock type before removing the component, this can't happend!");                       
        }

        public void OnBlockEnabledChanged(MyCubeBlock obj)
        {
            System.Diagnostics.Debug.Assert(obj == Block, "Called from entity which doesn't owns this component?");

            if (IsBlockEnabled)
            {
                m_paused = false;

                m_operatingItemTimerMs = m_operatingItemLastTimeMs;
                m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;
                m_elapsedTimeMs = 0;

                UpdateOperatingLevel();

                UpdateBlock();
            }
            else
            {
                m_paused = true;
            }
        }

        public override bool IsSerialized()
        {
            return true;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            Entity.Components.ComponentAdded -= OnNewComponentAdded;
            System.Diagnostics.Debug.Assert(Block != null, "Entity is null or not MyCubeBlock type before removing the component, this can't happend!");                       
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);

            var ob = builder as MyObjectBuilder_CraftingComponentBlock;
            
            m_insertedItemUseLevel = ob.InsertedItemUseLevel;

            foreach (var item in ob.InsertedItems)
            {
                if (item.Amount <= 0)
                {
                    // Remove all items with 0 amount when loading...
                    System.Diagnostics.Debug.Fail(string.Format("Removing item with invalid amount: {0}x '{1}'. This is safe to ignore.", item.Amount, item.PhysicalContent.GetObjectId()));
                    continue;
                }

                if (item.PhysicalContent != null && (m_currentInsertedItemsCount + (float)item.Amount <= m_maxInsertedItems))
                {
                    MyPhysicalInventoryItem inventoryItem = new MyPhysicalInventoryItem(item);
                    m_currentInsertedItemsCount += (float)inventoryItem.Amount;

                    m_insertedItems.Add(inventoryItem);
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Trying to add operating item to crafting component but it can't fit there or doesn't have content!");
                }
            }
            
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            UpdateOperatingLevel();
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var ob = base.Serialize() as MyObjectBuilder_CraftingComponentBlock;
            
            ob.InsertedItemUseLevel = m_insertedItemUseLevel;

            foreach (var item in m_insertedItems)
                ob.InsertedItems.Add(item.GetObjectBuilder());

            return ob;
        }

        public override VRage.ObjectBuilders.MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            System.Diagnostics.Debug.Fail("This should not be called, components are serialized through serialize!");
            return null;
        }

        #endregion



        #region Implementation
                
        protected override void UpdateProduction_Implementation()
        {
            if (!CanOperate)
                return;

            if (!IsBlockEnabled)
                return;

            if (IsProducing)
            {
                UpdateCurrentItem();
            }
            else if (!IsProductionDone)
            {
                SelectItemToProduction();
                
                if (m_currentItem != -1)
                {
                    UpdateCurrentItem();
                }
            }
        }
        
        public override bool IsOperatingItem(MyPhysicalInventoryItem item)
        {
            var itemDefId = item.Content.GetObjectId();
            var result = m_acceptedOperatingItems.Contains(itemDefId);
            return result;
        }        

        override protected void UpdateOperatingLevel()
        {
            if (m_updatingOperatingLevel)
                return;

            m_updatingOperatingLevel = true;

            //TODO: This should be handled differently, for now just make it simple..
            System.Diagnostics.Debug.Assert(m_maxInsertedItems != 0, "Data of the component weren't properly initialized! Max inserted items can't be 0");

            float insertedItemsCount = 0;
            
            foreach (var item in m_insertedItems)
            {
                insertedItemsCount += (float)item.Amount;
            }

            if (IsBlockEnabled && insertedItemsCount > 0)
            {
                m_insertedItemUseLevel += m_elapsedTimeMs / (float)m_operatingItemLastTimeMs;
                m_insertedItemUseLevel = Math.Min(m_insertedItemUseLevel, 1.0f);
            }

            m_operatingItemsLevel = Math.Max(0, (insertedItemsCount - m_insertedItemUseLevel) / (float)m_maxInsertedItems);           

            if (m_insertedItemUseLevel >= 1f)            
            {                
                m_operatingItemsLevel = Math.Max(0, ((insertedItemsCount - 1.0f) / (float)m_maxInsertedItems));
                m_lastOperatingLevel = m_operatingItemsLevel;
                
                if (Sync.IsServer)
                {
                    if (insertedItemsCount == 1)
                    {
                        StopOperating();
                    }
                    else if (insertedItemsCount > 1)
                    {
                        RemoveOperatingItem(m_insertedItems.First(), 1);
                    }
                }
            }            
            else if (Math.Abs(m_operatingItemsLevel - m_lastOperatingLevel) > 0.01f)
            {
                RaiseEvent_OperatingChanged();
                m_lastOperatingLevel = m_operatingItemsLevel;
            }

            m_updatingOperatingLevel = false;
        }

        private void TurnBlockOff()
        {
            var handler = OnBlockTurnedOff;
            if (handler != null)
            {
                handler(this, Block);
            }
        }

        private void TurnBlockOn()
        {
            var handler = OnBlockTurnedOn;
            if (handler != null)
            {
                handler(this, Block);
            }
        }

        public override void GetInsertedOperatingItems(List<MyPhysicalInventoryItem> itemsList)
        {
            itemsList.AddList(m_insertedItems);
        }        

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            m_elapsedTimeMs = MySandboxGame.TotalGamePlayTimeInMilliseconds - m_lastUpdateTime;
            m_lastUpdateTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_paused)
                return;

            UpdateOperatingLevel();

            UpdateBlock();

            if (!IsProductionDone && CanOperate && IsBlockEnabled)
            {
                UpdateProduction_Implementation();
            }
        }

        private void UpdateBlock()
        {
            if (Block == null)
                return;
            
            if (CanOperate && !IsBlockEnabled && !IsProductionDone)
            {
                TurnBlockOn();
                RaiseEvent_OperatingChanged();
            }
            else if (!CanOperate && IsBlockEnabled)
            {
                TurnBlockOff();
                RaiseEvent_OperatingChanged();
            }

            if (IsBlockEnabled)
            {
                Block.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
            else
            {
                Block.NeedsUpdate &= ~VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;
            }
        }

        protected override void RemoveOperatingItem_Implementation(MyPhysicalInventoryItem item, MyFixedPoint amount)
        {
            var index = m_insertedItems.FindIndex(x => x.Content.GetId() == item.Content.GetId());

            if (!m_insertedItems.IsValidIndex(index))
            {
                System.Diagnostics.Debug.Fail("Trying to remove item that is not contained!");
                return;
            }

            base.RemoveOperatingItem_Implementation(item, amount);            

            var amountToRemove = MyFixedPoint.Min(amount, GetOperatingItemRemovableAmount(item));

            if (amountToRemove > 0)
            {
                item.Amount = item.Amount - amountToRemove;
                m_currentInsertedItemsCount -= (float)amountToRemove;

                if (item.Amount > 0)
                {
                    m_insertedItems[index] = item;
                }
                else
                {
                    m_insertedItems.RemoveAt(index);
                }

                m_insertedItemUseLevel = 0.0f;

                UpdateOperatingLevel();

                UpdateBlock();

                RaiseEvent_OperatingChanged();
            }            
        }

        protected override void InsertOperatingItem_Implementation(MyPhysicalInventoryItem item)
        {
            if (!AcceptsOperatingItems || !IsOperatingItem(item) || (float)item.Amount > AvailableOperatingSpace)
            {
                return;
            }

            base.InsertOperatingItem_Implementation(item);

            var index = m_insertedItems.FindIndex(x => x.Content.GetId() == item.Content.GetId());

            if (m_insertedItems.IsValidIndex(index))
            {
                m_currentInsertedItemsCount += (float)item.Amount;
                item.Amount = item.Amount + m_insertedItems[index].Amount;
                m_insertedItems[index] = item;                
            }
            else
            {
                m_insertedItems.Add(item);
                m_currentInsertedItemsCount += (float)item.Amount;
            }            

            UpdateOperatingLevel();

            UpdateBlock();
        }

        public override bool ContainsOperatingItem(MyPhysicalInventoryItem item) 
        {
            if (m_insertedItems == null) return false;
            return m_insertedItems.Contains(item); 
        }

        public override MyFixedPoint GetOperatingItemRemovableAmount(MyPhysicalInventoryItem item) 
        {
            var index = m_insertedItems.FindIndex(x => x.Content.GetId() == item.Content.GetId());
 
            if (m_insertedItems.IsValidIndex(index))
            {
                var itemAmount = m_insertedItems[index].Amount;
                if (index == 0 && m_insertedItemUseLevel > 0)
                {
                    return itemAmount - 1;
                }
                return itemAmount;
            }
            return 0;
        }

        protected override void StopOperating_Implementation()
        {
            m_insertedItems.Clear();
            m_currentInsertedItemsCount = 0;
            m_operatingItemsLevel = 0f;
            m_insertedItemUseLevel = 0f;

            RaiseEvent_OperatingChanged();
        }

        protected override void StopProduction_Implementation()
        {
            base.StopProduction_Implementation();

            TurnBlockOff();
        }

        #endregion
    }
}
