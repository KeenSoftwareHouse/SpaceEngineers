using System;
using System.Text;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;

namespace Sandbox.Game.Screens.Helpers
{    
    public abstract class MyToolbarItem
    {
        /// <summary>
        /// Tells which data of MyToolbarItem changed during an update
        /// </summary>
        [Flags]
        public enum ChangeInfo
        {
            None = 0x0,
            Enabled = 1 << 0,
            Icon = 1 << 1,
            SubIcon = 1 << 2,
            IconText = 1 << 3,
            DisplayName = 1 << 4,
            All = Enabled | Icon | SubIcon | IconText | DisplayName
        }

        public bool Enabled { get; private set; }
        public string[] Icons { get; private set; }
        public string SubIcon { get; private set; }
        public StringBuilder IconText { get; private set; }
        public StringBuilder DisplayName { get; private set; }

        //Each item type says whether or not it wants to be activated when dragged to toolbar (ie, yes for weapons/cube blocks, no for ship blocks/ship groups)
        public bool WantsToBeActivated { get; protected set; }
        public bool WantsToBeSelected { get; protected set; }
        public bool ActivateOnClick { get; protected set; }

        public MyToolbarItem()
        {
            Icons = new string[] { MyGuiConstants.TEXTURE_ICON_FAKE.Texture };
            IconText = new StringBuilder();
            DisplayName = new StringBuilder();
        }

        public virtual void OnRemovedFromToolbar(MyToolbar toolbar) { }
        public virtual void OnAddedToToolbar(MyToolbar toolbar) { }

        public abstract bool Activate();
        public abstract bool Init(MyObjectBuilder_ToolbarItem data);
        public abstract MyObjectBuilder_ToolbarItem GetObjectBuilder();

        public abstract bool AllowedInToolbarType(MyToolbarType type);

        /// <summary>
        /// Return value should contain information about the stuff that changed during the update
        /// </summary>
        public abstract ChangeInfo Update(MyEntity owner, long playerID = 0);

        public ChangeInfo SetEnabled(bool newEnabled)
        {
            if (newEnabled == Enabled) return ChangeInfo.None;
            Enabled = newEnabled;
            return ChangeInfo.Enabled;
        }

        public ChangeInfo SetIcons(string[] newIcons)
        {
            if (newIcons == Icons) return ChangeInfo.None;
            Icons = newIcons;
            return ChangeInfo.Icon;
        }

        public ChangeInfo SetSubIcon(string newSubIcon)
        {
            if (newSubIcon == SubIcon) return ChangeInfo.None;
            SubIcon = newSubIcon;
            return ChangeInfo.SubIcon;
        }

        public ChangeInfo SetIconText(StringBuilder newIconText)
        {
            if (newIconText == null) return ChangeInfo.None;
            if (IconText.CompareTo(newIconText) == 0) return ChangeInfo.None;
            IconText.Clear();
            IconText.AppendStringBuilder(newIconText);
            return ChangeInfo.IconText;
        }

        public ChangeInfo ClearIconText()
        {
            if (IconText.Length == 0) return ChangeInfo.None;
            IconText.Clear();
            return ChangeInfo.IconText;
        }

        public ChangeInfo SetDisplayName(String newDisplayName)
        {
            if (newDisplayName == null) return ChangeInfo.None;
            if (DisplayName.CompareTo(newDisplayName) == 0) return ChangeInfo.None;

            DisplayName.Clear();
            DisplayName.Append(newDisplayName);
            return ChangeInfo.DisplayName;
        }

        public virtual void FillGridItem(MyGuiControlGrid.Item gridItem)
        {
            if (IconText.Length == 0)
                gridItem.ClearText(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
            else
                gridItem.AddText(IconText, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM);
        }

        public override bool Equals(object obj)
        {
            throw new InvalidOperationException("GetHashCode and Equals must be overridden");
        }

        public override int GetHashCode()
        {
            throw new InvalidOperationException("GetHashCode and Equals must be overridden");
        }
    }
}
