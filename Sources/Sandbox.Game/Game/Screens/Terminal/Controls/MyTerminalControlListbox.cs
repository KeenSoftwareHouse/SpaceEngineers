
using Sandbox.Common.ObjectBuilders.Gui;
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
using VRage.Library.Collections;

namespace Sandbox.Game.Gui
{
    class MyTerminalControlListbox<TBlock> : MyTerminalControl<TBlock>, ITerminalControlSync
        where TBlock : MyTerminalBlock
    {
        public delegate void ListContentDelegate(TBlock block, ICollection<MyGuiControlListbox.Item> listBoxContent, ICollection<MyGuiControlListbox.Item> listBoxSelectedItems);
        public delegate void SelectItemDelegate(TBlock block, List<MyGuiControlListbox.Item> items);
        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

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
            if (ItemSelected != null && obj.SelectedItems.Count >0 )
            {
                foreach (var block in TargetBlocks)
                {
                    ItemSelected(block, obj.SelectedItems);
                }
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
