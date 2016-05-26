using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Screens.Helpers;
using VRage.Game;
using VRage.Serialization;
using VRage.ObjectBuilders;

namespace Sandbox.Game.Entities.Blocks
{
    [ProtoContract]
    public struct ToolbarItem
    {
        [ProtoMember]
        public long EntityID;
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string GroupName;
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public string Action;
        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public List<MyObjectBuilder_ToolbarItemActionParameter> Parameters;

        [Serialize(MyObjectFlags.Nullable)]
        public SerializableDefinitionId? GunId;

        public static ToolbarItem FromItem(MyToolbarItem item)
        {
            var tItem = new ToolbarItem();
            tItem.EntityID = 0;
            var terminalItem = item as MyToolbarItemTerminalBlock;
            if (terminalItem != null)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalBlock;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block._Action;
                tItem.Parameters = block.Parameters;
            }
            else if (item is MyToolbarItemTerminalGroup)
            {
                var block = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemTerminalGroup;
                tItem.EntityID = block.BlockEntityId;
                tItem.Action = block._Action;
                tItem.GroupName = block.GroupName;
                tItem.Parameters = block.Parameters;
            }
            else if ( item is MyToolbarItemWeapon)
            {
                var weapon = item.GetObjectBuilder() as MyObjectBuilder_ToolbarItemWeapon;
                tItem.GunId = weapon.DefinitionId;
            }
            return tItem;
        }

        public static MyToolbarItem ToItem(ToolbarItem msgItem)
        {
            MyToolbarItem item = null;
             if (msgItem.GunId.HasValue)
            {
                MyObjectBuilder_ToolbarItemWeapon builder = MyToolbarItemFactory.WeaponObjectBuilder();
                builder.defId = msgItem.GunId.Value;
                item = MyToolbarItemFactory.CreateToolbarItem(builder);
            }
            else if (string.IsNullOrEmpty(msgItem.GroupName))
            {
                MyTerminalBlock block;
                if (MyEntities.TryGetEntityById(msgItem.EntityID, out block))
                {
                    var builder = MyToolbarItemFactory.TerminalBlockObjectBuilderFromBlock(block);
                    builder._Action = msgItem.Action;
                    builder.Parameters = msgItem.Parameters;
                    item = MyToolbarItemFactory.CreateToolbarItem(builder);
                }
            }
            else
            {
                MyCubeBlock parent;
                if (MyEntities.TryGetEntityById(msgItem.EntityID, out parent))
                {
                    var grid = parent.CubeGrid;
                    var groupName = msgItem.GroupName;
                    var group = grid.GridSystems.TerminalSystem.BlockGroups.Find(x => x.Name.ToString() == groupName);
                    if (group != null)
                    {
                        var builder = MyToolbarItemFactory.TerminalGroupObjectBuilderFromGroup(@group);
                        builder._Action = msgItem.Action;
                        builder.Parameters = msgItem.Parameters;
                        builder.BlockEntityId = msgItem.EntityID;
                        item = MyToolbarItemFactory.CreateToolbarItem(builder);
                    }
                }
            }
            return item;
        }

        public bool ShouldSerializeParameters()
        {
            return Parameters != null && Parameters.Count > 0;
        }
    }
}