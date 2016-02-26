using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using VRage.Game;
using VRage.Serialization;

namespace Sandbox.Game.Entities.Blocks
{
    public struct ToolbarItemCache
    {
        private MyToolbarItem m_cachedItem;
        private ToolbarItem m_item;

        public ToolbarItem Item
        {
            get { return m_item; }
            set { m_item = value; m_cachedItem = null; }
        }

        [NoSerialize]
        public MyToolbarItem CachedItem
        {
            get
            {
                if(m_cachedItem == null)
                {
                    m_cachedItem = ToolbarItem.ToItem(Item);
                }
                return m_cachedItem;
            }
        }

        public MyObjectBuilder_ToolbarItem ToObjectBuilder()
        {
            var item = m_cachedItem;
            return item != null ? item.GetObjectBuilder() : null;

        }

        public void SetToToolbar(MyToolbar toolbar, int index)
        {
            var item = m_cachedItem;
            if (item != null)
                toolbar.SetItemAtIndex(index, item);
        }

        public static implicit operator ToolbarItemCache(ToolbarItem item)
        {
            return new ToolbarItemCache() { m_item = item };
        }
    }
}