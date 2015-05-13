
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;

namespace Sandbox.Game.Gui
{
    public struct TerminalComboBoxItem
    {
        public long Key;
        public MyStringId Value;
    }

    class MyTerminalControlCombobox<TBlock> : MyTerminalControl<TBlock>
        where TBlock : MyTerminalBlock
    {
        private static List<TerminalComboBoxItem> m_handlerItems = new List<TerminalComboBoxItem>();

        public readonly MyStringId Title;
        public readonly MyStringId Tooltip;

        private MyGuiControlCombobox m_comboBox;

        public Action<List<TerminalComboBoxItem>> ComboBoxContent;
        public Func<TBlock, long> Getter;
        public Action<TBlock, long> Setter;

        public MyTerminalControlCombobox(string id, MyStringId title, MyStringId tooltip)
            : base(id)
        {
            Title = title;
            Tooltip = tooltip;
        }

        protected override MyGuiControlBase CreateGui()
        {
            m_comboBox = new MyGuiControlCombobox(toolTip: MyTexts.GetString(Tooltip), size: new VRageMath.Vector2(0.23f, 0.04f));
            m_comboBox.VisualStyle = MyGuiControlComboboxStyleEnum.Terminal;
            m_comboBox.ItemSelected += OnItemSelected;

            return new MyGuiControlBlockProperty(
                MyTexts.GetString(Title),
                MyTexts.GetString(Tooltip),
                m_comboBox,
                MyGuiControlBlockPropertyLayoutEnum.Vertical);
        }

        void OnItemSelected()
        {
            if (m_comboBox.GetItemsCount() > 0)
            {
                var selected = m_comboBox.GetSelectedKey();
                if (Setter != null)
                {
                    foreach (var block in TargetBlocks)
                    {
                        Setter(block, selected);
                    }
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
                m_comboBox.ClearItems();
                m_handlerItems.Clear();
                // add items
                if (ComboBoxContent != null)
                {
                    ComboBoxContent(m_handlerItems);
                    foreach (var item in m_handlerItems)
                    {
                        m_comboBox.AddItem(item.Key, item.Value);
                    }

                    if (Getter != null)
                    {
                        if (m_comboBox.GetSelectedKey() != Getter(first))
                            m_comboBox.SelectItemByKey(Getter(first));
                    }
                }
            }
        }
    }
}
