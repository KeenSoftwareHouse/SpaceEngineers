using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Graphics.GUI
{
    /// <summary>
    /// This is here because we have stupid labels
    /// </summary>
    static class MyControlHelpers
    {
        public static void SetDetailedInfo<TBlock>(this MyGuiControlBlockProperty control, MyTerminalControl<TBlock>.WriterDelegate writer, TBlock block)
            where TBlock : MyTerminalBlock
        {
            var sb = control.ExtraInfoLabel.TextToDraw;
            sb.Clear();
            if (writer != null && block != null)
                writer(block, sb);
            control.ExtraInfoLabel.TextToDraw = sb;
            control.ExtraInfoLabel.Visible = sb.Length > 0;
        }
    }
}
