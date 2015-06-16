using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics;
using Sandbox.Common;
using Sandbox.Game.GUI;
using VRage;
using Sandbox.Common.Components;
using Sandbox.Game.SessionComponents;
using VRage.Components;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Screens.Helpers
{
    public class MyToolbar
    {
        public struct SlotArgs
        {
            public int? SlotNumber;
        }

        public struct IndexArgs
        {
            public int ItemIndex;
        }

        public struct PageChangeArgs
        {
            public int PageIndex;
        }

        public const int DEF_SLOT_COUNT = 9;
        public const int DEF_PAGE_COUNT = 9;

        public int SlotCount;
        public int PageCount;
        public int ItemCount { get { return SlotCount * PageCount; } }

        private MyToolbarItem[] m_items;

        private MyToolbarType m_toolbarType;
        public MyToolbarType ToolbarType
        {
            get { return m_toolbarType; }
            private set { m_toolbarType = value; }
        }

        private MyEntity m_owner;
        public MyEntity Owner
        {
            get { return  m_owner; }
            private set { m_owner = value; }
        }

        private bool? m_enabledOverride;
        private int? m_selectedSlot;
        private int? m_stagedSelectedSlot;
        private bool m_activateSelectedItem;
        private int m_currentPage;
        private const int m_colorMaskSlotCount = 14;
        public static List<Vector3> m_colorMaskHSVSlots;
        public static int m_currentColorMaskHSV = 0;

        #region Properties

        public static Vector3 ColorMaskHSV
        {
            get
            {
                if (m_colorMaskHSVSlots == null || !MyFakes.ENABLE_BLOCK_COLORING)
                    return Vector3.Zero;
                return m_colorMaskHSVSlots[m_currentColorMaskHSV];
            }
            set
            {
                m_colorMaskHSVSlots[m_currentColorMaskHSV] = value;
            }
        }

        public static int ColorMaskSlotCount
        {
            get
            {
                return m_colorMaskSlotCount;
            }
        }

        public bool ShowHolsterSlot
        {
            get { return m_toolbarType == MyToolbarType.Character; }
        }

        public int? SelectedSlot
        {
            get { return m_selectedSlot; }
            private set
            {
                if (m_selectedSlot != value)
                {
                    m_selectedSlot = value;
                }
            }
        }

        public int? StagedSelectedSlot
        {
            get { return m_stagedSelectedSlot; }
            private set
            {
                m_stagedSelectedSlot = value;
                m_activateSelectedItem = false;
            }
        }

        public bool ShouldActivateSlot
        {
            get { return m_activateSelectedItem; }
        }

        public bool DrawNumbers = true;

        public Func<int, Sandbox.Graphics.GUI.MyGuiControlGrid.ColoredIcon> GetSymbol = (x) => new Sandbox.Graphics.GUI.MyGuiControlGrid.ColoredIcon();

        public int CurrentPage { get { return m_currentPage; } }

        public int SlotToIndex(int i)
        {
            Debug.Assert(i >= 0 && i < SlotCount);
            return SlotCount * m_currentPage + i;
        }

        public int IndexToSlot(int i)
        {
            Debug.Assert(m_items.IsValidIndex(i));
            if (i / SlotCount != m_currentPage) return -1;
            return MyMath.Mod(i, SlotCount);
        }

        public MyToolbarItem SelectedItem
        {
            get
            {
                if (!SelectedSlot.HasValue) return null;
                return GetSlotItem(SelectedSlot.Value);
            }
        }

        public MyToolbarItem this[int i]
        {
            get
            {
                return m_items[i];
            }
        }

        public MyToolbarItem GetSlotItem(int slot)
        {
            if (!IsValidSlot(slot)) return null;
            int index = SlotToIndex(slot);
            if (!IsValidIndex(index)) return null;

            return this[index];
        }

        public MyToolbarItem GetItemAtIndex(int index)
        {
            if (!IsValidIndex(index)) 
                return null;

            return this[index];
        }

        /// <summary>
        /// Override value for Enabled state of items. null means that per item state is reported, otherwise this value is reported.
        /// </summary>
        public bool? EnabledOverride
        {
            get { return m_enabledOverride; }
            private set
            {
                if (value != m_enabledOverride)
                {
                    m_enabledOverride = value;

                    if (ItemEnabledChanged != null)
                        ItemEnabledChanged(this, new SlotArgs());
                }
            }
        }

        #endregion

        #region Events

        public event Action<MyToolbar, IndexArgs> ItemChanged;                           // Is raised whenever the item instance at the given index changes
        public event Action<MyToolbar, IndexArgs, MyToolbarItem.ChangeInfo> ItemUpdated; // Is raissed when the item data changes
        public event Action<MyToolbar, SlotArgs> SelectedSlotChanged;
        public event Action<MyToolbar, SlotArgs> SlotActivated;
        public event Action<MyToolbar, SlotArgs> ItemEnabledChanged;
        public event Action<MyToolbar, PageChangeArgs> CurrentPageChanged;
        public event Action<MyToolbar> Unselected; // Is raised when Unselect method is called

        #endregion

        #region Construction and serialization

        public MyToolbar(MyToolbarType type, int slotCount = DEF_SLOT_COUNT, int pageCount = DEF_PAGE_COUNT)
        {
            SlotCount = slotCount;
            PageCount = pageCount;
            m_items = new MyToolbarItem[SlotCount*PageCount];

            m_toolbarType = type;
            Owner = null;
            SetDefaults();
        }

        public void Init(MyObjectBuilder_Toolbar builder, MyEntity owner, bool skipAssert = false)
        {
            // TODO: remove skipAssert when spectator is MyEntity
            Debug.Assert(skipAssert || owner != null, "Toolbar has no owner");
            Owner = owner;

            if (builder == null)
                return;

            if (builder.Slots != null)
            {
                Clear();
                foreach (var slot in builder.Slots)
                {
                    SetItemAtSerialized(slot.Index, slot.Item, slot.Data);
                }
            }

            if ((builder.ColorMaskHSVList == null) || (builder.ColorMaskHSVList.Count == 0))
            {
                SetDefaultColors();
            }
            else if (builder.ColorMaskHSVList.Count == m_colorMaskSlotCount)
            {
                if(ColorsSetToDefaults(m_colorMaskHSVSlots))
                    m_colorMaskHSVSlots = builder.ColorMaskHSVList;
            }
            else if (builder.ColorMaskHSVList.Count > m_colorMaskSlotCount)
            {
                m_colorMaskHSVSlots = new List<Vector3>(m_colorMaskSlotCount);
                for (int i = 0; i < m_colorMaskSlotCount; i++)
                    m_colorMaskHSVSlots.Add(builder.ColorMaskHSVList[i]);
            }
            else
            {
                m_colorMaskHSVSlots = builder.ColorMaskHSVList;
                for (int i = m_colorMaskHSVSlots.Count - 1; i < m_colorMaskSlotCount; i++)
                    m_colorMaskHSVSlots.Add(MyRenderComponentBase.OldBlackToHSV);
            }
        }

        public static bool ColorsSetToDefaults(List<Vector3> colors)
        {
            if(colors[0] != (MyRenderComponentBase.OldGrayToHSV))
                return false;
            if (colors[1] != (MyRenderComponentBase.OldRedToHSV))
                return false;
            if (colors[2] != (MyRenderComponentBase.OldGreenToHSV))
                return false;
            if (colors[3] != (MyRenderComponentBase.OldBlueToHSV))
                return false;
            if (colors[4] != (MyRenderComponentBase.OldYellowToHSV))
                return false;
            if (colors[5] != (MyRenderComponentBase.OldWhiteToHSV))
                return false;
            if (colors[6] != (MyRenderComponentBase.OldBlackToHSV))
                return false;
            
            return true;
        }

        public static void SetDefaultColors()
        {
            if (m_colorMaskHSVSlots == null)
                m_colorMaskHSVSlots = new List<Vector3>(m_colorMaskSlotCount);
            if (m_colorMaskHSVSlots.Count < m_colorMaskSlotCount)
            {
                int x = (m_colorMaskSlotCount - m_colorMaskHSVSlots.Count);
                for (int i = 0; i < x; i++)
                    m_colorMaskHSVSlots.Add(MyRenderComponentBase.OldBlackToHSV);
            }
            m_colorMaskHSVSlots[0] = (MyRenderComponentBase.OldGrayToHSV);
            m_colorMaskHSVSlots[1] = (MyRenderComponentBase.OldRedToHSV);
            m_colorMaskHSVSlots[2] = (MyRenderComponentBase.OldGreenToHSV);
            m_colorMaskHSVSlots[3] = (MyRenderComponentBase.OldBlueToHSV);
            m_colorMaskHSVSlots[4] = (MyRenderComponentBase.OldYellowToHSV);
            m_colorMaskHSVSlots[5] = (MyRenderComponentBase.OldWhiteToHSV);
            m_colorMaskHSVSlots[6] = (MyRenderComponentBase.OldBlackToHSV);
            for (int i = 7; i < m_colorMaskSlotCount; i++)
                m_colorMaskHSVSlots[i] = (m_colorMaskHSVSlots[i - 7] + new Vector3(0, 0.15f, 0.2f));
        }

        public static void AddOrSwitchToColor(Vector3 color)
        {
            for (int i = 0; i < m_colorMaskSlotCount; i++)
            {
                if (m_colorMaskHSVSlots[i] == color)
                {
                    m_currentColorMaskHSV = i;
                    return;
                }
            }
            ColorMaskHSV = color;
        }

        public MyObjectBuilder_Toolbar GetObjectBuilder()
        {
            var objectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Toolbar>();

            if (objectBuilder.Slots == null)
                objectBuilder.Slots = new List<MyObjectBuilder_Toolbar.Slot>(m_items.Length);

            objectBuilder.SelectedSlot = SelectedSlot;
            objectBuilder.Slots.Clear();
            for (int i = 0; i < m_items.Length; ++i)
            {
                if (m_items[i] != null)
                {
                    MyObjectBuilder_ToolbarItem slotObjectBuilder = m_items[i].GetObjectBuilder();
                    var data = m_items[i].GetObjectBuilder();
                    if (data != null)
                    {
                        objectBuilder.Slots.Add(new MyObjectBuilder_Toolbar.Slot()
                        {
                            Index = i,
                            Item = "", // "Item" field is only for backwards compatibility, new items serialize into "Data"
                            Data = data
                        });
                    }
                }
            }

            objectBuilder.ColorMaskHSVList = new List<Vector3>(m_colorMaskHSVSlots);
            return objectBuilder;
        }

        #endregion

        public static void NextColorSlot()
        {
            m_currentColorMaskHSV++;
            m_currentColorMaskHSV %= m_colorMaskSlotCount;
        }

        public static void PrevColorSlot()
        {
            m_currentColorMaskHSV--;
            if (m_currentColorMaskHSV < 0)
                m_currentColorMaskHSV = m_colorMaskSlotCount - 1;
        }

        public static int CurrentColorMaskHSV
        {
            get { return m_currentColorMaskHSV; }
        }

        public void PageUp()
        {
            if (PageCount <= 0)
                return;

            m_currentPage = MyMath.Mod(m_currentPage + 1, PageCount);
            if (CurrentPageChanged != null)
                CurrentPageChanged(this, new PageChangeArgs() { PageIndex = m_currentPage } );
        }

        public void PageDown()
        {
            if (PageCount <= 0)
                return;

            m_currentPage = MyMath.Mod(m_currentPage - 1, PageCount);
            if (CurrentPageChanged != null)
                CurrentPageChanged(this, new PageChangeArgs() { PageIndex = m_currentPage } );
        }

        public void SwitchToPage(int page)
        {
            Debug.Assert(page >= 0 && page < PageCount);
            if (page < 0 || page >= PageCount) return;
            if (m_currentPage == page) return;

            m_currentPage = page;
            if (CurrentPageChanged != null)
                CurrentPageChanged(this, new PageChangeArgs() { PageIndex = m_currentPage });
        }

        public void SetItemAtIndex(int i, MyDefinitionId defId, MyObjectBuilder_ToolbarItem data)
        {
            if (!m_items.IsValidIndex(i))
                return;

            MyDefinitionBase definition;
            if (MyDefinitionManager.Static.TryGetDefinition(defId, out definition))
                SetItemAtIndex(i, MyToolbarItemFactory.CreateToolbarItem(data));
        }

        public void SetItemAtSlot(int slot, MyToolbarItem item)
        {
            SetItemAtIndex(SlotToIndex(slot), item);
        }

        public void SetItemAtIndex(int i, MyToolbarItem item)
        {
            SetItemAtIndexInternal(i, item, false);
        }

        private void SetItemAtIndexInternal(int i, MyToolbarItem item, bool initialization = false)
        {
            if (!m_items.IsValidIndex(i))
                return;

			var definitionItem = item as MyToolbarItemDefinition;
			if (definitionItem != null && !definitionItem.Definition.AvailableInSurvival && MySession.Static.SurvivalMode)
				return;
            
            if (item != null && !item.AllowedInToolbarType(m_toolbarType))
                return;

            bool oldEnabledState = true;
            bool newEnabledState = true;

            if (m_items[i] != null)
            {
                oldEnabledState = m_items[i].Enabled;
                m_items[i].OnClose();
            }

            m_items[i] = item;

            if (m_items[i] != null)
            {
                newEnabledState = true;
            }

            if (initialization)
                return;

            UpdateItem(i);
            if (ItemChanged != null)
                ItemChanged(this, new IndexArgs() { ItemIndex = i });

            if (oldEnabledState != newEnabledState)
            {
                int slot = IndexToSlot(i);
                if (IsValidSlot(slot))
                    SlotEnabledChanged(slot);
            }
        }

        void ToolbarItemUpdated(MyToolbarItem obj, MyToolbarItem.ChangeInfo changed)
        {
            if (ItemUpdated != null)
            {
                int index = Array.IndexOf(m_items, obj);
                if (index != -1)
                {
                    ItemUpdated(this, new IndexArgs() { ItemIndex = index }, changed);
                }
            }
        }

        void ToolbarItem_EnabledChanged(MyToolbarItem obj)
        {
            // Do not propagate this event when all enabled states are overriden.
            if (EnabledOverride.HasValue)
                return;

            int index = Array.IndexOf(m_items, obj);

            if (ItemEnabledChanged != null && index != -1)
            {
                int slot = IndexToSlot(index);
                if (IsValidSlot(slot))
                {
                    ItemEnabledChanged(this, new SlotArgs() { SlotNumber = slot });
                }
            }
        }

        void SlotEnabledChanged(int slotIndex)
        {
            // Do not create an event when all enabled states are overriden.
            if (EnabledOverride.HasValue)
                return;

            if (ItemEnabledChanged != null)
                ItemEnabledChanged(this, new SlotArgs() { SlotNumber = slotIndex });
        }

        public void CharacterInventory_OnContentsChanged(MyInventory inventory)
        {
            Update();
        }

        public void SetDefaults(bool sendEvent = true)
        {
            if (m_toolbarType == MyToolbarType.Character)
            {
                MyDefinitionBase armorblock, cockpit, smallgenerator, smallthrust, gyro;
                MyDefinitionId armorblockid = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock");
                MyDefinitionId cockpitid = new MyDefinitionId(typeof(MyObjectBuilder_Cockpit), "LargeBlockCockpit");
                MyDefinitionId smallgeneratorid = new MyDefinitionId(typeof(MyObjectBuilder_Reactor), "LargeBlockSmallGenerator");
                MyDefinitionId smallthrustid = new MyDefinitionId(typeof(MyObjectBuilder_Thrust), "LargeBlockSmallThrust");
                MyDefinitionId gyroid = new MyDefinitionId(typeof(MyObjectBuilder_Gyro), "LargeBlockGyro");

                int v = 0;
                if (MyDefinitionManager.Static.TryGetDefinition(armorblockid, out armorblock))
                    SetItemAtIndex(v++, armorblockid, MyToolbarItemFactory.ObjectBuilderFromDefinition(armorblock));
                if (MyDefinitionManager.Static.TryGetDefinition(cockpitid, out cockpit))
                    SetItemAtIndex(v++, armorblockid, MyToolbarItemFactory.ObjectBuilderFromDefinition(cockpit));
                if (MyDefinitionManager.Static.TryGetDefinition(smallgeneratorid, out smallgenerator))
                    SetItemAtIndex(v++, armorblockid, MyToolbarItemFactory.ObjectBuilderFromDefinition(smallgenerator));
                if (MyDefinitionManager.Static.TryGetDefinition(smallthrustid, out smallthrust))
                    SetItemAtIndex(v++, armorblockid, MyToolbarItemFactory.ObjectBuilderFromDefinition(smallthrust));
                if (MyDefinitionManager.Static.TryGetDefinition(gyroid, out gyro))
                    SetItemAtIndex(v++, armorblockid, MyToolbarItemFactory.ObjectBuilderFromDefinition(gyro));

                for (int i = v; i < m_items.Length; ++i)
                    SetItemAtIndex(i, null);

                m_currentColorMaskHSV = 0;
                SetDefaultColors();
            }
        }

        public void Clear()
        {
            for (int i = 0; i < m_items.Length; ++i)
                SetItemAtIndex(i, null);
        }

        public void ActivateItemAtSlot(int slot, bool checkIfWantsToBeActivated = false, bool playActivationSound = true)
        {
            if (!IsValidSlot(slot) && !IsHolsterSlot(slot))
                return;

            if (IsValidSlot(slot))
            {
                if (ActivateItemAtIndex(SlotToIndex(slot), checkIfWantsToBeActivated))
                {
                    if (playActivationSound)
                    {
                        MyGuiAudio.PlaySound(MyGuiSounds.HudClick);
                    }
                    if (SlotActivated != null)
                        SlotActivated(this, new SlotArgs { SlotNumber = slot });
                }
            }
            else
                Unselect();
        }

        public void SelectNextSlot()
        {
            if (m_selectedSlot.HasValue)
            {
                var nextSlot = GetNextValidSlot(m_selectedSlot.Value);
                if (!IsValidSlot(nextSlot) || IsHolsterSlot(nextSlot))
                {
                    Unselect();
                    StagedSelectedSlot = null;
                }
                else
                {
                    StagedSelectedSlot = nextSlot;
                }
            }
            else
            {
                var nextSlot = 0;
                StagedSelectedSlot = nextSlot;
            }
        }

        public void SelectPreviousSlot()
        {
            if (m_selectedSlot.HasValue)
            {
                var previousSlot = GetPreviousValidSlot(m_selectedSlot.Value);
                if (!IsValidSlot(previousSlot) || IsHolsterSlot(previousSlot))
                {
                    Unselect();
                    StagedSelectedSlot = null;
                }
                else
                {
                    StagedSelectedSlot = previousSlot;
                }
            }
            else
            {
                int previousSlot = GetPreviousValidSlot(SlotCount);
                StagedSelectedSlot = previousSlot;
            }
        }

        public int GetNextValidSlot(int startSlot)
        {
            int nextSlot = startSlot + 1;
            if (IsHolsterSlot(nextSlot))
                return SlotCount;
            else
                return nextSlot;
        }

        public int GetPreviousValidSlot(int startSlot)
        {
            int previousSlot = startSlot - 1;
            if (previousSlot < 0)
                return SlotCount;
            else
                return previousSlot;
        }

        public void ActivateStagedSelectedItem()
        {
            ActivateItemAtSlot(m_stagedSelectedSlot.Value);
        }

        public bool ActivateItemAtIndex(int index, bool checkIfWantsToBeActivated = false)
        {
            var itemToActivate = m_items[index];
            if (StagedSelectedSlot.HasValue && SlotToIndex(StagedSelectedSlot.Value) != index)
                    StagedSelectedSlot = null;
            if (itemToActivate != null && itemToActivate.Enabled)
            {
                if (checkIfWantsToBeActivated && !itemToActivate.WantsToBeActivated)
                    return false;
                return itemToActivate.Activate();
            }
            if (itemToActivate == null)
                Unselect();
            return false;
        }

        public void Unselect()
        {
            if (MyToolbarComponent.CurrentToolbar != this)
                return;
            if (SelectedItem != null)
                MyGuiAudio.PlaySound(MyGuiSounds.HudClick);

            var controlledObject = MySession.ControlledEntity as IMyControllableEntity;
            if (controlledObject != null)
                controlledObject.SwitchToWeapon(null);

            if (MyCubeBuilder.Static.IsActivated)
            {
                MyCubeBuilder.Static.Deactivate();
            }

            if (Unselected != null)
                Unselected(this);
        }

        public bool IsValidIndex(int idx)
        {
            return m_items.IsValidIndex(idx);
        }

        public bool IsValidSlot(int slot)
        {
            return slot >= 0 && slot < SlotCount;
        }

        public bool IsEnabled(int idx)
        {
            if (EnabledOverride.HasValue)
                return EnabledOverride.Value;

            if (idx == SlotCount && ShowHolsterSlot)
                return true;

            if (!IsValidIndex(idx))
                return false;

            if (m_items[idx] != null)
                return m_items[idx].Enabled;

            return true;
        }

        public string GetItemIcon(int idx)
        {
            if (!IsValidIndex(idx))
                return "";

            if (m_items[idx] != null)
                return m_items[idx].Icon;

            return "";
        }

        public long GetControllerPlayerID()
        {
            var cockpit = Owner as MyCockpit;
            if (cockpit == null)
                return 0;

            var controller = cockpit.ControllerInfo.Controller;
            if (controller == null)
                return 0;

            // Controller should alwasy be bound to an identity
            return controller.Player.Identity.IdentityId;
        }

        public void Update()
        {
            ProfilerShort.Begin("MyToolbar.Update");
            if (MySession.Static == null)
            {
                ProfilerShort.End();
                return;
            }

            long playerID = GetControllerPlayerID();

            for (int i = 0; i < m_items.Length; i++)
            {
                if (m_items[i] != null)
                {
                    var updated = m_items[i].Update(Owner, playerID);
                    if (updated == MyToolbarItem.ChangeInfo.None) continue;

                    ToolbarItemUpdated(m_items[i], updated);
                }
            }

            int? previousSelectedSlot = m_selectedSlot;
            if (!StagedSelectedSlot.HasValue)
            {   
                m_selectedSlot = null;
                for (int i = 0; i < SlotCount; i++)
                {
                    if (m_items[SlotToIndex(i)] != null)
                    {
                        if (m_items[SlotToIndex(i)].WantsToBeSelected)
                        {
                            m_selectedSlot = i;
                        }
                    }
                }
            }
            else
            {
                if (!m_selectedSlot.HasValue || m_selectedSlot.Value != StagedSelectedSlot.Value)
                {
                    m_selectedSlot = StagedSelectedSlot;
                    var item = m_items[SlotToIndex(m_selectedSlot.Value)];
                    if (item != null && !item.ActivateOnClick)
                    {
                        ActivateItemAtSlot(m_selectedSlot.Value);
                        m_activateSelectedItem = false;
                    }
                    else
                    {
                        m_activateSelectedItem = true;
                        Unselect();
                    }
                }
            }

            if (previousSelectedSlot != m_selectedSlot && SelectedSlotChanged != null)
                SelectedSlotChanged(this, new SlotArgs() { SlotNumber = m_selectedSlot });

            EnabledOverride = null;

            ProfilerShort.End();
        }

        public void UpdateItem(int index)
        {
            if (MySession.Static == null)
                return;

            if (m_items[index] != null)
            {
                m_items[index].Update(Owner, GetControllerPlayerID());
            }
        }

        private void SetItemAtSerialized(int i, string serializedItem, MyObjectBuilder_ToolbarItem data)
        {
            if (!m_items.IsValidIndex(i))
                return;

            //old saves
            if (data == null)
            {
                if (String.IsNullOrEmpty(serializedItem))
                    return;

                var split = serializedItem.Split(':');
                MyObjectBuilderType typeId;

                if (!MyObjectBuilderType.TryParse(split[0], out typeId))
                    return;

                string subtypeString = (split.Length == 2) ? split[1] : null;
                var defId = new MyDefinitionId(typeId, subtypeString);

                SetItemAtSerializedCompat(i, defId);
            }

            //new saves
            else
                SetItemAtIndexInternal(i, MyToolbarItemFactory.CreateToolbarItem(data), true);
        }

        //THIS FUNCTION IS ONLY NEEDED FOR OLD SAVE COMPATIBILITY
        public void SetItemAtSerializedCompat(int i, MyDefinitionId defId)
        {
            if (!m_items.IsValidIndex(i))
                return;

            MyDefinitionBase definition;
            if (MyDefinitionManager.Static.TryGetDefinition(defId, out definition))
            {
                MyObjectBuilder_ToolbarItem data = MyToolbarItemFactory.ObjectBuilderFromDefinition(definition);
                SetItemAtIndexInternal(i, MyToolbarItemFactory.CreateToolbarItem(data), true);
            }
        }

        private bool IsHolsterSlot(int idx)
        {
            return idx == SlotCount && ShowHolsterSlot;
        }
    }
}
