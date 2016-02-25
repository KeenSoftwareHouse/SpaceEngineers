using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.World.Triggers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Input;
using VRageMath;

namespace Sandbox.Game.Screens.Triggers
{
    public class MyGuiScreenTriggerBlockDestroyed : MyGuiScreenTrigger
    {
        private MyGuiControlTable m_selectedBlocks;
        private MyGuiControlButton m_buttonPaste, m_buttonDelete;
        private MyGuiControlTextbox m_textboxSingleMessage;
        private MyGuiControlLabel m_labelSingleMessage;

        private MyTriggerBlockDestroyed trigger;
        
        public MyGuiScreenTriggerBlockDestroyed(MyTrigger trig) : base(trig,new Vector2(0.5f,0.8f))
        {
            trigger=(MyTriggerBlockDestroyed)trig;
            AddCaption(MySpaceTexts.GuiTriggerCaptionBlockDestroyed);

            var layout = new MyLayoutTable(this);
            layout.SetColumnWidthsNormalized(10, 30, 3, 30, 10);
            layout.SetRowHeightsNormalized(20, 35, 6, 4, 4, 5, 33);

            m_selectedBlocks = new MyGuiControlTable();
            m_selectedBlocks.VisibleRowsCount = 8;
            m_selectedBlocks.ColumnsCount = 1;
            m_selectedBlocks.SetCustomColumnWidths(new float[]{1});
            m_selectedBlocks.SetColumnName(0, MyTexts.Get(MySpaceTexts.GuiTriggerBlockDestroyed_ColumnName));

            layout.AddWithSize(m_selectedBlocks, MyAlignH.Left, MyAlignV.Top, 1, 1, rowSpan: 1, colSpan: 3);

            m_buttonPaste = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.GuiTriggerPasteBlocks),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                onButtonClick: OnPasteButtonClick
                );
            m_buttonPaste.SetToolTip(MySpaceTexts.GuiTriggerPasteBlocksTooltip);
            layout.AddWithSize(m_buttonPaste, MyAlignH.Left, MyAlignV.Top, 2, 1, rowSpan: 1, colSpan: 1);

            m_buttonDelete = new MyGuiControlButton(
                text: MyTexts.Get(MySpaceTexts.GuiTriggerDeleteBlocks),
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                onButtonClick: OnDeleteButtonClick);
            layout.AddWithSize(m_buttonDelete, MyAlignH.Left, MyAlignV.Top, 2, 3, rowSpan: 1, colSpan: 1);

            m_labelSingleMessage = new MyGuiControlLabel(
                text: MyTexts.Get(MySpaceTexts.GuiTriggerBlockDestroyedSingleMessage).ToString()
                );
            layout.AddWithSize(m_labelSingleMessage, MyAlignH.Left, MyAlignV.Top, 3, 1, rowSpan: 1, colSpan: 1);
            m_textboxSingleMessage = new MyGuiControlTextbox(
                defaultText: trigger.SingleMessage,
                maxLength: 85);
            layout.AddWithSize(m_textboxSingleMessage, MyAlignH.Left, MyAlignV.Top, 4, 1, rowSpan: 1, colSpan: 3);

            foreach(var block in trigger.Blocks)
                AddRow(block.Key);
            m_tempSb.Clear().Append(trigger.SingleMessage);
            m_textboxSingleMessage.SetText(m_tempSb);

        }
        public override bool Update(bool hasFocus)
        {
            if (m_selectedBlocks.SelectedRowIndex != null && m_selectedBlocks.SelectedRowIndex<m_selectedBlocks.RowsCount)
                m_buttonDelete.Enabled = true;
            else
                m_buttonDelete.Enabled = false;
            return base.Update(hasFocus);
        }
        private void AddRow(MyTerminalBlock block)
        {
            MyGuiControlTable.Row row;
            MyGuiControlTable.Cell cell;
            row = new MyGuiControlTable.Row(block);
            cell = new MyGuiControlTable.Cell(block.CustomName);
            row.AddCell(cell);
            m_selectedBlocks.Add(row);
        }
        private void OnPasteButtonClick(MyGuiControlButton sender)
        {
            foreach (var block in MyScenarioBuildingBlock.Clipboard)
            {
                int i;
                for (i = 0; i < m_selectedBlocks.RowsCount;i++)
                    if(m_selectedBlocks.GetRow(i).UserData==block)
                        break;
                if(i==m_selectedBlocks.RowsCount)
                    AddRow(block);
            }
        }
        private static StringBuilder m_tempSb = new StringBuilder();
        protected override void OnOkButtonClick(MyGuiControlButton sender)
        {
            trigger.Blocks.Clear();
            for (int i=0;i<m_selectedBlocks.RowsCount;i++)
                trigger.Blocks.Add((MyTerminalBlock)m_selectedBlocks.GetRow(i).UserData,MyTriggerBlockDestroyed.BlockState.Ok);
            trigger.SingleMessage = m_textboxSingleMessage.Text;
            base.OnOkButtonClick(sender);
        }
        private void OnDeleteButtonClick(MyGuiControlButton sender)
        {
            m_selectedBlocks.RemoveSelectedRow();
        }
        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
                m_selectedBlocks.RemoveSelectedRow();
            base.HandleInput(receivedFocusInThisUpdate);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenTriggerBlockDestroyed";
        }
    }
}
