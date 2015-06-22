
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Terminal.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
namespace Sandbox.Game.Gui
{
    public static class MyTerminalControlExtensions
    {
        private static StringBuilder Combine(MyStringId prefix, MyStringId title)
        {
            var sb = new StringBuilder();
            var prefixText = MyTexts.Get(prefix);
            if (prefixText.Length > 0) sb.Append(prefixText).Append(" ");

            return sb.Append(MyTexts.GetString(title)).TrimTrailingWhitespace();
        }

        private static StringBuilder GetTitle(MyStringId title)
        {
            var sb = new StringBuilder();
            var titleText = MyTexts.GetString(title);
            if (titleText.Length > 0) sb.Append(titleText);
            return sb;
        }

        private static StringBuilder CombineOnOff(MyStringId title, MyStringId? on = null, MyStringId? off = null)
        {
            return GetTitle(title).Append(" ")
                .Append(MyTexts.GetString(on ?? MySpaceTexts.SwitchText_On)).Append("/")
                .Append(MyTexts.GetString(off ?? MySpaceTexts.SwitchText_Off));
        }

        public static void EnableActions<TBlock>(this MyTerminalControlSlider<TBlock> slider, float step = 0.05f)
            where TBlock : MyTerminalBlock
        {
            EnableActions(slider, MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE, step);
        }

        public static void EnableActions<TBlock>(this MyTerminalControlSlider<TBlock> slider, string increaseIcon, string decreaseIcon, float step = 0.05f)
            where TBlock : MyTerminalBlock
        {
            var increaseName = Combine(MySpaceTexts.ToolbarAction_Increase, slider.Title);
            var decreaseName = Combine(MySpaceTexts.ToolbarAction_Decrease, slider.Title);
            slider.EnableActions(increaseIcon, decreaseIcon, increaseName, decreaseName, step, null, null);
        }

        public static void EnableActionsWithReset<TBlock>(this MyTerminalControlSlider<TBlock> slider, float step = 0.05f)
            where TBlock : MyTerminalBlock
        {
            EnableActionsWithReset(slider, MyTerminalActionIcons.INCREASE, MyTerminalActionIcons.DECREASE, MyTerminalActionIcons.RESET, step);
        }

        public static void EnableActionsWithReset<TBlock>(this MyTerminalControlSlider<TBlock> slider, string increaseIcon, string decreaseIcon, string resetIcon, float step = 0.05f)
            where TBlock : MyTerminalBlock
        {
            var increaseName = Combine(MySpaceTexts.ToolbarAction_Increase, slider.Title);
            var decreaseName = Combine(MySpaceTexts.ToolbarAction_Decrease, slider.Title);
            var resetName = Combine(MySpaceTexts.ToolbarAction_Reset, slider.Title);
            slider.EnableActions(increaseIcon, decreaseIcon, increaseName, decreaseName, step, resetIcon, resetName);
        }

        public static MyTerminalAction<TBlock> EnableAction<TBlock>(this MyTerminalControlButton<TBlock> button, string icon = null, MyStringId? title = null, MyTerminalControl<TBlock>.WriterDelegate writer = null)
            where TBlock : MyTerminalBlock
        {
            return button.EnableAction(icon ?? MyTerminalActionIcons.TOGGLE, MyTexts.Get(title ?? button.Title), writer);
        }

        public static MyTerminalAction<TBlock> EnableAction<TBlock>(this MyTerminalControlCheckbox<TBlock> checkbox)
            where TBlock : MyTerminalBlock
        {
            StringBuilder name = CombineOnOff(checkbox.Title);
            StringBuilder onText = MyTexts.Get(checkbox.OnText);
            StringBuilder offText = MyTexts.Get(checkbox.OffText);
            return checkbox.EnableAction(MyTerminalActionIcons.TOGGLE, name, onText, offText);
        }

        public static MyTerminalAction<TBlock> EnableToggleAction<TBlock>(this MyTerminalControlOnOffSwitch<TBlock> onOff)
            where TBlock : MyTerminalBlock
        {
           return EnableToggleAction(onOff, MyTerminalActionIcons.TOGGLE);
        }

        public static MyTerminalAction<TBlock> EnableToggleAction<TBlock>(this MyTerminalControlOnOffSwitch<TBlock> onOff,string iconPath)
           where TBlock : MyTerminalBlock
        {
            StringBuilder name = CombineOnOff(onOff.Title, onOff.OnText, onOff.OffText);
            StringBuilder onText = MyTexts.Get(onOff.OnText);
            StringBuilder offText = MyTexts.Get(onOff.OffText);
            return onOff.EnableToggleAction(iconPath, name, onText, offText);
        }

        public static void EnableOnOffActions<TBlock>(this MyTerminalControlOnOffSwitch<TBlock> onOff)
            where TBlock : MyTerminalBlock
        {
            EnableOnOffActions(onOff, MyTerminalActionIcons.ON, MyTerminalActionIcons.OFF);
        }

        public static void EnableOnOffActions<TBlock>(this MyTerminalControlOnOffSwitch<TBlock> onOff,string onIcon,string OffIcon)
            where TBlock : MyTerminalBlock
        {
            StringBuilder onText = MyTexts.Get(onOff.OnText);
            StringBuilder offText = MyTexts.Get(onOff.OffText);
            onOff.EnableOnAction(onIcon, GetTitle(onOff.Title).Append(" ").Append(onText), onText, offText);
            onOff.EnableOffAction(OffIcon, GetTitle(onOff.Title).Append(" ").Append(offText), onText, offText);
        }
    }
}
