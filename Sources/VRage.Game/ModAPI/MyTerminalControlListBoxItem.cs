using VRage.Utils;

namespace VRage.ModAPI
{
    /// <summary>
    /// This is a listbox item in a list box terminal control
    /// </summary>
    public class MyTerminalControlListBoxItem
    {
        /// <summary>
        /// The text that appears for the item
        /// </summary>
        public MyStringId Text { get; set; }
        /// <summary>
        /// The tooltip that is displayed when the item is hovered over
        /// </summary>
        public MyStringId Tooltip { get; set; }
        /// <summary>
        /// User supplied data for the item
        /// </summary>
        public object UserData { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tooltip"></param>
        /// <param name="userData"></param>
        public MyTerminalControlListBoxItem(MyStringId text, MyStringId tooltip, object userData)
        {
            Text = text;
            Tooltip = tooltip;
            UserData = userData;
        }
    }
}
