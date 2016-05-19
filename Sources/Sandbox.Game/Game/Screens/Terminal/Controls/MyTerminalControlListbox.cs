using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Utils;
using VRageMath;
using VRage.Library.Utils;
using System.Diagnostics;
using VRage.Game;
using VRage.Library.Collections;
using Sandbox.ModAPI.Interfaces.Terminal;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sandbox.Game.Gui
{
    public class MyTerminalControlListbox<TBlock> : MyTerminalControl<TBlock>, ITerminalControlSync, IMyTerminalControlTitleTooltip, IMyTerminalControlListbox
        where TBlock : MyTerminalBlock
    {
        public delegate void ListContentDelegate(TBlock block, ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems);
        public delegate void SelectItemDelegate(TBlock block, List<MyGuiControlListbox.Item> items);
        public MyStringId Title;
        public MyStringId Tooltip;

        public ListContentDelegate ListContent;
        public SelectItemDelegate ItemSelected;

        private MyGuiControlListbox m_listbox;

        bool m_enableMultiSelect = false;
        int m_visibleRowsCount = 8;

        public MyTerminalControlListbox(string id, MyStringId title, MyStringId tooltip, bool multiSelect = false, int visibleRowsCount = 8)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
            m_enableMultiSelect = multiSelect;
            m_visibleRowsCount = visibleRowsCount;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_listbox = new MyGuiControlListbox()
            {
                VisualStyle = MyGuiControlListboxStyleEnum.Terminal,
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                VisibleRowsCount = m_visibleRowsCount,
                MultiSelect = m_enableMultiSelect,
            };
            m_listbox.ItemsSelected += OnItemsSelected;

            return new MyGuiControlBlockProperty(
                MyTexts.GetString(Title),
                MyTexts.GetString(Tooltip),
                m_listbox,
                MyGuiControlBlockPropertyLayoutEnum.Vertical);
        }

        void OnItemsSelected(MyGuiControlListbox obj)
        {
            if (ItemSelected != null && obj.SelectedItems.Count > 0)
            {
                foreach (var block in TargetBlocks)
                {
                    ItemSelected(block, obj.SelectedItems);
                }
            }
        }

        /// <summary>
        /// Implements IMyTerminalControlListBox for Mods
        /// </summary>
        MyStringId IMyTerminalControlTitleTooltip.Title
        {
            get
            {
                return Title;
            }

            set
            {
                Title = value;
            }
        }

        MyStringId IMyTerminalControlTitleTooltip.Tooltip
        {
            get
            {
                return Tooltip;
            }

            set
            {
                Tooltip = value;
            }
        }

        bool IMyTerminalControlListbox.Multiselect
        {
            get
            {
                return m_enableMultiSelect;
            }

            set
            {
                m_enableMultiSelect = value;
            }
        }

        int IMyTerminalControlListbox.VisibleRowsCount
        {
            get
            {
                return m_visibleRowsCount;
            }

            set
            {
                m_visibleRowsCount = value;
            }
        }

        Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> IMyTerminalControlListbox.ListContent
        {
            set
            {
                ListContent = new ListContentDelegate((block, contentList, selectedList) =>
                {
                    List<MyTerminalControlListBoxItem> wrapList = new List<MyTerminalControlListBoxItem>();
                    List<MyTerminalControlListBoxItem> wrapSelectedList = new List<MyTerminalControlListBoxItem>();
                    value(block, wrapList, wrapSelectedList);
                    foreach(var wrapItem in wrapList)
                    {
                        var item = new MyGuiControlListbox.Item(text: new StringBuilder(wrapItem.Text.ToString()), toolTip: wrapItem.Tooltip.ToString(), userData: wrapItem.UserData);
                        contentList.Add(item);
                        if (wrapSelectedList.Contains(wrapItem))
                            selectedList.Add(item);
                    }
                });
            }
        }

        Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> IMyTerminalControlListbox.ItemSelected
        {
            set
            {
                ItemSelected = new SelectItemDelegate((block, selectedList) =>
                {
                    List<MyTerminalControlListBoxItem> wrapSelectedList = new List<MyTerminalControlListBoxItem>();
                    foreach (var selectedItem in selectedList)
                    {
                        string toolTip = selectedItem.ToolTip != null && selectedItem.ToolTip.ToolTips.Count > 0 ? selectedItem.ToolTip.ToolTips.First().ToString() : null;
                        var wrapItem = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(selectedItem.Text.ToString()), MyStringId.GetOrCompute(toolTip), selectedItem.UserData);
                        wrapSelectedList.Add(wrapItem);
                    }

                    value(block, wrapSelectedList);
                });
            }
        }

        protected override void OnUpdateVisual()
        {
            base.OnUpdateVisual();

            var first = FirstBlock;
            if (first != null)
            {
                // clear current listed items
                m_listbox.Items.Clear();

                // add items
                if (ListContent != null)
                    ListContent(first, m_listbox.Items, m_listbox.SelectedItems);
            }
        }

        public void Serialize(BitStream stream, MyTerminalBlock block)
        {
            //Debug.Fail("List sync not implemented yet");
        }
    }
}
